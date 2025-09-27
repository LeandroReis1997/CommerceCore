using CommerceCore.Application.DTOs.Common;
using CommerceCore.Application.DTOs.Users;

namespace CommerceCore.Application.DTOs.Carts
{
    public class CartDto : BaseDto
    {
        public Guid UserId { get; set; } // ID do usuário proprietário do carrinho
        public UserDto? User { get; set; } // Dados do usuário (opcional, controlado por flags de performance)
        public List<CartItemDto> Items { get; set; } = new(); // Lista de itens no carrinho

        // Propriedades calculadas
        public int ItemCount => Items?.Sum(x => x.Quantity) ?? 0; // Total de itens no carrinho (soma das quantidades)
        public decimal TotalAmount => Items?.Sum(x => x.TotalPrice) ?? 0; // Valor total do carrinho (soma dos preços dos itens)
        public bool IsEmpty => Items?.Any() != true; // Indica se o carrinho está vazio
        public bool HasItems => Items?.Any() == true; // Indica se o carrinho possui itens
        public decimal AverageItemPrice => HasItems ? TotalAmount / ItemCount : 0; // Preço médio por item no carrinho
    }
}
