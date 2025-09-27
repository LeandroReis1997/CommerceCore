//using CommerceCore.Application.Interfaces.Repositories;
//using CommerceCore.Domain.Entities;
//using CommerceCore.Infrastructure.Repositories;
//using Dapper;
//using Microsoft.Data.SqlClient;
//using Microsoft.Extensions.Configuration;

//namespace CommerceCore.Infrastructure.Repositories
//{
//    public class BrandRepository : IBrandRepository
//    {
//        private readonly string _connectionString; // String de conexão com SQL Server

//        public BrandRepository(IConfiguration configuration)
//        {
//            _connectionString = configuration.GetConnectionString("DefaultConnection")
//                ?? throw new InvalidOperationException("Connection string não encontrada");
//        }

//        // Método para GetBrandsQueryHandler - busca paginada com filtros
//        public async Task<(IEnumerable<Brand> Brands, int TotalCount)> GetPagedAsync(
//            int page,
//            int pageSize,
//            string? searchTerm = null,
//            bool? isActive = null,
//            bool includeProductCount = false,
//            bool includeStatistics = false,
//            bool includeProducts = false,
//            CancellationToken cancellationToken = default)
//        {
//            using var connection = new SqlConnection(_connectionString);

//            var whereClause = BuildWhereClause(searchTerm, isActive);
//            var selectClause = BuildSelectClause(includeProductCount, includeStatistics);
//            var joinClause = BuildJoinClause(includeProducts);

//            var sql = $@"
//                {selectClause}
//                FROM Brands b
//                {joinClause}
//                {whereClause}
//                ORDER BY b.Name ASC
//                OFFSET @Offset ROWS
//                FETCH NEXT @PageSize ROWS ONLY;

//                -- Query para contar total de registros
//                SELECT COUNT(1)
//                FROM Brands b
//                {whereClause}";

//            var parameters = new
//            {
//                Offset = (page - 1) * pageSize,
//                PageSize = pageSize,
//                SearchTerm = $"%{searchTerm}%",
//                IsActive = isActive
//            };

//            using var multi = await connection.QueryMultipleAsync(sql, parameters);
//            var brands = await multi.ReadAsync<Brand>();
//            var totalCount = await multi.ReadSingleAsync<int>();

//            return (brands, totalCount);
//        }

//        // Método para GetBrandByIdQueryHandler - busca por ID com includes
//        public async Task<Brand?> GetByIdAsync(
//            Guid id,
//            bool includeProducts = false,
//            bool includeStatistics = false,
//            bool includeProductDetails = false,
//            CancellationToken cancellationToken = default)
//        {
//            using var connection = new SqlConnection(_connectionString);

//            var selectClause = BuildSelectClause(includeProductCount: true, includeStatistics);
//            var joinClause = BuildJoinClause(includeProducts);

//            var sql = $@"
//                {selectClause}
//                FROM Brands b
//                {joinClause}
//                WHERE b.Id = @Id";

//            return await connection.QuerySingleOrDefaultAsync<Brand>(sql, new { Id = id });
//        }

//        // Métodos auxiliares para construção dinâmica de SQL
//        private string BuildWhereClause(string? searchTerm, bool? isActive)
//        {
//            var conditions = new List<string> { "1=1" }; // Base condition

//            if (!string.IsNullOrWhiteSpace(searchTerm))
//                conditions.Add("b.Name LIKE @SearchTerm");

//            if (isActive.HasValue)
//                conditions.Add("b.IsActive = @IsActive");

//            return $"WHERE {string.Join(" AND ", conditions)}";
//        }

//        private string BuildSelectClause(bool includeProductCount = false, bool includeStatistics = false)
//        {
//            var columns = new List<string>
//            {
//                "b.*" // Todas as colunas de Brands
//            };

//            if (includeProductCount)
//                columns.Add("(SELECT COUNT(*) FROM Products WHERE BrandId = b.Id AND IsActive = 1) as ProductCount");

//            if (includeStatistics)
//            {
//                columns.Add("(SELECT AVG(Price) FROM Products WHERE BrandId = b.Id AND IsActive = 1) as AveragePrice");
//                columns.Add("(SELECT COUNT(*) FROM Products p INNER JOIN OrderItems oi ON p.Id = oi.ProductId WHERE p.BrandId = b.Id) as TotalSales");
//            }

//            return $"SELECT {string.Join(", ", columns)}";
//        }

//        private string BuildJoinClause(bool includeProducts = false)
//        {
//            var joins = new List<string>();

//            if (includeProducts)
//                joins.Add("LEFT JOIN Products p ON b.Id = p.BrandId AND p.IsActive = 1");

//            return string.Join("\n", joins);
//        }

//        // Métodos CRUD básicos
//        public async Task<Brand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
//        {
//            using var connection = new SqlConnection(_connectionString);
//            var sql = "SELECT * FROM Brands WHERE Id = @Id";
//            return await connection.QuerySingleOrDefaultAsync<Brand>(sql, new { Id = id });
//        }

//        public async Task<Brand> AddAsync(Brand brand, CancellationToken cancellationToken = default)
//        {
//            using var connection = new SqlConnection(_connectionString);

//            var sql = @"
//                INSERT INTO Brands (Id, Name, Description, LogoUrl, IsActive, CreatedAt, UpdatedAt)
//                VALUES (@Id, @Name, @Description, @LogoUrl, @IsActive, @CreatedAt, @UpdatedAt)";

//            await connection.ExecuteAsync(sql, brand);
//            return brand;
//        }

//        public async Task UpdateAsync(Brand brand, CancellationToken cancellationToken = default)
//        {
//            using var connection = new SqlConnection(_connectionString);

//            var sql = @"
//                UPDATE Brands 
//                SET Name = @Name, Description = @Description, LogoUrl = @LogoUrl, 
//                    IsActive = @IsActive, UpdatedAt = @UpdatedAt
//                WHERE Id = @Id";

//            await connection.ExecuteAsync(sql, brand);
//        }

//        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
//        {
//            using var connection = new SqlConnection(_connectionString);
//            var sql = "DELETE FROM Brands WHERE Id = @Id";
//            await connection.ExecuteAsync(sql, new { Id = id });
//        }

//        // Métodos utilitários
//        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
//        {
//            using var connection = new SqlConnection(_connectionString);
//            var sql = "SELECT COUNT(1) FROM Brands WHERE Id = @Id";
//            var count = await connection.QuerySingleAsync<int>(sql, new { Id = id });
//            return count > 0;
//        }

//        public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
//        {
//            using var connection = new SqlConnection(_connectionString);

//            var sql = "SELECT COUNT(1) FROM Brands WHERE Name = @Name";
//            if (excludeId.HasValue)
//            {
//                sql += " AND Id != @ExcludeId";
//            }

//            var parameters = new DynamicParameters();
//            parameters.Add("Name", name);
//            if (excludeId.HasValue)
//            {
//                parameters.Add("ExcludeId", excludeId.Value);
//            }

//            var count = await connection.QuerySingleAsync<int>(sql, parameters);
//            return count > 0;
//        }

//        public async Task<int> GetProductCountAsync(Guid brandId, CancellationToken cancellationToken = default)
//        {
//            using var connection = new SqlConnection(_connectionString);
//            var sql = "SELECT COUNT(*) FROM Products WHERE BrandId = @BrandId AND IsActive = 1";
//            return await connection.QuerySingleAsync<int>(sql, new { BrandId = brandId });
//        }
//    }
//}


using CommerceCore.Application.Interfaces.Repositories;
using CommerceCore.Domain.Entities;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace CommerceCore.Infrastructure.Repositories
{
    public class BrandRepository : IBrandRepository
    {
        private readonly string _connectionString; // String de conexão com SQL Server
        private readonly SqlConnection? _connection; // Conexão compartilhada (para UnitOfWork)
        private SqlTransaction? _transaction; // Transação compartilhada (para UnitOfWork)

        // Construtor para uso independente (cria nova conexão a cada operação)
        public BrandRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string não encontrada");
            _connection = null;
            _transaction = null;
        }

        // Construtor para uso com UnitOfWork (usa conexão e transação compartilhadas)
        public BrandRepository(string connectionString, SqlConnection connection, SqlTransaction? transaction = null)
        {
            _connectionString = connectionString;
            _connection = connection;
            _transaction = transaction;
        }

        // Método helper para obter conexão (usa compartilhada ou cria nova)
        private SqlConnection GetConnection()
        {
            return _connection ?? new SqlConnection(_connectionString);
        }

        // Método helper para executar operação com controle de conexão
        private async Task<T> ExecuteWithConnectionAsync<T>(Func<SqlConnection, Task<T>> operation)
        {
            var connection = GetConnection();
            var shouldCloseConnection = _connection == null;

            try
            {
                if (connection.State != System.Data.ConnectionState.Open)
                    await connection.OpenAsync();

                return await operation(connection);
            }
            finally
            {
                if (shouldCloseConnection && connection.State == System.Data.ConnectionState.Open)
                {
                    connection.Close();
                    connection.Dispose();
                }
            }
        }

        // Busca paginada de marcas com filtros avançados e includes opcionais (usado pelo GetBrandsQueryHandler)
        public async Task<(IEnumerable<Brand> Brands, int TotalCount)> GetPagedAsync(
            int page, // Número da página (inicia em 1)
            int pageSize, // Quantidade de marcas por página
            string? searchTerm = null, // Termo para busca por nome da marca
            bool? isActive = null, // Filtro por marcas ativas/inativas (null = todas)
            bool includeProductCount = false, // Incluir quantidade de produtos ativos de cada marca
            bool includeStatistics = false, // Incluir estatísticas da marca (preço médio, total de vendas)
            bool includeProducts = false, // Incluir lista de produtos da marca
            CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                // Constrói cláusulas SQL dinamicamente baseado nos parâmetros
                var whereClause = BuildWhereClause(searchTerm, isActive);
                var selectClause = BuildSelectClause(includeProductCount, includeStatistics);
                var joinClause = BuildJoinClause(includeProducts);

                // Query principal com paginação + query para contagem total
                var sql = $@"
                    {selectClause}
                    FROM Brands b
                    {joinClause}
                    {whereClause}
                    ORDER BY b.Name ASC
                    OFFSET @Offset ROWS
                    FETCH NEXT @PageSize ROWS ONLY;

                    -- Query para contar total de registros sem paginação
                    SELECT COUNT(1)
                    FROM Brands b
                    {whereClause}";

                // Monta parâmetros para a query
                var parameters = new
                {
                    Offset = (page - 1) * pageSize, // Calcula offset para paginação
                    PageSize = pageSize,
                    SearchTerm = $"%{searchTerm}%", // Adiciona wildcards para LIKE
                    IsActive = isActive
                };

                // Executa múltiplas queries e retorna resultados
                using var multi = await connection.QueryMultipleAsync(sql, parameters, _transaction);
                var brands = await multi.ReadAsync<Brand>(); // Primeira query: marcas paginadas
                var totalCount = await multi.ReadSingleAsync<int>(); // Segunda query: contagem total

                return (brands, totalCount);
            });
        }

        // Busca marca por ID específico com includes opcionais (usado pelo GetBrandByIdQueryHandler)
        public async Task<Brand?> GetByIdAsync(
            Guid id, // ID único da marca a ser buscada
            bool includeProducts = false, // Incluir lista de produtos da marca
            bool includeStatistics = false, // Incluir estatísticas calculadas da marca
            bool includeProductDetails = false, // Incluir detalhes completos dos produtos (nome, preço, etc)
            CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                // Constrói query baseada nas flags de inclusão
                var selectClause = BuildSelectClause(includeProductCount: true, includeStatistics);
                var joinClause = BuildJoinClause(includeProducts);

                var sql = $@"
                    {selectClause}
                    FROM Brands b
                    {joinClause}
                    WHERE b.Id = @Id";

                // Executa query e retorna marca encontrada (ou null se não existir)
                return await connection.QuerySingleOrDefaultAsync<Brand>(sql, new { Id = id }, _transaction);
            });
        }

        // MÉTODOS AUXILIARES PRIVADOS PARA CONSTRUÇÃO DINÂMICA DE SQL

        // Constrói cláusula WHERE baseada nos filtros fornecidos
        private string BuildWhereClause(string? searchTerm, bool? isActive)
        {
            var conditions = new List<string> { "1=1" }; // Condição base sempre verdadeira

            // Adiciona filtro por nome se termo de busca foi fornecido
            if (!string.IsNullOrWhiteSpace(searchTerm))
                conditions.Add("b.Name LIKE @SearchTerm");

            // Adiciona filtro por status ativo se especificado
            if (isActive.HasValue)
                conditions.Add("b.IsActive = @IsActive");

            return $"WHERE {string.Join(" AND ", conditions)}";
        }

        // Constrói cláusula SELECT com colunas condicionais baseadas nas flags
        private string BuildSelectClause(bool includeProductCount = false, bool includeStatistics = false)
        {
            var columns = new List<string>
            {
                "b.*" // Todas as colunas básicas da tabela Brands
            };

            // Adiciona contagem de produtos ativos se solicitado
            if (includeProductCount)
                columns.Add("(SELECT COUNT(*) FROM Products WHERE BrandId = b.Id AND IsActive = 1) as ProductCount");

            // Adiciona estatísticas calculadas se solicitadas
            if (includeStatistics)
            {
                columns.Add("(SELECT AVG(Price) FROM Products WHERE BrandId = b.Id AND IsActive = 1) as AveragePrice");
                columns.Add("(SELECT COUNT(*) FROM Products p INNER JOIN OrderItems oi ON p.Id = oi.ProductId WHERE p.BrandId = b.Id) as TotalSales");
            }

            return $"SELECT {string.Join(", ", columns)}";
        }

        // Constrói cláusula JOIN baseada nas flags de inclusão
        private string BuildJoinClause(bool includeProducts = false)
        {
            var joins = new List<string>();

            // Adiciona JOIN com produtos se solicitado (apenas produtos ativos)
            if (includeProducts)
                joins.Add("LEFT JOIN Products p ON b.Id = p.BrandId AND p.IsActive = 1");

            return string.Join("\n", joins);
        }

        // MÉTODOS CRUD BÁSICOS (Create, Read, Update, Delete)

        // Busca marca por ID de forma simples (sem includes) - usado pelos Commands
        public async Task<Brand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var sql = "SELECT * FROM Brands WHERE Id = @Id";
                return await connection.QuerySingleOrDefaultAsync<Brand>(sql, new { Id = id }, _transaction);
            });
        }

        // Adiciona nova marca no banco de dados
        public async Task<Brand> AddAsync(Brand brand, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var sql = @"
                    INSERT INTO Brands (Id, Name, Description, LogoUrl, IsActive, CreatedAt, UpdatedAt)
                    VALUES (@Id, @Name, @Description, @LogoUrl, @IsActive, @CreatedAt, @UpdatedAt)";

                await connection.ExecuteAsync(sql, brand, _transaction);
                return brand; // Retorna a marca inserida
            });
        }

        // Atualiza marca existente no banco de dados
        public async Task UpdateAsync(Brand brand, CancellationToken cancellationToken = default)
        {
            await ExecuteWithConnectionAsync(async connection =>
            {
                var sql = @"
                    UPDATE Brands 
                    SET Name = @Name, Description = @Description, LogoUrl = @LogoUrl, 
                        IsActive = @IsActive, UpdatedAt = @UpdatedAt
                    WHERE Id = @Id";

                await connection.ExecuteAsync(sql, brand, _transaction);
                return true; // Para satisfazer o delegate
            });
        }

        // Remove marca do banco de dados por ID
        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await ExecuteWithConnectionAsync(async connection =>
            {
                var sql = "DELETE FROM Brands WHERE Id = @Id";
                await connection.ExecuteAsync(sql, new { Id = id }, _transaction);
                return true; // Para satisfazer o delegate
            });
        }

        // MÉTODOS UTILITÁRIOS E VALIDAÇÕES

        // Verifica se marca existe no banco de dados por ID
        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var sql = "SELECT COUNT(1) FROM Brands WHERE Id = @Id";
                var count = await connection.QuerySingleAsync<int>(sql, new { Id = id }, _transaction);
                return count > 0; // Retorna true se encontrou pelo menos 1 registro
            });
        }

        // Verifica se já existe marca com o mesmo nome (para validação de duplicatas)
        public async Task<bool> ExistsByNameAsync(
            string name, // Nome da marca a verificar
            Guid? excludeId = null, // ID da marca a excluir da verificação (útil para updates)
            CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var sql = "SELECT COUNT(1) FROM Brands WHERE Name = @Name";

                // Se foi fornecido ID para excluir, adiciona condição para ignorá-lo
                if (excludeId.HasValue)
                {
                    sql += " AND Id != @ExcludeId";
                }

                // Monta parâmetros dinamicamente
                var parameters = new DynamicParameters();
                parameters.Add("Name", name);
                if (excludeId.HasValue)
                {
                    parameters.Add("ExcludeId", excludeId.Value);
                }

                var count = await connection.QuerySingleAsync<int>(sql, parameters, _transaction);
                return count > 0; // Retorna true se encontrou marca com o mesmo nome
            });
        }

        // Conta quantos produtos ativos uma marca possui
        public async Task<int> GetProductCountAsync(Guid brandId, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var sql = "SELECT COUNT(*) FROM Products WHERE BrandId = @BrandId AND IsActive = 1";
                return await connection.QuerySingleAsync<int>(sql, new { BrandId = brandId }, _transaction);
            });
        }

        // Busca marcas mais populares por quantidade de vendas
        public async Task<IEnumerable<Brand>> GetMostPopularBrandsAsync(
            int limit = 10, // Limite de marcas a retornar
            CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var sql = $@"
                    SELECT TOP {limit} b.*, COUNT(oi.Id) as SalesCount
                    FROM Brands b
                    INNER JOIN Products p ON b.Id = p.BrandId
                    INNER JOIN OrderItems oi ON p.Id = oi.ProductId
                    WHERE b.IsActive = 1 AND p.IsActive = 1
                    GROUP BY b.Id, b.Name, b.Description, b.LogoUrl, b.IsActive, b.CreatedAt, b.UpdatedAt
                    ORDER BY SalesCount DESC";

                return await connection.QueryAsync<Brand>(sql, transaction: _transaction);
            });
        }

        // Busca marcas por padrão no nome (busca flexível)
        public async Task<IEnumerable<Brand>> SearchByNameAsync(
            string searchPattern, // Padrão a buscar no nome
            int limit = 20, // Limite de resultados
            bool activeOnly = true, // Se deve buscar apenas marcas ativas
            CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var sql = $@"
                    SELECT TOP {limit} *
                    FROM Brands 
                    WHERE Name LIKE @SearchPattern";

                // Adiciona filtro por ativas se solicitado
                if (activeOnly)
                    sql += " AND IsActive = 1";

                sql += " ORDER BY Name ASC";

                return await connection.QueryAsync<Brand>(sql, new
                {
                    SearchPattern = $"%{searchPattern}%"
                }, _transaction);
            });
        }

        // Obtém estatísticas detalhadas de uma marca específica
        public async Task<BrandStatistics> GetBrandStatisticsAsync(
            Guid brandId, // ID da marca
            CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var sql = @"
                    SELECT 
                        COUNT(DISTINCT p.Id) as TotalProducts,
                        COUNT(CASE WHEN p.IsActive = 1 THEN 1 END) as ActiveProducts,
                        AVG(CASE WHEN p.IsActive = 1 THEN p.Price END) as AveragePrice,
                        MIN(CASE WHEN p.IsActive = 1 THEN p.Price END) as MinPrice,
                        MAX(CASE WHEN p.IsActive = 1 THEN p.Price END) as MaxPrice,
                        COUNT(DISTINCT oi.OrderId) as TotalOrders,
                        SUM(oi.Quantity) as TotalItemsSold,
                        SUM(oi.TotalPrice) as TotalRevenue
                    FROM Brands b
                    LEFT JOIN Products p ON b.Id = p.BrandId
                    LEFT JOIN OrderItems oi ON p.Id = oi.ProductId
                    WHERE b.Id = @BrandId
                    GROUP BY b.Id";

                return await connection.QuerySingleAsync<BrandStatistics>(sql, new { BrandId = brandId }, _transaction);
            });
        }

        // Atualiza apenas o status ativo/inativo de uma marca
        public async Task UpdateActiveStatusAsync(
            Guid brandId, // ID da marca
            bool isActive, // Novo status ativo/inativo
            CancellationToken cancellationToken = default)
        {
            await ExecuteWithConnectionAsync(async connection =>
            {
                var sql = @"
                    UPDATE Brands 
                    SET IsActive = @IsActive, UpdatedAt = @UpdatedAt 
                    WHERE Id = @BrandId";

                await connection.ExecuteAsync(sql, new
                {
                    BrandId = brandId,
                    IsActive = isActive,
                    UpdatedAt = DateTime.UtcNow
                }, _transaction);

                return true;
            });
        }

        // Obtém lista de todas as marcas ativas (para dropdowns/selects)
        public async Task<IEnumerable<Brand>> GetActiveBrandsAsync(CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var sql = @"
                    SELECT * 
                    FROM Brands 
                    WHERE IsActive = 1 
                    ORDER BY Name ASC";

                return await connection.QueryAsync<Brand>(sql, transaction: _transaction);
            });
        }
        public void SetTransaction(SqlTransaction? transaction)
        {
            _transaction = transaction;
        }
    }

    // Classe auxiliar para estatísticas da marca
    public class BrandStatistics
    {
        public int TotalProducts { get; set; } // Total de produtos da marca
        public int ActiveProducts { get; set; } // Produtos ativos da marca
        public decimal? AveragePrice { get; set; } // Preço médio dos produtos
        public decimal? MinPrice { get; set; } // Menor preço entre os produtos
        public decimal? MaxPrice { get; set; } // Maior preço entre os produtos
        public int TotalOrders { get; set; } // Total de pedidos com produtos da marca
        public int TotalItemsSold { get; set; } // Total de itens vendidos
        public decimal TotalRevenue { get; set; } // Receita total gerada pela marca
    }
}