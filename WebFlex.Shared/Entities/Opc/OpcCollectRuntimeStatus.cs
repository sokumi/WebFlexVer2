using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using WebFlex.Shared.Entities;

namespace WebFlex.Shared;

/// <summary>
/// OPC 수집 런타임 상태
/// </summary>
[DebuggerDisplay("{Id} {DEVICE_ID} {ENDPOINT_URL} {IS_CONNECTED}")]
[Table("opc_collect_runtime_status"), KeyFieldColumn("RUNTIME_STATUS_ID"), Comment("OPC 수집 런타임 상태")]
public class OpcCollectRuntimeStatus : BaseEntity {

    [ColumnStringLength(15)]
    [Column(Order = 11), Comment("디바이스 아이디")]
    [DisplayName("entity.OpcCollectRuntimeStatus.DEVICE_ID")]
    public string? DEVICE_ID { get; set; }

    [ForeignKey("DEVICE_ID")]
    public OpcDevice? Device { get; set; }

    [ColumnStringLength(500)]
    [Column(Order = 12), Comment("Endpoint URL")]
    [DisplayName("entity.OpcCollectRuntimeStatus.ENDPOINT_URL")]
    public string? ENDPOINT_URL { get; set; }

    [Column(Order = 13), Comment("연결 여부")]
    [DisplayName("entity.OpcCollectRuntimeStatus.IS_CONNECTED")]
    public bool IS_CONNECTED { get; set; }

    [Column(Order = 14), Comment("구독 태그 수")]
    [DisplayName("entity.OpcCollectRuntimeStatus.SUBSCRIBED_COUNT")]
    public int? SUBSCRIBED_COUNT { get; set; }

    [Column(Order = 15), Comment("마지막 연결 일시")]
    [DisplayName("entity.OpcCollectRuntimeStatus.LAST_CONNECTED_AT")]
    public DateTime? LAST_CONNECTED_AT { get; set; }

    [Column(Order = 16), Comment("마지막 연결 해제 일시")]
    [DisplayName("entity.OpcCollectRuntimeStatus.LAST_DISCONNECTED_AT")]
    public DateTime? LAST_DISCONNECTED_AT { get; set; }

    [Column(Order = 17), Comment("마지막 수신 일시")]
    [DisplayName("entity.OpcCollectRuntimeStatus.LAST_RECEIVED_AT")]
    public DateTime? LAST_RECEIVED_AT { get; set; }

    [ColumnStringLength(1000)]
    [Column(Order = 18), Comment("마지막 오류 메시지")]
    [DisplayName("entity.OpcCollectRuntimeStatus.LAST_ERROR_MESSAGE")]
    public string? LAST_ERROR_MESSAGE { get; set; }

    [Column(Order = 19), Comment("상태 갱신 일시")]
    [DisplayName("entity.OpcCollectRuntimeStatus.STATUS_UPDATED_AT")]
    public DateTime? STATUS_UPDATED_AT { get; set; }
}