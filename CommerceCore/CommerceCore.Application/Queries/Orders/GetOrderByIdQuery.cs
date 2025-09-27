using CommerceCore.Application.DTOs.Orders;
using MediatR;

namespace CommerceCore.Application.Queries.Orders
{
    public class GetOrderByIdQuery : IRequest<OrderDto?>
    {
        public Guid Id { get; set; } // ID único do pedido
        public bool IncludeItems { get; set; } = true; // Incluir itens do pedido (produtos, quantidades, preços)
        public bool IncludeUser { get; set; } = false; // Incluir dados do usuário que fez o pedido
        public bool IncludePaymentInfo { get; set; } = false; // Incluir informações de pagamento
    }
}
