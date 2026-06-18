namespace WebFlex.OpcCollector.Logging;

public class MemoryLogger : ILogger {
    private readonly string _categoryName;

    public MemoryLogger(string categoryName) {
        _categoryName = categoryName;
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull {
        return NullScope.Instance;
    }

    public bool IsEnabled(LogLevel logLevel) {
        return logLevel >= LogLevel.Information;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter) {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);

        if (exception != null) {
            message += Environment.NewLine + exception.Message;
        }

        MemoryLogStore.Add(new MemoryLogEntry {
            Time = DateTime.Now,
            Level = logLevel.ToString(),
            Category = _categoryName,
            Message = message
        });
    }

    private sealed class NullScope : IDisposable {
        public static readonly NullScope Instance = new();

        public void Dispose() {
        }
    }
}