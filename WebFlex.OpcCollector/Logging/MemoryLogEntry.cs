namespace WebFlex.OpcCollector.Logging;

public class MemoryLogEntry {
    public DateTime Time { get; set; }
    public string Level { get; set; } = "";
    public string Category { get; set; } = "";
    public string Message { get; set; } = "";
}