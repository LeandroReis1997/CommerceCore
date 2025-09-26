using CommerceCore.Application.DTOs.Orders;
using CommerceCore.Domain.Enums;
using MediatR;

namespace CommerceCore.Application.Commands.Orders
{
    public class UpdateOrderPaymentStatusCommand : IRequest<OrderPaymentDto>
    {
        public Guid PaymentId { get; set; }
        public PaymentStatus Status { get; set; }
        public string? TransactionId { get; set; }
        public string? Notes { get; set; }
    }
}
