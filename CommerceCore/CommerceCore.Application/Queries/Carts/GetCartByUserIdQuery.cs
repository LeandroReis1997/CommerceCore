using CommerceCore.Application.DTOs.Carts;
using MediatR;

namespace CommerceCore.Application.Queries.Carts
{
    public class GetCartByUserIdQuery : IRequest<CartDto?>
    {
        public Guid UserId { get; set; } // ID do usuário para buscar seu carrinho ativo
        public bool IncludeItems { get; set; } = true; // Incluir itens do carrinho (produtos, quantidades)
        public bool IncludeProductDetails { get; set; } = false; // Incluir detalhes completos dos produtos (nome, preço, imagens)
        public bool CalculateTotals { get; set; } = true; // Calcular subtotal, desconto e total do carrinho
    }
}
