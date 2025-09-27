using CommerceCore.Application.DTOs.Categories;
using CommerceCore.Application.DTOs.Common;
using MediatR;

namespace CommerceCore.Application.Queries.Categories
{
    public class GetCategoriesQuery : IRequest<PagedResultDto<CategoryListDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public bool? IsActive { get; set; }
        public Guid? ParentCategoryId { get; set; }
    }
}
