using WebFlex.Shared.Entities;

namespace WebFlex.Shared.Entities.Opc;

public class OpcTag : BaseEntity {
    public long OpcDeviceId { get; set; }

    public OpcDevice? Device { get; set; }

    public long? OpcGroupId { get; set; }

    public OpcGroup? Group { get; set; }

    public string TagCode { get; set; } = "";

    public string NodeId { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string? GroupName { get; set; }

    public string? DataType { get; set; }

    public bool IsCollectEnabled { get; set; } = true;

    public bool SaveToDatabase { get; set; } = true;

    public bool ShowOnDashboard { get; set; } = true;

    public int? SamplingIntervalMs { get; set; }

    public int? QueueSize { get; set; }

    public int SortOrder { get; set; }

    public string? Description { get; set; }
}