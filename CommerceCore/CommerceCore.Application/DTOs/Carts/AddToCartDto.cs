namespace CommerceCore.Application.DTOs.Carts
{
    public class AddToCartDto
    {
        public Guid UserId { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; } = 1;
    }
}
