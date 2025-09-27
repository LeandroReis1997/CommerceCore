using AutoMapper;
using CommerceCore.Application.DTOs.Common;
using CommerceCore.Application.DTOs.Products;
using CommerceCore.Application.Interfaces.Repositories;
using CommerceCore.Application.Queries.Products;
using MediatR;

namespace CommerceCore.Application.Handlers.Products
{
    public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PagedResultDto<ProductListDto>>
    {
        private readonly IProductRepository _productRepository; // Repository para acesso aos dados
        private readonly IMapper _mapper; // AutoMapper para conversão Entity → DTO

        public GetProductsQueryHandler(IProductRepository productRepository, IMapper mapper)
        {
            _productRepository = productRepository;
            _mapper = mapper;
        }

        public async Task<PagedResultDto<ProductListDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
        {
            // Busca paginada com filtros
            var (products, totalCount) = await _productRepository.GetPagedAsync(
                page: request.Page,
                pageSize: request.PageSize,
                searchTerm: request.SearchTerm,
                categoryId: request.CategoryId,
                brandId: request.BrandId,
                isActive: request.IsActive,
                inStock: request.InStock,
                minPrice: request.MinPrice,
                maxPrice: request.MaxPrice,
                includeBrand: request.IncludeBrandInfo,
                includeCategory: request.IncludeCategoryInfo,
                includeImages: request.IncludeImages,
                maxImagesPerProduct: request.MaxImagesPerProduct,
                sortBy: request.SortBy,
                sortDirection: request.SortDirection,
                cancellationToken: cancellationToken
            );

            // Converte Entity → ProductListDto (otimizado para listagem)
            var productListDtos = _mapper.Map<List<ProductListDto>>(products);

            // Retorna resultado paginado
            return new PagedResultDto<ProductListDto>
            {
                Items = productListDtos,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                //TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
            };
        }
    }
}
