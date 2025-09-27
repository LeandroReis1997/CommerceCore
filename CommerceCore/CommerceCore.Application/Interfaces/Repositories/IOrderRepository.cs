using CommerceCore.Domain.Entities;
using CommerceCore.Domain.Enums;

namespace CommerceCore.Application.Interfaces.Repositories
{
    public interface IOrderRepository
    {
        // Busca pedidos paginados com filtros e includes opcionais (para admin)
        Task<(IEnumerable<Order> Orders, int TotalCount)> GetPagedAsync(
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
            CancellationToken cancellationToken = default
        );

        // Busca pedido por ID com includes condicionais
        Task<Order?> GetByIdAsync(
            Guid id, // ID do pedido
            bool includeItems = true, // Incluir itens do pedido (geralmente sempre necessário)
            bool includeItemDetails = false, // Incluir detalhes completos dos produtos dos itens
            bool includeUser = false, // Incluir dados do usuário
            bool includeUserProfile = false, // Incluir perfil completo do usuário
            bool includePaymentInfo = false, // Incluir informações de pagamento
            bool includeShippingInfo = false, // Incluir informações de entrega
            CancellationToken cancellationToken = default
        );

        // Busca pedidos de um usuário específico com paginação
        Task<(IEnumerable<Order> Orders, int TotalCount)> GetByUserIdAsync(
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
            CancellationToken cancellationToken = default
        );

        // Busca pedidos por número do pedido (código público)
        Task<Order?> GetByOrderNumberAsync(
            string orderNumber, // Número público do pedido
            bool includeItems = true, // Incluir itens do pedido
            bool includeUser = false, // Incluir dados do usuário
            CancellationToken cancellationToken = default
        );

        // Métodos básicos de CRUD para Commands
        Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default); // Busca simples por ID
        Task<Order> AddAsync(Order order, CancellationToken cancellationToken = default); // Adiciona novo pedido
        Task UpdateAsync(Order order, CancellationToken cancellationToken = default); // Atualiza pedido existente
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default); // Remove pedido por ID

        // Métodos utilitários
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default); // Verifica se pedido existe
        Task<bool> ExistsByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default); // Verifica se número do pedido existe
        Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken = default); // Gera número único para pedido
        Task<bool> BelongsToUserAsync(Guid orderId, Guid userId, CancellationToken cancellationToken = default); // Verifica se pedido pertence ao usuário (autorização)

        // Métodos de estatísticas e relatórios
        Task<int> GetOrderCountByUserAsync(Guid userId, CancellationToken cancellationToken = default); // Conta pedidos do usuário
        Task<decimal> GetTotalSpentByUserAsync(Guid userId, CancellationToken cancellationToken = default); // Soma total gasto pelo usuário
        Task<int> GetOrderCountByStatusAsync(OrderStatus status, DateTime? fromDate = null, CancellationToken cancellationToken = default); // Conta pedidos por status
        Task<decimal> GetRevenueByPeriodAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default); // Calcula receita por período

        // Métodos de validação e autorização
        Task<bool> CanUserAccessOrderAsync(Guid orderId, Guid userId, CancellationToken cancellationToken = default); // Verifica se usuário pode acessar o pedido
        Task<bool> CanCancelOrderAsync(Guid orderId, CancellationToken cancellationToken = default); // Verifica se pedido pode ser cancelado
        Task<bool> IsOrderEditableAsync(Guid orderId, CancellationToken cancellationToken = default); // Verifica se pedido pode ser editado

        // Métodos de auditoria e histórico (TODO: implementar quando OrderStatusHistory for criado)
        // Task AddStatusHistoryAsync(Guid orderId, OrderStatus newStatus, string? notes = null, CancellationToken cancellationToken = default);
        // Task<IEnumerable<OrderStatusHistory>> GetStatusHistoryAsync(Guid orderId, CancellationToken cancellationToken = default);
    }
}