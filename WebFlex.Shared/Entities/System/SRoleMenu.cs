using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using WebFlex.Shared.Entities;

namespace WebFlex.Shared.Entities.System;

/// <summary>
/// 시스템 권한별 메뉴 정보
/// </summary>
[DebuggerDisplay("{Id} {ROLE_ID} {MENU_ID}")]
[Table("s_role_menu"), KeyFieldColumn("ROLE_MENU_ID"), Comment("시스템 권한별 메뉴 정보")]
public class SRoleMenu : BaseEntity {

    [ColumnRequired]
    [ColumnStringLength(15)]
    [Column(Order = 11), Comment("권한 아이디")]
    [DisplayName("entity.SRoleMenu.ROLE_ID")]
    public string ROLE_ID { get; set; }

    [ForeignKey("ROLE_ID")]
    public SRole? Role { get; set; }

    [ColumnRequired]
    [ColumnStringLength(15)]
    [Column(Order = 12), Comment("메뉴 아이디")]
    [DisplayName("entity.SRoleMenu.MENU_ID")]
    public string MENU_ID { get; set; }

    [ForeignKey("MENU_ID")]
    public SMenu? Menu { get; set; }

    [Column(Order = 13), Comment("조회 권한")]
    [DisplayName("entity.SRoleMenu.CAN_READ")]
    public bool CAN_READ { get; set; }

    [Column(Order = 14), Comment("생성 권한")]
    [DisplayName("entity.SRoleMenu.CAN_CREATE")]
    public bool CAN_CREATE { get; set; }

    [Column(Order = 15), Comment("수정 권한")]
    [DisplayName("entity.SRoleMenu.CAN_UPDATE")]
    public bool CAN_UPDATE { get; set; }

    [Column(Order = 16), Comment("삭제 권한")]
    [DisplayName("entity.SRoleMenu.CAN_DELETE")]
    public bool CAN_DELETE { get; set; }

    [Column(Order = 17), Comment("내보내기 권한")]
    [DisplayName("entity.SRoleMenu.CAN_EXPORT")]
    public bool CAN_EXPORT { get; set; }
}