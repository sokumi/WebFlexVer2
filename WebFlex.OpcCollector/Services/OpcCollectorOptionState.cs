using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using WebFlex.OpcCollector.Data;
using WebFlex.Shared;
using WebFlex.Shared.Dtos.Opc;

namespace WebFlex.OpcCollector.Services;

public class OpcCollectorOptionState {
    private const string OptionCode = "DEFAULT";

    private readonly IDbContextFactory<WebFlexConfigDbContext> _dbContextFactory;
    private readonly ILogger<OpcCollectorOptionState> _logger;
    private readonly object _lock = new();

    private OpcCollectorRuntimeOptionsDto _current = new();

    public OpcCollectorOptionState(
        IDbContextFactory<WebFlexConfigDbContext> dbContextFactory,
        ILogger<OpcCollectorOptionState> logger) {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public OpcCollectorRuntimeOptionsDto Current {
        get {
            lock (_lock) {
                return OpcCollectorRuntimeOptionsDto.Clone(_current);
            }
        }
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default) {
        const int maxAttempts = 3;
        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++) {
            try {
                await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

                var row = await db.Set<OpcCollectOption>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.OPTION_CODE == OptionCode && x.IsEnabled, cancellationToken);

                if (row == null || string.IsNullOrWhiteSpace(row.OPTION_JSON)) {
                    throw new InvalidOperationException("OPC Collector 옵션 DB 기본값이 없습니다. opc_collect_option seed 데이터를 확인하세요.");
                }

                var next = JsonSerializer.Deserialize<OpcCollectorRuntimeOptionsDto>(
                    row.OPTION_JSON,
                    JsonOptions()
                ) ?? throw new InvalidOperationException("OPC Collector 옵션 JSON 역직렬화 실패");

                Normalize(next);

                lock (_lock) {
                    _current = next;
                }

                _logger.LogInformation("OPC Collector 옵션 DB 로드 완료");

                return;
            } catch (Exception ex) when (IsTransient(ex) && attempt < maxAttempts) {
                lastException = ex;

                _logger.LogWarning(
                    ex,
                    "OPC Collector 옵션 DB 로드 재시도 | Attempt={Attempt}/{MaxAttempts}",
                    attempt,
                    maxAttempts);

                await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
            } catch (Exception ex) {
                lastException = ex;
                break;
            }
        }

        throw new InvalidOperationException("OPC Collector 옵션 DB 로드를 완료하지 못했습니다.", lastException);
    }

    public OpcCollectorRuntimeOptionsDto Update(OpcCollectorRuntimeOptionsDto request) {
        var next = OpcCollectorRuntimeOptionsDto.Clone(request);
        Normalize(next);

        lock (_lock) {
            _current = next;
            return OpcCollectorRuntimeOptionsDto.Clone(_current);
        }
    }

    private static void Normalize(OpcCollectorRuntimeOptionsDto options) {
        options.ReloadIntervalSeconds = Math.Max(1, options.ReloadIntervalSeconds);
        options.SaveIntervalMilliseconds = Math.Max(100, options.SaveIntervalMilliseconds);
        options.MaxBatchSize = Math.Max(1, options.MaxBatchSize);
        options.WriterLogIntervalSeconds = Math.Max(1, options.WriterLogIntervalSeconds);

        options.DefaultPublishingIntervalMs = Math.Max(100, options.DefaultPublishingIntervalMs);
        options.DefaultSamplingIntervalMs = Math.Max(100, options.DefaultSamplingIntervalMs);
        options.DefaultQueueSize = Math.Max(1, options.DefaultQueueSize);

        options.SubscriptionPriority = Math.Min((byte)255, options.SubscriptionPriority);

        options.MinimumCertificateKeySize = Math.Max(1024, options.MinimumCertificateKeySize);
        options.CertificateStoreRootPath = string.IsNullOrWhiteSpace(options.CertificateStoreRootPath)
            ? "pki"
            : options.CertificateStoreRootPath.Trim();

        options.OperationTimeoutMilliseconds = options.OperationTimeoutMilliseconds == -1
            ? -1
            : Math.Max(1000, options.OperationTimeoutMilliseconds);

        options.MaxStringLength = options.MaxStringLength <= 0 ? int.MaxValue : options.MaxStringLength;
        options.MaxByteStringLength = options.MaxByteStringLength <= 0 ? int.MaxValue : options.MaxByteStringLength;
        options.MaxArrayLength = options.MaxArrayLength <= 0 ? 65535 : options.MaxArrayLength;
        options.MaxMessageSize = options.MaxMessageSize <= 0 ? 419430400 : options.MaxMessageSize;
        options.MaxBufferSize = options.MaxBufferSize <= 0 ? 65535 : options.MaxBufferSize;
    }

    private static bool IsTransient(Exception ex) {
        return ex is TimeoutException
            or NpgsqlException
            or InvalidOperationException
            || ex.InnerException is TimeoutException
            or NpgsqlException;
    }

    private static JsonSerializerOptions JsonOptions() {
        return new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }
}