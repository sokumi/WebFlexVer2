using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using WebFlex.Shared.Entities;

namespace WebFlex.Shared;

/// <summary>
/// OPC 대그룹 정보
/// </summary>
[DebuggerDisplay("{Id} {MAJOR_GROUP_CODE} {MAJOR_GROUP_NAME}")]
[Table("opc_major_group"), KeyFieldColumn("MAJOR_GROUP_ID"), Comment("OPC 대그룹 정보")]
public class OpcMajorGroup : BaseEntity {

    [ColumnRequired]
    [ColumnStringLength(100)]
    [Column(Order = 12), Comment("대그룹명")]
    [DisplayName("entity.OpcMajorGroup.MAJOR_GROUP_NAME")]
    public string MAJOR_GROUP_NAME { get; set; }  

    [Column(Order = 13), Comment("정렬 순서")]
    [DisplayName("entity.OpcMajorGroup.SORT_ORDER")]
    public int? SORT_ORDER { get; set; }

    [ColumnStringLength(500)]
    [Column(Order = 14), Comment("설명")]
    [DisplayName("entity.OpcMajorGroup.DESCRIPTION")]
    public string? DESCRIPTION { get; set; }

    public List<OpcGroup>? Groups { get; set; }
}