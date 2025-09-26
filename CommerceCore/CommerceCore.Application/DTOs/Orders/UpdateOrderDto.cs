using CommerceCore.Domain.Enums;

namespace CommerceCore.Application.DTOs.Orders
{
    public class UpdateOrderDto
    {
        public Guid Id { get; set; }
        public OrderStatus Status { get; set; }
        public string? Notes { get; set; }
    }
}
