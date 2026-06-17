namespace WebFlex.UI.DTO.Device;

public class DeviceTagSaveRequest {
    public long DeviceId { get; set; }
    public List<DeviceTagNodeRequest> Nodes { get; set; } = [];
}

public class DeviceTagNodeRequest {
    public string NodeId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? DataType { get; set; }
}