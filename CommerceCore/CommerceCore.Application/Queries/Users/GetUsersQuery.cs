using CommerceCore.Application.DTOs.Common;
using CommerceCore.Application.DTOs.Users;
using MediatR;

namespace CommerceCore.Application.Queries.Users
{
    public class GetUsersQuery : IRequest<PagedResultDto<UserListDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
    }
}
