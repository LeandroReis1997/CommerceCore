using CommerceCore.Domain.Common;
using CommerceCore.Domain.Enums;

namespace CommerceCore.Domain.Entities
{
    public class Payment : BaseEntity
    {
        #region Constants

        private const decimal MIN_AMOUNT = 0.01m;
        private const decimal MAX_AMOUNT = 999999.99m;
        private const int MAX_TRANSACTION_ID_LENGTH = 200;

        #endregion

        #region Properties

        // ID do pedido ao qual este pagamento pertence
        public Guid OrderId { get; private set; }

        // Valor do pagamento
        public decimal Amount { get; private set; }

        // Método de pagamento utilizado
        public PaymentMethod Method { get; private set; }

        // Status atual do pagamento
        public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;

        // ID da transação no gateway de pagamento (opcional)
        public string? TransactionId { get; private set; }

        // Data e hora do processamento (quando foi aprovado/negado)
        public DateTime? ProcessedAt { get; private set; }

        #endregion

        #region Navigation Properties

        // Pedido ao qual este pagamento pertence
        public Order Order { get; set; } = null!;

        #endregion

        #region Constructors

        // Construtor vazio para ORMs (Dapper) e serialização JSON
        private Payment() { }

        // Cria um novo pagamento
        public Payment(Guid orderId, decimal amount, PaymentMethod method, string? transactionId = null)
        {
            ValidatePaymentCreation(orderId, amount, transactionId); // Valida dados
            SetPaymentProperties(orderId, amount, method, transactionId); // Define propriedades
            InitializePaymentState();                                // Define estado inicial
        }

        #endregion

        #region Public Methods - Status Transitions

        // Inicia processamento do pagamento
        public void StartProcessing()
        {
            ValidateStatusTransition(PaymentStatus.Processing); // Verifica se transição é válida
            ChangeStatus(PaymentStatus.Processing);            // Muda status
        }

        // Completa o pagamento (aprovado)
        public void Complete()
        {
            ValidateStatusTransition(PaymentStatus.Completed); // Verifica se transição é válida
            ChangeStatus(PaymentStatus.Completed);            // Muda status
            SetProcessedAt();                                 // Define data de processamento
        }

        // Falha no pagamento (negado)
        public void Fail()
        {
            ValidateStatusTransition(PaymentStatus.Failed);   // Verifica se transição é válida
            ChangeStatus(PaymentStatus.Failed);              // Muda status
            SetProcessedAt();                                // Define data de processamento
        }

        // Cancela o pagamento
        public void Cancel()
        {
            ValidateStatusTransition(PaymentStatus.Cancelled); // Verifica se transição é válida
            ChangeStatus(PaymentStatus.Cancelled);            // Muda status
        }

        // Estorna o pagamento
        public void Refund()
        {
            ValidateStatusTransition(PaymentStatus.Refunded); // Verifica se transição é válida
            ChangeStatus(PaymentStatus.Refunded);            // Muda status
            SetProcessedAt();                                // Define data de processamento
        }

        #endregion

        #region Public Methods - Payment Management

        // Atualiza ID da transação (do gateway de pagamento)
        public void UpdateTransactionId(string transactionId)
        {
            ValidateTransactionId(transactionId);     // Valida ID da transação
            SetTransactionId(transactionId);         // Define novo ID
            MarkAsUpdated();                         // Marca como alterado
        }

        // Atualiza valor do pagamento (apenas se pendente)
        public void UpdateAmount(decimal amount)
        {
            ValidateCanUpdateAmount();               // Verifica se pode alterar valor
            ValidateAmount(amount);                  // Valida novo valor
            SetAmount(amount);                      // Define novo valor
            MarkAsUpdated();                        // Marca como alterado
        }

        #endregion

        #region Query Methods - Métodos para consultar estado do pagamento

        // Verifica se pagamento está pendente
        public bool IsPending() => Status == PaymentStatus.Pending;

        // Verifica se pagamento está sendo processado
        public bool IsProcessing() => Status == PaymentStatus.Processing;

        // Verifica se pagamento foi completado
        public bool IsCompleted() => Status == PaymentStatus.Completed;

        // Verifica se pagamento falhou
        public bool IsFailed() => Status == PaymentStatus.Failed;

        // Verifica se pagamento foi cancelado
        public bool IsCancelled() => Status == PaymentStatus.Cancelled;

        // Verifica se pagamento foi estornado
        public bool IsRefunded() => Status == PaymentStatus.Refunded;

        // Verifica se pagamento foi processado (sucesso ou falha)
        public bool IsProcessed() => ProcessedAt.HasValue;

        // Verifica se pagamento está em status final (não pode mais mudar)
        public bool IsFinalStatus() => IsCompleted() || IsFailed() || IsCancelled() || IsRefunded();

        // Verifica se é pagamento com cartão
        public bool IsCardPayment() => Method == PaymentMethod.CreditCard || Method == PaymentMethod.DebitCard;

        // Verifica se é pagamento instantâneo
        public bool IsInstantPayment() => Method == PaymentMethod.Pix;

        // Verifica se é pagamento à prazo
        public bool IsDelayedPayment() => Method == PaymentMethod.Boleto;

        // Retorna tempo de processamento (se processado)
        public TimeSpan? GetProcessingTime()
        {
            return ProcessedAt.HasValue ? ProcessedAt.Value - CreatedAt : null;
        }

        // Verifica se tem ID de transação
        public bool HasTransactionId() => !string.IsNullOrWhiteSpace(TransactionId);

        #endregion

        #region Private Helper Methods - Métodos auxiliares internos

        // Define propriedades do pagamento durante criação
        private void SetPaymentProperties(Guid orderId, decimal amount, PaymentMethod method, string? transactionId)
        {
            OrderId = orderId;                      // Define pedido
            SetAmount(amount);                     // Define valor
            Method = method;                       // Define método
            SetTransactionId(transactionId);       // Define ID da transação
        }

        // Inicializa estado do pagamento
        private void InitializePaymentState()
        {
            Status = PaymentStatus.Pending;        // Pagamento começa pendente
            ProcessedAt = null;                   // Ainda não foi processado
        }

        // Muda status do pagamento
        private void ChangeStatus(PaymentStatus newStatus)
        {
            Status = newStatus;
            MarkAsUpdated();
        }

        // Define valor do pagamento
        private void SetAmount(decimal amount)
        {
            Amount = amount;
        }

        // Define ID da transação
        private void SetTransactionId(string? transactionId)
        {
            TransactionId = string.IsNullOrWhiteSpace(transactionId) ? null : transactionId.Trim();
        }

        // Define data de processamento
        private void SetProcessedAt()
        {
            ProcessedAt = DateTime.UtcNow;
        }

        // Marca pagamento como atualizado
        private void MarkAsUpdated() => SetUpdatedAt();

        #endregion

        #region Validation Methods - Métodos de validação

        // Valida dados durante criação do pagamento
        private static void ValidatePaymentCreation(Guid orderId, decimal amount, string? transactionId)
        {
            ValidateOrderId(orderId);              // Valida OrderId
            ValidateAmount(amount);                // Valida valor
            ValidateTransactionId(transactionId);  // Valida ID da transação (se fornecido)
        }

        // Valida OrderId
        private static void ValidateOrderId(Guid orderId)
        {
            if (orderId == Guid.Empty)
                throw new ArgumentException("OrderId é obrigatório", nameof(orderId));
        }

        // Valida valor do pagamento
        private static void ValidateAmount(decimal amount)
        {
            if (amount < MIN_AMOUNT)
                throw new ArgumentException($"Valor deve ser maior ou igual a {MIN_AMOUNT:C}", nameof(amount));

            if (amount > MAX_AMOUNT)
                throw new ArgumentException($"Valor não pode exceder {MAX_AMOUNT:C}", nameof(amount));
        }

        // Valida ID da transação
        private static void ValidateTransactionId(string? transactionId)
        {
            if (!string.IsNullOrWhiteSpace(transactionId) && transactionId.Trim().Length > MAX_TRANSACTION_ID_LENGTH)
                throw new ArgumentException($"ID da transação não pode exceder {MAX_TRANSACTION_ID_LENGTH} caracteres", nameof(transactionId));
        }

        // Valida se transição de status é permitida
        private void ValidateStatusTransition(PaymentStatus newStatus)
        {
            var allowedTransitions = GetAllowedStatusTransitions();
            if (!allowedTransitions.Contains(newStatus))
                throw new InvalidOperationException($"Não é possível alterar status de {Status} para {newStatus}");
        }

        // Retorna transições de status permitidas baseado no status atual
        private List<PaymentStatus> GetAllowedStatusTransitions()
        {
            return Status switch
            {
                PaymentStatus.Pending => [PaymentStatus.Processing, PaymentStatus.Cancelled],
                PaymentStatus.Processing => [PaymentStatus.Completed, PaymentStatus.Failed, PaymentStatus.Cancelled],
                PaymentStatus.Completed => [PaymentStatus.Refunded],
                PaymentStatus.Failed => [], // Status final
                PaymentStatus.Cancelled => [], // Status final
                PaymentStatus.Refunded => [], // Status final
                _ => []
            };
        }

        // Valida se pode alterar valor do pagamento
        private void ValidateCanUpdateAmount()
        {
            if (!IsPending())
                throw new InvalidOperationException("Valor só pode ser alterado em pagamentos pendentes");
        }

        #endregion
    }
}
