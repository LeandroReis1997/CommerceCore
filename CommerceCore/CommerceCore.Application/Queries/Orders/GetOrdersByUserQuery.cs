using CommerceCore.Application.DTOs.Common;
using CommerceCore.Application.DTOs.Orders;
using CommerceCore.Domain.Enums;
using MediatR;

namespace CommerceCore.Application.Queries.Orders
{
    public class GetOrdersByUserIdQuery : IRequest<PagedResultDto<OrderListDto>>
    {
        public Guid UserId { get; set; } // ID do usuário para buscar seus pedidos
        public int Page { get; set; } = 1; // Página atual para paginação
        public int PageSize { get; set; } = 10; // Quantidade de itens por página
        public OrderStatus? Status { get; set; } // Filtrar por status específico do pedido
        public DateTime? CreatedAfter { get; set; } // Pedidos criados após esta data
        public DateTime? CreatedBefore { get; set; } // Pedidos criados antes desta data
    }
}
