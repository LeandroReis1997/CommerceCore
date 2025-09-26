using CommerceCore.Application.DTOs.Categories;
using MediatR;

namespace CommerceCore.Application.Commands.Categories
{
    public class UpdateCategoryCommand : IRequest<CategoryDto>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid? ParentId { get; set; }
        public bool IsActive { get; set; }
    }
}
