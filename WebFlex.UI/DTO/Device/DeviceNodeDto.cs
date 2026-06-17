public class DeviceNodeDto {
    public string NodeId { get; set; }
    public string ParentNodeId { get; set; }
    public string DisplayName { get; set; }
    public string BrowseName { get; set; }
    public string NodeClass { get; set; }
    public string DataType { get; set; }
    public bool HasChildren { get; set; }
    public bool IsCollectable { get; set; }

    // KEPServer 속성
    public string Description { get; set; } = "";
    public string AccessLevel { get; set; } = "";
    public string EngineeringUnit { get; set; } = "";
}