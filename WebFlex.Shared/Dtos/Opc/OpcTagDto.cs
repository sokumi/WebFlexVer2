namespace WebFlex.Shared.Dtos.Opc;

public class OpcTagDto {
    public long Id { get; set; }

    public long OpcDeviceId { get; set; }

    public long? OpcGroupId { get; set; }

    public string TagCode { get; set; } = "";

    public string NodeId { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string? GroupName { get; set; }

    public string? DataType { get; set; }

    public bool IsEnabled { get; set; }

    public bool IsCollectEnabled { get; set; }

    public bool SaveToDatabase { get; set; }

    public bool ShowOnDashboard { get; set; }

    public int? SamplingIntervalMs { get; set; }

    public int? QueueSize { get; set; }

    public string? Description { get; set; }
}