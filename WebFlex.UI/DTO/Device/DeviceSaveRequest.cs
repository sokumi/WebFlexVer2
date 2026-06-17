namespace WebFlex.UI.DTO.Device;

public class DeviceSaveRequest {
    public long? Id { get; set; }

    public string DeviceName { get; set; } = "";
    public string DeviceAddress { get; set; } = "";
    public int Port { get; set; }

    public string DeviceType { get; set; } = "OPCUA";
    public string? EndpointUrl { get; set; }

    public bool IsCollectEnabled { get; set; } = true;

    public bool UseSecurity { get; set; }
    public string? SecurityPolicy { get; set; }
    public string? SecurityMode { get; set; }

    public bool UseAnonymous { get; set; } = true;
    public string? UserName { get; set; }
    public string? Password { get; set; }

    public int PublishingIntervalMs { get; set; } = 1000;
    public int SamplingIntervalMs { get; set; } = 1000;
    public int QueueSize { get; set; } = 100;

    public int SortOrder { get; set; }
    public string? Description { get; set; }

    public bool IsEnabled { get; set; } = true;
}