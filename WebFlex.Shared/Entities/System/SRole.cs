using WebFlex.Shared.Entities;

namespace WebFlex.Shared.Entities.System;

public class SRole : BaseEntity {
    public string RoleCode { get; set; } = "";

    public string RoleName { get; set; } = "";

    public string? Description { get; set; }

    public ICollection<SUserRole> UserRoles { get; set; } = new List<SUserRole>();

    public ICollection<SRoleMenu> RoleMenus { get; set; } = new List<SRoleMenu>();
}