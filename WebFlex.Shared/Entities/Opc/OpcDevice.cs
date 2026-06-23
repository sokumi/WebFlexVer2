using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using WebFlex.Shared.Entities;

namespace WebFlex.Shared;

/// <summary>
/// 디바이스 정보
/// </summary>
[DebuggerDisplay("{Id} {GROUP_ID} {DEVICE_NAME} {ENDPOINT_URL}")]
[Table("opc_device"), KeyFieldColumn("DEVICE_ID"), Comment("디바이스 정보")]
public class OpcDevice : BaseEntity {

    [ColumnStringLength(15)]
    [Column(Order = 11), Comment("중그룹 아이디")]
    [DisplayName("entity.OpcDevice.GROUP_ID")]
    public string? GROUP_ID { get; set; }

    /// <summary>
    /// 중그룹
    /// </summary>
    [ForeignKey("GROUP_ID")]
    public OpcGroup? Group { get; set; }

    [ColumnRequired]
    [ColumnStringLength(100)]
    [Column(Order = 12), Comment("디바이스명")]
    [DisplayName("entity.OpcDevice.DEVICE_NAME")]
    public string DEVICE_NAME { get; set; }

    [ColumnStringLength(100)]
    [Column(Order = 13), Comment("디바이스 주소")]
    [DisplayName("entity.OpcDevice.DEVICE_ADDRESS")]
    public string? DEVICE_ADDRESS { get; set; }

    [Column(Order = 14), Comment("포트")]
    [DisplayName("entity.OpcDevice.PORT")]
    public int? PORT { get; set; }

    [ColumnRequired]
    [ColumnStringLength(500)]
    [Column(Order = 15), Comment("Endpoint URL")]
    [DisplayName("entity.OpcDevice.ENDPOINT_URL")]
    public string ENDPOINT_URL { get; set; }

    [ColumnStringLength(30)]
    [Column(Order = 16), Comment("디바이스 타입")]
    [DisplayName("entity.OpcDevice.DEVICE_TYPE")]
    public string? DEVICE_TYPE { get; set; }

    [Column(Order = 17), Comment("수집 사용 여부")]
    [DisplayName("entity.OpcDevice.IS_COLLECTENABLED")]
    public bool IS_COLLECTENABLED { get; set; } = true;

    [Column(Order = 30), Comment("익명 접속 사용 여부")]
    [DisplayName("entity.OpcDevice.USE_ANONYMOUS")]
    public bool USE_ANONYMOUS { get; set; } = true;

    [Column(Order = 31), Comment("Publishing Interval(ms)")]
    [DisplayName("entity.OpcDevice.PUBLISHINGINTERVALMS")]
    public int? PUBLISHINGINTERVALMS { get; set; }

    [Column(Order = 32), Comment("Sampling Interval(ms)")]
    [DisplayName("entity.OpcDevice.SAMPLINGINTERVALMS")]
    public int? SAMPLINGINTERVALMS { get; set; }

    [ColumnStringLength(500)]
    [Column(Order = 33), Comment("설명")]
    [DisplayName("entity.OpcDevice.DESCRIPTION")]
    public string? DESCRIPTION { get; set; }

    [Column(Order = 34), Comment("보안 사용 여부")]
    [DisplayName("entity.OpcDevice.USESECURITY")]
    public bool USESECURITY { get; set; }

    [ColumnStringLength(50)]
    [Column(Order = 35), Comment("보안 모드")]
    [DisplayName("entity.OpcDevice.SECURITYMODE")]
    public string? SECURITYMODE { get; set; }

    [ColumnStringLength(100)]
    [Column(Order = 36), Comment("보안 정책")]
    [DisplayName("entity.OpcDevice.SECURITYPOLICY")]
    public string? SECURITYPOLICY { get; set; }

    [ColumnStringLength(100)]
    [Column(Order = 37), Comment("OPC 서버 접속 사용자명")]
    [DisplayName("entity.OpcDevice.USER_NAME")]
    public string? USER_NAME { get; set; }

    [ColumnStringLength(500)]
    [Column(Order = 38), Comment("OPC 서버 접속 비밀번호")]
    [DisplayName("entity.OpcDevice.PASSWORD")]
    public string? PASSWORD { get; set; }

    public List<OpcTag>? Tags { get; set; }
}