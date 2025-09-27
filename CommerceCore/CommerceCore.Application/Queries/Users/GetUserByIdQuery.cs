using CommerceCore.Application.DTOs.Users;
using MediatR;

namespace CommerceCore.Application.Queries.Users
{
    public class GetUserByIdQuery : IRequest<UserDto?>
    {
        public Guid Id { get; set; }
        public bool IncludeProfile { get; set; } = false;
        public bool IncludeOrderCount { get; set; } = true;
    }
}
