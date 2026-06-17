using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using WebFlex.OpcCollector.Options;
using WebFlex.OpcCollector.Runtime;

namespace WebFlex.OpcCollector.Services;

public class TimescaleDbWriter : IDisposable {
    private readonly ConcurrentQueue<OpcCollectedValue> _queue = new();
    private readonly ILogger<TimescaleDbWriter> _logger;
    private readonly string _connectionString;
    private readonly OpcCollectorOptions _options;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _workerTask;
    private DateTime _lastInsertLogAt = DateTime.MinValue;

    private long _totalEnqueuedCount;
    private long _totalInsertedCount;

    public TimescaleDbWriter(
        IConfiguration configuration,
        IOptions<OpcCollectorOptions> options,
        ILogger<TimescaleDbWriter> logger) {
        _logger = logger;
        _options = options.Value;
        _connectionString = configuration.GetConnectionString("WebFlexTsd")
            ?? throw new InvalidOperationException("ConnectionStrings:WebFlexTsd 설정이 없습니다.");

        _workerTask = Task.Run(ProcessQueueAsync);
    }

    public int QueueCount => _queue.Count;

    public long TotalEnqueuedCount => Interlocked.Read(ref _totalEnqueuedCount);

    public long TotalInsertedCount => Interlocked.Read(ref _totalInsertedCount);

    public void Enqueue(OpcCollectedValue value) {
        _queue.Enqueue(value);
        Interlocked.Increment(ref _totalEnqueuedCount);
    }

    private async Task ProcessQueueAsync() {
        while (!_cts.Token.IsCancellationRequested) {
            try {
                await Task.Delay(_options.FlushIntervalMilliseconds, _cts.Token);
                await FlushOnceAsync(_cts.Token);
            } catch (OperationCanceledException) {
                break;
            } catch (Exception ex) {
                _logger.LogError(ex, "Timescale DB Writer 처리 오류");
            }
        }

        try {
            await FlushOnceAsync(CancellationToken.None);
        } catch (Exception ex) {
            _logger.LogError(ex, "Timescale DB Writer 종료 전 Flush 실패");
        }
    }

    private async Task FlushOnceAsync(CancellationToken cancellationToken) {
        var batch = new List<OpcCollectedValue>();

        while (batch.Count < _options.MaxBatchSize &&
               _queue.TryDequeue(out var item)) {
            batch.Add(item);
        }

        if (batch.Count == 0)
            return;

        var startedAt = DateTime.UtcNow;

        try {
            await BulkInsertTimescaleAsync(batch, cancellationToken);
            await UpsertCurrentValueAsync(batch, cancellationToken);

            Interlocked.Add(ref _totalInsertedCount, batch.Count);

            var elapsedMs = (DateTime.UtcNow - startedAt).TotalMilliseconds;

            if ((DateTime.UtcNow - _lastInsertLogAt).TotalSeconds >= 30) {
                _lastInsertLogAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "Timescale 상태 | QueueRemain={QueueRemain} | TotalInserted={Inserted}",
                    _queue.Count,
                    TotalInsertedCount);
            }
        } catch (Exception ex) {
            _logger.LogError(
                ex,
                "Timescale 저장 실패 | Count={Count}",
                batch.Count);

            foreach (var item in batch.Take(5)) {
                _logger.LogError(
                    "저장 실패 샘플 | Time={Time:o} | Endpoint={EndpointUrl} | NodeId={NodeId} | Value={Value} | Status={Status}",
                    item.Time,
                    item.EndpointUrl,
                    item.NodeId,
                    item.Value,
                    item.Status);
            }
        }
    }

    private async Task BulkInsertTimescaleAsync(
        List<OpcCollectedValue> batch,
        CancellationToken cancellationToken) {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

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

        await writer.CompleteAsync(cancellationToken);
    }

    private async Task UpsertCurrentValueAsync(
        List<OpcCollectedValue> batch,
        CancellationToken cancellationToken) {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        await using var tx = await conn.BeginTransactionAsync(cancellationToken);

        await using var cmd = new NpgsqlCommand(@"
INSERT INTO public.currentvalue
(
    endpoint_url,
    node_id,
    value,
    status,
    source_timestamp,
    received_at,
    updated_at
)
VALUES
(
    @endpoint_url,
    @node_id,
    @value,
    @status,
    @source_timestamp,
    @received_at,
    now()
)
ON CONFLICT (endpoint_url, node_id)
DO UPDATE SET
    value = EXCLUDED.value,
    status = EXCLUDED.status,
    source_timestamp = EXCLUDED.source_timestamp,
    received_at = EXCLUDED.received_at,
    updated_at = now();
", conn, tx);

        var endpointParam = cmd.Parameters.Add("@endpoint_url", NpgsqlDbType.Text);
        var nodeIdParam = cmd.Parameters.Add("@node_id", NpgsqlDbType.Text);
        var valueParam = cmd.Parameters.Add("@value", NpgsqlDbType.Text);
        var statusParam = cmd.Parameters.Add("@status", NpgsqlDbType.Text);
        var sourceTimestampParam = cmd.Parameters.Add("@source_timestamp", NpgsqlDbType.TimestampTz);
        var receivedAtParam = cmd.Parameters.Add("@received_at", NpgsqlDbType.TimestampTz);

        foreach (var item in batch) {
            endpointParam.Value = item.EndpointUrl;
            nodeIdParam.Value = item.NodeId;
            valueParam.Value = (object?)item.Value ?? DBNull.Value;
            statusParam.Value = (object?)item.Status ?? DBNull.Value;
            sourceTimestampParam.Value = item.SourceTimestamp.HasValue
                ? item.SourceTimestamp.Value
                : DBNull.Value;
            receivedAtParam.Value = item.ReceivedAt;

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        await tx.CommitAsync(cancellationToken);
    }

    public void Dispose() {
        _cts.Cancel();

        try {
            _workerTask.Wait(TimeSpan.FromSeconds(5));
        } catch {
            // ignore
        }

        _cts.Dispose();
    }
}