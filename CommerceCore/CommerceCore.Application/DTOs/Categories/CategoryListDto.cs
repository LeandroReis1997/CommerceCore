using CommerceCore.Application.DTOs.Common;

namespace CommerceCore.Application.DTOs.Categories
{
    public class CategoryListDto : BaseDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public Guid? ParentId { get; set; }
        public string? ParentName { get; set; }
        public int ProductCount { get; set; }
        public bool HasChildren { get; set; }
        public int Level { get; set; }
    }
}
