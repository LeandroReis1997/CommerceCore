namespace CommerceCore.Application.DTOs.Products
{
    public class UpdateProductDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? CompareAtPrice { get; set; }
        public decimal? CostPrice { get; set; }
        public int StockQuantity { get; set; }
        public int MinStockLevel { get; set; }
        public string? Barcode { get; set; }
        public decimal Weight { get; set; }
        public string? Dimensions { get; set; }
        public bool IsActive { get; set; }
        public bool IsFeatured { get; set; }

        public Guid CategoryId { get; set; }
        public Guid BrandId { get; set; }
    }
}
