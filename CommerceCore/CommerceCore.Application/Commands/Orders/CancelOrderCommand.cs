using CommerceCore.Application.DTOs.Orders;
using MediatR;

namespace CommerceCore.Application.Commands.Orders
{
    public class CancelOrderCommand : IRequest<OrderDto>
    {
        public Guid Id { get; set; }
        public string Reason { get; set; } = string.Empty;
        public Guid CancelledBy { get; set; } // Quem cancelou (cliente ou admin)
        public bool RefundPayments { get; set; } = true;
    }
}
