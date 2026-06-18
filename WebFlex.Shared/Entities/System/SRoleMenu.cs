using System.ComponentModel.DataAnnotations.Schema;
using WebFlex.Shared.Entities;

namespace WebFlex.Shared.Entities.System;

public class SRoleMenu : BaseEntity {
    [Column("rlmn_id")]
    public new string Id { get; set; }
    public string SRoleId { get; set; }

    public SRole? Role { get; set; }

    public string SMenuId { get; set; }

    public SMenu? Menu { get; set; }

    public bool CanRead { get; set; } = true;

    public bool CanCreate { get; set; }

    public bool CanUpdate { get; set; }

    public bool CanDelete { get; set; }

    public bool CanExport { get; set; }
}