using WebFlex.Shared.Entities;

namespace WebFlex.Shared.Entities.System;

public class SMenu : BaseEntity {
    public long? ParentMenuId { get; set; }

    public SMenu? ParentMenu { get; set; }

    public string MenuCode { get; set; } = "";

    public string MenuName { get; set; } = "";

    public string? ControllerName { get; set; }

    public string? ActionName { get; set; }

    public string? Url { get; set; }

    public string? Icon { get; set; }

    public int SortOrder { get; set; }

    public bool ShowInMenu { get; set; } = true;

    public ICollection<SMenu> Children { get; set; } = new List<SMenu>();

    public ICollection<SRoleMenu> RoleMenus { get; set; } = new List<SRoleMenu>();
}