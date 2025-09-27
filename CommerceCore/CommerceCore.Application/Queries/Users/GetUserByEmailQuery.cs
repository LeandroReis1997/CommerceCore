using CommerceCore.Application.DTOs.Users;
using MediatR;

namespace CommerceCore.Application.Queries.Users
{
    public class GetUserByEmailQuery : IRequest<UserDto?>
    {
        public string Email { get; set; } = string.Empty;
        public bool IncludeProfile { get; set; } = false;
        public bool IncludeOrderCount { get; set; } = false;
    }
}
