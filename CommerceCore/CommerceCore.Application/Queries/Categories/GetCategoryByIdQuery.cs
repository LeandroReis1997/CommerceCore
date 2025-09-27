using CommerceCore.Application.DTOs.Categories;
using MediatR;

namespace CommerceCore.Application.Queries.Categories
{
    public class GetCategoryByIdQuery : IRequest<CategoryDto?>
    {
        public Guid Id { get; set; }
        public bool IncludeSubcategories { get; set; } = false;
        public bool IncludeProductCount { get; set; } = true;
    }
}
