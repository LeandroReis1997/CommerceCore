using CommerceCore.Application.DTOs.Users;
using CommerceCore.Domain.Enums;
using MediatR;

namespace CommerceCore.Application.Commands.Users
{
    public class UpdateUserCommand : IRequest<UserDto>
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public UserRole Role { get; set; }
        public bool IsActive { get; set; }
    }
}
