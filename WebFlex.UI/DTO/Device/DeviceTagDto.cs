namespace WebFlex.UI.DTO.Device;

public class DeviceTagDto {
    public string Id { get; set; }
    public string TagName { get; set; } = "";
    public string? GroupId { get; set; }
    public string? DataType { get; set; }
    public bool IsCollectEnabled { get; set; }
    public bool SaveToDatabase { get; set; }
    public bool ShowOnDashboard { get; set; }
    public int? SamplingIntervalMs { get; set; }
    public int? SortOrder { get; set; }
    public bool IsEnabled { get; set; }
}