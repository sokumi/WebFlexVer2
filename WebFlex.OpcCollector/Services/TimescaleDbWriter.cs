using Npgsql;
using NpgsqlTypes;
using WebFlex.OpcCollector.Runtime;

namespace WebFlex.OpcCollector.Services;

public class TimescaleDbWriter : IDisposable {
    private const int DefaultChunkSize = 500;

    private readonly ILogger<TimescaleDbWriter> _logger;
    private readonly OpcCollectorOptionState _optionState;
    private readonly NpgsqlDataSource _dataSource;
    private readonly SemaphoreSlim _saveLock = new(1, 1);

    private long _totalRequestedCount;
    private long _totalInsertedCount;
    private long _totalFailedCount;
    private long _totalCurrentValueUpdatedCount;
    private double _lastSaveMs;
    private DateTime _lastSavedAt = DateTime.MinValue;

    public TimescaleDbWriter(
        IConfiguration configuration,
        OpcCollectorOptionState optionState,
        ILogger<TimescaleDbWriter> logger) {
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

        _logger.LogInformation("Timescale History Writer 시작 | Mode=DirectSnapshot | CurrentValue=InlineUpsert");
    }

    public int QueueCount => 0;

    public long TotalEnqueuedCount => Interlocked.Read(ref _totalRequestedCount);

    public long TotalInsertedCount => Interlocked.Read(ref _totalInsertedCount);

    public long TotalDroppedRowCount => 0;

    public long TotalFailedCount => Interlocked.Read(ref _totalFailedCount);

    public long TotalCurrentValueUpdatedCount => Interlocked.Read(ref _totalCurrentValueUpdatedCount);

    public double LastSaveMs => _lastSaveMs;

    public DateTime LastSavedAt => _lastSavedAt;

    public void Enqueue(OpcCollectedValue value) {
        _logger.LogWarning(
            "TimescaleDbWriter.Enqueue(row) 호출됨. SaveSnapshotAsync만 사용해야 합니다. NodeId={NodeId}",
            value.TagId);
    }

    public async Task<HistorySaveResult> SaveSnapshotAsync(
        OpcHistorySnapshot snapshot,
        CancellationToken cancellationToken) {
        if (snapshot.Values.Count == 0) {
            return HistorySaveResult.Empty;
        }

        var options = _optionState.Current;

        if (!options.EnableTimescaleHistorySave && !options.EnableCurrentValueSave) {
            return HistorySaveResult.Empty;
        }

        await _saveLock.WaitAsync(cancellationToken);

        try {
            Interlocked.Add(ref _totalRequestedCount, snapshot.Values.Count);

            var totalStartedAt = DateTime.UtcNow;
            var historyInserted = 0;
            var currentValueAffected = 0;
            var historyMs = 0d;
            var currentValueMs = 0d;

            await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);

            if (options.EnableTimescaleHistorySave) {
                (historyInserted, historyMs) = await InsertHistoryAsync(conn, snapshot, cancellationToken);
                Interlocked.Add(ref _totalInsertedCount, historyInserted);
            }

            if (options.EnableCurrentValueSave) {
                (currentValueAffected, currentValueMs) = await UpsertCurrentValueAsync(conn, snapshot.Values, cancellationToken);
                Interlocked.Add(ref _totalCurrentValueUpdatedCount, currentValueAffected);
            }

            // 수정: totalMs 변수 올바르게 계산
            var totalMs = (DateTime.UtcNow - totalStartedAt).TotalMilliseconds;

            _lastSaveMs = totalMs;
            _lastSavedAt = DateTime.UtcNow;

            _logger.LogInformation(
                "History Snapshot 저장 완료 | SnapshotTime={SnapshotTime:yyyy-MM-dd HH:mm:ss.fff} | Rows={Rows} | HistoryInserted={HistoryInserted} | CurrentValueAffected={CurrentValueAffected} | TotalMs={TotalMs:N0} | HistoryMs={HistoryMs:N0} | CurrentValueMs={CurrentValueMs:N0}",
                snapshot.SnapshotTime,
                snapshot.Values.Count,
                historyInserted,
                currentValueAffected,
                totalMs,
                historyMs,
                currentValueMs);

            if (totalMs > 1000) {
                _logger.LogWarning(
                    "History Snapshot 저장 지연 | SnapshotTime={SnapshotTime:yyyy-MM-dd HH:mm:ss.fff} | Rows={Rows} | TotalMs={TotalMs:N0}",
                    snapshot.SnapshotTime,
                    snapshot.Values.Count,
                    totalMs);
            }

            return new HistorySaveResult(
                snapshot.Values.Count,
                historyInserted,
                currentValueAffected,
                false,
                totalMs);
        } catch (Exception ex) {
            Interlocked.Add(ref _totalFailedCount, snapshot.Values.Count);

            _logger.LogError(
                ex,
                "Timescale History Snapshot 저장 실패 | SnapshotTime={SnapshotTime:yyyy-MM-dd HH:mm:ss.fff} | Rows={Rows}",
                snapshot.SnapshotTime,
                snapshot.Values.Count);

            return new HistorySaveResult(snapshot.Values.Count, 0, 0, true, 0);
        } finally {
            _saveLock.Release();
        }
    }

    private async Task<(int inserted, double elapsedMs)> InsertHistoryAsync(
        NpgsqlConnection conn,
        OpcHistorySnapshot snapshot,
        CancellationToken cancellationToken) {
        var startedAt = DateTime.UtcNow;
        var inserted = 0;
        var chunkSize = GetChunkSize();

        for (var offset = 0; offset < snapshot.Values.Count; offset += chunkSize) {
            var count = Math.Min(chunkSize, snapshot.Values.Count - offset);
            var chunk = snapshot.Values.Skip(offset).Take(count);

            await using var writer = await conn.BeginBinaryImportAsync(@"
COPY public.timescale
(
    time,
    tag_id,
    group_id,
    value,
    status,
    cookie_value,
    source_timestamp,
    received_at
)
FROM STDIN (FORMAT BINARY)
", cancellationToken);

            foreach (var item in chunk) {
                await writer.StartRowAsync(cancellationToken);

                await writer.WriteAsync(item.Time, NpgsqlDbType.TimestampTz, cancellationToken);
                await writer.WriteAsync(item.TagId, NpgsqlDbType.Text, cancellationToken);

                if (string.IsNullOrWhiteSpace(item.GroupId))
                    await writer.WriteNullAsync(cancellationToken);
                else
                    await writer.WriteAsync(item.GroupId, NpgsqlDbType.Text, cancellationToken);

                if (item.Value == null)
                    await writer.WriteNullAsync(cancellationToken);
                else
                    await writer.WriteAsync(item.Value, NpgsqlDbType.Text, cancellationToken);

                if (item.Status.HasValue)
                    await writer.WriteAsync((int)item.Status.Value, NpgsqlDbType.Integer, cancellationToken);
                else
                    await writer.WriteNullAsync(cancellationToken);

                if (string.IsNullOrWhiteSpace(item.CookieValue))
                    await writer.WriteNullAsync(cancellationToken);
                else
                    await writer.WriteAsync(item.CookieValue, NpgsqlDbType.Text, cancellationToken);

                if (item.SourceTimestamp.HasValue)
                    await writer.WriteAsync(item.SourceTimestamp.Value, NpgsqlDbType.TimestampTz, cancellationToken);
                else
                    await writer.WriteNullAsync(cancellationToken);

                await writer.WriteAsync(item.ReceivedAt, NpgsqlDbType.TimestampTz, cancellationToken);

                inserted++;
            }

            await writer.CompleteAsync(cancellationToken);
        }

        return (inserted, (DateTime.UtcNow - startedAt).TotalMilliseconds);
    }

    private async Task<(int affected, double elapsedMs)> UpsertCurrentValueAsync(
        NpgsqlConnection conn,
        IReadOnlyList<OpcCollectedValue> snapshot,
        CancellationToken cancellationToken) {
        var startedAt = DateTime.UtcNow;
        var totalAffected = 0;
        var chunkSize = GetChunkSize();

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
    update_count,
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
    1,
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
    update_count      = COALESCE(public.currentvalue.update_count, 0) + 1
WHERE public.currentvalue.value IS DISTINCT FROM EXCLUDED.value
   OR public.currentvalue.status IS DISTINCT FROM EXCLUDED.status
   OR public.currentvalue.cookie_value IS DISTINCT FROM EXCLUDED.cookie_value
   OR public.currentvalue.group_id IS DISTINCT FROM EXCLUDED.group_id
   OR public.currentvalue.source_timestamp IS DISTINCT FROM EXCLUDED.source_timestamp
   OR public.currentvalue.received_at IS DISTINCT FROM EXCLUDED.received_at;
", conn);

            cmd.CommandTimeout = 0;

            cmd.Parameters.Add(new NpgsqlParameter("@tag_ids", NpgsqlDbType.Array | NpgsqlDbType.Text) {
                Value = chunk.Select(x => x.TagId).ToArray()
            });

            cmd.Parameters.Add(new NpgsqlParameter("@group_ids", NpgsqlDbType.Array | NpgsqlDbType.Text) {
                Value = chunk.Select(x => string.IsNullOrWhiteSpace(x.GroupId)
                    ? DBNull.Value
                    : (object)x.GroupId).ToArray()
            });

            cmd.Parameters.Add(new NpgsqlParameter("@values", NpgsqlDbType.Array | NpgsqlDbType.Text) {
                Value = chunk.Select(x => x.Value == null
                    ? DBNull.Value
                    : (object)x.Value).ToArray()
            });

            cmd.Parameters.Add(new NpgsqlParameter("@statuses", NpgsqlDbType.Array | NpgsqlDbType.Integer) {
                Value = chunk.Select(x => x.Status.HasValue
                    ? (object)(int)x.Status.Value
                    : DBNull.Value).ToArray()
            });

            cmd.Parameters.Add(new NpgsqlParameter("@cookie_values", NpgsqlDbType.Array | NpgsqlDbType.Text) {
                Value = chunk.Select(x => string.IsNullOrWhiteSpace(x.CookieValue)
                    ? DBNull.Value
                    : (object)x.CookieValue).ToArray()
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

        var elapsedMs = (DateTime.UtcNow - startedAt).TotalMilliseconds;
        return (totalAffected, elapsedMs);
    }

    private int GetChunkSize() {
        var configured = _optionState.Current.MaxBatchSize;
        return Math.Max(100, Math.Min(configured, DefaultChunkSize));
    }

    public void Dispose() {
        _saveLock.Dispose();
        _dataSource.Dispose();
    }
}

public readonly record struct HistorySaveResult(
    int RequestedRows,
    int HistoryInsertedRows,
    int CurrentValueAffectedRows,
    bool HasError,
    double TotalMs) {
    public static HistorySaveResult Empty => new(0, 0, 0, false, 0);
}
