using CommerceCore.Application.DTOs.Common;
using CommerceCore.Domain.Enums;

namespace CommerceCore.Application.DTOs.Users
{
    public class UserListDto : BaseDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public UserRole Role { get; set; }
        public bool IsActive { get; set; }
        public bool IsEmailConfirmed { get; set; }
        public DateTime? LastLoginAt { get; set; }

        // Propriedades calculadas
        public string FullName => $"{FirstName} {LastName}".Trim();
        public string RoleDisplay => Role switch
        {
            UserRole.Customer => "Cliente",
            UserRole.Admin => "Administrador",
            UserRole.SuperAdmin => "Gerente",
            _ => "Desconhecido"
        };
        public string StatusDisplay => IsActive switch
        {
            true when IsEmailConfirmed => "Ativo",
            true when !IsEmailConfirmed => "Pendente confirmação",
            false => "Bloqueado"
        };
    }
}
