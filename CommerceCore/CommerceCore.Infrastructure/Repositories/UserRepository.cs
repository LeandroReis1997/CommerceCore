using CommerceCore.Application.Interfaces.Repositories;
using CommerceCore.Domain.Entities;
using CommerceCore.Infrastructure.Repositories;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Numerics;

namespace CommerceCore.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly string _connectionString; // String de conexão com SQL Server
        private readonly SqlConnection? _connection; // Conexão compartilhada (para UnitOfWork)
        private SqlTransaction? _transaction; // Transação compartilhada (para UnitOfWork)

        public UserRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string não encontrada");
            _connection = null;
            _transaction = null;
        }

        // Construtor para uso com UnitOfWork (usa conexão e transação compartilhadas)
        public UserRepository(string connectionString, SqlConnection connection, SqlTransaction? transaction = null)
        {
            _connectionString = connectionString;
            _connection = connection;
            _transaction = transaction;
        }

        // Busca usuários paginados com filtros e includes opcionais
        public async Task<(IEnumerable<User> Users, int TotalCount)> GetPagedAsync(
            int page, // Número da página
            int pageSize, // Itens por página
            string? searchTerm = null, // Busca por nome, email ou CPF
            bool? isActive = null, // Filtro por status ativo/inativo
            DateTime? createdAfter = null, // Usuários criados após esta data
            DateTime? createdBefore = null, // Usuários criados antes desta data
            bool includeProfile = false, // Incluir dados completos do perfil
            bool includeOrderHistory = false, // Incluir histórico de pedidos
            bool includeOrderCount = false, // Incluir contagem de pedidos
            bool includeLastLogin = false, // Incluir informações de último login
            CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);

            var whereClause = BuildWhereClause(searchTerm, isActive, createdAfter, createdBefore);
            var selectClause = BuildSelectClause(includeProfile, includeOrderCount, includeLastLogin);
            var joinClause = BuildJoinClause(includeOrderHistory);

            var sql = $@"
                {selectClause}
                FROM Users u
                {joinClause}
                {whereClause}
                ORDER BY u.CreatedAt DESC
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY;

                -- Query para contar total de registros
                SELECT COUNT(1)
                FROM Users u
                {whereClause}";

            var parameters = BuildParameters(page, pageSize, searchTerm, isActive, createdAfter, createdBefore);

            using var multi = await connection.QueryMultipleAsync(sql, parameters);
            var users = await multi.ReadAsync<User>();
            var totalCount = await multi.ReadSingleAsync<int>();

            return (users, totalCount);
        }

        // Busca usuário por ID com includes condicionais
        public async Task<User?> GetByIdAsync(
            Guid id, // ID do usuário
            bool includeProfile = false, // Incluir dados completos do perfil (endereços, telefones)
            bool includeOrders = false, // Incluir pedidos do usuário
            bool includeOrderDetails = false, // Incluir detalhes completos dos pedidos
            bool includeCart = false, // Incluir carrinho ativo do usuário
            bool includeAddresses = false, // Incluir endereços cadastrados
            CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);

            var selectClause = BuildSelectClause(includeProfile, includeOrderCount: true);
            var joinClause = BuildJoinClause(includeOrders, includeCart: includeCart);

            var sql = $@"
                {selectClause}
                FROM Users u
                {joinClause}
                WHERE u.Id = @Id";

            return await connection.QuerySingleOrDefaultAsync<User>(sql, new { Id = id });
        }

        // Busca usuário por email (método essencial para login e validações)
        public async Task<User?> GetByEmailAsync(
            string email, // Email do usuário
            bool includeProfile = false, // Incluir dados do perfil
            bool includeOrderCount = false, // Incluir contagem de pedidos
            CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);

            var selectClause = BuildSelectClause(includeProfile, includeOrderCount);

            var sql = $@"
                {selectClause}
                FROM Users u
                WHERE u.Email = @Email";

            return await connection.QuerySingleOrDefaultAsync<User>(sql, new { Email = email });
        }

        // Busca usuário por CPF (para validações de duplicidade)
        public async Task<User?> GetByCpfAsync(
            string cpf, // CPF do usuário
            bool includeProfile = false, // Incluir dados do perfil
            CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);

            var selectClause = BuildSelectClause(includeProfile);

            var sql = $@"
                {selectClause}
                FROM Users u
                WHERE u.Cpf = @Cpf";

            return await connection.QuerySingleOrDefaultAsync<User>(sql, new { Cpf = cpf });
        }

        // Busca usuários por lista de IDs (útil para relatórios e operações em lote)
        public async Task<IEnumerable<User>> GetByIdsAsync(
            IEnumerable<Guid> ids, // Lista de IDs de usuários
            bool includeProfile = false, // Incluir dados do perfil
            CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);

            var selectClause = BuildSelectClause(includeProfile);

            var sql = $@"
                {selectClause}
                FROM Users u
                WHERE u.Id IN @Ids";

            return await connection.QueryAsync<User>(sql, new { Ids = ids });
        }

        // Métodos auxiliares para construção dinâmica de SQL
        private string BuildWhereClause(string? searchTerm, bool? isActive, DateTime? createdAfter, DateTime? createdBefore)
        {
            var conditions = new List<string> { "1=1" }; // Base condition

            if (!string.IsNullOrWhiteSpace(searchTerm))
                conditions.Add("(u.FirstName LIKE @SearchTerm OR u.LastName LIKE @SearchTerm OR u.Email LIKE @SearchTerm OR u.Cpf LIKE @SearchTerm)");

            if (isActive.HasValue)
                conditions.Add("u.IsActive = @IsActive");

            if (createdAfter.HasValue)
                conditions.Add("u.CreatedAt >= @CreatedAfter");

            if (createdBefore.HasValue)
                conditions.Add("u.CreatedAt <= @CreatedBefore");

            return $"WHERE {string.Join(" AND ", conditions)}";
        }

        private string BuildSelectClause(bool includeProfile = false, bool includeOrderCount = false, bool includeLastLogin = false)
        {
            var columns = new List<string> { "u.*" }; // Todas as colunas de Users

            if (includeProfile)
            {
                columns.Add("u.Phone");
                columns.Add("u.DateOfBirth");
                columns.Add("u.Gender");
            }

            if (includeOrderCount)
                columns.Add("(SELECT COUNT(*) FROM Orders WHERE UserId = u.Id) as OrderCount");

            if (includeLastLogin)
                columns.Add("u.LastLoginAt");

            return $"SELECT {string.Join(", ", columns)}";
        }

        private string BuildJoinClause(bool includeOrderHistory = false, bool includeCart = false)
        {
            var joins = new List<string>();

            if (includeOrderHistory)
                joins.Add("LEFT JOIN Orders o ON u.Id = o.UserId");

            if (includeCart)
                joins.Add("LEFT JOIN Carts c ON u.Id = c.UserId");

            return string.Join("\n                ", joins);
        }

        private object BuildParameters(int page, int pageSize, string? searchTerm, bool? isActive, DateTime? createdAfter, DateTime? createdBefore)
        {
            var parameters = new DynamicParameters();

            parameters.Add("Offset", (page - 1) * pageSize);
            parameters.Add("PageSize", pageSize);

            if (!string.IsNullOrWhiteSpace(searchTerm))
                parameters.Add("SearchTerm", $"%{searchTerm}%");

            if (isActive.HasValue)
                parameters.Add("IsActive", isActive.Value);

            if (createdAfter.HasValue)
                parameters.Add("CreatedAfter", createdAfter.Value);

            if (createdBefore.HasValue)
                parameters.Add("CreatedBefore", createdBefore.Value);

            return parameters;
        }

        // Métodos CRUD básicos para Commands
        public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) // Busca simples por ID
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT * FROM Users WHERE Id = @Id";
            return await connection.QuerySingleOrDefaultAsync<User>(sql, new { Id = id });
        }

        public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default) // Adiciona novo usuário
        {
            using var connection = new SqlConnection(_connectionString);

            var sql = @"
                INSERT INTO Users (Id, FirstName, LastName, Email, Cpf, Phone, DateOfBirth, Gender, PasswordHash, IsActive, CreatedAt, UpdatedAt)
                VALUES (@Id, @FirstName, @LastName, @Email, @Cpf, @Phone, @DateOfBirth, @Gender, @PasswordHash, @IsActive, @CreatedAt, @UpdatedAt)";

            await connection.ExecuteAsync(sql, user);
            return user;
        }

        public async Task UpdateAsync(User user, CancellationToken cancellationToken = default) // Atualiza usuário existente
        {
            using var connection = new SqlConnection(_connectionString);

            var sql = @"
                UPDATE Users 
                SET FirstName = @FirstName, LastName = @LastName, Email = @Email, Cpf = @Cpf,
                    Phone = @Phone, DateOfBirth = @DateOfBirth, Gender = @Gender, 
                    PasswordHash = @PasswordHash, IsActive = @IsActive, UpdatedAt = @UpdatedAt
                WHERE Id = @Id";

            await connection.ExecuteAsync(sql, user);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) // Remove usuário por ID (soft delete)
        {
            using var connection = new SqlConnection(_connectionString);
            // Soft delete - marca como inativo ao invés de deletar
            var sql = "UPDATE Users SET IsActive = 0, UpdatedAt = @UpdatedAt WHERE Id = @Id";
            await connection.ExecuteAsync(sql, new { Id = id, UpdatedAt = DateTime.UtcNow });
        }

        // Métodos utilitários e validações
        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) // Verifica se usuário existe
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT COUNT(1) FROM Users WHERE Id = @Id";
            var count = await connection.QuerySingleAsync<int>(sql, new { Id = id });
            return count > 0;
        }

        public async Task<bool> ExistsByEmailAsync(string email, Guid? excludeId = null, CancellationToken cancellationToken = default) // Verifica email único
        {
            using var connection = new SqlConnection(_connectionString);

            var parameters = new DynamicParameters();
            parameters.Add("Email", email);

            var sql = "SELECT COUNT(1) FROM Users WHERE Email = @Email";

            if (excludeId.HasValue)
            {
                sql += " AND Id != @ExcludeId";
                parameters.Add("ExcludeId", excludeId.Value);
            }

            var count = await connection.QuerySingleAsync<int>(sql, parameters);
            return count > 0;
        }

        public async Task<bool> ExistsByCpfAsync(string cpf, Guid? excludeId = null, CancellationToken cancellationToken = default) // Verifica CPF único
        {
            using var connection = new SqlConnection(_connectionString);

            var parameters = new DynamicParameters();
            parameters.Add("Cpf", cpf);

            var sql = "SELECT COUNT(1) FROM Users WHERE Cpf = @Cpf";

            if (excludeId.HasValue)
            {
                sql += " AND Id != @ExcludeId";
                parameters.Add("ExcludeId", excludeId.Value);
            }

            var count = await connection.QuerySingleAsync<int>(sql, parameters);
            return count > 0;
        }

        public async Task<int> GetOrderCountAsync(Guid userId, CancellationToken cancellationToken = default) // Conta pedidos do usuário
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT COUNT(*) FROM Orders WHERE UserId = @UserId";
            return await connection.QuerySingleAsync<int>(sql, new { UserId = userId });
        }

        public async Task<DateTime?> GetLastLoginAsync(Guid userId, CancellationToken cancellationToken = default) // Obtém último login
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT LastLoginAt FROM Users WHERE Id = @UserId";
            return await connection.QuerySingleOrDefaultAsync<DateTime?>(sql, new { UserId = userId });
        }

        public async Task UpdateLastLoginAsync(Guid userId, DateTime loginTime, CancellationToken cancellationToken = default) // Atualiza último login
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "UPDATE Users SET LastLoginAt = @LoginTime, UpdatedAt = @UpdatedAt WHERE Id = @UserId";
            await connection.ExecuteAsync(sql, new { UserId = userId, LoginTime = loginTime, UpdatedAt = DateTime.UtcNow });
        }

        // Métodos de segurança e auditoria
        public async Task<bool> IsAccountLockedAsync(Guid userId, CancellationToken cancellationToken = default) // Verifica se conta está bloqueada
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT CASE WHEN AccountLockedUntil > @Now THEN 1 ELSE 0 END FROM Users WHERE Id = @UserId";
            var isLocked = await connection.QuerySingleOrDefaultAsync<bool>(sql, new { UserId = userId, Now = DateTime.UtcNow });
            return isLocked;
        }

        public async Task LockAccountAsync(Guid userId, DateTime? lockUntil = null, CancellationToken cancellationToken = default) // Bloqueia conta
        {
            using var connection = new SqlConnection(_connectionString);
            var lockTime = lockUntil ?? DateTime.UtcNow.AddHours(24); // 24 horas por padrão
            var sql = "UPDATE Users SET AccountLockedUntil = @LockUntil, UpdatedAt = @UpdatedAt WHERE Id = @UserId";
            await connection.ExecuteAsync(sql, new { UserId = userId, LockUntil = lockTime, UpdatedAt = DateTime.UtcNow });
        }

        public async Task UnlockAccountAsync(Guid userId, CancellationToken cancellationToken = default) // Desbloqueia conta
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "UPDATE Users SET AccountLockedUntil = NULL, FailedLoginAttempts = 0, UpdatedAt = @UpdatedAt WHERE Id = @UserId";
            await connection.ExecuteAsync(sql, new { UserId = userId, UpdatedAt = DateTime.UtcNow });
        }

        public async Task<int> GetFailedLoginAttemptsAsync(Guid userId, CancellationToken cancellationToken = default) // Conta tentativas de login falhadas
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT FailedLoginAttempts FROM Users WHERE Id = @UserId";
            return await connection.QuerySingleOrDefaultAsync<int>(sql, new { UserId = userId });
        }

        public async Task IncrementFailedLoginAttemptsAsync(Guid userId, CancellationToken cancellationToken = default) // Incrementa tentativas falhadas
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "UPDATE Users SET FailedLoginAttempts = FailedLoginAttempts + 1, UpdatedAt = @UpdatedAt WHERE Id = @UserId";
            await connection.ExecuteAsync(sql, new { UserId = userId, UpdatedAt = DateTime.UtcNow });
        }

        public async Task ResetFailedLoginAttemptsAsync(Guid userId, CancellationToken cancellationToken = default) // Reseta contador de tentativas
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "UPDATE Users SET FailedLoginAttempts = 0, UpdatedAt = @UpdatedAt WHERE Id = @UserId";
            await connection.ExecuteAsync(sql, new { UserId = userId, UpdatedAt = DateTime.UtcNow });
        }

        public void SetTransaction(SqlTransaction? transaction)
        {
            _transaction = transaction;
        }
    }
}