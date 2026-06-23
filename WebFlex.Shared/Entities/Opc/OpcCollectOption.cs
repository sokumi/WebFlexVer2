using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using WebFlex.Shared.Entities;

namespace WebFlex.Shared;

/// <summary>
/// OPC 수집 옵션
/// </summary>
[DebuggerDisplay("{Id} {OPTION_CODE} {OPTION_NAME}")]
[Table("opc_collect_option"), KeyFieldColumn("COLLECT_OPTION_ID"), Comment("OPC 수집 옵션")]
public class OpcCollectOption : BaseEntity {

    [ColumnRequired]
    [ColumnStringLength(30)]
    [Column(Order = 11), Comment("옵션 코드")]
    [DisplayName("entity.OpcCollectOption.OPTION_CODE")]
    public string OPTION_CODE { get; set; }

    [ColumnRequired]
    [ColumnStringLength(100)]
    [Column(Order = 12), Comment("옵션명")]
    [DisplayName("entity.OpcCollectOption.OPTION_NAME")]
    public string OPTION_NAME { get; set; }

    [Column(Order = 13), Comment("수집 대상 재조회 주기(초)")]
    [DisplayName("entity.OpcCollectOption.RELOAD_INTERVAL_SECONDS")]
    public int? RELOAD_INTERVAL_SECONDS { get; set; } 

    [Column(Order = 14), Comment("저장 주기(ms)")]
    [DisplayName("entity.OpcCollectOption.SAVE_INTERVAL_MILLISECONDS")]
    public int? SAVE_INTERVAL_MILLISECONDS { get; set; }

    [Column(Order = 15), Comment("Flush 주기(ms)")]
    [DisplayName("entity.OpcCollectOption.FLUSH_INTERVAL_MILLISECONDS")]
    public int? FLUSH_INTERVAL_MILLISECONDS { get; set; } 

    [Column(Order = 16), Comment("최대 배치 크기")]
    [DisplayName("entity.OpcCollectOption.MAX_BATCH_SIZE")]
    public int? MAX_BATCH_SIZE { get; set; } 

    [Column(Order = 17), Comment("변경값만 저장 여부")]
    [DisplayName("entity.OpcCollectOption.SAVE_ONLY_CHANGED_VALUE")]
    public bool SAVE_ONLY_CHANGED_VALUE { get; set; }

    [Column(Order = 18), Comment("자동 재연결 여부")]
    [DisplayName("entity.OpcCollectOption.AUTO_RECONNECT")]
    public bool AUTO_RECONNECT { get; set; } 

    [Column(Order = 19), Comment("재연결 주기(초)")]
    [DisplayName("entity.OpcCollectOption.RECONNECT_INTERVAL_SECONDS")]
    public int? RECONNECT_INTERVAL_SECONDS { get; set; }

    [Column(Order = 20), Comment("연결 확인 주기(초)")]
    [DisplayName("entity.OpcCollectOption.CONNECTION_CHECK_INTERVAL_SECONDS")]
    public int? CONNECTION_CHECK_INTERVAL_SECONDS { get; set; } 

    [ColumnStringLength(500)]
    [Column(Order = 21), Comment("설명")]
    [DisplayName("entity.OpcCollectOption.DESCRIPTION")]
    public string? DESCRIPTION { get; set; }
}