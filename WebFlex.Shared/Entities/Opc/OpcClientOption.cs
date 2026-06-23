namespace WebFlex.Shared;

public class OpcClientOption {
    public long Id { get; set; }

    public string OptionCode { get; set; } = "";
    public string OptionName { get; set; } = "";
    public string OptionJson { get; set; } = "";
    public string? ConfiguredOptionNames { get; set; }

    public string? Description { get; set; }
    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}