using MediatR;

namespace CommerceCore.Application.Commands.Carts
{
    public class ClearCartCommand : IRequest<bool>
    {
        public Guid UserId { get; set; }
    }
}
