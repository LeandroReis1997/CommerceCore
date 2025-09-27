using CommerceCore.Application.DTOs.Common;

namespace CommerceCore.Application.DTOs.Carts
{
    public class CartListDto : BaseDto
    {
        public Guid UserId { get; set; }
        public string? UserName { get; set; } // Condicional
        public string? UserEmail { get; set; } // Condicional
        public int ItemCount { get; set; }
        public decimal TotalAmount { get; set; }
        // SEM User e Items completos
    }
}
