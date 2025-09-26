using CommerceCore.Application.DTOs.Common;

namespace CommerceCore.Application.DTOs.Brands
{
    public class BrandListDto : BaseDto
    {
        public string Name { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public bool IsActive { get; set; }
        public int ProductCount { get; set; }
    }
}
