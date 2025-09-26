using CommerceCore.Application.DTOs.Common;
using CommerceCore.Application.DTOs.Products;
using MediatR;

namespace CommerceCore.Application.Queries.Products
{
    public class GetProductsByCategoryQuery : IRequest<PagedResultDto<ProductDto>>
    {
        // ID da categoria (GUID obrigatório)
        public Guid CategoryId { get; set; }

        // Número da página para paginação
        public int Page { get; set; } = 1;

        // Quantidade de produtos por página
        public int PageSize { get; set; } = 20;

        // Incluir produtos das subcategorias filhas (hierarquia completa)
        public bool IncludeSubcategories { get; set; } = true;

        // Incluir informações da categoria atual (Name, Description, Path)
        public bool IncludeCategoryInfo { get; set; } = true;

        // Incluir dados da marca de cada produto
        public bool IncludeBrandInfo { get; set; } = false;

        // Incluir imagens dos produtos
        public bool IncludeImages { get; set; } = false;

        // Máximo de imagens por produto
        public int MaxImagesPerProduct { get; set; } = 3;

        // Filtro adicional por marca dentro da categoria
        public Guid? BrandId { get; set; }

        // Filtro por faixa de preço mínimo
        public decimal? MinPrice { get; set; }

        // Filtro por faixa de preço máximo
        public decimal? MaxPrice { get; set; }

        // Filtro por produtos em estoque
        public bool? InStock { get; set; }

        // Campo para ordenação: Name, Price, CreatedAt, Brand
        public string SortBy { get; set; } = "Name";

        // Direção da ordenação: ASC ou DESC
        public string SortDirection { get; set; } = "ASC";

        public GetProductsByCategoryQuery() { }

        public GetProductsByCategoryQuery(Guid categoryId, int page = 1, int pageSize = 20)
        {
            CategoryId = categoryId;
            Page = Math.Max(1, page);
            PageSize = Math.Max(1, Math.Min(200, pageSize));
        }

        // Factory method para página pública da categoria
        public static GetProductsByCategoryQuery ForPublicCategoryPage(Guid categoryId, int page = 1)
        {
            return new GetProductsByCategoryQuery(categoryId, page)
            {
                IncludeSubcategories = true,
                IncludeCategoryInfo = true,
                IncludeBrandInfo = true,
                IncludeImages = true,
                MaxImagesPerProduct = 3,
                InStock = true // Só produtos disponíveis
            };
        }

        // Factory method para navegação rápida (sem imagens)
        public static GetProductsByCategoryQuery ForQuickListing(Guid categoryId, int page = 1)
        {
            return new GetProductsByCategoryQuery(categoryId, page)
            {
                IncludeSubcategories = true,
                IncludeCategoryInfo = false, // Não precisa da categoria
                IncludeBrandInfo = true,
                IncludeImages = false, // Sem imagens = mais rápido
                InStock = true
            };
        }

        // Factory method para admin com todos os dados
        public static GetProductsByCategoryQuery ForAdmin(Guid categoryId, int page = 1)
        {
            return new GetProductsByCategoryQuery(categoryId, page, 50)
            {
                IncludeSubcategories = true,
                IncludeCategoryInfo = true,
                IncludeBrandInfo = true,
                IncludeImages = false, // Admin não precisa na listagem
                InStock = null, // Mostra todos (com e sem estoque)
                SortBy = "UpdatedAt",
                SortDirection = "DESC" // Mais recentes primeiro
            };
        }
    }
}
