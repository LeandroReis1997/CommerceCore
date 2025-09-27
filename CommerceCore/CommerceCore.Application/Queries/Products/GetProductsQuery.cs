using CommerceCore.Application.DTOs.Common;
using CommerceCore.Application.DTOs.Products;
using MediatR;

namespace CommerceCore.Application.Queries.Products
{
    public class GetProductsQuery : IRequest<PagedResultDto<ProductListDto>>
    {
        // Número da página para paginação (começa em 1)
        public int Page { get; set; } = 1;

        // Quantidade de produtos por página (recomendado: 10-50, máx: 200)
        public int PageSize { get; set; } = 20;

        // Termo de busca para nome, descrição ou SKU (case-insensitive)
        public string? SearchTerm { get; set; }

        // Filtro por categoria (inclui subcategorias automaticamente)
        public Guid? CategoryId { get; set; }

        // Filtro por marca específica
        public Guid? BrandId { get; set; }

        // Preço mínimo (considera SalePrice se disponível)
        public decimal? MinPrice { get; set; }

        // Preço máximo (deve ser >= MinPrice)
        public decimal? MaxPrice { get; set; }

        // Filtro por status: true=ativo, false=inativo, null=todos
        public bool? IsActive { get; set; } = true;

        // Filtro por estoque: true=disponível, false=esgotado, null=todos
        public bool? InStock { get; set; }

        // Incluir dados da marca (Name, LogoUrl) - adiciona ~20ms
        public bool IncludeBrandInfo { get; set; } = false;

        // Incluir dados da categoria (Name, Path hierárquico) - adiciona ~50ms
        public bool IncludeCategoryInfo { get; set; } = false;

        // Incluir imagens do produto - adiciona ~80ms
        public bool IncludeImages { get; set; } = false;

        // Máximo de imagens por produto (quando IncludeImages=true)
        public int MaxImagesPerProduct { get; set; } = 5;

        // Campo para ordenação: Name, Price, CreatedAt, UpdatedAt, StockQuantity
        public string SortBy { get; set; } = "Name";

        // Direção da ordenação: ASC (crescente) ou DESC (decrescente)
        public string SortDirection { get; set; } = "ASC";

        public GetProductsQuery() { }

        public GetProductsQuery(int page, int pageSize)
        {
            Page = Math.Max(1, page);
            PageSize = Math.Max(1, Math.Min(200, pageSize));
        }

        // Factory method para catálogo público
        public static GetProductsQuery ForPublicCatalog(int page = 1, int pageSize = 20)
        {
            return new GetProductsQuery(page, pageSize)
            {
                IsActive = true,
                InStock = true,
                IncludeBrandInfo = true,
                IncludeCategoryInfo = true,
                IncludeImages = true,
                MaxImagesPerProduct = 3
            };
        }

        // Factory method para painel administrativo
        public static GetProductsQuery ForAdminPanel(int page = 1, int pageSize = 50)
        {
            return new GetProductsQuery(page, pageSize)
            {
                IsActive = null, // Mostra todos
                InStock = null,  // Mostra todos
                IncludeBrandInfo = true,
                IncludeCategoryInfo = true,
                IncludeImages = false, // Admin não precisa na listagem
                SortBy = "UpdatedAt",
                SortDirection = "DESC"
            };
        }

        // Factory method para busca com termo
        public static GetProductsQuery ForSearch(string searchTerm, int page = 1, int pageSize = 20)
        {
            return new GetProductsQuery(page, pageSize)
            {
                SearchTerm = searchTerm,
                IsActive = true,
                IncludeBrandInfo = true,
                IncludeCategoryInfo = true,
                IncludeImages = true,
                MaxImagesPerProduct = 1 // Apenas imagem principal para busca rápida
            };
        }
    }
}
