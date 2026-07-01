using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using WebFlex.Shared.Entities;

namespace WebFlex.Shared;

[DebuggerDisplay("{Id} {TAG_ID} {STATE} {MATCH_TYPE}")]
[Table("opc_option_card"), KeyFieldColumn("CARD_ID"), Comment("OPC 카드 대시보드 태그 표시 옵션")]
public class OpcCardOption : BaseEntity {

    [ColumnRequired]
    [ColumnStringLength(15)]
    [Column(Order = 11), Comment("태그 아이디")]
    [DisplayName("entity.OpcCardOption.TAG_ID")]
    public string TAG_ID { get; set; }

    [ForeignKey("TAG_ID")]
    public OpcTag? Tag { get; set; }

    [ColumnRequired]
    [ColumnStringLength(20)]
    [Column(Order = 12), Comment("표시 상태")]
    [DisplayName("entity.OpcCardOption.STATE")]
    public string STATE { get; set; }

    [ColumnRequired]
    [ColumnStringLength(30)]
    [Column(Order = 13), Comment("조건 타입")]
    [DisplayName("entity.OpcCardOption.MATCH_TYPE")]
    public string MATCH_TYPE { get; set; }

    [ColumnStringLength(200)]
    [Column(Order = 14), Comment("문자 조건값")]
    [DisplayName("entity.OpcCardOption.TEXT_VALUE")]
    public string? TEXT_VALUE { get; set; }

    [Column(Order = 15), Comment("최소값")]
    [DisplayName("entity.OpcCardOption.MIN_VALUE")]
    public decimal? MIN_VALUE { get; set; }

    [Column(Order = 16), Comment("최대값")]
    [DisplayName("entity.OpcCardOption.MAX_VALUE")]
    public decimal? MAX_VALUE { get; set; }

    [Column(Order = 17), Comment("정렬 순서")]
    [DisplayName("entity.OpcCardOption.SORT_ORDER")]
    public int? SORT_ORDER { get; set; }

    [ColumnStringLength(500)]
    [Column(Order = 18), Comment("설명")]
    [DisplayName("entity.OpcCardOption.DESCRIPTION")]
    public string? DESCRIPTION { get; set; }
}