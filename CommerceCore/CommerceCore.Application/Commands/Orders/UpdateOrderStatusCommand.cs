using CommerceCore.Application.DTOs.Orders;
using CommerceCore.Domain.Enums;
using MediatR;

namespace CommerceCore.Application.Commands.Orders
{
    public class UpdateOrderStatusCommand : IRequest<OrderDto>
    {
        public Guid Id { get; set; }
        public OrderStatus Status { get; set; }
        public string? Notes { get; set; }
        public Guid UpdatedBy { get; set; } // Usuário que está atualizando
    }
}
