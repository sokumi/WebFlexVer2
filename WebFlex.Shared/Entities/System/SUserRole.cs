using System.ComponentModel.DataAnnotations.Schema;
using WebFlex.Shared.Entities;

namespace WebFlex.Shared.Entities.System;

public class SUserRole : BaseEntity {
    [Column("usrl_id")]
    public new string Id { get; set; }
    public string SUserId { get; set; }

    public SUser? User { get; set; }

    public string SRoleId { get; set; }

    public SRole? Role { get; set; }
}