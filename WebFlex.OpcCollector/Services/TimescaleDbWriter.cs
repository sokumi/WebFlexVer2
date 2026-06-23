using Npgsql;
using NpgsqlTypes;
using WebFlex.OpcCollector.Runtime;

namespace WebFlex.OpcCollector.Services;

public class TimescaleDbWriter : IDisposable {
    private readonly ILogger<TimescaleDbWriter> _logger;
    private readonly string _connectionString;
    private readonly OpcCollectorOptionState _optionState;
    private readonly SemaphoreSlim _saveLock = new(1, 1);

    private long _totalRequestedCount;
    private long _totalInsertedCount;
    private long _totalFailedCount;
    private double _lastSaveMs;
    private DateTime _lastSavedAt = DateTime.MinValue;

    public TimescaleDbWriter(
        IConfiguration configuration,
        OpcCollectorOptionState optionState,
        ILogger<TimescaleDbWriter> logger) {
        _logger = logger;
        _optionState = optionState;

        _connectionString = configuration.GetConnectionString("WebFlexTsd")
            ?? throw new InvalidOperationException("ConnectionStrings:WebFlexTsd 설정이 없습니다.");

        _logger.LogInformation("Timescale History Writer 시작 | Mode=DirectSnapshot | Queue=Disabled");
    }

    // WebFlex 구조로 변경하면서 큐를 제거했다.
    // 기존 상태 API 호환을 위해 0을 반환한다.
    public int QueueCount => 0;

    public long TotalEnqueuedCount => Interlocked.Read(ref _totalRequestedCount);

    public long TotalInsertedCount => Interlocked.Read(ref _totalInsertedCount);

    public long TotalDroppedRowCount => 0;

    public long TotalFailedCount => Interlocked.Read(ref _totalFailedCount);

    public double LastSaveMs => _lastSaveMs;

    public DateTime LastSavedAt => _lastSavedAt;

    // 예전 row 단위 enqueue 호출이 남아 있으면 바로 확인할 수 있게 로그만 남긴다.
    // 정상 구조에서는 호출되면 안 된다.
    public void Enqueue(OpcCollectedValue value) {
        _logger.LogWarning(
            "TimescaleDbWriter.Enqueue(row) 호출됨. WebFlex식 구조에서는 SaveSnapshotAsync만 사용해야 합니다. NodeId={NodeId}",
            value.NodeId);
    }

    public async Task<int> SaveSnapshotAsync(
        OpcHistorySnapshot snapshot,
        CancellationToken cancellationToken) {
        if (snapshot.Values.Count == 0)
            return 0;

        var options = _optionState.Current;

        if (!options.EnableTimescaleHistorySave)
            return 0;

        await _saveLock.WaitAsync(cancellationToken);

        try {
            Interlocked.Add(ref _totalRequestedCount, snapshot.Values.Count);

            var totalStartedAt = DateTime.UtcNow;

            var result = await BulkInsertTimescaleAsync(snapshot.Values, cancellationToken);

            var totalMs = (DateTime.UtcNow - totalStartedAt).TotalMilliseconds;

            _lastSaveMs = totalMs;
            _lastSavedAt = DateTime.UtcNow;

            Interlocked.Add(ref _totalInsertedCount, snapshot.Values.Count);

            _logger.LogInformation(
                "History Snapshot 저장 완료 | SnapshotTime={SnapshotTime:yyyy-MM-dd HH:mm:ss.fff} | Rows={Rows} | TotalMs={TotalMs:N0} | CopyTotalMs={CopyTotalMs:N0} | OpenMs={OpenMs:N0} | BeginCopyMs={BeginCopyMs:N0} | WriteRowsMs={WriteRowsMs:N0} | CompleteMs={CompleteMs:N0} | InsertedRows={InsertedRows}",
                snapshot.SnapshotTime,
                snapshot.Values.Count,
                totalMs,
                result.copyTotalMs,
                result.openMs,
                result.beginCopyMs,
                result.writeRowsMs,
                result.completeMs,
                TotalInsertedCount);

            if (totalMs > 1000) {
                _logger.LogWarning(
                    "History Snapshot 저장 지연 | SnapshotTime={SnapshotTime:yyyy-MM-dd HH:mm:ss.fff} | Rows={Rows} | TotalMs={TotalMs:N0}",
                    snapshot.SnapshotTime,
                    snapshot.Values.Count,
                    totalMs);
            }

            return snapshot.Values.Count;
        } catch (Exception ex) {
            Interlocked.Add(ref _totalFailedCount, snapshot.Values.Count);

            _logger.LogError(
                ex,
                "Timescale History Snapshot 저장 실패 | SnapshotTime={SnapshotTime:yyyy-MM-dd HH:mm:ss.fff} | Rows={Rows} | FailedRows={FailedRows}",
                snapshot.SnapshotTime,
                snapshot.Values.Count,
                TotalFailedCount);

            throw;
        } finally {
            _saveLock.Release();
        }
    }

    private async Task<(double copyTotalMs, double openMs, double beginCopyMs, double writeRowsMs, double completeMs)> BulkInsertTimescaleAsync(
        IReadOnlyList<OpcCollectedValue> batch,
        CancellationToken cancellationToken) {
        var copyStartedAt = DateTime.UtcNow;

        var openStartedAt = DateTime.UtcNow;
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        var openMs = (DateTime.UtcNow - openStartedAt).TotalMilliseconds;

        var beginCopyStartedAt = DateTime.UtcNow;
        await using var writer = await conn.BeginBinaryImportAsync(@"
COPY public.timescale
(
    time,
    endpoint_url,
    node_id,
    value,
    status,
    source_timestamp,
    received_at
)
FROM STDIN (FORMAT BINARY)
", cancellationToken);
        var beginCopyMs = (DateTime.UtcNow - beginCopyStartedAt).TotalMilliseconds;

        var writeRowsStartedAt = DateTime.UtcNow;

        foreach (var item in batch) {
            await writer.StartRowAsync(cancellationToken);

            await writer.WriteAsync(item.Time, NpgsqlDbType.TimestampTz, cancellationToken);
            await writer.WriteAsync(item.EndpointUrl, NpgsqlDbType.Text, cancellationToken);
            await writer.WriteAsync(item.NodeId, NpgsqlDbType.Text, cancellationToken);
            await writer.WriteAsync(item.Value, NpgsqlDbType.Text, cancellationToken);
            await writer.WriteAsync(item.Status, NpgsqlDbType.Text, cancellationToken);

            if (item.SourceTimestamp.HasValue) {
                await writer.WriteAsync(item.SourceTimestamp.Value, NpgsqlDbType.TimestampTz, cancellationToken);
            } else {
                await writer.WriteNullAsync(cancellationToken);
            }

            await writer.WriteAsync(item.ReceivedAt, NpgsqlDbType.TimestampTz, cancellationToken);
        }

        var writeRowsMs = (DateTime.UtcNow - writeRowsStartedAt).TotalMilliseconds;

        var completeStartedAt = DateTime.UtcNow;
        await writer.CompleteAsync(cancellationToken);
        var completeMs = (DateTime.UtcNow - completeStartedAt).TotalMilliseconds;

        var copyTotalMs = (DateTime.UtcNow - copyStartedAt).TotalMilliseconds;

        return (copyTotalMs, openMs, beginCopyMs, writeRowsMs, completeMs);
    }

    public void Dispose() {
        _saveLock.Dispose();
    }
}
