namespace WebFlex.Shared.Dtos.Opc;

public class OpcClientOptionViewDto {
    public OpcClientOptionDto Options { get; set; } = new();
    public List<string> ConfiguredOptionNames { get; set; } = new();
    public List<string> UsedOptionNames { get; set; } = new();
    public bool HasSavedOptions { get; set; }
}