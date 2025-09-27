using CommerceCore.Application.Interfaces.Repositories;
using CommerceCore.Domain.Entities;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace CommerceCore.Infrastructure.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly string _connectionString; // String de conexão com SQL Server
        private readonly SqlConnection? _connection; // Conexão compartilhada (para UnitOfWork)
        private SqlTransaction? _transaction; // Transação compartilhada (para UnitOfWork)

        // Construtor que recebe a configuração e inicializa a connection string
        public CategoryRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string não encontrada");
            _connection = null;
            _transaction = null;
        }

        // Construtor para uso com UnitOfWork (usa conexão e transação compartilhadas)
        public CategoryRepository(string connectionString, SqlConnection connection, SqlTransaction? transaction = null)
        {
            _connectionString = connectionString;
            _connection = connection;
            _transaction = transaction;
        }

        // Busca categorias paginadas com filtros e includes opcionais
        public async Task<(IEnumerable<Category> Categories, int TotalCount)> GetPagedAsync(
            int page, // Número da página
            int pageSize, // Itens por página
            string? searchTerm = null, // Busca por nome da categoria
            bool? isActive = null, // Filtro por status ativo/inativo
            Guid? parentCategoryId = null, // Filtro por categoria pai (null = categorias raiz)
            bool includeSubcategories = false, // Incluir subcategorias filhas
            bool includeProductCount = false, // Incluir contagem de produtos na categoria
            bool includeProducts = false, // Incluir produtos da categoria
            bool includeHierarchy = false, // Incluir caminho hierárquico completo
            CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);

            var whereClause = BuildWhereClause(searchTerm, isActive, parentCategoryId);
            var selectClause = BuildSelectClause(includeProductCount, includeHierarchy);
            var joinClause = BuildJoinClause(includeProducts);

            var sql = $@"
                {selectClause}
                FROM Categories c
                {joinClause}
                {whereClause}
                ORDER BY c.Name ASC
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY;

                -- Query para contar total de registros
                SELECT COUNT(1)
                FROM Categories c
                {whereClause}";

            var parameters = BuildParameters(page, pageSize, searchTerm, isActive, parentCategoryId);

            using var multi = await connection.QueryMultipleAsync(sql, parameters);
            var categories = await multi.ReadAsync<Category>();
            var totalCount = await multi.ReadSingleAsync<int>();

            return (categories, totalCount);
        }

        // Busca categoria por ID com includes condicionais
        public async Task<Category?> GetByIdAsync(
            Guid id, // ID da categoria
            bool includeParent = false, // Incluir categoria pai
            bool includeSubcategories = false, // Incluir subcategorias filhas
            bool includeProducts = false, // Incluir produtos da categoria
            bool includeProductDetails = false, // Incluir detalhes completos dos produtos
            bool includeHierarchy = false, // Incluir caminho hierárquico completo
            CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);

            var selectClause = BuildSelectClause(includeProductCount: true, includeHierarchy);
            var joinClause = BuildJoinClause(includeProducts, includeParent);

            var sql = $@"
                {selectClause}
                FROM Categories c
                {joinClause}
                WHERE c.Id = @Id";

            var category = await connection.QuerySingleOrDefaultAsync<Category>(sql, new { Id = id });

            // Se solicitado, busca subcategorias separadamente para melhor performance
            if (category != null && includeSubcategories)
            {
                var subcategories = await GetSubcategoriesAsync(id, includeInactive: false, recursive: false, cancellationToken);
                category.Children = subcategories.ToList(); // ← Corrigido: usa Children ao invés de Subcategories
            }

            return category;
        }

        // Busca hierarquia completa de categorias em formato de árvore
        public async Task<IEnumerable<Category>> GetHierarchyAsync(
            Guid? rootCategoryId = null, // Categoria raiz (null = desde o topo)
            bool? isActive = true, // Filtro por status ativo/inativo
            bool includeProductCount = true, // Incluir contagem de produtos em cada categoria
            int? maxDepth = null, // Máximo de níveis de profundidade (null = sem limite)
            CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);

            var activeFilter = isActive.HasValue ? "AND IsActive = @IsActive" : "";
            var depthLimit = maxDepth.HasValue ? $"AND Level <= {maxDepth.Value}" : "";
            var rootFilter = rootCategoryId.HasValue ? "WHERE Id = @RootCategoryId" : "WHERE ParentId IS NULL"; // ← Corrigido

            var sql = $@"
                WITH CategoryHierarchy AS (
                    -- Anchor: categorias raiz ou categoria específica
                    SELECT 
                        Id, Name, ParentId, IsActive, CreatedAt, UpdatedAt,
                        0 as Level,
                        CAST(Name as NVARCHAR(4000)) as Path
                    FROM Categories 
                    {rootFilter} {activeFilter}
                    
                    UNION ALL
                    
                    -- Recursive: subcategorias
                    SELECT 
                        c.Id, c.Name, c.ParentId, c.IsActive, c.CreatedAt, c.UpdatedAt,
                        ch.Level + 1 as Level,
                        CAST(ch.Path + ' > ' + c.Name as NVARCHAR(4000)) as Path
                    FROM Categories c
                    INNER JOIN CategoryHierarchy ch ON c.ParentId = ch.Id -- ← Corrigido
                    WHERE c.IsActive = 1 {depthLimit}
                )
                SELECT 
                    ch.*,
                    {(includeProductCount ? "(SELECT COUNT(*) FROM Products WHERE CategoryId = ch.Id AND IsActive = 1) as ProductCount" : "0 as ProductCount")}
                FROM CategoryHierarchy ch
                ORDER BY ch.Level, ch.Name";

            var parameters = new DynamicParameters();

            if (rootCategoryId.HasValue)
                parameters.Add("RootCategoryId", rootCategoryId.Value);

            if (isActive.HasValue)
                parameters.Add("IsActive", isActive.Value);

            return await connection.QueryAsync<Category>(sql, parameters);
        }

        // Busca todas as subcategorias de uma categoria usando recursão SQL
        public async Task<IEnumerable<Category>> GetSubcategoriesAsync(
            Guid parentId, // ID da categoria pai
            bool includeInactive = false, // Incluir subcategorias inativas
            bool recursive = true, // Buscar recursivamente todos os níveis
            CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);

            var activeFilter = includeInactive ? "" : "AND IsActive = 1";

            // Query recursiva ou apenas primeiro nível baseado no parâmetro
            var sql = recursive ? $@"
                WITH SubcategoryHierarchy AS (
                    -- Subcategorias diretas
                    SELECT Id, Name, ParentId, IsActive, CreatedAt, UpdatedAt, 1 as Level
                    FROM Categories 
                    WHERE ParentId = @ParentId {activeFilter} -- ← Corrigido
                    
                    UNION ALL
                    
                    -- Subcategorias recursivas
                    SELECT 
                        c.Id, c.Name, c.ParentId, c.IsActive, c.CreatedAt, c.UpdatedAt,
                        sh.Level + 1 as Level
                    FROM Categories c
                    INNER JOIN SubcategoryHierarchy sh ON c.ParentId = sh.Id -- ← Corrigido
                    WHERE c.IsActive = 1
                )
                SELECT * FROM SubcategoryHierarchy
                ORDER BY Level, Name"
            :
                $@"SELECT * FROM Categories 
                   WHERE ParentId = @ParentId {activeFilter} -- ← Corrigido
                   ORDER BY Name";

            return await connection.QueryAsync<Category>(sql, new { ParentId = parentId });
        }

        // Busca o caminho hierárquico completo de uma categoria para breadcrumb
        public async Task<IEnumerable<Category>> GetCategoryPathAsync(
            Guid categoryId, // ID da categoria
            CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);

            var sql = @"
                WITH CategoryPath AS (
                    -- Categoria atual
                    SELECT Id, Name, ParentId, IsActive, CreatedAt, UpdatedAt, 0 as Level
                    FROM Categories 
                    WHERE Id = @CategoryId
                    
                    UNION ALL
                    
                    -- Categorias pai (recursivo para cima)
                    SELECT 
                        c.Id, c.Name, c.ParentId, c.IsActive, c.CreatedAt, c.UpdatedAt,
                        cp.Level + 1 as Level
                    FROM Categories c
                    INNER JOIN CategoryPath cp ON c.Id = cp.ParentId -- ← Corrigido
                )
                SELECT * FROM CategoryPath
                ORDER BY Level DESC"; // Do pai para o filho para formar breadcrumb

            return await connection.QueryAsync<Category>(sql, new { CategoryId = categoryId });
        }

        // Constrói a cláusula WHERE dinamicamente baseada nos filtros fornecidos
        private string BuildWhereClause(string? searchTerm, bool? isActive, Guid? parentCategoryId)
        {
            var conditions = new List<string> { "1=1" };

            if (!string.IsNullOrWhiteSpace(searchTerm))
                conditions.Add("c.Name LIKE @SearchTerm"); // Removido Description pois não existe na sua entidade

            if (isActive.HasValue)
                conditions.Add("c.IsActive = @IsActive");

            if (parentCategoryId.HasValue)
                conditions.Add("c.ParentId = @ParentCategoryId"); // ← Corrigido
            else
                conditions.Add("c.ParentId IS NULL"); // ← Corrigido

            return $"WHERE {string.Join(" AND ", conditions)}";
        }

        // Constrói a cláusula SELECT com campos condicionais baseados nas flags
        private string BuildSelectClause(bool includeProductCount = false, bool includeHierarchy = false)
        {
            var columns = new List<string> { "c.*" };

            if (includeProductCount)
                columns.Add("(SELECT COUNT(*) FROM Products WHERE CategoryId = c.Id AND IsActive = 1) as ProductCount");

            if (includeHierarchy)
                columns.Add("(SELECT Name FROM Categories WHERE Id = c.ParentId) as ParentName"); // ← Corrigido

            return $"SELECT {string.Join(", ", columns)}";
        }

        // Constrói JOINs opcionais baseados nas flags de inclusão
        private string BuildJoinClause(bool includeProducts = false, bool includeParent = false)
        {
            var joins = new List<string>();

            if (includeProducts)
                joins.Add("LEFT JOIN Products p ON c.Id = p.CategoryId AND p.IsActive = 1");

            if (includeParent)
                joins.Add("LEFT JOIN Categories parent ON c.ParentId = parent.Id"); // ← Corrigido

            return string.Join("\n                ", joins);
        }

        // Constrói parâmetros dinâmicos para a query usando DynamicParameters
        private object BuildParameters(int page, int pageSize, string? searchTerm, bool? isActive, Guid? parentCategoryId)
        {
            var parameters = new DynamicParameters();

            parameters.Add("Offset", (page - 1) * pageSize);
            parameters.Add("PageSize", pageSize);

            if (!string.IsNullOrWhiteSpace(searchTerm))
                parameters.Add("SearchTerm", $"%{searchTerm}%");

            if (isActive.HasValue)
                parameters.Add("IsActive", isActive.Value);

            if (parentCategoryId.HasValue)
                parameters.Add("ParentCategoryId", parentCategoryId.Value);

            return parameters;
        }

        // Busca categoria simples por ID sem includes (usado pelos Commands)
        public async Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT * FROM Categories WHERE Id = @Id";
            return await connection.QuerySingleOrDefaultAsync<Category>(sql, new { Id = id });
        }

        // Adiciona nova categoria no banco de dados
        public async Task<Category> AddAsync(Category category, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);

            var sql = @"
                INSERT INTO Categories (Id, Name, ParentId, IsActive, CreatedAt, UpdatedAt)
                VALUES (@Id, @Name, @ParentId, @IsActive, @CreatedAt, @UpdatedAt)"; // ← Corrigido

            await connection.ExecuteAsync(sql, category);
            return category;
        }

        // Atualiza categoria existente no banco de dados
        public async Task UpdateAsync(Category category, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);

            var sql = @"
                UPDATE Categories 
                SET Name = @Name, ParentId = @ParentId, IsActive = @IsActive, UpdatedAt = @UpdatedAt
                WHERE Id = @Id"; // ← Corrigido (removido Slug e Description)

            await connection.ExecuteAsync(sql, category);
        }

        // Remove categoria do banco de dados por ID
        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "DELETE FROM Categories WHERE Id = @Id";
            await connection.ExecuteAsync(sql, new { Id = id });
        }

        // Verifica se categoria existe no banco de dados
        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT COUNT(1) FROM Categories WHERE Id = @Id";
            var count = await connection.QuerySingleAsync<int>(sql, new { Id = id });
            return count > 0;
        }

        // Verifica se já existe categoria com o mesmo nome (para validação de duplicatas)
        public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);

            var parameters = new DynamicParameters();
            parameters.Add("Name", name);

            var sql = "SELECT COUNT(1) FROM Categories WHERE Name = @Name";

            if (excludeId.HasValue)
            {
                sql += " AND Id != @ExcludeId";
                parameters.Add("ExcludeId", excludeId.Value);
            }

            var count = await connection.QuerySingleAsync<int>(sql, parameters);
            return count > 0;
        }

        // Conta quantos produtos estão associados a uma categoria (com opção de incluir subcategorias)
        public async Task<int> GetProductCountAsync(Guid categoryId, bool includeSubcategories = false, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);

            // Query recursiva se incluir subcategorias, senão apenas da categoria atual
            var sql = includeSubcategories ? @"
                WITH CategoryHierarchy AS (
                    SELECT Id FROM Categories WHERE Id = @CategoryId
                    UNION ALL
                    SELECT c.Id FROM Categories c
                    INNER JOIN CategoryHierarchy ch ON c.ParentId = ch.Id -- ← Corrigido
                )
                SELECT COUNT(*) FROM Products p
                INNER JOIN CategoryHierarchy ch ON p.CategoryId = ch.Id
                WHERE p.IsActive = 1"
            :
                "SELECT COUNT(*) FROM Products WHERE CategoryId = @CategoryId AND IsActive = 1";

            return await connection.QuerySingleAsync<int>(sql, new { CategoryId = categoryId });
        }

        // Verifica se categoria possui subcategorias filhas
        public async Task<bool> HasSubcategoriesAsync(Guid categoryId, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT COUNT(1) FROM Categories WHERE ParentId = @CategoryId"; // ← Corrigido
            var count = await connection.QuerySingleAsync<int>(sql, new { CategoryId = categoryId });
            return count > 0;
        }

        // Valida se uma categoria pode ser pai de outra (evita loops circulares na hierarquia)
        public async Task<bool> IsValidParentAsync(Guid categoryId, Guid? newParentId, CancellationToken cancellationToken = default)
        {
            if (!newParentId.HasValue)
                return true; // Pode ser categoria raiz

            using var connection = new SqlConnection(_connectionString);

            // Verifica se não é ela mesma
            if (categoryId == newParentId.Value)
                return false;

            // Verifica se o novo pai não é descendente da categoria atual (evita loops circulares)
            var sql = @"
                WITH CategoryHierarchy AS (
                    SELECT Id FROM Categories WHERE Id = @CategoryId
                    UNION ALL
                    SELECT c.Id FROM Categories c
                    INNER JOIN CategoryHierarchy ch ON c.ParentId = ch.Id -- ← Corrigido
                )
                SELECT COUNT(1) FROM CategoryHierarchy WHERE Id = @NewParentId";

            var count = await connection.QuerySingleAsync<int>(sql, new { CategoryId = categoryId, NewParentId = newParentId.Value });
            return count == 0; // Se não encontrou na árvore de descendentes, é válido
        }

        // Calcula a profundidade de uma categoria na hierarquia (quantos níveis até a raiz)
        public async Task<int> GetDepthAsync(Guid categoryId, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);

            var sql = @"
                WITH CategoryPath AS (
                    SELECT Id, ParentId, 0 as Depth FROM Categories WHERE Id = @CategoryId
                    UNION ALL
                    SELECT c.Id, c.ParentId, cp.Depth + 1 FROM Categories c
                    INNER JOIN CategoryPath cp ON c.Id = cp.ParentId -- ← Corrigido
                )
                SELECT MAX(Depth) FROM CategoryPath";

            return await connection.QuerySingleOrDefaultAsync<int>(sql, new { CategoryId = categoryId });
        }

        public void SetTransaction(SqlTransaction? transaction)
        {
            _transaction = transaction;
        }
    }
}