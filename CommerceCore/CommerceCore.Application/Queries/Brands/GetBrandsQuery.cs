using CommerceCore.Application.DTOs.Brands;
using CommerceCore.Application.DTOs.Common;
using MediatR;

namespace CommerceCore.Application.Queries.Brands
{
    public class GetBrandsQuery : IRequest<PagedResultDto<BrandListDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public bool? IsActive { get; set; }
    }
}
