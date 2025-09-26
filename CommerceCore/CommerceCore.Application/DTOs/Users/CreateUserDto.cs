using CommerceCore.Domain.Enums;

namespace CommerceCore.Application.DTOs.Users
{
    public class CreateUserDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public UserRole Role { get; set; } = UserRole.Customer;
    }
}
