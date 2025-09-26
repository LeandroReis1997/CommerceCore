namespace CommerceCore.Application.DTOs.Users
{
    // Endpoint separado para trocar senha
    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
}
