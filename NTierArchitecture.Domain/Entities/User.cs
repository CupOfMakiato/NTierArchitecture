using NTierArchitecture.Domain.Enums;

namespace NTierArchitecture.Domain.Entities
{
    public class User : BaseEntity
    {
        public int RoleId { get; set; }
        public Role? Role { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserStatus Status { get; set; }
    }
}
