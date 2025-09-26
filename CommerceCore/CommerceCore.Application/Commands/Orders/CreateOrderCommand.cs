using CommerceCore.Application.DTOs.Orders;
using MediatR;

namespace CommerceCore.Application.Commands.Orders
{
    public class CreateOrderCommand : IRequest<OrderDto>
    {
        public Guid UserId { get; set; }
        public string? Notes { get; set; }

        // Endereço de entrega
        public string ShippingStreet { get; set; } = string.Empty;
        public string ShippingNumber { get; set; } = string.Empty;
        public string? ShippingComplement { get; set; }
        public string ShippingNeighborhood { get; set; } = string.Empty;
        public string ShippingCity { get; set; } = string.Empty;
        public string ShippingState { get; set; } = string.Empty;
        public string ShippingZipCode { get; set; } = string.Empty;

        public List<CreateOrderItemCommand> Items { get; set; } = new();
    }

    public class CreateOrderItemCommand
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
