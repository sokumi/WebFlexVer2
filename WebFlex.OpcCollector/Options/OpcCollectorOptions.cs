namespace WebFlex.OpcCollector.Options;

public class OpcCollectorOptions {
    public int ReloadIntervalSeconds { get; set; } = 10;

    public int SaveIntervalMilliseconds { get; set; } = 1000;

    public int FlushIntervalMilliseconds { get; set; } = 200;

    public int MaxBatchSize { get; set; } = 5000;
}