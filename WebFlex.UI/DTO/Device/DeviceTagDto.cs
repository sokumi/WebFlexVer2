namespace WebFlex.UI.DTO.Device;

public class DeviceTagDto {
    public long Id { get; set; }
    public string TagCode { get; set; } = "";
    public string NodeId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? GroupName { get; set; }
    public string? DataType { get; set; }
    public bool IsCollectEnabled { get; set; }
    public bool SaveToDatabase { get; set; }
    public bool ShowOnDashboard { get; set; }
    public int? SamplingIntervalMs { get; set; }
    public int? QueueSize { get; set; }
    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; }
}