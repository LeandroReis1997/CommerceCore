using CommerceCore.Application.DTOs.Carts;
using MediatR;

namespace CommerceCore.Application.Commands.Carts
{
    public class UpdateCartItemCommand : IRequest<CartItemDto>
    {
        public Guid CartItemId { get; set; }
        public int Quantity { get; set; }
    }
}
