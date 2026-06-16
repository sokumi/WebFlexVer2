using WebFlex.Shared.Entities;

namespace WebFlex.Shared.Entities.System;

public class SUserRole : BaseEntity {
    public long SUserId { get; set; }

    public SUser? User { get; set; }

    public long SRoleId { get; set; }

    public SRole? Role { get; set; }
}