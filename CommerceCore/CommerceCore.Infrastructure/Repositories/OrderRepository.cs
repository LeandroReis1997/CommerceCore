using CommerceCore.Application.Interfaces.Repositories;
using CommerceCore.Domain.Entities;
using CommerceCore.Domain.Enums;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace CommerceCore.Infrastructure.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly string _connectionString; // String de conexão com SQL Server
        private readonly SqlConnection? _connection; // Conexão compartilhada (para UnitOfWork)
        private SqlTransaction? _transaction; // Transação compartilhada (para UnitOfWork)

        // Construtor para uso independente (cria nova conexão a cada operação)
        public OrderRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string não encontrada");
            _connection = null;
            _transaction = null;
        }

        // Construtor para uso com UnitOfWork (usa conexão e transação compartilhadas)
        public OrderRepository(string connectionString, SqlConnection connection, SqlTransaction? transaction = null)
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
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();

                return await operation(connection);
            }
            finally
            {
                if (shouldCloseConnection && connection.State == ConnectionState.Open)
                {
                    connection.Close();
                    connection.Dispose();
                }
            }
        }

        // Busca pedidos paginados com filtros e includes opcionais (para admin)
        public async Task<(IEnumerable<Order> Orders, int TotalCount)> GetPagedAsync(
            int page, // Número da página
            int pageSize, // Itens por página
            Guid? userId = null, // Filtro por usuário específico
            OrderStatus? status = null, // Filtro por status do pedido
            DateTime? createdAfter = null, // Pedidos criados após esta data
            DateTime? createdBefore = null, // Pedidos criados antes desta data
            decimal? minTotal = null, // Valor mínimo do pedido
            decimal? maxTotal = null, // Valor máximo do pedido
            string? searchTerm = null, // Busca por número do pedido, email do cliente
            bool includeItems = false, // Incluir itens do pedido
            bool includeItemDetails = false, // Incluir detalhes completos dos itens/produtos
            bool includeUser = false, // Incluir dados do usuário
            bool includeUserProfile = false, // Incluir perfil completo do usuário
            bool includePaymentInfo = false, // Incluir informações de pagamento
            bool includeShippingInfo = false, // Incluir informações de entrega
            CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                // Constrói queries dinâmicas baseadas nos filtros e includes
                var whereClause = BuildWhereClause(userId, status, createdAfter, createdBefore, minTotal, maxTotal, searchTerm);
                var selectClause = BuildSelectClause(includeUser, includeUserProfile, includePaymentInfo, includeShippingInfo);
                var joinClause = BuildJoinClause(includeUser, includeUserProfile, includeShippingInfo);

                var sql = $@"
                    {selectClause}
                    FROM Orders o
                    {joinClause}
                    {whereClause}
                    ORDER BY o.PlacedAt DESC
                    OFFSET @Offset ROWS
                    FETCH NEXT @PageSize ROWS ONLY;

                    -- Query para contar total de registros
                    SELECT COUNT(1)
                    FROM Orders o
                    {BuildJoinClause(includeUser, false, false)} -- JOINs básicos para contagem
                    {whereClause}";

                // Monta parâmetros dinamicamente baseados nos filtros
                var parameters = BuildParameters(page, pageSize, userId, status, createdAfter, createdBefore, minTotal, maxTotal, searchTerm);

                using var multi = await connection.QueryMultipleAsync(sql, parameters, _transaction);
                var orders = await multi.ReadAsync<Order>();
                var totalCount = await multi.ReadSingleAsync<int>();

                // Se deve incluir itens, carrega para todos os pedidos
                if (includeItems && orders.Any())
                {
                    await LoadOrderItemsForMultipleOrdersAsync(orders.ToList(), includeItemDetails, connection);
                }

                return (orders, totalCount);
            });
        }

        // Busca pedido por ID com includes condicionais
        public async Task<Order?> GetByIdAsync(
            Guid id, // ID do pedido
            bool includeItems = true, // Incluir itens do pedido (geralmente sempre necessário)
            bool includeItemDetails = false, // Incluir detalhes completos dos produtos dos itens
            bool includeUser = false, // Incluir dados do usuário
            bool includeUserProfile = false, // Incluir perfil completo do usuário
            bool includePaymentInfo = false, // Incluir informações de pagamento
            bool includeShippingInfo = false, // Incluir informações de entrega
            CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                // Constrói query baseada nas flags de inclusão
                var selectClause = BuildSelectClause(includeUser, includeUserProfile, includePaymentInfo, includeShippingInfo);
                var joinClause = BuildJoinClause(includeUser, includeUserProfile, includeShippingInfo);

                var sql = $@"
                    {selectClause}
                    FROM Orders o
                    {joinClause}
                    WHERE o.Id = @Id";

                var order = await connection.QuerySingleOrDefaultAsync<Order>(sql, new { Id = id }, _transaction);

                if (order != null && includeItems)
                {
                    // Carrega itens do pedido se solicitado
                    await LoadOrderItemsAsync(order, includeItemDetails, connection);
                }

                return order;
            });
        }

        // Busca pedidos de um usuário específico com paginação
        public async Task<(IEnumerable<Order> Orders, int TotalCount)> GetByUserIdAsync(
            Guid userId, // ID do usuário
            int page, // Número da página
            int pageSize, // Itens por página
            OrderStatus? status = null, // Filtro por status do pedido
            DateTime? createdAfter = null, // Pedidos criados após esta data
            DateTime? createdBefore = null, // Pedidos criados antes desta data
            bool includeItems = true, // Incluir itens do pedido
            bool includeItemDetails = false, // Incluir detalhes dos produtos dos itens
            bool includePaymentInfo = false, // Incluir informações de pagamento
            bool includeShippingInfo = false, // Incluir informações de entrega
            CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                // Constrói WHERE clause específica para pedidos do usuário
                var whereClause = BuildUserWhereClause(userId, status, createdAfter, createdBefore);
                var selectClause = "SELECT o.*"; // Não inclui usuário pois já sabemos qual é
                var joinClause = BuildJoinClause(false, false, includeShippingInfo);

                var sql = $@"
                    {selectClause}
                    FROM Orders o
                    {joinClause}
                    {whereClause}
                    ORDER BY o.PlacedAt DESC
                    OFFSET @Offset ROWS
                    FETCH NEXT @PageSize ROWS ONLY;

                    -- Query para contar total
                    SELECT COUNT(1)
                    FROM Orders o
                    {whereClause}";

                var parameters = BuildUserParameters(page, pageSize, userId, status, createdAfter, createdBefore);

                using var multi = await connection.QueryMultipleAsync(sql, parameters, _transaction);
                var orders = await multi.ReadAsync<Order>();
                var totalCount = await multi.ReadSingleAsync<int>();

                // Se deve incluir itens, carrega para todos os pedidos
                if (includeItems && orders.Any())
                {
                    await LoadOrderItemsForMultipleOrdersAsync(orders.ToList(), includeItemDetails, connection);
                }

                return (orders, totalCount);
            });
        }

        // Busca pedidos por número do pedido (código público)
        public async Task<Order?> GetByOrderNumberAsync(
            string orderNumber, // Número público do pedido
            bool includeItems = true, // Incluir itens do pedido
            bool includeUser = false, // Incluir dados do usuário
            CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var selectClause = BuildSelectClause(includeUser, false, false, false);
                var joinClause = BuildJoinClause(includeUser, false, false);

                var sql = $@"
                    {selectClause}
                    FROM Orders o
                    {joinClause}
                    WHERE o.OrderNumber = @OrderNumber";

                var order = await connection.QuerySingleOrDefaultAsync<Order>(sql, new { OrderNumber = orderNumber }, _transaction);

                if (order != null && includeItems)
                {
                    await LoadOrderItemsAsync(order, false, connection);
                }

                return order;
            });
        }

        // MÉTODOS BÁSICOS DE CRUD PARA COMMANDS

        // Busca simples por ID (sem includes) - usado pelos Commands
        public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var sql = "SELECT * FROM Orders WHERE Id = @Id";
                return await connection.QuerySingleOrDefaultAsync<Order>(sql, new { Id = id }, _transaction);
            });
        }

        // Adiciona novo pedido no banco de dados
        public async Task<Order> AddAsync(Order order, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var sql = @"
                    INSERT INTO Orders (Id, OrderNumber, UserId, Status, TotalAmount, ShippingAddressId, PlacedAt, CreatedAt, UpdatedAt)
                    VALUES (@Id, @OrderNumber, @UserId, @Status, @TotalAmount, @ShippingAddressId, @PlacedAt, @CreatedAt, @UpdatedAt)";

                await connection.ExecuteAsync(sql, new
                {
                    Id = order.Id,
                    OrderNumber = order.OrderNumber,
                    UserId = order.UserId,
                    Status = order.Status.ToString(), // Converte enum para string
                    TotalAmount = order.TotalAmount,
                    ShippingAddressId = order.ShippingAddressId,
                    PlacedAt = order.PlacedAt,
                    CreatedAt = order.CreatedAt,
                    UpdatedAt = order.UpdatedAt
                }, _transaction);

                return order;
            });
        }

        // Atualiza pedido existente no banco de dados
        public async Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
        {
            await ExecuteWithConnectionAsync(async connection =>
            {
                var sql = @"
                    UPDATE Orders 
                    SET OrderNumber = @OrderNumber, UserId = @UserId, Status = @Status, TotalAmount = @TotalAmount,
                        ShippingAddressId = @ShippingAddressId, PlacedAt = @PlacedAt, UpdatedAt = @UpdatedAt
                    WHERE Id = @Id";

                await connection.ExecuteAsync(sql, new
                {
                    Id = order.Id,
                    OrderNumber = order.OrderNumber,
                    UserId = order.UserId,
                    Status = order.Status.ToString(), // Converte enum para string
                    TotalAmount = order.TotalAmount,
                    ShippingAddressId = order.ShippingAddressId,
                    PlacedAt = order.PlacedAt,
                    UpdatedAt = order.UpdatedAt
                }, _transaction);

                return true;
            });
        }

        // Remove pedido do banco de dados por ID
        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await ExecuteWithConnectionAsync(async connection =>
            {
                // Remove primeiro os itens do pedido (foreign key constraint)
                await connection.ExecuteAsync("DELETE FROM OrderItems WHERE OrderId = @Id", new { Id = id }, _transaction);

                // Depois remove o pedido
                await connection.ExecuteAsync("DELETE FROM Orders WHERE Id = @Id", new { Id = id }, _transaction);
                return true;
            });
        }

        // MÉTODOS UTILITÁRIOS

        // Verifica se pedido existe no banco de dados
        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var sql = "SELECT COUNT(1) FROM Orders WHERE Id = @Id";
                var count = await connection.QuerySingleAsync<int>(sql, new { Id = id }, _transaction);
                return count > 0;
            });
        }

        // Verifica se já existe pedido com o mesmo número
        public async Task<bool> ExistsByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var sql = "SELECT COUNT(1) FROM Orders WHERE OrderNumber = @OrderNumber";
                var count = await connection.QuerySingleAsync<int>(sql, new { OrderNumber = orderNumber }, _transaction);
                return count > 0;
            });
        }

        // Gera número único para novo pedido
        public async Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                string orderNumber;
                bool exists;

                do
                {
                    // Formato: ORD-YYYYMMDD-XXXX (onde XXXX é sequencial)
                    var today = DateTime.Now.ToString("yyyyMMdd");
                    var sequence = await GetNextSequenceForDateAsync(today, connection);
                    orderNumber = $"ORD-{today}-{sequence:D4}";

                    exists = await ExistsByOrderNumberAsync(orderNumber, cancellationToken);
                }
                while (exists);

                return orderNumber;
            });
        }

        // Verifica se pedido pertence ao usuário (para autorização)
        public async Task<bool> BelongsToUserAsync(Guid orderId, Guid userId, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var sql = "SELECT COUNT(1) FROM Orders WHERE Id = @OrderId AND UserId = @UserId";
                var count = await connection.QuerySingleAsync<int>(sql, new { OrderId = orderId, UserId = userId }, _transaction);
                return count > 0;
            });
        }

        // MÉTODOS DE ESTATÍSTICAS E RELATÓRIOS

        // Conta pedidos de um usuário específico
        public async Task<int> GetOrderCountByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var sql = "SELECT COUNT(*) FROM Orders WHERE UserId = @UserId";
                return await connection.QuerySingleAsync<int>(sql, new { UserId = userId }, _transaction);
            });
        }

        // Calcula total gasto por um usuário
        public async Task<decimal> GetTotalSpentByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var sql = @"
                    SELECT ISNULL(SUM(TotalAmount), 0) 
                    FROM Orders 
                    WHERE UserId = @UserId 
                      AND Status NOT IN ('Cancelled', 'Refunded')";

                return await connection.QuerySingleAsync<decimal>(sql, new { UserId = userId }, _transaction);
            });
        }

        // Conta pedidos por status em um período
        public async Task<int> GetOrderCountByStatusAsync(OrderStatus status, DateTime? fromDate = null, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var sql = "SELECT COUNT(*) FROM Orders WHERE Status = @Status";
                var parameters = new DynamicParameters();
                parameters.Add("Status", status.ToString());

                if (fromDate.HasValue)
                {
                    sql += " AND PlacedAt >= @FromDate";
                    parameters.Add("FromDate", fromDate.Value);
                }

                return await connection.QuerySingleAsync<int>(sql, parameters, _transaction);
            });
        }

        // Calcula receita total em um período específico
        public async Task<decimal> GetRevenueByPeriodAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var sql = @"
                    SELECT ISNULL(SUM(TotalAmount), 0) 
                    FROM Orders 
                    WHERE PlacedAt >= @StartDate 
                      AND PlacedAt <= @EndDate 
                      AND Status IN ('Confirmed', 'Processing', 'Shipped', 'Delivered')";

                return await connection.QuerySingleAsync<decimal>(sql, new { StartDate = startDate, EndDate = endDate }, _transaction);
            });
        }

        // MÉTODOS DE VALIDAÇÃO E AUTORIZAÇÃO

        // Verifica se usuário pode acessar o pedido (mesmo que BelongsToUserAsync)
        public async Task<bool> CanUserAccessOrderAsync(Guid orderId, Guid userId, CancellationToken cancellationToken = default)
        {
            return await BelongsToUserAsync(orderId, userId, cancellationToken);
        }

        // Verifica se pedido pode ser cancelado baseado no status atual
        public async Task<bool> CanCancelOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var sql = "SELECT Status FROM Orders WHERE Id = @OrderId";
                var statusString = await connection.QuerySingleOrDefaultAsync<string>(sql, new { OrderId = orderId }, _transaction);

                if (string.IsNullOrEmpty(statusString) || !Enum.TryParse<OrderStatus>(statusString, out var status))
                    return false;

                // Só pode cancelar se estiver em status que permite cancelamento
                return status == OrderStatus.Pending || status == OrderStatus.Confirmed;
            });
        }

        // Verifica se pedido pode ser editado baseado no status atual
        public async Task<bool> IsOrderEditableAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var sql = "SELECT Status FROM Orders WHERE Id = @OrderId";
                var statusString = await connection.QuerySingleOrDefaultAsync<string>(sql, new { OrderId = orderId }, _transaction);

                if (string.IsNullOrEmpty(statusString) || !Enum.TryParse<OrderStatus>(statusString, out var status))
                    return false;

                // Só pode editar se ainda não foi processado
                return status == OrderStatus.Pending;
            });
        }

        // MÉTODOS AUXILIARES PRIVADOS PARA CONSTRUÇÃO DE QUERIES

        // Constrói cláusula WHERE para busca geral de pedidos
        private string BuildWhereClause(Guid? userId, OrderStatus? status, DateTime? createdAfter, DateTime? createdBefore, decimal? minTotal, decimal? maxTotal, string? searchTerm)
        {
            var conditions = new List<string> { "1=1" };

            if (userId.HasValue)
                conditions.Add("o.UserId = @UserId");

            if (status.HasValue)
                conditions.Add("o.Status = @Status");

            if (createdAfter.HasValue)
                conditions.Add("o.PlacedAt >= @CreatedAfter");

            if (createdBefore.HasValue)
                conditions.Add("o.PlacedAt <= @CreatedBefore");

            if (minTotal.HasValue)
                conditions.Add("o.TotalAmount >= @MinTotal");

            if (maxTotal.HasValue)
                conditions.Add("o.TotalAmount <= @MaxTotal");

            if (!string.IsNullOrWhiteSpace(searchTerm))
                conditions.Add("(o.OrderNumber LIKE @SearchTerm OR u.Email LIKE @SearchTerm)");

            return $"WHERE {string.Join(" AND ", conditions)}";
        }

        // Constrói cláusula WHERE específica para pedidos de um usuário
        private string BuildUserWhereClause(Guid userId, OrderStatus? status, DateTime? createdAfter, DateTime? createdBefore)
        {
            var conditions = new List<string> { "o.UserId = @UserId" };

            if (status.HasValue)
                conditions.Add("o.Status = @Status");

            if (createdAfter.HasValue)
                conditions.Add("o.PlacedAt >= @CreatedAfter");

            if (createdBefore.HasValue)
                conditions.Add("o.PlacedAt <= @CreatedBefore");

            return $"WHERE {string.Join(" AND ", conditions)}";
        }

        // Constrói cláusula SELECT com campos condicionais baseados nas flags
        private string BuildSelectClause(bool includeUser = false, bool includeUserProfile = false, bool includePaymentInfo = false, bool includeShippingInfo = false)
        {
            var columns = new List<string> { "o.*" };

            if (includeUser)
            {
                columns.Add("u.FirstName as UserFirstName");
                columns.Add("u.LastName as UserLastName");
                columns.Add("u.Email as UserEmail");
            }

            if (includeUserProfile)
            {
                columns.Add("u.PhoneNumber as UserPhoneNumber");
                columns.Add("u.DateOfBirth as UserDateOfBirth");
            }

            if (includeShippingInfo)
            {
                columns.Add("a.Street as ShippingStreet");
                columns.Add("a.City as ShippingCity");
                columns.Add("a.State as ShippingState");
                columns.Add("a.ZipCode as ShippingZipCode");
            }

            return $"SELECT {string.Join(", ", columns)}";
        }

        // Constrói JOINs opcionais baseados nas flags de inclusão
        private string BuildJoinClause(bool includeUser = false, bool includeUserProfile = false, bool includeShippingInfo = false)
        {
            var joins = new List<string>();

            if (includeUser || includeUserProfile)
                joins.Add("LEFT JOIN Users u ON o.UserId = u.Id");

            if (includeShippingInfo)
                joins.Add("LEFT JOIN Addresses a ON o.ShippingAddressId = a.Id");

            return string.Join("\n                ", joins);
        }

        // Constrói parâmetros dinâmicos para busca geral
        private object BuildParameters(int page, int pageSize, Guid? userId, OrderStatus? status, DateTime? createdAfter, DateTime? createdBefore, decimal? minTotal, decimal? maxTotal, string? searchTerm)
        {
            var parameters = new DynamicParameters();

            parameters.Add("Offset", (page - 1) * pageSize);
            parameters.Add("PageSize", pageSize);

            if (userId.HasValue)
                parameters.Add("UserId", userId.Value);

            if (status.HasValue)
                parameters.Add("Status", status.Value.ToString());

            if (createdAfter.HasValue)
                parameters.Add("CreatedAfter", createdAfter.Value);

            if (createdBefore.HasValue)
                parameters.Add("CreatedBefore", createdBefore.Value);

            if (minTotal.HasValue)
                parameters.Add("MinTotal", minTotal.Value);

            if (maxTotal.HasValue)
                parameters.Add("MaxTotal", maxTotal.Value);

            if (!string.IsNullOrWhiteSpace(searchTerm))
                parameters.Add("SearchTerm", $"%{searchTerm}%");

            return parameters;
        }

        // Constrói parâmetros dinâmicos para busca por usuário
        private object BuildUserParameters(int page, int pageSize, Guid userId, OrderStatus? status, DateTime? createdAfter, DateTime? createdBefore)
        {
            var parameters = new DynamicParameters();

            parameters.Add("Offset", (page - 1) * pageSize);
            parameters.Add("PageSize", pageSize);
            parameters.Add("UserId", userId);

            if (status.HasValue)
                parameters.Add("Status", status.Value.ToString());

            if (createdAfter.HasValue)
                parameters.Add("CreatedAfter", createdAfter.Value);

            if (createdBefore.HasValue)
                parameters.Add("CreatedBefore", createdBefore.Value);

            return parameters;
        }

        // Obtém próximo número sequencial para uma data específica
        private async Task<int> GetNextSequenceForDateAsync(string date, SqlConnection connection)
        {
            var sql = @"
                SELECT ISNULL(MAX(CAST(RIGHT(OrderNumber, 4) AS INT)), 0) + 1
                FROM Orders 
                WHERE OrderNumber LIKE @Pattern";

            var pattern = $"ORD-{date}-%";
            return await connection.QuerySingleAsync<int>(sql, new { Pattern = pattern }, _transaction);
        }

        // Carrega itens para múltiplos pedidos de forma eficiente
        private async Task LoadOrderItemsForMultipleOrdersAsync(List<Order> orders, bool includeItemDetails, SqlConnection connection)
        {
            var orderIds = orders.Select(o => o.Id).ToList();

            var itemsSql = includeItemDetails ?
                @"SELECT oi.*, p.Name as ProductName, p.ImageUrl as ProductImageUrl
                  FROM OrderItems oi
                  INNER JOIN Products p ON oi.ProductId = p.Id
                  WHERE oi.OrderId IN @OrderIds
                  ORDER BY oi.OrderId" :
                @"SELECT * FROM OrderItems WHERE OrderId IN @OrderIds ORDER BY OrderId";

            var allItems = await connection.QueryAsync<OrderItem>(itemsSql, new { OrderIds = orderIds }, _transaction);

            // Agrupa itens por pedido
            var itemsByOrder = allItems.GroupBy(i => i.OrderId).ToDictionary(g => g.Key, g => g.ToList());

            // Atribui itens aos pedidos correspondentes
            foreach (var order in orders)
            {
                order.Items = itemsByOrder.TryGetValue(order.Id, out var items) ? items : new List<OrderItem>();
            }
        }

        // Carrega itens de um pedido específico
        private async Task LoadOrderItemsAsync(Order order, bool includeItemDetails, SqlConnection connection)
        {
            var itemsSql = includeItemDetails ?
                @"SELECT oi.*, p.Name as ProductName, p.ImageUrl as ProductImageUrl
                  FROM OrderItems oi
                  INNER JOIN Products p ON oi.ProductId = p.Id
                  WHERE oi.OrderId = @OrderId
                  ORDER BY oi.CreatedAt" :
                @"SELECT * FROM OrderItems WHERE OrderId = @OrderId ORDER BY CreatedAt";

            var items = await connection.QueryAsync<OrderItem>(itemsSql, new { OrderId = order.Id }, _transaction);
            order.Items = items.ToList();
        }

        // MÉTODOS AVANÇADOS DE CONSULTA E RELATÓRIOS

        // Obtém pedidos mais recentes de um usuário
        public async Task<IEnumerable<Order>> GetRecentOrdersByUserAsync(
            Guid userId, // ID do usuário
            int limit = 5, // Limite de pedidos a retornar
            CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var sql = $@"
                    SELECT TOP {limit} *
                    FROM Orders 
                    WHERE UserId = @UserId
                    ORDER BY PlacedAt DESC";

                return await connection.QueryAsync<Order>(sql, new { UserId = userId }, _transaction);
            });
        }

        // Obtém estatísticas detalhadas de vendas por período
        public async Task<OrderStatistics> GetOrderStatisticsByPeriodAsync(
            DateTime startDate, // Data inicial do período
            DateTime endDate, // Data final do período
            CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var sql = @"
                    SELECT 
                        COUNT(*) as TotalOrders,
                        COUNT(CASE WHEN Status = 'Pending' THEN 1 END) as PendingOrders,
                        COUNT(CASE WHEN Status = 'Confirmed' THEN 1 END) as ConfirmedOrders,
                        COUNT(CASE WHEN Status = 'Processing' THEN 1 END) as ProcessingOrders,
                        COUNT(CASE WHEN Status = 'Shipped' THEN 1 END) as ShippedOrders,
                        COUNT(CASE WHEN Status = 'Delivered' THEN 1 END) as DeliveredOrders,
                        COUNT(CASE WHEN Status = 'Cancelled' THEN 1 END) as CancelledOrders,
                        COUNT(CASE WHEN Status = 'Refunded' THEN 1 END) as RefundedOrders,
                        ISNULL(SUM(TotalAmount), 0) as TotalRevenue,
                        ISNULL(AVG(TotalAmount), 0) as AverageOrderValue,
                        ISNULL(MIN(TotalAmount), 0) as MinOrderValue,
                        ISNULL(MAX(TotalAmount), 0) as MaxOrderValue
                    FROM Orders
                    WHERE PlacedAt >= @StartDate AND PlacedAt <= @EndDate";

                return await connection.QuerySingleAsync<OrderStatistics>(sql, new { StartDate = startDate, EndDate = endDate }, _transaction);
            });
        }

        // Atualiza apenas o status de um pedido
        public async Task UpdateOrderStatusAsync(
            Guid orderId, // ID do pedido
            OrderStatus newStatus, // Novo status
            CancellationToken cancellationToken = default)
        {
            await ExecuteWithConnectionAsync(async connection =>
            {
                var sql = @"
                    UPDATE Orders 
                    SET Status = @Status, UpdatedAt = @UpdatedAt 
                    WHERE Id = @OrderId";

                await connection.ExecuteAsync(sql, new
                {
                    OrderId = orderId,
                    Status = newStatus.ToString(),
                    UpdatedAt = DateTime.UtcNow
                }, _transaction);

                return true;
            });
        }
        public void SetTransaction(SqlTransaction? transaction)
        {
            _transaction = transaction;
        }
    }

    // Classe auxiliar para estatísticas de pedidos
    public class OrderStatistics
    {
        public int TotalOrders { get; set; } // Total de pedidos no período
        public int PendingOrders { get; set; } // Pedidos pendentes
        public int ConfirmedOrders { get; set; } // Pedidos confirmados
        public int ProcessingOrders { get; set; } // Pedidos sendo processados
        public int ShippedOrders { get; set; } // Pedidos enviados
        public int DeliveredOrders { get; set; } // Pedidos entregues
        public int CancelledOrders { get; set; } // Pedidos cancelados
        public int RefundedOrders { get; set; } // Pedidos estornados
        public decimal TotalRevenue { get; set; } // Receita total do período
        public decimal AverageOrderValue { get; set; } // Valor médio dos pedidos
        public decimal MinOrderValue { get; set; } // Menor valor de pedido
        public decimal MaxOrderValue { get; set; } // Maior valor de pedido
    }
}