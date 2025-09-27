using CommerceCore.Application.DTOs.Categories;
using MediatR;

namespace CommerceCore.Application.Queries.Categories
{
    public class GetCategoryHierarchyQuery : IRequest<List<CategoryHierarchyDto>>
    {
        public Guid? RootCategoryId { get; set; } // null = desde a raiz
        public bool? IsActive { get; set; } = true;
        public bool IncludeProductCount { get; set; } = true;
        public int? MaxDepth { get; set; } // limitar níveis de profundidade
    }
}
