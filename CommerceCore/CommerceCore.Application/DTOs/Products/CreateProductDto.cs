namespace CommerceCore.Application.DTOs.Products
{
    public class CreateProductDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? CompareAtPrice { get; set; }
        public decimal? CostPrice { get; set; }
        public int StockQuantity { get; set; }
        public int MinStockLevel { get; set; } = 10;
        public string Sku { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public decimal Weight { get; set; }
        public string? Dimensions { get; set; }
        public bool IsFeatured { get; set; }

        public Guid CategoryId { get; set; }
        public Guid BrandId { get; set; }
        public List<CreateProductImageDto> Images { get; set; } = new();
    }

    public class CreateProductImageDto
    {
        public string ImageUrl { get; set; } = string.Empty;
        public string AltText { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsMain { get; set; }
    }
}
