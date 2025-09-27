using CommerceCore.Application.DTOs.Common;

namespace CommerceCore.Application.DTOs.Products
{
    public class ProductListDto : BaseDto
    {
        public string Name { get; set; } = string.Empty; // Nome do produto
        public string Slug { get; set; } = string.Empty; // URL amigável para SEO
        public decimal Price { get; set; } // Preço atual do produto
        public decimal? OriginalPrice { get; set; } // Preço original (para mostrar desconto)
        public string? MainImageUrl { get; set; } // Apenas 1 imagem principal para performance
        public bool IsActive { get; set; } // Indica se o produto está ativo
        public int StockQuantity { get; set; } // Quantidade disponível em estoque
        public bool IsInStock { get; set; } // Indica se tem estoque disponível
        public decimal? DiscountPercentage { get; set; } // Percentual de desconto calculado

        // Informações condicionais (baseadas nas performance flags)
        public Guid? BrandId { get; set; } // ID da marca (sempre incluído)
        public string? BrandName { get; set; } // Nome da marca (se IncludeBrandInfo = true)
        public Guid? CategoryId { get; set; } // ID da categoria (sempre incluído)
        public string? CategoryName { get; set; } // Nome da categoria (se IncludeCategoryInfo = true)

        // Propriedades calculadas
        public bool HasDiscount => OriginalPrice.HasValue && OriginalPrice > Price;
        public decimal SavingsAmount => OriginalPrice.GetValueOrDefault() - Price;
    }
}
