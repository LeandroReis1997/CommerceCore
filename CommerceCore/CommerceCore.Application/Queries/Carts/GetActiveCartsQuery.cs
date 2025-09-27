using CommerceCore.Application.DTOs.Carts;
using CommerceCore.Application.DTOs.Common;
using MediatR;

namespace CommerceCore.Application.Queries.Carts
{
    public class GetActiveCartsQuery : IRequest<PagedResultDto<CartListDto>>
    {
        public int Page { get; set; } = 1; // Página atual para paginação
        public int PageSize { get; set; } = 10; // Quantidade de itens por página
        public DateTime? UpdatedAfter { get; set; } // Carrinhos atualizados após esta data
        public DateTime? UpdatedBefore { get; set; } // Carrinhos atualizados antes desta data
        public bool HasItems { get; set; } = true; // Filtrar apenas carrinhos com itens
        public bool IncludeUser { get; set; } = false; // Incluir dados do usuário dono do carrinho
        public bool IncludeItemCount { get; set; } = true; // Incluir quantidade total de itens no carrinho
        public decimal? MinCartValue { get; set; } // Filtrar carrinhos com valor mínimo
    }
}
