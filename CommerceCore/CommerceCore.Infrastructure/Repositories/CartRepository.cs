using CommerceCore.Application.Interfaces.Repositories;
using CommerceCore.Domain.Entities;
using CommerceCore.Domain.Enums;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace CommerceCore.Infrastructure.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly string _connectionString; // String de conexão com SQL Server
        private readonly SqlConnection? _connection; // Conexão compartilhada (para UnitOfWork)
        private SqlTransaction? _transaction; // Transação compartilhada (para UnitOfWork)

        // Construtor para uso independente (cria nova conexão a cada operação)
        public CartRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string não encontrada");
            _connection = null;
            _transaction = null;
        }

        // Construtor para uso com UnitOfWork (usa conexão e transação compartilhadas)
        public CartRepository(string connectionString, SqlConnection connection, SqlTransaction? transaction = null)
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

        // Busca carrinho ativo de um usuário específico
        public async Task<Cart?> GetByUserIdAsync(
            Guid userId, // ID do usuário
            bool includeItems = true, // Incluir itens do carrinho
            bool includeItemDetails = false, // Incluir detalhes completos dos produtos dos itens
            bool includeProductImages = false, // Incluir imagens dos produtos
            bool includeBrandInfo = false, // Incluir informações das marcas dos produtos
            bool includeAvailability = false, // Verificar disponibilidade/estoque dos produtos
            bool calculateTotals = true, // Calcular subtotal, desconto e total do carrinho
            CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                // Query para buscar o carrinho mais recente do usuário (sem filtro de status)
                var cartSql = @"
                    SELECT c.*, 
                           u.FirstName as UserFirstName, 
                           u.LastName as UserLastName, 
                           u.Email as UserEmail
                    FROM Carts c
                    LEFT JOIN Users u ON c.UserId = u.Id
                    WHERE c.UserId = @UserId
                    ORDER BY c.UpdatedAt DESC";

                var cart = await connection.QuerySingleOrDefaultAsync<Cart>(cartSql,
                    new { UserId = userId }, _transaction);

                if (cart == null) return null;

                // Se deve incluir itens, carrega os itens do carrinho
                if (includeItems)
                {
                    var itemsSql = BuildCartItemsQuery(includeItemDetails, includeProductImages, includeBrandInfo, includeAvailability);
                    var items = await connection.QueryAsync<CartItem>(itemsSql, new { CartId = cart.Id }, _transaction);

                    cart.Items = items.ToList();
                }

                return cart;
            });
        }

        // Busca carrinhos ativos no sistema (para admin/relatórios)
        public async Task<(IEnumerable<Cart> Carts, int TotalCount)> GetActiveCartsPagedAsync(
            int page, // Número da página
            int pageSize, // Itens por página
            DateTime? updatedAfter = null, // Carrinhos atualizados após esta data
            DateTime? updatedBefore = null, // Carrinhos atualizados antes desta data
            bool hasItems = true, // Filtrar apenas carrinhos com itens
            decimal? minCartValue = null, // Valor mínimo do carrinho
            bool includeUser = false, // Incluir dados do usuário dono do carrinho
            bool includeItemCount = true, // Incluir quantidade total de itens no carrinho
            bool calculateTotals = false, // Calcular totais de cada carrinho
            CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                // Constrói WHERE clause baseado nos filtros (sem status)
                var whereConditions = new List<string> { "1=1" };

                if (updatedAfter.HasValue)
                    whereConditions.Add("c.UpdatedAt >= @UpdatedAfter");

                if (updatedBefore.HasValue)
                    whereConditions.Add("c.UpdatedAt <= @UpdatedBefore");

                // Filtrar apenas carrinhos que têm itens
                if (hasItems)
                    whereConditions.Add("EXISTS (SELECT 1 FROM CartItems ci WHERE ci.CartId = c.Id)");

                // Filtro por valor mínimo (calculado dinamicamente)
                if (minCartValue.HasValue)
                {
                    whereConditions.Add(@"
                        (SELECT ISNULL(SUM(ci.Quantity * p.Price), 0) 
                         FROM CartItems ci 
                         INNER JOIN Products p ON ci.ProductId = p.Id 
                         WHERE ci.CartId = c.Id) >= @MinCartValue");
                }

                var whereClause = "WHERE " + string.Join(" AND ", whereConditions);

                // Constrói SELECT clause com includes opcionais
                var selectClause = "SELECT c.*";
                var joinClause = "";

                if (includeUser)
                {
                    selectClause = "SELECT c.*, u.FirstName as UserFirstName, u.LastName as UserLastName, u.Email as UserEmail";
                    joinClause = "LEFT JOIN Users u ON c.UserId = u.Id";
                }

                var sql = $@"
                    {selectClause}
                    FROM Carts c
                    {joinClause}
                    {whereClause}
                    ORDER BY c.UpdatedAt DESC
                    OFFSET @Offset ROWS
                    FETCH NEXT @PageSize ROWS ONLY;

                    -- Query para contar total de carrinhos
                    SELECT COUNT(1)
                    FROM Carts c
                    {joinClause}
                    {whereClause}";

                // Monta parâmetros dinamicamente
                var parameters = new DynamicParameters();
                parameters.Add("Offset", (page - 1) * pageSize);
                parameters.Add("PageSize", pageSize);

                if (updatedAfter.HasValue)
                    parameters.Add("UpdatedAfter", updatedAfter.Value);

                if (updatedBefore.HasValue)
                    parameters.Add("UpdatedBefore", updatedBefore.Value);

                if (minCartValue.HasValue)
                    parameters.Add("MinCartValue", minCartValue.Value);

                using var multi = await connection.QueryMultipleAsync(sql, parameters, _transaction);
                var carts = await multi.ReadAsync<Cart>();
                var totalCount = await multi.ReadSingleAsync<int>();

                return (carts, totalCount);
            });
        }

        // Busca carrinho por ID específico
        public async Task<Cart?> GetByIdAsync(
            Guid id, // ID do carrinho
            bool includeItems = true, // Incluir itens do carrinho
            bool includeUser = false, // Incluir dados do usuário
            CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                // Constrói query baseada nas flags de inclusão
                var selectClause = includeUser ?
                    "SELECT c.*, u.FirstName as UserFirstName, u.LastName as UserLastName, u.Email as UserEmail" :
                    "SELECT c.*";

                var joinClause = includeUser ? "LEFT JOIN Users u ON c.UserId = u.Id" : "";

                var sql = $@"
                    {selectClause}
                    FROM Carts c
                    {joinClause}
                    WHERE c.Id = @Id";

                var cart = await connection.QuerySingleOrDefaultAsync<Cart>(sql, new { Id = id }, _transaction);

                if (cart != null && includeItems)
                {
                    // Carrega itens do carrinho se solicitado
                    var itemsSql = "SELECT * FROM CartItems WHERE CartId = @CartId ORDER BY CreatedAt";
                    var items = await connection.QueryAsync<CartItem>(itemsSql, new { CartId = cart.Id }, _transaction);
                    cart.Items = items.ToList();
                }

                return cart;
            });
        }

        // Busca carrinhos abandonados (para remarketing)
        public async Task<IEnumerable<Cart>> GetAbandonedCartsAsync(
            int daysAgo = 7, // Carrinhos não atualizados há X dias
            decimal? minValue = null, // Valor mínimo para considerar relevante
            bool includeUser = true, // Incluir dados do usuário para contato
            bool includeItems = true, // Incluir itens para análise
            int? limit = null, // Limite de resultados
            CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var abandonedThreshold = DateTime.UtcNow.AddDays(-daysAgo);

                // Constrói SELECT baseado nas flags
                var selectClause = includeUser ?
                    "SELECT c.*, u.FirstName as UserFirstName, u.LastName as UserLastName, u.Email as UserEmail" :
                    "SELECT c.*";

                var joinClause = includeUser ? "LEFT JOIN Users u ON c.UserId = u.Id" : "";

                // Filtra carrinhos abandonados que têm itens (sem filtro de status)
                var whereClause = @"
                    WHERE c.UpdatedAt <= @AbandonedThreshold 
                      AND EXISTS (SELECT 1 FROM CartItems ci WHERE ci.CartId = c.Id)";

                if (minValue.HasValue)
                {
                    // Adiciona filtro por valor mínimo calculado
                    whereClause += @" 
                        AND (SELECT ISNULL(SUM(ci.Quantity * p.Price), 0) 
                             FROM CartItems ci 
                             INNER JOIN Products p ON ci.ProductId = p.Id 
                             WHERE ci.CartId = c.Id) >= @MinValue";
                }

                var limitClause = limit.HasValue ? $"TOP {limit.Value}" : "";

                var sql = $@"
                    SELECT {limitClause} {selectClause.Replace("SELECT ", "")}
                    FROM Carts c
                    {joinClause}
                    {whereClause}
                    ORDER BY c.UpdatedAt ASC";

                var parameters = new DynamicParameters();
                parameters.Add("AbandonedThreshold", abandonedThreshold);
                if (minValue.HasValue)
                    parameters.Add("MinValue", minValue.Value);

                var carts = await connection.QueryAsync<Cart>(sql, parameters, _transaction);

                if (includeItems && carts.Any())
                {
                    await LoadCartItemsForMultipleCartsAsync(carts.ToList(), connection);
                }

                return carts;
            });
        }

        // MÉTODOS BÁSICOS DE CRUD PARA COMMANDS

        // Busca simples por ID (sem includes) - usado pelos Commands
        public async Task<Cart?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var sql = "SELECT * FROM Carts WHERE Id = @Id";
                return await connection.QuerySingleOrDefaultAsync<Cart>(sql, new { Id = id }, _transaction);
            });
        }

        // Adiciona novo carrinho no banco de dados
        public async Task<Cart> AddAsync(Cart cart, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                // Insere apenas campos que existem na entidade Cart
                var sql = @"
                    INSERT INTO Carts (Id, UserId, CreatedAt, UpdatedAt)
                    VALUES (@Id, @UserId, @CreatedAt, @UpdatedAt)";

                await connection.ExecuteAsync(sql, new
                {
                    Id = cart.Id,
                    UserId = cart.UserId,
                    CreatedAt = cart.CreatedAt,
                    UpdatedAt = cart.UpdatedAt
                }, _transaction);

                return cart;
            });
        }

        // Atualiza carrinho existente no banco de dados
        public async Task UpdateAsync(Cart cart, CancellationToken cancellationToken = default)
        {
            await ExecuteWithConnectionAsync(async connection =>
            {
                // Atualiza apenas campos que existem na entidade Cart
                var sql = @"
                    UPDATE Carts 
                    SET UserId = @UserId, UpdatedAt = @UpdatedAt
                    WHERE Id = @Id";

                await connection.ExecuteAsync(sql, new
                {
                    Id = cart.Id,
                    UserId = cart.UserId,
                    UpdatedAt = cart.UpdatedAt
                }, _transaction);

                return true;
            });
        }

        // Remove carrinho do banco de dados por ID
        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await ExecuteWithConnectionAsync(async connection =>
            {
                // Remove primeiro os itens do carrinho (foreign key constraint)
                await connection.ExecuteAsync("DELETE FROM CartItems WHERE CartId = @Id", new { Id = id }, _transaction);

                // Depois remove o carrinho
                await connection.ExecuteAsync("DELETE FROM Carts WHERE Id = @Id", new { Id = id }, _transaction);
                return true;
            });
        }

        // MÉTODOS DE GESTÃO DE ITENS DO CARRINHO

        // Busca item específico no carrinho
        public async Task<CartItem?> GetCartItemAsync(Guid cartId, Guid productId, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var sql = @"
                    SELECT ci.*, p.Name as ProductName, p.Price as ProductPrice
                    FROM CartItems ci
                    INNER JOIN Products p ON ci.ProductId = p.Id
                    WHERE ci.CartId = @CartId AND ci.ProductId = @ProductId";

                return await connection.QuerySingleOrDefaultAsync<CartItem>(sql,
                    new { CartId = cartId, ProductId = productId }, _transaction);
            });
        }

        // Adiciona item ao carrinho
        public async Task<CartItem> AddItemAsync(CartItem cartItem, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                // Busca o carrinho para validação usando métodos da entidade
                var cart = await GetByIdAsync(cartItem.CartId, true, false, cancellationToken);
                if (cart == null)
                    throw new InvalidOperationException("Carrinho não encontrado");

                // Verifica se item já existe no carrinho
                var existingItemSql = "SELECT * FROM CartItems WHERE CartId = @CartId AND ProductId = @ProductId";
                var existingItem = await connection.QuerySingleOrDefaultAsync<CartItem>(existingItemSql,
                    new { CartId = cartItem.CartId, ProductId = cartItem.ProductId }, _transaction);

                if (existingItem != null)
                {
                    // Item já existe, atualiza quantidade
                    existingItem.AddQuantity(cartItem.Quantity); // Usa método da entidade

                    var updateSql = @"
                        UPDATE CartItems 
                        SET Quantity = @Quantity, UpdatedAt = @UpdatedAt
                        WHERE CartId = @CartId AND ProductId = @ProductId";

                    await connection.ExecuteAsync(updateSql, new
                    {
                        CartId = cartItem.CartId,
                        ProductId = cartItem.ProductId,
                        Quantity = existingItem.Quantity,
                        UpdatedAt = DateTime.UtcNow
                    }, _transaction);

                    await UpdateCartTimestampAsync(cartItem.CartId, connection);
                    return existingItem;
                }
                else
                {
                    // Item novo, insere no banco
                    var productInfo = await GetProductInfoAsync(cartItem.ProductId, connection);

                    var insertSql = @"
                        INSERT INTO CartItems (Id, CartId, ProductId, Quantity, UnitPrice, CreatedAt, UpdatedAt)
                        VALUES (@Id, @CartId, @ProductId, @Quantity, @UnitPrice, @CreatedAt, @UpdatedAt)";

                    var newItem = new CartItem(cartItem.CartId, cartItem.ProductId, cartItem.Quantity);

                    await connection.ExecuteAsync(insertSql, new
                    {
                        Id = newItem.Id,
                        CartId = newItem.CartId,
                        ProductId = newItem.ProductId,
                        Quantity = newItem.Quantity,
                        UnitPrice = productInfo.Price,
                        CreatedAt = newItem.CreatedAt,
                        UpdatedAt = newItem.UpdatedAt
                    }, _transaction);

                    await UpdateCartTimestampAsync(cartItem.CartId, connection);
                    return newItem;
                }
            });
        }

        // Atualiza quantidade/preço do item
        public async Task UpdateItemAsync(CartItem cartItem, CancellationToken cancellationToken = default)
        {
            await ExecuteWithConnectionAsync(async connection =>
            {
                var sql = @"
                    UPDATE CartItems 
                    SET Quantity = @Quantity, UpdatedAt = @UpdatedAt
                    WHERE CartId = @CartId AND ProductId = @ProductId";

                await connection.ExecuteAsync(sql, new
                {
                    CartId = cartItem.CartId,
                    ProductId = cartItem.ProductId,
                    Quantity = cartItem.Quantity,
                    UpdatedAt = DateTime.UtcNow
                }, _transaction);

                await UpdateCartTimestampAsync(cartItem.CartId, connection);
                return true;
            });
        }

        // Remove item do carrinho
        public async Task RemoveItemAsync(Guid cartId, Guid productId, CancellationToken cancellationToken = default)
        {
            await ExecuteWithConnectionAsync(async connection =>
            {
                var sql = "DELETE FROM CartItems WHERE CartId = @CartId AND ProductId = @ProductId";
                await connection.ExecuteAsync(sql, new { CartId = cartId, ProductId = productId }, _transaction);

                await UpdateCartTimestampAsync(cartId, connection);
                return true;
            });
        }

        // Remove todos os itens do carrinho
        public async Task ClearCartAsync(Guid cartId, CancellationToken cancellationToken = default)
        {
            await ExecuteWithConnectionAsync(async connection =>
            {
                var sql = "DELETE FROM CartItems WHERE CartId = @CartId";
                await connection.ExecuteAsync(sql, new { CartId = cartId }, _transaction);

                await UpdateCartTimestampAsync(cartId, connection);
                return true;
            });
        }

        // MÉTODOS UTILITÁRIOS

        // Verifica se carrinho existe
        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var sql = "SELECT COUNT(1) FROM Carts WHERE Id = @Id";
                var count = await connection.QuerySingleAsync<int>(sql, new { Id = id }, _transaction);
                return count > 0;
            });
        }

        // Verifica se usuário já tem carrinho (remove filtro por status)
        public async Task<bool> UserHasActiveCartAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var sql = "SELECT COUNT(1) FROM Carts WHERE UserId = @UserId";
                var count = await connection.QuerySingleAsync<int>(sql, new { UserId = userId }, _transaction);
                return count > 0;
            });
        }

        // Busca ou cria carrinho para usuário
        public async Task<Cart> GetOrCreateCartAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var existingCart = await GetByUserIdAsync(userId, false, false, false, false, false, false, cancellationToken);
            if (existingCart != null)
                return existingCart;

            // Cria novo carrinho usando construtor da entidade
            var newCart = new Cart(userId);
            return await AddAsync(newCart, cancellationToken);
        }

        // Verifica se carrinho pertence ao usuário (para autorização)
        public async Task<bool> BelongsToUserAsync(Guid cartId, Guid userId, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var sql = "SELECT COUNT(1) FROM Carts WHERE Id = @CartId AND UserId = @UserId";
                var count = await connection.QuerySingleAsync<int>(sql, new { CartId = cartId, UserId = userId }, _transaction);
                return count > 0;
            });
        }

        // MÉTODOS DE CÁLCULOS E VALIDAÇÕES

        // Calcula total do carrinho dinamicamente
        public async Task<decimal> CalculateCartTotalAsync(Guid cartId, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var sql = @"
                    SELECT ISNULL(SUM(ci.Quantity * p.Price), 0) 
                    FROM CartItems ci
                    INNER JOIN Products p ON ci.ProductId = p.Id 
                    WHERE ci.CartId = @CartId";

                return await connection.QuerySingleAsync<decimal>(sql, new { CartId = cartId }, _transaction);
            });
        }

        // Conta itens no carrinho
        public async Task<int> GetItemCountAsync(Guid cartId, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var sql = "SELECT ISNULL(SUM(Quantity), 0) FROM CartItems WHERE CartId = @CartId";
                return await connection.QuerySingleAsync<int>(sql, new { CartId = cartId }, _transaction);
            });
        }

        // Valida se todos os itens estão disponíveis
        public async Task<bool> IsCartValidAsync(Guid cartId, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var sql = @"
                    SELECT COUNT(1)
                    FROM CartItems ci
                    INNER JOIN Products p ON ci.ProductId = p.Id
                    WHERE ci.CartId = @CartId 
                      AND (p.IsActive = 0 OR p.Stock < ci.Quantity)";

                var invalidItemsCount = await connection.QuerySingleAsync<int>(sql, new { CartId = cartId }, _transaction);
                return invalidItemsCount == 0;
            });
        }

        // Atualiza preços dos itens com valores atuais
        public async Task UpdateItemPricesAsync(Guid cartId, CancellationToken cancellationToken = default)
        {
            await ExecuteWithConnectionAsync(async connection =>
            {
                var sql = @"
                    UPDATE ci 
                    SET ci.UnitPrice = p.Price, ci.UpdatedAt = @UpdatedAt
                    FROM CartItems ci
                    INNER JOIN Products p ON ci.ProductId = p.Id
                    WHERE ci.CartId = @CartId";

                await connection.ExecuteAsync(sql, new { CartId = cartId, UpdatedAt = DateTime.UtcNow }, _transaction);
                await UpdateCartTimestampAsync(cartId, connection);
                return true;
            });
        }

        // MÉTODOS DE LIMPEZA E MANUTENÇÃO

        // Remove carrinhos muito antigos sem itens
        public async Task DeleteExpiredCartsAsync(int daysOld = 30, CancellationToken cancellationToken = default)
        {
            await ExecuteWithConnectionAsync(async connection =>
            {
                var expiredThreshold = DateTime.UtcNow.AddDays(-daysOld);

                // Remove carrinhos antigos que não têm itens (sem filtro de status)
                var sql = @"
                    DELETE FROM Carts 
                    WHERE CreatedAt <= @ExpiredThreshold
                      AND NOT EXISTS (SELECT 1 FROM CartItems ci WHERE ci.CartId = Carts.Id)";

                await connection.ExecuteAsync(sql, new { ExpiredThreshold = expiredThreshold }, _transaction);
                return true;
            });
        }

        // Remove carrinhos vazios antigos
        public async Task DeleteEmptyCartsAsync(int daysOld = 7, CancellationToken cancellationToken = default)
        {
            await ExecuteWithConnectionAsync(async connection =>
            {
                var expiredThreshold = DateTime.UtcNow.AddDays(-daysOld);

                // Remove carrinhos vazios não atualizados há muito tempo
                var sql = @"
                    DELETE FROM Carts 
                    WHERE UpdatedAt <= @ExpiredThreshold
                      AND NOT EXISTS (SELECT 1 FROM CartItems ci WHERE ci.CartId = Carts.Id)";

                await connection.ExecuteAsync(sql, new { ExpiredThreshold = expiredThreshold }, _transaction);
                return true;
            });
        }

        // MÉTODOS DE CONVERSÃO

        // Converte carrinho em pedido (checkout)
        public async Task<Order?> ConvertToOrderAsync(Guid cartId, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                // Busca o carrinho com itens
                var cart = await GetByIdAsync(cartId, true, false, cancellationToken);
                if (cart == null || cart.IsEmpty()) return null;

                // Gera número único do pedido
                var orderNumber = await GenerateOrderNumberAsync(connection);

                // Calcula total do pedido usando preços atuais
                var orderTotal = await CalculateSubtotalFromItems(cart.Items, connection);

                // Para criar o pedido, você vai precisar de um endereço de entrega real
                var shippingAddressId = Guid.NewGuid(); // Substituir pela lógica real

                // Cria novo pedido usando construtor correto da entidade
                var order = new Order(orderNumber, cart.UserId, orderTotal, shippingAddressId);

                // Insere o pedido no banco
                var orderSql = @"
                    INSERT INTO Orders (Id, OrderNumber, UserId, Status, TotalAmount, ShippingAddressId, PlacedAt, CreatedAt, UpdatedAt)
                    VALUES (@Id, @OrderNumber, @UserId, @Status, @TotalAmount, @ShippingAddressId, @PlacedAt, @CreatedAt, @UpdatedAt)";

                await connection.ExecuteAsync(orderSql, new
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

                // Converte itens do carrinho em itens do pedido
                foreach (var cartItem in cart.Items)
                {
                    // Busca preço e nome atual do produto
                    var productInfo = await GetProductInfoAsync(cartItem.ProductId, connection);

                    // Cria OrderItem usando construtor correto da entidade
                    var orderItem = new OrderItem(
                        order.Id,
                        cartItem.ProductId,
                        productInfo.Name,
                        cartItem.Quantity,
                        productInfo.Price
                    );

                    // Insere OrderItem no banco
                    var orderItemSql = @"
                        INSERT INTO OrderItems (Id, OrderId, ProductId, ProductName, Quantity, UnitPrice, TotalPrice, CreatedAt, UpdatedAt)
                        VALUES (@Id, @OrderId, @ProductId, @ProductName, @Quantity, @UnitPrice, @TotalPrice, @CreatedAt, @UpdatedAt)";

                    await connection.ExecuteAsync(orderItemSql, new
                    {
                        Id = orderItem.Id,
                        OrderId = orderItem.OrderId,
                        ProductId = orderItem.ProductId,
                        ProductName = orderItem.ProductName,
                        Quantity = orderItem.Quantity,
                        UnitPrice = orderItem.UnitPrice,
                        TotalPrice = orderItem.TotalPrice,
                        CreatedAt = orderItem.CreatedAt,
                        UpdatedAt = orderItem.UpdatedAt
                    }, _transaction);
                }

                // Marca carrinho como convertido (assumindo que você tem essa coluna)
                var updateCartSql = @"
                    UPDATE Carts 
                    SET ConvertedToOrderId = @OrderId, UpdatedAt = @UpdatedAt
                    WHERE Id = @CartId";

                await connection.ExecuteAsync(updateCartSql, new
                {
                    OrderId = order.Id,
                    UpdatedAt = DateTime.UtcNow,
                    CartId = cartId
                }, _transaction);

                return order;
            });
        }

        // Mescla dois carrinhos (usuário logou)
        public async Task MergeCartsAsync(Guid sourceCartId, Guid targetCartId, CancellationToken cancellationToken = default)
        {
            await ExecuteWithConnectionAsync(async connection =>
            {
                // Move todos os itens do carrinho origem para o destino
                var moveItemsSql = @"
                    UPDATE CartItems 
                    SET CartId = @TargetCartId, UpdatedAt = @UpdatedAt
                    WHERE CartId = @SourceCartId";

                await connection.ExecuteAsync(moveItemsSql, new
                {
                    SourceCartId = sourceCartId,
                    TargetCartId = targetCartId,
                    UpdatedAt = DateTime.UtcNow
                }, _transaction);

                // Remove o carrinho origem
                await DeleteAsync(sourceCartId, cancellationToken);

                // Atualiza timestamp do carrinho destino
                await UpdateCartTimestampAsync(targetCartId, connection);

                return true;
            });
        }

        // MÉTODOS AUXILIARES PRIVADOS PARA CONSTRUÇÃO DE QUERIES

        // Constrói query para buscar itens do carrinho com includes opcionais
        private string BuildCartItemsQuery(bool includeItemDetails, bool includeProductImages, bool includeBrandInfo, bool includeAvailability)
        {
            var columns = new List<string> { "ci.*" };

            if (includeItemDetails)
            {
                columns.AddRange(new[] { "p.Name as ProductName", "p.Description as ProductDescription" });
            }

            if (includeProductImages)
            {
                columns.Add("p.ImageUrl as ProductImageUrl");
            }

            if (includeBrandInfo)
            {
                columns.AddRange(new[] { "b.Name as BrandName", "b.LogoUrl as BrandLogoUrl" });
            }

            if (includeAvailability)
            {
                columns.AddRange(new[] { "p.Stock as ProductStock", "p.IsActive as ProductIsActive" });
            }

            var joins = new List<string> { "FROM CartItems ci" };

            if (includeItemDetails || includeProductImages || includeAvailability)
            {
                joins.Add("INNER JOIN Products p ON ci.ProductId = p.Id");
            }

            if (includeBrandInfo)
            {
                joins.Add("LEFT JOIN Brands b ON p.BrandId = b.Id");
            }

            return $@"
                SELECT {string.Join(", ", columns)}
                {string.Join("\n                ", joins)}
                WHERE ci.CartId = @CartId
                ORDER BY ci.CreatedAt";
        }

        // Carrega itens para múltiplos carrinhos de forma eficiente
        private async Task LoadCartItemsForMultipleCartsAsync(List<Cart> carts, SqlConnection connection)
        {
            var cartIds = carts.Select(c => c.Id).ToList();

            var sql = @"
                SELECT ci.*, p.Name as ProductName, p.ImageUrl as ProductImageUrl
                FROM CartItems ci
                INNER JOIN Products p ON ci.ProductId = p.Id
                WHERE ci.CartId IN @CartIds
                ORDER BY ci.CartId, ci.CreatedAt";

            var allItems = await connection.QueryAsync<CartItem>(sql, new { CartIds = cartIds }, _transaction);

            // Agrupa itens por carrinho
            var itemsByCart = allItems.GroupBy(i => i.CartId).ToDictionary(g => g.Key, g => g.ToList());

            // Atribui itens aos carrinhos correspondentes
            foreach (var cart in carts)
            {
                cart.Items = itemsByCart.TryGetValue(cart.Id, out var items) ? items : new List<CartItem>();
            }
        }

        // Atualiza timestamp de atualização do carrinho
        private async Task UpdateCartTimestampAsync(Guid cartId, SqlConnection connection)
        {
            var sql = "UPDATE Carts SET UpdatedAt = @UpdatedAt WHERE Id = @CartId";
            await connection.ExecuteAsync(sql, new { CartId = cartId, UpdatedAt = DateTime.UtcNow }, _transaction);
        }

        // Método auxiliar para gerar número único do pedido
        private async Task<string> GenerateOrderNumberAsync(SqlConnection connection)
        {
            // Gera número no formato ORD-YYYY-NNNNNN
            var year = DateTime.UtcNow.Year;
            var sequence = await GetNextOrderSequenceAsync(year, connection);
            return $"ORD-{year}-{sequence:D6}";
        }

        // Obtém próximo número sequencial para o ano
        private async Task<int> GetNextOrderSequenceAsync(int year, SqlConnection connection)
        {
            var sql = @"
                SELECT ISNULL(MAX(CAST(RIGHT(OrderNumber, 6) AS INT)), 0) + 1
                FROM Orders 
                WHERE OrderNumber LIKE @Pattern";

            var pattern = $"ORD-{year}-%";
            return await connection.QuerySingleAsync<int>(sql, new { Pattern = pattern }, _transaction);
        }

        // Método auxiliar para buscar informações do produto (nome e preço)
        private async Task<ProductInfo> GetProductInfoAsync(Guid productId, SqlConnection connection)
        {
            var sql = "SELECT Name, Price FROM Products WHERE Id = @ProductId";
            var result = await connection.QuerySingleAsync(sql, new { ProductId = productId }, _transaction);

            return new ProductInfo
            {
                Name = (string)result.Name,
                Price = (decimal)result.Price
            };
        }

        // Método auxiliar para calcular subtotal dos itens usando preços atuais
        private async Task<decimal> CalculateSubtotalFromItems(List<CartItem> items, SqlConnection connection)
        {
            decimal subtotal = 0;

            foreach (var item in items)
            {
                // Busca o preço atual do produto para cada item
                var productInfo = await GetProductInfoAsync(item.ProductId, connection);
                subtotal += item.Quantity * productInfo.Price;
            }

            return subtotal;
        }

        public void SetTransaction(SqlTransaction? transaction)
        {
            _transaction = transaction;
        }

        // Classe auxiliar para informações do produto
        private class ProductInfo
        {
            public string Name { get; set; } = string.Empty;
            public decimal Price { get; set; }
        }
    }
}