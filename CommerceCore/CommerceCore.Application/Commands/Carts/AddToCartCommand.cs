using CommerceCore.Application.DTOs.Carts;
using MediatR;

namespace CommerceCore.Application.Commands.Carts
{
    public class AddToCartCommand : IRequest<CartItemDto>
    {
        public Guid UserId { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; } = 1;
    }
}
