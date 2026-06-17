namespace WebFlex.UI.DTO.Device;

public class DeviceNodeDto {
    public string NodeId { get; set; } = "";
    public string ParentNodeId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string BrowseName { get; set; } = "";
    public string NodeClass { get; set; } = "";
    public string DataType { get; set; } = "";
    public bool HasChildren { get; set; }
}