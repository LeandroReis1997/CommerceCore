using CommerceCore.Application.DTOs.Common;

namespace CommerceCore.Application.DTOs.Users
{
    public class UserAddressDto : BaseDto
    {
        public Guid UserId { get; set; }
        public string Street { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
        public string? Complement { get; set; }
        public string Neighborhood { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public string Country { get; set; } = "Brasil";
        public bool IsDefault { get; set; }

        // Propriedades calculadas
        public string FullAddress => $"{Street}, {Number}" +
            (!string.IsNullOrEmpty(Complement) ? $", {Complement}" : "") +
            $" - {Neighborhood}, {City}/{State}, {ZipCode}";

        public string ShortAddress => $"{Street}, {Number} - {Neighborhood}";
    }
}
