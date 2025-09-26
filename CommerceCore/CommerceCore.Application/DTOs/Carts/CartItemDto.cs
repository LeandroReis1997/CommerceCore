using CommerceCore.Application.DTOs.Common;
using CommerceCore.Application.DTOs.Products;

namespace CommerceCore.Application.DTOs.Carts
{
    public class CartItemDto : BaseDto
    {
        public Guid CartId { get; set; }
        public Guid ProductId { get; set; }
        public ProductDto Product { get; set; } = new();
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        // Propriedades calculadas
        public decimal TotalPrice => Quantity * UnitPrice;
        public bool IsValidQuantity => Quantity > 0 && Quantity <= Product.StockQuantity;
        public bool IsOutOfStock => Product.StockQuantity == 0;
        public bool IsLowStock => Product.StockQuantity > 0 && Product.StockQuantity < Quantity;
    }
}
