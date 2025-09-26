using CommerceCore.Application.DTOs.Products;
using MediatR;

namespace CommerceCore.Application.Commands.Products
{
    public class UpdateProductStockCommand : IRequest<ProductDto>
    {
        public Guid ProductId { get; set; }
        public int NewQuantity { get; set; }
        public string? Reason { get; set; }
    }
}
