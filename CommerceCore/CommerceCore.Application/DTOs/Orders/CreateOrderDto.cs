namespace CommerceCore.Application.DTOs.Orders
{
    public class CreateOrderDto
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

        public List<CreateOrderItemDto> Items { get; set; } = new();
    }

    public class CreateOrderItemDto
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
