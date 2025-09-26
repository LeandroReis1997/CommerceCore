using CommerceCore.Application.DTOs.Users;
using MediatR;

namespace CommerceCore.Application.Commands.Users
{
    public class UpdateUserAddressCommand : IRequest<UserAddressDto>
    {
        public Guid Id { get; set; }
        public string Street { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
        public string? Complement { get; set; }
        public string Neighborhood { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }
}
