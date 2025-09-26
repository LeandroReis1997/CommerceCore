using CommerceCore.Application.DTOs.Common;
using CommerceCore.Application.DTOs.Users;
using CommerceCore.Domain.Enums;

namespace CommerceCore.Application.DTOs.Orders
{
    public class OrderDto : BaseDto
    {
        public string OrderNumber { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public UserDto User { get; set; } = new();
        public OrderStatus Status { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Notes { get; set; }

        // Endereço de entrega
        public string ShippingStreet { get; set; } = string.Empty;
        public string ShippingNumber { get; set; } = string.Empty;
        public string? ShippingComplement { get; set; }
        public string ShippingNeighborhood { get; set; } = string.Empty;
        public string ShippingCity { get; set; } = string.Empty;
        public string ShippingState { get; set; } = string.Empty;
        public string ShippingZipCode { get; set; } = string.Empty;

        public List<OrderItemDto> Items { get; set; } = new();
        public List<OrderPaymentDto> Payments { get; set; } = new();

        // Propriedades calculadas
        public string ShippingAddress => $"{ShippingStreet}, {ShippingNumber}" +
            (!string.IsNullOrEmpty(ShippingComplement) ? $", {ShippingComplement}" : "") +
            $" - {ShippingNeighborhood}, {ShippingCity}/{ShippingState}, {ShippingZipCode}";

        public int ItemCount => Items.Sum(x => x.Quantity);
        public decimal ItemsTotal => Items.Sum(x => x.TotalPrice);
        public string StatusDisplay => Status switch
        {
            OrderStatus.Pending => "Pendente",
            OrderStatus.Confirmed => "Confirmado",
            OrderStatus.Shipped => "Enviado",
            OrderStatus.Delivered => "Entregue",
            OrderStatus.Cancelled => "Cancelado",
            _ => "Desconhecido"
        };
    }
}
