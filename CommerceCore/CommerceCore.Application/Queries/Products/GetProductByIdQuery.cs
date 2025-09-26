using CommerceCore.Application.DTOs.Products;
using MediatR;

namespace CommerceCore.Application.Queries.Products
{
    public class GetProductByIdQuery : IRequest<ProductDto?>
    {
        // ID único do produto (GUID obrigatório)
        public Guid Id { get; set; }

        // Incluir dados completos da marca - adiciona ~10ms
        public bool IncludeBrandInfo { get; set; } = false;

        // Incluir dados da categoria com path hierárquico - adiciona ~50ms
        public bool IncludeCategoryInfo { get; set; } = false;

        // Incluir todas as imagens do produto - adiciona ~30ms
        public bool IncludeImages { get; set; } = false;

        // Incluir produtos similares/relacionados - adiciona ~300ms
        public bool IncludeRelatedProducts { get; set; } = false;

        // Quantidade de produtos relacionados (quando IncludeRelatedProducts=true)
        public int RelatedProductsLimit { get; set; } = 6;

        // Incluir dados de estoque (quantidade, níveis) - dados sensíveis
        public bool IncludeInventoryInfo { get; set; } = false;

        // Incluir histórico de alterações de preço - adiciona ~150ms
        public bool IncludePriceHistory { get; set; } = false;

        // Incluir dados de SEO (meta tags, slug, keywords)
        public bool IncludeSeoInfo { get; set; } = false;

        public GetProductByIdQuery() { }

        public GetProductByIdQuery(Guid id) => Id = id;

        // Factory method para página pública do produto
        public static GetProductByIdQuery ForPublicPage(Guid id)
        {
            return new GetProductByIdQuery(id)
            {
                IncludeBrandInfo = true,
                IncludeCategoryInfo = true,
                IncludeImages = true,
                IncludeRelatedProducts = true,
                IncludeSeoInfo = true
            };
        }

        // Factory method para painel administrativo
        public static GetProductByIdQuery ForAdmin(Guid id)
        {
            return new GetProductByIdQuery(id)
            {
                IncludeBrandInfo = true,
                IncludeCategoryInfo = true,
                IncludeImages = true,
                IncludeInventoryInfo = true,
                IncludePriceHistory = true,
                IncludeSeoInfo = true
            };
        }

        // Factory method para validação rápida (só dados básicos)
        public static GetProductByIdQuery ForValidation(Guid id)
        {
            return new GetProductByIdQuery(id); // Todos os flags false = máxima performance
        }
    }
}
