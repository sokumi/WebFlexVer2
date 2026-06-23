using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using WebFlex.Shared.Entities;

namespace WebFlex.Shared.Entities.System;

/// <summary>
/// 시스템 사용자별 권한 정보
/// </summary>
[DebuggerDisplay("{Id} {USER_UID} {ROLE_ID}")]
[Table("s_user_role"), KeyFieldColumn("USER_ROLE_ID"), Comment("시스템 사용자별 권한 정보")]
public class SUserRole : BaseEntity {

    [ColumnRequired]
    [ColumnStringLength(15)]
    [Column(Order = 11), Comment("사용자 고유 아이디")]
    [DisplayName("entity.SUserRole.USER_UID")]
    public string USER_UID { get; set; }

    [ForeignKey("USER_UID")]
    public SUser? User { get; set; }

    [ColumnRequired]
    [ColumnStringLength(15)]
    [Column(Order = 12), Comment("권한 아이디")]
    [DisplayName("entity.SUserRole.ROLE_ID")]
    public string ROLE_ID { get; set; }

    [ForeignKey("ROLE_ID")]
    public SRole? Role { get; set; }
}