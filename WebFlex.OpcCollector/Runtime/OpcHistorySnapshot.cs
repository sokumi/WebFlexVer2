namespace WebFlex.OpcCollector.Runtime;

public class OpcHistorySnapshot {
    public DateTime SnapshotTime { get; set; }

    public List<OpcCollectedValue> Values { get; set; } = new();
}
