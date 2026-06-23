using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using WebFlex.Shared.Entities;

namespace WebFlex.Shared.Entities.System;

/// <summary>
/// 시스템 사용자 정보
/// </summary>
[DebuggerDisplay("{Id} {USER_ID} {USER_NAME}")]
[Table("s_user"), KeyFieldColumn("USER_UID"), Comment("시스템 사용자 정보")]
public class SUser : BaseEntity {

    [ColumnRequired]
    [ColumnStringLength(50)]
    [Column(Order = 11), Comment("사용자 아이디")]
    [DisplayName("entity.SUser.USER_ID")]
    public string USER_ID { get; set; }

    [ColumnRequired]
    [ColumnStringLength(100)]
    [Column(Order = 12), Comment("사용자명")]
    [DisplayName("entity.SUser.USER_NAME")]
    public string USER_NAME { get; set; }

    [ColumnStringLength(500)]
    [Column(Order = 13), Comment("비밀번호 해시")]
    [DisplayName("entity.SUser.PASSWORD_HASH")]
    public string? PASSWORD_HASH { get; set; }

    [ColumnStringLength(200)]
    [Column(Order = 14), Comment("이메일")]
    [DisplayName("entity.SUser.EMAIL")]
    public string? EMAIL { get; set; }

    [ColumnStringLength(50)]
    [Column(Order = 15), Comment("전화번호")]
    [DisplayName("entity.SUser.PHONE_NUMBER")]
    public string? PHONE_NUMBER { get; set; }

    [Column(Order = 16), Comment("관리자 여부")]
    [DisplayName("entity.SUser.IS_ADMIN")]
    public bool IS_ADMIN { get; set; }

    [Column(Order = 17), Comment("마지막 로그인 일시")]
    [DisplayName("entity.SUser.LAST_LOGIN_AT")]
    public DateTime? LAST_LOGIN_AT { get; set; }

    public List<SUserRole>? UserRoles { get; set; }
}