using Npgsql;
using NpgsqlTypes;
using WebFlex.OpcCollector.Runtime;

namespace WebFlex.OpcCollector.Services;

public class OpcSnapshotPersistenceService : IDisposable {
    private const int DefaultWriteChunkSize = 200;

    private readonly ILogger<OpcSnapshotPersistenceService> _logger;
    private readonly NpgsqlDataSource _dataSource;
    private readonly OpcCollectorOptionState _optionState;
    private readonly SemaphoreSlim _flushLock = new(1, 1);

    private long _totalSnapshotCount;
    private long _totalHistoryInsertedCount;
    private long _totalCurrentValueUpdatedCount;
    private long _totalSkippedSnapshotCount;
    private double _lastFlushTotalMs;
    private double _lastHistoryMs;
    private double _lastCurrentValueMs;
    private int _lastSnapshotRowCount;
    private DateTime _lastFlushCompletedAt = DateTime.MinValue;

    public OpcSnapshotPersistenceService(
        IConfiguration configuration,
        OpcCollectorOptionState optionState,
        ILogger<OpcSnapshotPersistenceService> logger) {
        _logger = logger;
        _optionState = optionState;

        var rawConnectionString = configuration.GetConnectionString("WebFlexTsd")
            ?? throw new InvalidOperationException("ConnectionStrings:WebFlexTsd 설정이 없습니다.");

        var builder = new NpgsqlConnectionStringBuilder(rawConnectionString) {
            CommandTimeout = 0,
            CancellationTimeout = 0,
            KeepAlive = 30
        };

        _dataSource = NpgsqlDataSource.Create(builder.ConnectionString);
    }

    public long TotalSnapshotCount => Interlocked.Read(ref _totalSnapshotCount);

    public long TotalHistoryInsertedCount => Interlocked.Read(ref _totalHistoryInsertedCount);

    public long TotalCurrentValueUpdatedCount => Interlocked.Read(ref _totalCurrentValueUpdatedCount);

    public long TotalSkippedSnapshotCount => Interlocked.Read(ref _totalSkippedSnapshotCount);

    public double LastFlushTotalMs => Volatile.Read(ref _lastFlushTotalMs);

    public double LastHistoryMs => Volatile.Read(ref _lastHistoryMs);

    public double LastCurrentValueMs => Volatile.Read(ref _lastCurrentValueMs);

    public int LastSnapshotRowCount => Volatile.Read(ref _lastSnapshotRowCount);

    public DateTime LastFlushCompletedAt => _lastFlushCompletedAt;

    public bool IsFlushRunning => _flushLock.CurrentCount == 0;

    public async Task<SnapshotPersistResult> PersistSnapshotAsync(
        IReadOnlyList<OpcCollectedValue> snapshot,
        bool saveHistory,
        bool saveCurrentValue,
        CancellationToken cancellationToken) {
        if (snapshot.Count == 0) {
            return SnapshotPersistResult.Empty;
        }

        if (!await _flushLock.WaitAsync(0, cancellationToken)) {
            Interlocked.Increment(ref _totalSkippedSnapshotCount);

            _logger.LogWarning(
                "Snapshot 저장 건너뜀 | Reason=PreviousFlushRunning | Rows={Rows} | SkippedSnapshots={SkippedSnapshots}",
                snapshot.Count,
                TotalSkippedSnapshotCount);

            return SnapshotPersistResult.CreateSkipped(snapshot.Count);
        }

        try {
            var totalStartedAt = DateTime.UtcNow;
            var historyMs = 0d;
            var currentValueMs = 0d;
            var currentValueAffected = 0;

            await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);

            if (saveHistory) {
                historyMs = await InsertHistoryAsync(conn, snapshot, cancellationToken);
                Interlocked.Add(ref _totalHistoryInsertedCount, snapshot.Count);
            }

            if (saveCurrentValue) {
                (currentValueAffected, currentValueMs) = await UpsertCurrentValueAsync(conn, snapshot, cancellationToken);
            }

            var totalMs = (DateTime.UtcNow - totalStartedAt).TotalMilliseconds;

            Interlocked.Increment(ref _totalSnapshotCount);
            Volatile.Write(ref _lastSnapshotRowCount, snapshot.Count);
            Volatile.Write(ref _lastHistoryMs, historyMs);
            Volatile.Write(ref _lastFlushTotalMs, totalMs);
            _lastFlushCompletedAt = DateTime.UtcNow;

            _logger.LogInformation(
                "Snapshot 저장 완료 | Rows={Rows} | TotalMs={TotalMs:N0} | HistoryMs={HistoryMs:N0} | CurrentValueMs={CurrentValueMs:N0} | CurrentValueAffected={CurrentValueAffected} | TotalSnapshots={TotalSnapshots}",
                snapshot.Count,
                totalMs,
                historyMs,
                currentValueMs,
                currentValueAffected,
                TotalSnapshotCount);

            if (totalMs > 1000) {
                _logger.LogWarning(
                    "Snapshot 저장 지연 | Rows={Rows} | TotalMs={TotalMs:N0}",
                    snapshot.Count,
                    totalMs);
            }

            return new SnapshotPersistResult(
                snapshot.Count,
                saveHistory ? snapshot.Count : 0,
                currentValueAffected,
                false,
                totalMs);
        } catch (Exception ex) {
            _logger.LogError(
                ex,
                "Snapshot 저장 실패 | Rows={Rows}",
                snapshot.Count);

            return new SnapshotPersistResult(
                snapshot.Count,
                0,
                0,
                false,
                0,
                true);
        } finally {
            _flushLock.Release();
        }
    }

    private async Task<double> InsertHistoryAsync(
        NpgsqlConnection conn,
        IReadOnlyList<OpcCollectedValue> snapshot,
        CancellationToken cancellationToken) {
        var startedAt = DateTime.UtcNow;
        var chunkSize = GetWriteChunkSize();

        for (var offset = 0; offset < snapshot.Count; offset += chunkSize) {
            var count = Math.Min(chunkSize, snapshot.Count - offset);
            var chunk = snapshot.Skip(offset).Take(count);

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

            foreach (var item in chunk) {
                await writer.StartRowAsync(cancellationToken);
                await writer.WriteAsync(item.Time, NpgsqlDbType.TimestampTz, cancellationToken);
                await writer.WriteAsync(item.GroupId, NpgsqlDbType.Text, cancellationToken);
                await writer.WriteAsync(item.TagId, NpgsqlDbType.Text, cancellationToken);
                await writer.WriteAsync(item.Value, NpgsqlDbType.Text, cancellationToken);
                if (item.Status.HasValue)
                    await writer.WriteAsync((int)item.Status.Value, NpgsqlDbType.Integer, cancellationToken);
                else
                    await writer.WriteNullAsync(cancellationToken);

                if (item.SourceTimestamp.HasValue) {
                    await writer.WriteAsync(item.SourceTimestamp.Value, NpgsqlDbType.TimestampTz, cancellationToken);
                } else {
                    await writer.WriteNullAsync(cancellationToken);
                }

                await writer.WriteAsync(item.ReceivedAt, NpgsqlDbType.TimestampTz, cancellationToken);
            }

            await writer.CompleteAsync(cancellationToken);
        }

        return (DateTime.UtcNow - startedAt).TotalMilliseconds;
    }

    private async Task<(int affected, double elapsedMs)> UpsertCurrentValueAsync(
        NpgsqlConnection conn,
        IReadOnlyList<OpcCollectedValue> snapshot,
        CancellationToken cancellationToken) {
        var startedAt = DateTime.UtcNow;
        var totalAffected = 0;
        var chunkSize = GetWriteChunkSize();

        for (var offset = 0; offset < snapshot.Count; offset += chunkSize) {
            var count = Math.Min(chunkSize, snapshot.Count - offset);
            var chunk = snapshot.Skip(offset).Take(count).ToArray();

            await using var cmd = new NpgsqlCommand(@"
INSERT INTO public.currentvalue
(
    tag_id,
    group_id,
    value,
    status,
    cookie_value,
    source_timestamp,
    received_at,
    updated_at
)
SELECT
    unnest(@tag_ids),
    unnest(@group_ids),
    unnest(@values),
    unnest(@statuses),
    unnest(@cookie_values),
    unnest(@source_timestamps),
    unnest(@received_ats),
    now()
ON CONFLICT (tag_id)
DO UPDATE SET
    group_id          = EXCLUDED.group_id,
    value             = EXCLUDED.value,
    status            = EXCLUDED.status,
    cookie_value      = EXCLUDED.cookie_value,
    source_timestamp  = EXCLUDED.source_timestamp,
    received_at       = EXCLUDED.received_at,
    updated_at        = now(),
    update_count      = public.currentvalue.update_count + 1
WHERE public.currentvalue.value IS DISTINCT FROM EXCLUDED.value
   OR public.currentvalue.status IS DISTINCT FROM EXCLUDED.status
   OR public.currentvalue.cookie_value IS DISTINCT FROM EXCLUDED.cookie_value
   OR public.currentvalue.group_id IS DISTINCT FROM EXCLUDED.group_id;
", conn);

            cmd.CommandTimeout = 0;

            cmd.Parameters.Add(new NpgsqlParameter("@tag_ids", NpgsqlDbType.Array | NpgsqlDbType.Text) {
                Value = chunk.Select(x => x.TagId).ToArray()
            });

            cmd.Parameters.Add(new NpgsqlParameter("@group_ids", NpgsqlDbType.Array | NpgsqlDbType.Text) {
                Value = chunk.Select(x => (object?)x.GroupId ?? DBNull.Value).ToArray()
            });

            cmd.Parameters.Add(new NpgsqlParameter("@values", NpgsqlDbType.Array | NpgsqlDbType.Text) {
                Value = chunk.Select(x => (object?)x.Value ?? DBNull.Value).ToArray()
            });

            cmd.Parameters.Add(new NpgsqlParameter("@statuses", NpgsqlDbType.Array | NpgsqlDbType.Text) {
                Value = chunk.Select(x => (object?)x.Status ?? DBNull.Value).ToArray()
            });

            cmd.Parameters.Add(new NpgsqlParameter("@cookie_values", NpgsqlDbType.Array | NpgsqlDbType.Text) {
                Value = chunk.Select(x => (object?)x.CookieValue ?? DBNull.Value).ToArray()
            });

            cmd.Parameters.Add(new NpgsqlParameter("@source_timestamps", NpgsqlDbType.Array | NpgsqlDbType.TimestampTz) {
                Value = chunk.Select(x => x.SourceTimestamp.HasValue
                    ? (object)x.SourceTimestamp.Value
                    : DBNull.Value).ToArray()
            });

            cmd.Parameters.Add(new NpgsqlParameter("@received_ats", NpgsqlDbType.Array | NpgsqlDbType.TimestampTz) {
                Value = chunk.Select(x => x.ReceivedAt).ToArray()
            });

            totalAffected += await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        Interlocked.Add(ref _totalCurrentValueUpdatedCount, totalAffected);

        var elapsedMs = (DateTime.UtcNow - startedAt).TotalMilliseconds;
        Volatile.Write(ref _lastCurrentValueMs, elapsedMs);
        return (totalAffected, elapsedMs);
    }

    private int GetWriteChunkSize() {
        var configured = _optionState.Current.MaxBatchSize;
        return Math.Max(50, Math.Min(configured, DefaultWriteChunkSize));
    }

    public void Dispose() {
        _flushLock.Dispose();
        _dataSource.Dispose();
    }
}

public readonly record struct SnapshotPersistResult(
    int RowCount,
    int HistoryInsertedCount,
    int CurrentValueAffectedCount,
    bool Skipped,
    double TotalMs,
    bool HasError = false) {
    public static SnapshotPersistResult Empty => new(0, 0, 0, false, 0, false);

    public static SnapshotPersistResult CreateSkipped(int rowCount) => new(rowCount, 0, 0, true, 0, false);
}
