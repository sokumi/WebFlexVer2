using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using WebFlex.Shared.Entities;

namespace WebFlex.Shared;

/// <summary>
/// OPC 중그룹 정보
/// </summary>
[DebuggerDisplay("{Id} {MAJOR_GROUP_ID} {GROUP_CODE} {GROUP_NAME}")]
[Table("opc_group"), KeyFieldColumn("GROUP_ID"), Comment("OPC 중그룹 정보")]
public class OpcGroup : BaseEntity {

    [ColumnStringLength(15)]
    [Column(Order = 11), Comment("대그룹 아이디")]
    [DisplayName("entity.OpcGroup.MAJOR_GROUP_ID")]
    public string? MAJOR_GROUP_ID { get; set; }

    [ForeignKey("MAJOR_GROUP_ID")]
    public OpcMajorGroup? MajorGroup { get; set; }

    [ColumnRequired]
    [ColumnStringLength(100)]
    [Column(Order = 13), Comment("그룹명")]
    [DisplayName("entity.OpcGroup.GROUP_NAME")]
    public string GROUP_NAME { get; set; } 

    [Column(Order = 14), Comment("정렬 순서")]
    [DisplayName("entity.OpcGroup.SORT_ORDER")]
    public int? SORT_ORDER { get; set; }

    [ColumnStringLength(500)]
    [Column(Order = 15), Comment("설명")]
    [DisplayName("entity.OpcGroup.DESCRIPTION")]
    public string? DESCRIPTION { get; set; } 

    public List<OpcDevice>? Devices { get; set; }
    public List<OpcTag>? Tags { get; set; }
}