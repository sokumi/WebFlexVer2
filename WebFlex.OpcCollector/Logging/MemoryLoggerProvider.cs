namespace WebFlex.OpcCollector.Logging;

public class MemoryLoggerProvider : ILoggerProvider {
    public ILogger CreateLogger(string categoryName) {
        return new MemoryLogger(categoryName);
    }

    public void Dispose() {
    }
}