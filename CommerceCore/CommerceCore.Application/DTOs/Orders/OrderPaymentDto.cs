using CommerceCore.Application.DTOs.Common;
using CommerceCore.Domain.Enums;

namespace CommerceCore.Application.DTOs.Orders
{
    public class OrderPaymentDto : BaseDto
    {
        public Guid OrderId { get; set; }
        public PaymentMethod Method { get; set; }
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; }
        public string? TransactionId { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? Notes { get; set; }

        // Propriedades calculadas
        public string MethodDisplay => Method switch
        {
            PaymentMethod.CreditCard => "Cartão de Crédito",
            PaymentMethod.DebitCard => "Cartão de Débito",
            PaymentMethod.Pix => "PIX",
            PaymentMethod.Boleto => "Boleto Bancário",
            _ => "Não informado"
        };

        public string StatusDisplay => Status switch
        {
            PaymentStatus.Pending => "Pendente",
            PaymentStatus.Processing => "Processando",
            PaymentStatus.Confirmed => "Aprovado",
            PaymentStatus.Failed => "Rejeitado",
            PaymentStatus.Cancelled => "Cancelado",
            _ => "Desconhecido"
        };

        public bool IsCompleted => Status == PaymentStatus.Confirmed;
        public bool IsPending => Status == PaymentStatus.Pending || Status == PaymentStatus.Processing;
        public bool IsFailed => Status == PaymentStatus.Failed || Status == PaymentStatus.Cancelled;
    }
}
