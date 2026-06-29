using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using WebFlex.Shared.Entities;

namespace WebFlex.Shared.Entities.System;

/// <summary>
/// 공통 번호 채번 정보
/// </summary>
[DebuggerDisplay("{Id} {PREFIX} {DATE_PART} {CURRENT_NO}")]
[Table("s_new_no"), KeyFieldColumn("NO_ID"), Comment("공통 번호 채번 정보")]
public class SNewNo : BaseEntity {

    [ColumnRequired]
    [ColumnStringLength(10)]
    [Column(Order = 11), Comment("번호 접두어")]
    [DisplayName("entity.SNewNo.PREFIX")]
    public string PREFIX { get; set; }

    [ColumnRequired]
    [ColumnStringLength(6)]
    [Column(Order = 12), Comment("날짜")]
    [DisplayName("entity.SNewNo.DATE_PART")]
    public string DATE_PART { get; set; }

    [ColumnRequired]
    [Column(Order = 13), Comment("현재 번호")]
    [DisplayName("entity.SNewNo.CURRENT_NO")]
    public int CURRENT_NO { get; set; }
}