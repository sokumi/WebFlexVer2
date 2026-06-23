using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using WebFlex.Shared.Entities;

namespace WebFlex.Shared.Entities.System;

/// <summary>
/// 시스템 메뉴 정보
/// </summary>
[DebuggerDisplay("{Id} {PARENT_MENU_ID} {MENU_CODE} {MENU_NAME}")]
[Table("s_menu"), KeyFieldColumn("MENU_ID"), Comment("시스템 메뉴 정보")]
public class SMenu : BaseEntity {

    [ColumnStringLength(15)]
    [Column(Order = 11), Comment("상위 메뉴 아이디")]
    [DisplayName("entity.SMenu.PARENT_MENU_ID")]
    public string? PARENT_MENU_ID { get; set; }

    /// <summary>
    /// 상위 메뉴
    /// </summary>
    [ForeignKey("PARENT_MENU_ID")]
    public SMenu? ParentMenu { get; set; }

    [ColumnRequired]
    [ColumnStringLength(50)]
    [Column(Order = 12), Comment("메뉴 코드")]
    [DisplayName("entity.SMenu.MENU_CODE")]
    public string MENU_CODE { get; set; }

    [ColumnRequired]
    [ColumnStringLength(100)]
    [Column(Order = 13), Comment("메뉴명")]
    [DisplayName("entity.SMenu.MENU_NAME")]
    public string MENU_NAME { get; set; }

    [ColumnStringLength(100)]
    [Column(Order = 14), Comment("컨트롤러명")]
    [DisplayName("entity.SMenu.CONTROLLER_NAME")]
    public string? CONTROLLER_NAME { get; set; }

    [ColumnStringLength(100)]
    [Column(Order = 15), Comment("액션명")]
    [DisplayName("entity.SMenu.ACTION_NAME")]
    public string? ACTION_NAME { get; set; }

    [ColumnStringLength(300)]
    [Column(Order = 16), Comment("URL")]
    [DisplayName("entity.SMenu.URL")]
    public string? URL { get; set; }

    [ColumnStringLength(100)]
    [Column(Order = 17), Comment("아이콘")]
    [DisplayName("entity.SMenu.ICON")]
    public string? ICON { get; set; }

    [Column(Order = 18), Comment("정렬 순서")]
    [DisplayName("entity.SMenu.SORT_ORDER")]
    public int? SORT_ORDER { get; set; }

    [Column(Order = 19), Comment("메뉴 표시 여부")]
    [DisplayName("entity.SMenu.SHOW_IN_MENU")]
    public bool SHOW_IN_MENU { get; set; }


    public List<SMenu>? ChildrenMenus { get; set; }
    public List<SRoleMenu>? RoleMenus { get; set; }
}