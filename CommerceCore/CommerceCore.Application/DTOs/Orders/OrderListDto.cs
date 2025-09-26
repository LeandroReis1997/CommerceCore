using CommerceCore.Application.DTOs.Common;
using CommerceCore.Domain.Enums;

namespace CommerceCore.Application.DTOs.Orders
{
    public class OrderListDto : BaseDto
    {
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public OrderStatus Status { get; set; }
        public decimal TotalAmount { get; set; }
        public int ItemCount { get; set; }
        public string ShippingCity { get; set; } = string.Empty;
        public string ShippingState { get; set; } = string.Empty;

        // Propriedades calculadas
        public string StatusDisplay => Status switch
        {
            OrderStatus.Pending => "Pendente",
            OrderStatus.Confirmed => "Confirmado",
            OrderStatus.Shipped => "Enviado",
            OrderStatus.Delivered => "Entregue",
            OrderStatus.Cancelled => "Cancelado",
            _ => "Desconhecido"
        };

        public string ShippingLocation => $"{ShippingCity}/{ShippingState}";
    }
}
