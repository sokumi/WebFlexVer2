using System.Threading.Channels;
using Npgsql;
using NpgsqlTypes;
using WebFlex.OpcCollector.Runtime;

namespace WebFlex.OpcCollector.Services;

public class TimescaleDbWriter : IDisposable {
    private const int HistoryChannelCapacity = 100000;

    private readonly Channel<OpcCollectedValue> _historyChannel;
    private readonly ILogger<TimescaleDbWriter> _logger;
    private readonly string _connectionString;
    private readonly OpcCollectorOptionState _optionState;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _workerTask;

    private DateTime _lastInsertLogAt = DateTime.MinValue;

    private long _queueCount;
    private long _totalEnqueuedCount;
    private long _totalInsertedCount;
    private long _totalDroppedRowCount;

    private DateTime _lastEnqueueLogAt = DateTime.UtcNow;
    private long _lastEnqueueLogCount;

    public TimescaleDbWriter(
        IConfiguration configuration,
        OpcCollectorOptionState optionState,
        ILogger<TimescaleDbWriter> logger) {
        _logger = logger;
        _optionState = optionState;

        _connectionString = configuration.GetConnectionString("WebFlexTsd")
            ?? throw new InvalidOperationException("ConnectionStrings:WebFlexTsd 설정이 없습니다.");

        _historyChannel = Channel.CreateBounded<OpcCollectedValue>(
            new BoundedChannelOptions(HistoryChannelCapacity) {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            });

        _workerTask = Task.Run(ProcessHistoryQueueAsync);
    }

    public int QueueCount => (int)Math.Max(0, Interlocked.Read(ref _queueCount));

    public long TotalEnqueuedCount => Interlocked.Read(ref _totalEnqueuedCount);

    public long TotalInsertedCount => Interlocked.Read(ref _totalInsertedCount);

    public long TotalDroppedRowCount => Interlocked.Read(ref _totalDroppedRowCount);

    public void Enqueue(OpcCollectedValue value) {
        var beforeCount = Interlocked.Read(ref _queueCount);

        if (beforeCount >= HistoryChannelCapacity) {
            // DropOldest로 기존 row 1개가 밀려나간 것으로 본다.
            Interlocked.Increment(ref _totalDroppedRowCount);
        }

        var written = _historyChannel.Writer.TryWrite(value);

        if (!written) {
            Interlocked.Increment(ref _totalDroppedRowCount);

            _logger.LogWarning(
                "Timescale History 채널 입력 실패 | QueueRemain={QueueRemain}",
                QueueCount);

            return;
        }

        if (beforeCount < HistoryChannelCapacity) {
            Interlocked.Increment(ref _queueCount);
        }

        Interlocked.Increment(ref _totalEnqueuedCount);

        var now = DateTime.UtcNow;
        var lastLogAt = _lastEnqueueLogAt;

        if ((now - lastLogAt).TotalSeconds >= 5) {
            var total = TotalEnqueuedCount;
            var diff = total - Interlocked.Read(ref _lastEnqueueLogCount);

            _lastEnqueueLogAt = now;
            Interlocked.Exchange(ref _lastEnqueueLogCount, total);

            _logger.LogInformation(
                "History Enqueue 속도 | Last5SecRows={Rows} | RowsPerSec={RowsPerSec:N0} | QueueRemain={QueueRemain} | TotalEnqueued={TotalEnqueued}",
                diff,
                diff / Math.Max(1, (now - lastLogAt).TotalSeconds),
                QueueCount,
                total);
        }
    }

    private async Task ProcessHistoryQueueAsync() {
        while (!_cts.Token.IsCancellationRequested) {
            try {
                var options = _optionState.Current;

                await Task.Delay(options.FlushIntervalMilliseconds, _cts.Token);

                await FlushOnceAsync(_cts.Token);
            } catch (OperationCanceledException) {
                break;
            } catch (Exception ex) {
                _logger.LogError(ex, "Timescale History Writer 처리 오류");
            }
        }

        try {
            await FlushAllAsync(CancellationToken.None);
        } catch (Exception ex) {
            _logger.LogError(ex, "Timescale History Writer 종료 전 Flush 실패");
        }
    }

    private async Task FlushOnceAsync(CancellationToken cancellationToken) {
        var options = _optionState.Current;

        if (!options.EnableTimescaleHistorySave)
            return;

        var maxBatchSize = Math.Max(1, options.MaxBatchSize);
        var batch = new List<OpcCollectedValue>(maxBatchSize);

        var beforeQueue = QueueCount;
        var drainStartedAt = DateTime.UtcNow;

        while (batch.Count < maxBatchSize &&
               _historyChannel.Reader.TryRead(out var item)) {
            Interlocked.Decrement(ref _queueCount);
            batch.Add(item);
        }

        var drainMs = (DateTime.UtcNow - drainStartedAt).TotalMilliseconds;

        if (batch.Count == 0)
            return;

        _logger.LogInformation(
            "History Queue Drain | BeforeQueue={BeforeQueue} | Drained={Drained} | AfterQueue={AfterQueue} | DrainMs={DrainMs:N0} | MaxBatchSize={MaxBatchSize}",
            beforeQueue,
            batch.Count,
            QueueCount,
            drainMs,
            maxBatchSize);

        await SaveHistoryAsync(batch, cancellationToken);
    }

    private async Task FlushAllAsync(CancellationToken cancellationToken) {
        var options = _optionState.Current;

        if (!options.EnableTimescaleHistorySave)
            return;

        while (true) {
            var maxBatchSize = Math.Max(1, options.MaxBatchSize);
            var batch = new List<OpcCollectedValue>(maxBatchSize);

            while (batch.Count < maxBatchSize &&
                   _historyChannel.Reader.TryRead(out var item)) {
                Interlocked.Decrement(ref _queueCount);
                batch.Add(item);
            }

            if (batch.Count == 0)
                break;

            await SaveHistoryAsync(batch, cancellationToken);
        }
    }

    private async Task SaveHistoryAsync(
        IReadOnlyList<OpcCollectedValue> batch,
        CancellationToken cancellationToken) {
        if (batch.Count == 0)
            return;

        var options = _optionState.Current;

        var totalStartedAt = DateTime.UtcNow;
        double copyTotalMs = 0;
        double openMs = 0;
        double beginCopyMs = 0;
        double writeRowsMs = 0;
        double completeMs = 0;

        try {
            (copyTotalMs, openMs, beginCopyMs, writeRowsMs, completeMs)
                = await BulkInsertTimescaleAsync(batch, cancellationToken);

            Interlocked.Add(ref _totalInsertedCount, batch.Count);

            var totalMs = (DateTime.UtcNow - totalStartedAt).TotalMilliseconds;

            _logger.LogInformation(
                "History 저장 상세 | Batch={Batch} | TotalMs={TotalMs:N0} | CopyTotalMs={CopyTotalMs:N0} | OpenMs={OpenMs:N0} | BeginCopyMs={BeginCopyMs:N0} | WriteRowsMs={WriteRowsMs:N0} | CompleteMs={CompleteMs:N0} | QueueRemain={QueueRemain} | Inserted={Inserted} | DroppedRows={DroppedRows}",
                batch.Count,
                totalMs,
                copyTotalMs,
                openMs,
                beginCopyMs,
                writeRowsMs,
                completeMs,
                QueueCount,
                TotalInsertedCount,
                TotalDroppedRowCount);

            if (totalMs > 1000) {
                _logger.LogWarning(
                    "History 저장 지연 | Batch={Batch} | TotalMs={TotalMs:N0} | QueueRemain={QueueRemain}",
                    batch.Count,
                    totalMs,
                    QueueCount);
            }
        } catch (Exception ex) {
            _logger.LogError(
                ex,
                "Timescale History 저장 실패 | Count={Count} | QueueRemain={QueueRemain}",
                batch.Count,
                QueueCount);
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
        _historyChannel.Writer.TryComplete();
        _cts.Cancel();

        try {
            _workerTask.Wait(TimeSpan.FromSeconds(5));
        } catch {
            // ignore
        }

        _cts.Dispose();
    }
}