using MediatR;

namespace CommerceCore.Application.Commands.Categories
{
    public class DeleteCategoryCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
        public bool ForceDelete { get; set; } = false;
    }
}
