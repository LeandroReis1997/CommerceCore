using CommerceCore.Application.DTOs.Common;
using CommerceCore.Application.DTOs.Products;

namespace CommerceCore.Application.DTOs.Orders
{
    public class OrderItemDto : BaseDto
    {
        public Guid OrderId { get; set; }
        public Guid ProductId { get; set; }
        public ProductDto Product { get; set; } = new();
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }

        // Propriedades calculadas
        public decimal CalculatedTotal => Quantity * UnitPrice;
        public bool PriceMatches => Math.Abs(TotalPrice - CalculatedTotal) < 0.01m;
    }
}
