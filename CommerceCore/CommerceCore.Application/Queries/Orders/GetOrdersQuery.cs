using CommerceCore.Application.DTOs.Common;
using CommerceCore.Application.DTOs.Orders;
using CommerceCore.Domain.Enums;
using MediatR;

namespace CommerceCore.Application.Queries.Orders
{
    public class GetOrdersQuery : IRequest<PagedResultDto<OrderListDto>>
    {
        public int Page { get; set; } = 1; // Página atual para paginação
        public int PageSize { get; set; } = 10; // Quantidade de itens por página
        public string? SearchTerm { get; set; } // Busca por número do pedido, email do cliente, etc.
        public OrderStatus? Status { get; set; } // Filtrar por status específico (Pending, Confirmed, Shipped, etc.)
        public Guid? UserId { get; set; } // Filtrar pedidos de um usuário específico
        public DateTime? CreatedAfter { get; set; } // Pedidos criados após esta data
        public DateTime? CreatedBefore { get; set; } // Pedidos criados antes desta data
    }
}
