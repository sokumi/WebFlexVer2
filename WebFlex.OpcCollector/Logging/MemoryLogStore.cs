using System.Collections.Concurrent;

namespace WebFlex.OpcCollector.Logging;

public static class MemoryLogStore {
    private const int MaxCount = 500;

    private static readonly ConcurrentQueue<MemoryLogEntry> Logs = new();

    public static void Add(MemoryLogEntry entry) {
        Logs.Enqueue(entry);

        while (Logs.Count > MaxCount) {
            Logs.TryDequeue(out _);
        }
    }

    public static List<MemoryLogEntry> GetLatest(int count = 100) {
        return Logs
            .Reverse()
            .Take(count)
            .OrderByDescending(x => x.Time)
            .ToList();
    }
}