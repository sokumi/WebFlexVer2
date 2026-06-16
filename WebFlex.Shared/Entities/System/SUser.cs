using WebFlex.Shared.Entities;

namespace WebFlex.Shared.Entities.System;

public class SUser : BaseEntity {
    public string UserId { get; set; } = "";

    public string UserName { get; set; } = "";

    public string? PasswordHash { get; set; }

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public bool IsAdmin { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public ICollection<SUserRole> UserRoles { get; set; } = new List<SUserRole>();
}