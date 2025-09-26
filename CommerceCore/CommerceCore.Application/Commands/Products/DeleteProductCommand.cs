using MediatR;

namespace CommerceCore.Application.Commands.Products
{
    public class DeleteProductCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
        public bool ForceDelete { get; set; } = false;
    }
}
