using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using WebFlex.Shared.Entities;

namespace WebFlex.Shared;

/// <summary>
/// OPC 태그 정보
/// </summary>
[DebuggerDisplay("{Id} {DEVICE_ID} {GROUP_ID} {TAG_NAME}")]
[Table("opc_tag"), KeyFieldColumn("TAG_ID"), Comment("OPC 태그 정보")]
public class OpcTag : BaseEntity {

    [ColumnRequired]
    [ColumnStringLength(15)]
    [Column(Order = 11), Comment("디바이스 아이디")]
    [DisplayName("entity.OpcTag.DEVICE_ID")]
    public string DEVICE_ID { get; set; }

    [ForeignKey("DEVICE_ID")]
    public OpcDevice? Device { get; set; }

    [ColumnRequired]
    [ColumnStringLength(100)]
    [Column(Order = 12), Comment("노드 아이디")]
    [DisplayName("entity.OpcTag.NODE_ID")]
    public string NODE_ID { get; set; }

    [ColumnStringLength(15)]
    [Column(Order = 13), Comment("중그룹 아이디")]
    [DisplayName("entity.OpcTag.GROUP_ID")]
    public string? GROUP_ID { get; set; }

    [ForeignKey("GROUP_ID")]
    public OpcGroup? Group { get; set; }

    [ColumnStringLength(200)]
    [Column(Order = 15), Comment("표시명")]
    [DisplayName("entity.OpcTag.TAG_NAME")]
    public string? TAG_NAME { get; set; }

    [ColumnStringLength(100)]
    [Column(Order = 17), Comment("데이터 타입")]
    [DisplayName("entity.OpcTag.DATA_TYPE")]
    public string? DATA_TYPE { get; set; }

    [ColumnStringLength(20)]
    [Column(Order = 18), Comment("권한 타입")]
    [DisplayName("entity.OpcTag.PROTECT_TYPE")]
    public string? PROTECT_TYPE { get; set; }

    [Column(Order = 19), Comment("계산식")]
    [DisplayName("entity.OpcTag.expressions")]
    public string? EXPRESSIONS { get; set; }

    [Column(Order = 30), Comment("수집 사용 여부")]
    [DisplayName("entity.OpcTag.IS_COLLECTENABLED")]
    public bool IS_COLLECTENABLED { get; set; }

    [Column(Order = 31), Comment("DB 저장 여부")]
    [DisplayName("entity.OpcTag.SAVE_TO_DATABASE")]
    public bool SAVE_TO_DATABASE { get; set; }

    [Column(Order = 32), Comment("대시보드 표시 여부")]
    [DisplayName("entity.OpcTag.SHOW_ON_DASHBOARD")]
    public bool SHOW_ON_DASHBOARD { get; set; }

    [Column(Order = 33), Comment("Sampling Interval(ms)")]
    [DisplayName("entity.OpcTag.SAMPLINGINTERVALMS")]
    public int? SAMPLINGINTERVALMS { get; set; }

    [Column(Order = 34), Comment("정렬 순서")]
    [DisplayName("entity.OpcTag.SORT_ORDER")]
    public int? SORT_ORDER { get; set; }

    [ColumnStringLength(500)]
    [Column(Order = 35), Comment("설명")]
    [DisplayName("entity.OpcTag.DESCRIPTION")]
    public string? DESCRIPTION { get; set; }
}