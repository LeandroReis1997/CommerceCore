using MediatR;

namespace CommerceCore.Application.Commands.Brands
{
    public class DeleteBrandCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
        public bool ForceDelete { get; set; } = false;
    }
}
