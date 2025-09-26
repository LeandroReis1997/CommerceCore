using MediatR;

namespace CommerceCore.Application.Commands.Carts
{
    public class RemoveFromCartCommand : IRequest<bool>
    {
        public Guid CartItemId { get; set; }
        public Guid UserId { get; set; } // Segurança: só pode remover do próprio carrinho
    }
}
