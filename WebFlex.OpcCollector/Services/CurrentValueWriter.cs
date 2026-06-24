using System.Threading.Channels;
using Npgsql;
using NpgsqlTypes;
using WebFlex.OpcCollector.Runtime;

namespace WebFlex.OpcCollector.Services;

public class CurrentValueWriter : IDisposable {
    private const int CurrentValueChannelCapacity = 50000;
    private const int FlushIntervalMilliseconds = 1000;
    private const int MaxDrainCount = 100000;

    private readonly Channel<OpcCollectedValue> _currentValueChannel;
    private readonly ILogger<CurrentValueWriter> _logger;
    private readonly string _connectionString;
    private readonly OpcCollectorOptionState _optionState;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _workerTask;

    private DateTime _lastLogAt = DateTime.MinValue;

    private long _queueCount;
    private long _totalEnqueuedCount;
    private long _totalUpdatedCount;
    private long _totalDroppedRowCount;

    public CurrentValueWriter(
        IConfiguration configuration,
        OpcCollectorOptionState optionState,
        ILogger<CurrentValueWriter> logger) {
        _logger = logger;
        _optionState = optionState;

        _connectionString = configuration.GetConnectionString("WebFlexTsd")
            ?? throw new InvalidOperationException("ConnectionStrings:WebFlexTsd 설정이 없습니다.");

        _currentValueChannel = Channel.CreateBounded<OpcCollectedValue>(
            new BoundedChannelOptions(CurrentValueChannelCapacity) {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            });

        _workerTask = Task.Run(ProcessCurrentValueQueueAsync);
    }

    public int QueueCount => (int)Math.Max(0, Interlocked.Read(ref _queueCount));

    public long TotalEnqueuedCount => Interlocked.Read(ref _totalEnqueuedCount);

    public long TotalUpdatedCount => Interlocked.Read(ref _totalUpdatedCount);

    public long TotalDroppedRowCount => Interlocked.Read(ref _totalDroppedRowCount);

    public void Enqueue(OpcCollectedValue value) {
        var beforeCount = Interlocked.Read(ref _queueCount);

        if (beforeCount >= CurrentValueChannelCapacity) {
            Interlocked.Increment(ref _totalDroppedRowCount);
        }

        var written = _currentValueChannel.Writer.TryWrite(value);

        if (!written) {
            Interlocked.Increment(ref _totalDroppedRowCount);

            _logger.LogWarning(
                "CurrentValue 채널 입력 실패 | QueueRemain={QueueRemain}",
                QueueCount);

            return;
        }

        if (beforeCount < CurrentValueChannelCapacity) {
            Interlocked.Increment(ref _queueCount);
        }

        Interlocked.Increment(ref _totalEnqueuedCount);
    }

    private async Task ProcessCurrentValueQueueAsync() {
        while (!_cts.Token.IsCancellationRequested) {
            try {
                await Task.Delay(FlushIntervalMilliseconds, _cts.Token);

                await FlushOnceAsync(_cts.Token);
            } catch (OperationCanceledException) {
                break;
            } catch (Exception ex) {
                _logger.LogError(ex, "CurrentValue Writer 처리 오류");
            }
        }

        try {
            await FlushOnceAsync(CancellationToken.None);
        } catch (Exception ex) {
            _logger.LogError(ex, "CurrentValue Writer 종료 전 Flush 실패");
        }
    }

    private async Task FlushOnceAsync(CancellationToken cancellationToken) {
        //var options = _optionState.Current;
        var options = _optionState.Current;

        if (!options.EnableCurrentValueSave)
            return;

        // 같은 endpoint/node가 여러 번 들어오면 마지막 값만 남긴다.
        // currentvalue는 최신값 테이블이므로 중간값을 모두 DB에 update할 필요가 없다.
        var latestMap = new Dictionary<string, OpcCollectedValue>(StringComparer.Ordinal);

        var drained = 0;

        while (drained < MaxDrainCount &&
               _currentValueChannel.Reader.TryRead(out var item)) {
            Interlocked.Decrement(ref _queueCount);

            var key = item.TagId;
            latestMap[key] = item;

            drained++;
        }

        if (latestMap.Count == 0)
            return;

        await UpsertCurrentValueAsync(latestMap.Values.ToList(), cancellationToken);
    }

    private async Task UpsertCurrentValueAsync(
      IReadOnlyList<OpcCollectedValue> batch,
      CancellationToken cancellationToken) {
        if (batch.Count == 0)
            return;

        var totalStartedAt = DateTime.UtcNow;

        double openMs = 0;
        double executeMs = 0;

        await using var conn = new NpgsqlConnection(_connectionString);

        var openStartedAt = DateTime.UtcNow;
        await conn.OpenAsync(cancellationToken);
        openMs = (DateTime.UtcNow - openStartedAt).TotalMilliseconds;

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
   OR public.currentvalue.cookie_value IS DISTINCT FROM EXCLUDED.cookie_value;
", conn);

        cmd.Parameters.Add(new NpgsqlParameter("@tag_ids", NpgsqlDbType.Array | NpgsqlDbType.Text) {
            Value = batch.Select(x => x.TagId).ToArray()
        });

        cmd.Parameters.Add(new NpgsqlParameter("@group_ids", NpgsqlDbType.Array | NpgsqlDbType.Text) {
            Value = batch.Select(x => (object?)x.GroupId ?? DBNull.Value).ToArray()
        });

        cmd.Parameters.Add(new NpgsqlParameter("@values", NpgsqlDbType.Array | NpgsqlDbType.Text) {
            Value = batch.Select(x => (object?)x.Value ?? DBNull.Value).ToArray()
        });

        cmd.Parameters.Add(new NpgsqlParameter("@statuses", NpgsqlDbType.Array | NpgsqlDbType.Text) {
            Value = batch.Select(x => (object?)x.Status ?? DBNull.Value).ToArray()
        });

        cmd.Parameters.Add(new NpgsqlParameter("@cookie_values", NpgsqlDbType.Array | NpgsqlDbType.Text) {
            Value = batch.Select(x => (object?)x.CookieValue ?? DBNull.Value).ToArray()
        });

        cmd.Parameters.Add(new NpgsqlParameter("@source_timestamps", NpgsqlDbType.Array | NpgsqlDbType.TimestampTz) {
            Value = batch.Select(x => x.SourceTimestamp.HasValue
                ? (object)x.SourceTimestamp.Value
                : DBNull.Value).ToArray()
        });

        cmd.Parameters.Add(new NpgsqlParameter("@received_ats", NpgsqlDbType.Array | NpgsqlDbType.TimestampTz) {
            Value = batch.Select(x => x.ReceivedAt).ToArray()
        });

        var executeStartedAt = DateTime.UtcNow;
        var affected = await cmd.ExecuteNonQueryAsync(cancellationToken);
        executeMs = (DateTime.UtcNow - executeStartedAt).TotalMilliseconds;

        Interlocked.Add(ref _totalUpdatedCount, affected);

        var totalMs = (DateTime.UtcNow - totalStartedAt).TotalMilliseconds;

        _logger.LogInformation(
            "CurrentValue 저장 상세 | Input={Input} | Affected={Affected} | TotalMs={TotalMs:N0} | OpenMs={OpenMs:N0} | ExecuteMs={ExecuteMs:N0} | QueueRemain={QueueRemain} | DroppedRows={DroppedRows}",
            batch.Count,
            affected,
            totalMs,
            openMs,
            executeMs,
            QueueCount,
            TotalDroppedRowCount);
    }

    private static string MakeKey(string endpointUrl, string nodeId) {
        return $"{endpointUrl}\u001F{nodeId}";
    }

    public void Dispose() {
        _currentValueChannel.Writer.TryComplete();
        _cts.Cancel();

        try {
            _workerTask.Wait(TimeSpan.FromSeconds(5));
        } catch {
            // ignore
        }

        _cts.Dispose();
    }
}