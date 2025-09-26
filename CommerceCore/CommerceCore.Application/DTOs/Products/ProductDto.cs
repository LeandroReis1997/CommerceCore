using CommerceCore.Application.DTOs.Brands;
using CommerceCore.Application.DTOs.Categories;
using CommerceCore.Application.DTOs.Common;

namespace CommerceCore.Application.DTOs.Products
{
    public class ProductDto : BaseDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? CompareAtPrice { get; set; }
        public decimal? CostPrice { get; set; }
        public int StockQuantity { get; set; }
        public int MinStockLevel { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public decimal Weight { get; set; }
        public string? Dimensions { get; set; }
        public bool IsActive { get; set; }
        public bool IsFeatured { get; set; }

        // Relacionamentos completos
        public Guid CategoryId { get; set; }
        public CategoryDto Category { get; set; } = new();
        public Guid BrandId { get; set; }
        public BrandDto Brand { get; set; } = new();
        public List<ProductImageDto> Images { get; set; } = new();

        // Propriedades calculadas
        public bool IsInStock => StockQuantity > 0;
        public bool IsLowStock => StockQuantity > 0 && StockQuantity <= MinStockLevel;
        public string? MainImageUrl => Images.FirstOrDefault(x => x.IsMain)?.ImageUrl ?? Images.FirstOrDefault()?.ImageUrl;
        public decimal? DiscountPercentage => CompareAtPrice.HasValue && CompareAtPrice > Price ?
            Math.Round(((CompareAtPrice.Value - Price) / CompareAtPrice.Value) * 100, 2) : null;
    }
}
