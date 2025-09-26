using CommerceCore.Application.DTOs.Common;

namespace CommerceCore.Application.DTOs.Products
{
    public class ProductImageDto : BaseDto
    {
        public Guid ProductId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string AltText { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsMain { get; set; }
    }
}
