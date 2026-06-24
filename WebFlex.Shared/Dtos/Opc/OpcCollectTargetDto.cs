namespace WebFlex.Shared.Dtos.Opc;

public class OpcCollectTargetDto {
    public string DeviceId { get; set; }

    public string DeviceCode { get; set; } = "";

    public string DeviceName { get; set; } = "";

    public string DeviceAddress { get; set; } = "";

    public int Port { get; set; }

    public string EndpointUrl { get; set; } = "";

    public bool UseSecurity { get; set; }

    public string? SecurityPolicy { get; set; }

    public string? SecurityMode { get; set; }

    public bool UseAnonymous { get; set; }

    public string? UserName { get; set; }

    public string? Password { get; set; }

    public int PublishingIntervalMs { get; set; } = 1000;

    public int SamplingIntervalMs { get; set; } = 1000;

    public int QueueSize { get; set; } = 10;

    public List<OpcCollectTargetTagDto> Tags { get; set; } = new();
}

public class OpcCollectTargetTagDto {
    public string TagId { get; set; }

    public string GroupId { get; set; }

    public string TagCode { get; set; } = "";

    public string NodeId { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public int SamplingIntervalMs { get; set; } = 1000;

    public int QueueSize { get; set; } = 10;

    public bool SaveToDatabase { get; set; } = true;
}