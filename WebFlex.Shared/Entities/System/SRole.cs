using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using WebFlex.Shared.Entities;

namespace WebFlex.Shared.Entities.System;

/// <summary>
/// 시스템 권한 정보
/// </summary>
[DebuggerDisplay("{Id} {ROLE_CODE} {ROLE_NAME}")]
[Table("s_role"), KeyFieldColumn("ROLE_ID"), Comment("시스템 권한 정보")]
public class SRole : BaseEntity {

    [ColumnRequired]
    [ColumnStringLength(50)]
    [Column(Order = 11), Comment("권한 코드")]
    [DisplayName("entity.SRole.ROLE_CODE")]
    public string ROLE_CODE { get; set; }

    [ColumnRequired]
    [ColumnStringLength(100)]
    [Column(Order = 12), Comment("권한명")]
    [DisplayName("entity.SRole.ROLE_NAME")]
    public string ROLE_NAME { get; set; }

    [ColumnStringLength(500)]
    [Column(Order = 13), Comment("설명")]
    [DisplayName("entity.SRole.DESCRIPTION")]
    public string? DESCRIPTION { get; set; }



    public List<SUserRole>? UserRoles { get; set; }
    public List<SRoleMenu>? RoleMenus { get; set; }
}