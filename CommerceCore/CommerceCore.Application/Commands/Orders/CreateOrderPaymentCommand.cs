using CommerceCore.Application.DTOs.Orders;
using CommerceCore.Domain.Enums;
using MediatR;

namespace CommerceCore.Application.Commands.Orders
{
    public class CreateOrderPaymentCommand : IRequest<OrderPaymentDto>
    {
        public Guid OrderId { get; set; }
        public PaymentMethod Method { get; set; }
        public decimal Amount { get; set; }
        public string? TransactionId { get; set; }
        public string? Notes { get; set; }
    }
}
