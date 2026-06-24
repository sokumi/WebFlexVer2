namespace WebFlex.UI.DTO;

public class DeviceTagSaveRequest {
    public string DeviceId { get; set; }
    public List<DeviceTagNodeRequest> Nodes { get; set; } = [];
}

public class DeviceTagNodeRequest {
    public string NodeId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? DataType { get; set; }
}