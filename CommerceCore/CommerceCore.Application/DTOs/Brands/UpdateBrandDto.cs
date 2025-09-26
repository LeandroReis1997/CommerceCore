namespace CommerceCore.Application.DTOs.Brands
{
    public class UpdateBrandDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string? Website { get; set; }
        public bool IsActive { get; set; }
    }
}
