using MediatR;

namespace CommerceCore.Application.Commands.Users
{
    public class DeleteUserAddressCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; } // Para validação de segurança
    }
}
