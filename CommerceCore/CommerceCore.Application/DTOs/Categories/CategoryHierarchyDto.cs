using CommerceCore.Application.DTOs.Common;

namespace CommerceCore.Application.DTOs.Categories
{
    public class CategoryHierarchyDto : BaseDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public Guid? ParentId { get; set; }
        public int ProductCount { get; set; }
        public int Level { get; set; }
        public List<CategoryHierarchyDto> Children { get; set; } = new();

        // Propriedades calculadas
        public bool HasChildren => Children.Any();
        public int TotalProductCount => ProductCount + Children.Sum(c => c.TotalProductCount);
    }
}
