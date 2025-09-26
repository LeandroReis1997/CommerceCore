using CommerceCore.Application.DTOs.Categories;
using MediatR;

namespace CommerceCore.Application.Commands.Categories
{
    public class CreateCategoryCommand : IRequest<CategoryDto>
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid? ParentId { get; set; }
    }
}
