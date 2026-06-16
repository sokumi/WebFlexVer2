using WebFlex.Shared.Entities;

namespace WebFlex.Shared.Entities.Opc;

public class OpcDevice : BaseEntity {
    public long? OpcGroupId { get; set; }

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