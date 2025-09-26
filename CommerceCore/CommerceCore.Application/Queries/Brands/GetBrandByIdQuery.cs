using CommerceCore.Application.DTOs.Brands;
using MediatR;

namespace CommerceCore.Application.Queries.Brands
{
    public class GetBrandByIdQuery : IRequest<BrandDto?>
    {
        public Guid Id { get; set; }
        public bool IncludeProductCount { get; set; } = true;
    }
}
