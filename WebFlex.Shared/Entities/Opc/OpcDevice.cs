using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using WebFlex.Shared.Entities;

namespace WebFlex.Shared;

/// <summary>
/// 디바이스 정보
/// </summary>
[DebuggerDisplay("{ID} {PARENT_ID} {DEVICE_NAME} {DESCRIPTION}")]
[Table("OPC_DEVICE"), KeyFieldColumn("DEVICE_ID"), Comment("디바이스 정보")]
public class OpcDevice : BaseEntity {

    [ColumnRequired]
    [ColumnStringLength(30)]
    [Column(Order = 10), Comment("디바이스명")]
    [DisplayName("entity.Device.DEVICE_NAME")]
    public string DEVICE_NAME { get; set; }

    [ColumnRequired]
    [ColumnStringLength(10)]
    [Column(Order = 11), Comment("중그룹 아이디")]
    [DisplayName("entity.MetrieField.GROUP_ID")]
    public string? GROUP_ID { get; set; }

    /// <summary>
    /// 메트리 그룹u
    /// </summary>
    [ForeignKey("GROUP_ID")]
    public OpcGroup? Group { get; set; }

    [ColumnRequired]
    [ColumnStringLength(30)]
    [Column(Order = 10), Comment("디바이스명")]
    [DisplayName("entity.Device.DEVICE_NAME")]
    public string DEVICE_NAME { get; set; }

    [ColumnRequired]
    [ColumnStringLength(30)]
    [Column(Order = 10), Comment("디바이스명")]
    [DisplayName("entity.Device.DEVICE_NAME")]
    public string DEVICE_NAME { get; set; }

    [ColumnRequired]
    [ColumnStringLength(30)]
    [Column(Order = 10), Comment("디바이스명")]
    [DisplayName("entity.Device.DEVICE_NAME")]
    public string DEVICE_NAME { get; set; }

    [ColumnRequired]
    [ColumnStringLength(30)]
    [Column(Order = 10), Comment("디바이스명")]
    [DisplayName("entity.Device.DEVICE_NAME")]
    public string DEVICE_NAME { get; set; }

    [ColumnRequired]
    [ColumnStringLength(30)]
    [Column(Order = 10), Comment("디바이스명")]
    [DisplayName("entity.Device.DEVICE_NAME")]
    public string DEVICE_NAME { get; set; }

    [ColumnRequired]
    [ColumnStringLength(30)]
    [Column(Order = 10), Comment("디바이스명")]
    [DisplayName("entity.Device.DEVICE_NAME")]
    public string DEVICE_NAME { get; set; }









    public string? OpcGroupId { get; set; }

    public OpcGroup? Group { get; set; }

    public string DeviceCode { get; set; } = "";

    public string DeviceName { get; set; } = "";

    public string DeviceAddress { get; set; } = "";

    public int Port { get; set; }

    public string EndpointUrl { get; set; } = "";

    public string DeviceType { get; set; } = "OPC_UA";

    public bool IsCollectEnabled { get; set; } = true;

    public bool UseSecurity { get; set; }

    public string? SecurityPolicy { get; set; }

    public string? SecurityMode { get; set; }

    public bool UseAnonymous { get; set; } = true;

    public string? UserName { get; set; }

    public string? Password { get; set; }

    public int PublishingIntervalMs { get; set; } = 1000;

    public int SamplingIntervalMs { get; set; } = 1000;

    public int QueueSize { get; set; } = 10;

    public int SortOrder { get; set; }

    public string? Description { get; set; }

    public ICollection<OpcTag> Tags { get; set; } = new List<OpcTag>();
}