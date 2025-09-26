using CommerceCore.Application.DTOs.Brands;
using MediatR;

namespace CommerceCore.Application.Commands.Brands
{
    public class CreateBrandCommand : IRequest<BrandDto>
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string? Website { get; set; }
    }
}
