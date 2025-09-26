using CommerceCore.Application.DTOs.Common;
using CommerceCore.Application.DTOs.Users;

namespace CommerceCore.Application.DTOs.Carts
{
    public class CartDto : BaseDto
    {
        public Guid UserId { get; set; }
        public UserDto User { get; set; } = new();
        public List<CartItemDto> Items { get; set; } = new();

        // Propriedades calculadas
        public int ItemCount => Items.Sum(x => x.Quantity);
        public decimal TotalAmount => Items.Sum(x => x.TotalPrice);
        public bool IsEmpty => !Items.Any();
        public bool HasItems => Items.Any();
        public decimal AverageItemPrice => HasItems ? TotalAmount / ItemCount : 0;
    }
}
