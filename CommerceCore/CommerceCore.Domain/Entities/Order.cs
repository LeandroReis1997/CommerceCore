using CommerceCore.Domain.Common;
using CommerceCore.Domain.Enums;

namespace CommerceCore.Domain.Entities
{
    public class Order : BaseEntity
    {
        #region Constants

        private const int MIN_ORDER_NUMBER_LENGTH = 5;
        private const int MAX_ORDER_NUMBER_LENGTH = 50;
        private const decimal MIN_TOTAL_AMOUNT = 0.01m;
        private const decimal MAX_TOTAL_AMOUNT = 999999.99m;

        #endregion

        #region Properties

        // Número único do pedido (ex: "ORD-2024-001234")
        public string OrderNumber { get; private set; } = string.Empty;

        // ID do usuário que fez o pedido
        public Guid UserId { get; private set; }

        // Status atual do pedido
        public OrderStatus Status { get; private set; } = OrderStatus.Pending;

        // Valor total do pedido
        public decimal TotalAmount { get; private set; }

        // ID do endereço para entrega
        public Guid ShippingAddressId { get; private set; }

        // Data e hora que o pedido foi realizado
        public DateTime PlacedAt { get; private set; }

        #endregion

        #region Navigation Properties

        // Usuário que fez o pedido
        public User User { get; set; } = null!;

        // Endereço para entrega
        public Address ShippingAddress { get; set; } = null!;

        // Itens do pedido
        public List<OrderItem> Items { get; set; } = [];

        // Pagamentos relacionados ao pedido
        public List<Payment> Payments { get; set; } = [];

        #endregion

        #region Constructors

        // Construtor vazio para ORMs (Dapper) e serialização JSON
        private Order() { }

        // Cria um novo pedido
        public Order(string orderNumber, Guid userId, decimal totalAmount, Guid shippingAddressId)
        {
            ValidateOrderCreation(orderNumber, userId, totalAmount, shippingAddressId); // Valida dados
            SetOrderProperties(orderNumber, userId, totalAmount, shippingAddressId);    // Define propriedades
            InitializeOrderState();                                                     // Define estado inicial
        }

        #endregion

        #region Public Methods - Status Transitions

        // Confirma o pedido (após pagamento aprovado)
        public void Confirm()
        {
            ValidateStatusTransition(OrderStatus.Confirmed); // Verifica se transição é válida
            ChangeStatus(OrderStatus.Confirmed);            // Muda status
        }

        // Inicia processamento do pedido (separação/embalagem)
        public void StartProcessing()
        {
            ValidateStatusTransition(OrderStatus.Processing);
            ChangeStatus(OrderStatus.Processing);
        }

        // Marca pedido como enviado
        public void Ship()
        {
            ValidateStatusTransition(OrderStatus.Shipped);
            ChangeStatus(OrderStatus.Shipped);
        }

        // Marca pedido como entregue
        public void Deliver()
        {
            ValidateStatusTransition(OrderStatus.Delivered);
            ChangeStatus(OrderStatus.Delivered);
        }

        // Cancela o pedido
        public void Cancel()
        {
            ValidateCanBeCancelled();               // Verifica se pode ser cancelado
            ChangeStatus(OrderStatus.Cancelled);   // Muda para cancelado
        }

        // Estorna o pedido (após cancelamento ou devolução)
        public void Refund()
        {
            ValidateCanBeRefunded();               // Verifica se pode ser estornado
            ChangeStatus(OrderStatus.Refunded);   // Muda para estornado
        }

        #endregion

        #region Public Methods - Order Management

        // Atualiza valor total do pedido
        public void UpdateTotalAmount(decimal totalAmount)
        {
            ValidateCanUpdateAmount();             // Verifica se pode alterar valor
            ValidateTotalAmount(totalAmount);      // Valida novo valor
            SetTotalAmount(totalAmount);          // Define novo valor
            MarkAsUpdated();                      // Marca como alterado
        }

        // Atualiza endereço de entrega
        public void UpdateShippingAddress(Guid shippingAddressId)
        {
            ValidateCanUpdateShipping();          // Verifica se pode alterar endereço
            ValidateShippingAddressId(shippingAddressId); // Valida novo endereço
            ShippingAddressId = shippingAddressId; // Define novo endereço
            MarkAsUpdated();                      // Marca como alterado
        }

        #endregion

        #region Query Methods - Métodos para consultar estado do pedido

        // Verifica se pedido está pendente
        public bool IsPending() => Status == OrderStatus.Pending;

        // Verifica se pedido foi confirmado
        public bool IsConfirmed() => Status == OrderStatus.Confirmed;

        // Verifica se pedido está sendo processado
        public bool IsProcessing() => Status == OrderStatus.Processing;

        // Verifica se pedido foi enviado
        public bool IsShipped() => Status == OrderStatus.Shipped;

        // Verifica se pedido foi entregue
        public bool IsDelivered() => Status == OrderStatus.Delivered;

        // Verifica se pedido foi cancelado
        public bool IsCancelled() => Status == OrderStatus.Cancelled;

        // Verifica se pedido foi estornado
        public bool IsRefunded() => Status == OrderStatus.Refunded;

        // Verifica se pedido está completo (entregue)
        public bool IsCompleted() => IsDelivered();

        // Verifica se pedido está ativo (não cancelado nem estornado)
        public bool IsActive() => !IsCancelled() && !IsRefunded();

        // Retorna quantidade de itens no pedido
        public int GetItemsCount() => Items.Count;

        // Retorna quantidade total de produtos (soma quantidades)
        public int GetTotalProductsQuantity() => Items.Sum(i => i.Quantity);

        // Retorna valor total pago
        public decimal GetPaidAmount() => CalculatePaidAmount();

        // Retorna valor pendente de pagamento
        public decimal GetPendingAmount() => TotalAmount - GetPaidAmount();

        // Verifica se pedido foi totalmente pago
        public bool IsFullyPaid() => GetPaidAmount() >= TotalAmount;

        // Verifica se pedido pode ser cancelado
        public bool CanBeCancelled() => IsPending() || IsConfirmed();

        // Verifica se pedido pode ser estornado
        public bool CanBeRefunded() => IsCancelled() || IsDelivered();

        #endregion

        #region Private Helper Methods - Métodos auxiliares internos

        // Define propriedades do pedido durante criação
        private void SetOrderProperties(string orderNumber, Guid userId, decimal totalAmount, Guid shippingAddressId)
        {
            OrderNumber = NormalizeOrderNumber(orderNumber); // Normaliza número do pedido
            UserId = userId;                                // Define usuário
            SetTotalAmount(totalAmount);                    // Define valor total
            ShippingAddressId = shippingAddressId;         // Define endereço
        }

        // Inicializa estado do pedido
        private void InitializeOrderState()
        {
            Status = OrderStatus.Pending;    // Pedido começa pendente
            PlacedAt = DateTime.UtcNow;     // Define data atual
        }

        // Muda status do pedido
        private void ChangeStatus(OrderStatus newStatus)
        {
            Status = newStatus;
            MarkAsUpdated();
        }

        // Define valor total
        private void SetTotalAmount(decimal totalAmount)
        {
            TotalAmount = totalAmount;
        }

        // Marca pedido como atualizado
        private void MarkAsUpdated() => SetUpdatedAt();

        // Normaliza número do pedido
        private static string NormalizeOrderNumber(string orderNumber) => orderNumber.Trim().ToUpperInvariant();

        // Calcula valor total pago
        private decimal CalculatePaidAmount() =>
            Payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount);

        #endregion

        #region Validation Methods - Métodos de validação

        // Valida dados durante criação do pedido
        private static void ValidateOrderCreation(string orderNumber, Guid userId, decimal totalAmount, Guid shippingAddressId)
        {
            ValidateOrderNumber(orderNumber);              // Valida número do pedido
            ValidateUserId(userId);                       // Valida usuário
            ValidateTotalAmount(totalAmount);             // Valida valor
            ValidateShippingAddressId(shippingAddressId); // Valida endereço
        }

        // Valida número do pedido
        private static void ValidateOrderNumber(string orderNumber)
        {
            if (string.IsNullOrWhiteSpace(orderNumber))
                throw new ArgumentException("Número do pedido é obrigatório", nameof(orderNumber));

            var trimmed = orderNumber.Trim();
            if (trimmed.Length < MIN_ORDER_NUMBER_LENGTH)
                throw new ArgumentException($"Número do pedido deve ter pelo menos {MIN_ORDER_NUMBER_LENGTH} caracteres", nameof(orderNumber));

            if (trimmed.Length > MAX_ORDER_NUMBER_LENGTH)
                throw new ArgumentException($"Número do pedido não pode exceder {MAX_ORDER_NUMBER_LENGTH} caracteres", nameof(orderNumber));
        }

        // Valida UserId
        private static void ValidateUserId(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId é obrigatório", nameof(userId));
        }

        // Valida valor total
        private static void ValidateTotalAmount(decimal totalAmount)
        {
            if (totalAmount < MIN_TOTAL_AMOUNT)
                throw new ArgumentException($"Valor total deve ser maior ou igual a {MIN_TOTAL_AMOUNT:C}", nameof(totalAmount));

            if (totalAmount > MAX_TOTAL_AMOUNT)
                throw new ArgumentException($"Valor total não pode exceder {MAX_TOTAL_AMOUNT:C}", nameof(totalAmount));
        }

        // Valida endereço de entrega
        private static void ValidateShippingAddressId(Guid shippingAddressId)
        {
            if (shippingAddressId == Guid.Empty)
                throw new ArgumentException("Endereço de entrega é obrigatório", nameof(shippingAddressId));
        }

        // Valida se transição de status é permitida
        private void ValidateStatusTransition(OrderStatus newStatus)
        {
            var allowedTransitions = GetAllowedStatusTransitions();
            if (!allowedTransitions.Contains(newStatus))
                throw new InvalidOperationException($"Não é possível alterar status de {Status} para {newStatus}");
        }

        // Retorna transições de status permitidas baseado no status atual
        private List<OrderStatus> GetAllowedStatusTransitions()
        {
            return Status switch
            {
                OrderStatus.Pending => [OrderStatus.Confirmed, OrderStatus.Cancelled],
                OrderStatus.Confirmed => [OrderStatus.Processing, OrderStatus.Cancelled],
                OrderStatus.Processing => [OrderStatus.Shipped, OrderStatus.Cancelled],
                OrderStatus.Shipped => [OrderStatus.Delivered],
                OrderStatus.Delivered => [OrderStatus.Refunded],
                OrderStatus.Cancelled => [OrderStatus.Refunded],
                OrderStatus.Refunded => [], // Status final
                _ => []
            };
        }

        // Valida se pedido pode ser cancelado
        private void ValidateCanBeCancelled()
        {
            if (!CanBeCancelled())
                throw new InvalidOperationException($"Pedido no status {Status} não pode ser cancelado");
        }

        // Valida se pedido pode ser estornado
        private void ValidateCanBeRefunded()
        {
            if (!CanBeRefunded())
                throw new InvalidOperationException($"Pedido no status {Status} não pode ser estornado");
        }

        // Valida se pode alterar valor do pedido
        private void ValidateCanUpdateAmount()
        {
            if (!IsPending())
                throw new InvalidOperationException("Valor só pode ser alterado em pedidos pendentes");
        }

        // Valida se pode alterar endereço de entrega
        private void ValidateCanUpdateShipping()
        {
            if (IsShipped() || IsDelivered())
                throw new InvalidOperationException("Endereço não pode ser alterado após envio");
        }

        #endregion
    }
}
