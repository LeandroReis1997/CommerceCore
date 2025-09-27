using CommerceCore.Application.Interfaces.Repositories;
using CommerceCore.Domain.Enums;

namespace CommerceCore.Application.Interfaces.Services
{
    public interface IPaymentService
    {
        // Métodos de processamento de pagamento
        Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default); // Processa pagamento com gateway
        Task<PaymentResult> RefundPaymentAsync(string transactionId, decimal amount, string reason, CancellationToken cancellationToken = default); // Estorna pagamento
        Task<PaymentStatus> GetPaymentStatusAsync(string transactionId, CancellationToken cancellationToken = default); // Consulta status do pagamento

        // Métodos de validação
        Task<bool> ValidateCardAsync(string cardNumber, string expiryMonth, string expiryYear, string cvv, CancellationToken cancellationToken = default); // Valida dados do cartão
        Task<bool> ValidatePixKeyAsync(string pixKey, CancellationToken cancellationToken = default); // Valida chave PIX
        Task<decimal> CalculateFeesAsync(decimal amount, PaymentMethod method, CancellationToken cancellationToken = default); // Calcula taxas do pagamento

        // Métodos PIX
        Task<string> GeneratePixQrCodeAsync(decimal amount, string description, CancellationToken cancellationToken = default); // Gera QR Code PIX
        Task<PaymentResult> ProcessPixPaymentAsync(decimal amount, string pixKey, string description, CancellationToken cancellationToken = default); // Processa pagamento PIX

        // Métodos de cartão
        Task<PaymentResult> ProcessCreditCardAsync(CreditCardPaymentRequest request, CancellationToken cancellationToken = default); // Processa pagamento com cartão de crédito
        Task<PaymentResult> ProcessDebitCardAsync(DebitCardPaymentRequest request, CancellationToken cancellationToken = default); // Processa pagamento com cartão de débito

        // Webhooks e notificações
        Task<bool> ValidateWebhookSignatureAsync(string payload, string signature, CancellationToken cancellationToken = default); // Valida assinatura do webhook
        Task ProcessWebhookNotificationAsync(string payload, CancellationToken cancellationToken = default); // Processa notificação do gateway
    }

    // Classes auxiliares
    public class PaymentRequest
    {
        public decimal Amount { get; set; } // Valor do pagamento
        public PaymentMethod Method { get; set; } // Método de pagamento
        public string Currency { get; set; } = "BRL"; // Moeda
        public string Description { get; set; } = string.Empty; // Descrição do pagamento
        public string OrderId { get; set; } = string.Empty; // ID do pedido
        public Dictionary<string, object> Metadata { get; set; } = new(); // Dados adicionais
    }

    public class CreditCardPaymentRequest : PaymentRequest
    {
        public string CardNumber { get; set; } = string.Empty; // Número do cartão
        public string CardHolderName { get; set; } = string.Empty; // Nome no cartão
        public string ExpiryMonth { get; set; } = string.Empty; // Mês de expiração
        public string ExpiryYear { get; set; } = string.Empty; // Ano de expiração
        public string Cvv { get; set; } = string.Empty; // Código de segurança
        public int Installments { get; set; } = 1; // Número de parcelas
    }

    public class DebitCardPaymentRequest : PaymentRequest
    {
        public string CardNumber { get; set; } = string.Empty; // Número do cartão
        public string CardHolderName { get; set; } = string.Empty; // Nome no cartão
        public string ExpiryMonth { get; set; } = string.Empty; // Mês de expiração
        public string ExpiryYear { get; set; } = string.Empty; // Ano de expiração
        public string Cvv { get; set; } = string.Empty; // Código de segurança
    }

    public class PaymentResult
    {
        public bool IsSuccess { get; set; } // Indica se pagamento foi bem-sucedido
        public string TransactionId { get; set; } = string.Empty; // ID da transação no gateway
        public PaymentStatus Status { get; set; } // Status atual do pagamento
        public string Message { get; set; } = string.Empty; // Mensagem de retorno
        public decimal ProcessedAmount { get; set; } // Valor processado
        public decimal Fees { get; set; } // Taxas cobradas
        public DateTime ProcessedAt { get; set; } // Data/hora do processamento
        public Dictionary<string, object> GatewayData { get; set; } = new(); // Dados adicionais do gateway
    }
}