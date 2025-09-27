using CommerceCore.Application.DTOs.Common;
using CommerceCore.Application.DTOs.Products;

namespace CommerceCore.Application.DTOs.Carts
{
    public class CartItemDto : BaseDto
    {
        public Guid CartId { get; set; } // ID do carrinho ao qual este item pertence
        public Guid ProductId { get; set; } // ID do produto adicionado ao carrinho
        public ProductDto? Product { get; set; } // Dados do produto (opcional, controlado por flags de performance)
        public int Quantity { get; set; } // Quantidade do produto no carrinho
        public decimal UnitPrice { get; set; } // Preço unitário do produto no momento da adição ao carrinho

        // Propriedades calculadas
        public decimal TotalPrice => Quantity * UnitPrice; // Preço total do item (quantidade × preço unitário)
        public bool IsValidQuantity => Quantity > 0 && (Product == null || Quantity <= Product.StockQuantity); // Verifica se a quantidade é válida e não excede o estoque
        public bool IsOutOfStock => Product?.StockQuantity == 0; // Indica se o produto está fora de estoque
        public bool IsLowStock => Product != null && Product.StockQuantity > 0 && Product.StockQuantity < Quantity; // Indica se há estoque insuficiente para a quantidade desejada
    }
}
