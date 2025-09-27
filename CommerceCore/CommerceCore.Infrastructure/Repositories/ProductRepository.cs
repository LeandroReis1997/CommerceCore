using CommerceCore.Application.Interfaces.Repositories;
using CommerceCore.Domain.Entities;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace CommerceCore.Infrastructure.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString; // String de conexão com SQL Server
        private readonly SqlConnection? _connection; // Conexão compartilhada (para UnitOfWork)
        private SqlTransaction? _transaction; // Transação compartilhada (para UnitOfWork)

        public ProductRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string não encontrada");
            _connection = null;
            _transaction = null;
        }
      
        // Construtor para uso com UnitOfWork (usa conexão e transação compartilhadas)
        public ProductRepository(string connectionString, SqlConnection connection, SqlTransaction? transaction = null)
        {
            _connectionString = connectionString;
            _connection = connection;
            _transaction = transaction;
        }

        // Método para GetProductsQueryHandler - busca paginada com filtros
        public async Task<(IEnumerable<Product> Products, int TotalCount)> GetPagedAsync(
            int page,
            int pageSize,
            string? searchTerm = null,
            Guid? categoryId = null,
            Guid? brandId = null,
            bool? isActive = null,
            bool? inStock = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            bool includeBrand = false,
            bool includeCategory = false,
            bool includeImages = false,
            int maxImagesPerProduct = 5,
            string sortBy = "Name",
            string sortDirection = "ASC",
            CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);

            // Construção dinâmica da query SQL
            var whereClause = BuildWhereClause(searchTerm, categoryId, brandId, isActive, inStock, minPrice, maxPrice);
            var joinClause = BuildJoinClause(includeBrand, includeCategory, includeImages);
            var selectClause = BuildSelectClause(includeBrand, includeCategory, includeImages, maxImagesPerProduct);
            var orderClause = $"ORDER BY p.{sortBy} {sortDirection}";

            // Query para buscar produtos paginados
            var sql = $@"
                {selectClause}
                FROM Products p
                {joinClause}
                {whereClause}
                {orderClause}
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY;

                -- Query para contar total de registros
                SELECT COUNT(1)
                FROM Products p
                {BuildJoinClause(false, false, false)} -- Joins básicos para contagem
                {whereClause}";

            var parameters = BuildParameters(page, pageSize, searchTerm, categoryId, brandId, isActive, inStock, minPrice, maxPrice);

            // Execução das queries
            using var multi = await connection.QueryMultipleAsync(sql, parameters);
            var products = await multi.ReadAsync<Product>();
            var totalCount = await multi.ReadSingleAsync<int>();

            return (products, totalCount);
        }

        // Método para GetProductByIdQueryHandler - busca por ID com includes
        public async Task<Product?> GetByIdAsync(
            Guid id,
            bool includeBrand = false,
            bool includeCategory = false,
            bool includeImages = false,
            bool includeReviews = false,
            CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);

            var joinClause = BuildJoinClause(includeBrand, includeCategory, includeImages, includeReviews);
            var selectClause = BuildSelectClause(includeBrand, includeCategory, includeImages, includeReviews: includeReviews);

            var sql = $@"
                {selectClause}
                FROM Products p
                {joinClause}
                WHERE p.Id = @Id";

            return await connection.QuerySingleOrDefaultAsync<Product>(sql, new { Id = id });
        }

        // Método para GetProductsByCategoryQueryHandler
        public async Task<(IEnumerable<Product> Products, int TotalCount)> GetByCategoryPagedAsync(
            Guid categoryId,
            int page,
            int pageSize,
            string? searchTerm = null,
            Guid? brandId = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            bool? isActive = null,
            bool? inStock = null,
            bool includeBrand = false,
            bool includeCategory = false,
            bool includeImages = false,
            bool includeSubcategories = false,
            string sortBy = "Name",
            string sortDirection = "ASC",
            CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);

            var whereClause = BuildCategoryWhereClause(categoryId, includeSubcategories, searchTerm, brandId, minPrice, maxPrice, isActive, inStock);
            var joinClause = BuildJoinClause(includeBrand, includeCategory, includeImages);
            var selectClause = BuildSelectClause(includeBrand, includeCategory, includeImages);
            var orderClause = $"ORDER BY p.{sortBy} {sortDirection}";

            var sql = $@"
                {selectClause}
                FROM Products p
                {joinClause}
                {whereClause}
                {orderClause}
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY;

                -- Query para contar total
                SELECT COUNT(1)
                FROM Products p
                {whereClause}";

            var parameters = BuildCategoryParameters(page, pageSize, categoryId, searchTerm, brandId, minPrice, maxPrice, isActive, inStock);

            using var multi = await connection.QueryMultipleAsync(sql, parameters);
            var products = await multi.ReadAsync<Product>();
            var totalCount = await multi.ReadSingleAsync<int>();

            return (products, totalCount);
        }

        // Métodos auxiliares para construção dinâmica de SQL
        private string BuildWhereClause(string? searchTerm, Guid? categoryId, Guid? brandId, bool? isActive, bool? inStock, decimal? minPrice, decimal? maxPrice)
        {
            var conditions = new List<string> { "1=1" }; // Base condition

            if (!string.IsNullOrWhiteSpace(searchTerm))
                conditions.Add("(p.Name LIKE @SearchTerm OR p.Description LIKE @SearchTerm OR p.Sku LIKE @SearchTerm)");

            if (categoryId.HasValue)
                conditions.Add("p.CategoryId = @CategoryId");

            if (brandId.HasValue)
                conditions.Add("p.BrandId = @BrandId");

            if (isActive.HasValue)
                conditions.Add("p.IsActive = @IsActive");

            if (inStock.HasValue)
                conditions.Add(inStock.Value ? "p.StockQuantity > 0" : "p.StockQuantity = 0");

            if (minPrice.HasValue)
                conditions.Add("p.Price >= @MinPrice");

            if (maxPrice.HasValue)
                conditions.Add("p.Price <= @MaxPrice");

            return $"WHERE {string.Join(" AND ", conditions)}";
        }

        private string BuildCategoryWhereClause(Guid categoryId, bool includeSubcategories, string? searchTerm, Guid? brandId, decimal? minPrice, decimal? maxPrice, bool? isActive, bool? inStock)
        {
            var conditions = new List<string>();

            if (includeSubcategories)
            {
                // Busca na categoria e suas subcategorias recursivamente
                conditions.Add(@"p.CategoryId IN (
                    WITH CategoryHierarchy AS (
                        SELECT Id FROM Categories WHERE Id = @CategoryId
                        UNION ALL
                        SELECT c.Id FROM Categories c
                        INNER JOIN CategoryHierarchy ch ON c.ParentCategoryId = ch.Id
                    )
                    SELECT Id FROM CategoryHierarchy
                )");
            }
            else
            {
                conditions.Add("p.CategoryId = @CategoryId");
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
                conditions.Add("(p.Name LIKE @SearchTerm OR p.Description LIKE @SearchTerm OR p.Sku LIKE @SearchTerm)");

            if (brandId.HasValue)
                conditions.Add("p.BrandId = @BrandId");

            if (isActive.HasValue)
                conditions.Add("p.IsActive = @IsActive");

            if (inStock.HasValue)
                conditions.Add(inStock.Value ? "p.StockQuantity > 0" : "p.StockQuantity = 0");

            if (minPrice.HasValue)
                conditions.Add("p.Price >= @MinPrice");

            if (maxPrice.HasValue)
                conditions.Add("p.Price <= @MaxPrice");

            return $"WHERE {string.Join(" AND ", conditions)}";
        }

        private string BuildJoinClause(bool includeBrand = false, bool includeCategory = false, bool includeImages = false, bool includeReviews = false)
        {
            var joins = new List<string>();

            if (includeBrand)
                joins.Add("LEFT JOIN Brands b ON p.BrandId = b.Id");

            if (includeCategory)
                joins.Add("LEFT JOIN Categories c ON p.CategoryId = c.Id");

            if (includeImages)
                joins.Add("LEFT JOIN ProductImages pi ON p.Id = pi.ProductId");

            if (includeReviews)
                joins.Add("LEFT JOIN ProductReviews pr ON p.Id = pr.ProductId");

            return string.Join("\n                ", joins);
        }

        private string BuildSelectClause(bool includeBrand = false, bool includeCategory = false, bool includeImages = false, int maxImagesPerProduct = 5, bool includeReviews = false)
        {
            var columns = new List<string>
            {
                "p.*" // Todas as colunas de Products
            };

            if (includeBrand)
                columns.Add("b.Name as BrandName, b.LogoUrl as BrandLogoUrl");

            if (includeCategory)
                columns.Add("c.Name as CategoryName, c.Slug as CategorySlug");

            if (includeImages)
                columns.Add($"(SELECT TOP {maxImagesPerProduct} ImageUrl FROM ProductImages WHERE ProductId = p.Id FOR JSON PATH) as Images");

            if (includeReviews)
                columns.Add("AVG(CAST(pr.Rating as FLOAT)) as AverageRating, COUNT(pr.Id) as ReviewCount");

            return $"SELECT {string.Join(", ", columns)}";
        }

        private object BuildParameters(int page, int pageSize, string? searchTerm, Guid? categoryId, Guid? brandId, bool? isActive, bool? inStock, decimal? minPrice, decimal? maxPrice)
        {
            var parameters = new DynamicParameters();

            parameters.Add("Offset", (page - 1) * pageSize);
            parameters.Add("PageSize", pageSize);

            if (!string.IsNullOrWhiteSpace(searchTerm))
                parameters.Add("SearchTerm", $"%{searchTerm}%");

            if (categoryId.HasValue)
                parameters.Add("CategoryId", categoryId.Value);

            if (brandId.HasValue)
                parameters.Add("BrandId", brandId.Value);

            if (isActive.HasValue)
                parameters.Add("IsActive", isActive.Value);

            if (minPrice.HasValue)
                parameters.Add("MinPrice", minPrice.Value);

            if (maxPrice.HasValue)
                parameters.Add("MaxPrice", maxPrice.Value);

            return parameters;
        }

        private object BuildCategoryParameters(int page, int pageSize, Guid categoryId, string? searchTerm, Guid? brandId, decimal? minPrice, decimal? maxPrice, bool? isActive, bool? inStock)
        {
            var parameters = new DynamicParameters();

            parameters.Add("Offset", (page - 1) * pageSize);
            parameters.Add("PageSize", pageSize);
            parameters.Add("CategoryId", categoryId);

            if (!string.IsNullOrWhiteSpace(searchTerm))
                parameters.Add("SearchTerm", $"%{searchTerm}%");

            if (brandId.HasValue)
                parameters.Add("BrandId", brandId.Value);

            if (isActive.HasValue)
                parameters.Add("IsActive", isActive.Value);

            if (minPrice.HasValue)
                parameters.Add("MinPrice", minPrice.Value);

            if (maxPrice.HasValue)
                parameters.Add("MaxPrice", maxPrice.Value);

            return parameters;
        }

        // Métodos CRUD básicos
        public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT * FROM Products WHERE Id = @Id";
            return await connection.QuerySingleOrDefaultAsync<Product>(sql, new { Id = id });
        }

        public async Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);

            var sql = @"
                INSERT INTO Products (Id, Name, Description, Sku, Price, StockQuantity, CategoryId, BrandId, IsActive, CreatedAt, UpdatedAt)
                VALUES (@Id, @Name, @Description, @Sku, @Price, @StockQuantity, @CategoryId, @BrandId, @IsActive, @CreatedAt, @UpdatedAt)";

            await connection.ExecuteAsync(sql, product);
            return product;
        }

        public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);

            var sql = @"
                UPDATE Products 
                SET Name = @Name, Description = @Description, Sku = @Sku, Price = @Price, 
                    StockQuantity = @StockQuantity, CategoryId = @CategoryId, BrandId = @BrandId,
                    IsActive = @IsActive, UpdatedAt = @UpdatedAt
                WHERE Id = @Id";

            await connection.ExecuteAsync(sql, product);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "DELETE FROM Products WHERE Id = @Id";
            await connection.ExecuteAsync(sql, new { Id = id });
        }

        // Métodos utilitários
        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT COUNT(1) FROM Products WHERE Id = @Id";
            var count = await connection.QuerySingleAsync<int>(sql, new { Id = id });
            return count > 0;
        }

        public async Task<bool> ExistsBySkuAsync(string sku, Guid? excludeId = null, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);

            var parameters = new DynamicParameters();
            parameters.Add("Sku", sku);

            var sql = "SELECT COUNT(1) FROM Products WHERE Sku = @Sku";

            if (excludeId.HasValue)
            {
                sql += " AND Id != @ExcludeId";
                parameters.Add("ExcludeId", excludeId.Value);
            }

            var count = await connection.QuerySingleAsync<int>(sql, parameters);
            return count > 0;
        }

        public void SetTransaction(SqlTransaction? transaction)
        {
            _transaction = transaction;
        }
    }
}