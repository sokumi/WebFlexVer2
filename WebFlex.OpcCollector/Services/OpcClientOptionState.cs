using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using WebFlex.OpcCollector.Data;
using WebFlex.Shared;
using WebFlex.Shared.Dtos.Opc;

namespace WebFlex.OpcCollector.Services;

public class OpcClientOptionState {
    private const string OptionCode = "DEFAULT";

    private readonly IDbContextFactory<WebFlexConfigDbContext> _dbContextFactory;
    private readonly ILogger<OpcClientOptionState> _logger;
    private readonly object _lock = new();

    private OpcClientOptionDto _current = new();

    public OpcClientOptionState(
        IDbContextFactory<WebFlexConfigDbContext> dbContextFactory,
        ILogger<OpcClientOptionState> logger) {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public OpcClientOptionDto Current {
        get {
            lock (_lock) {
                return Clone(_current);
            }
        }
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default) {
        const int maxAttempts = 3;
        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++) {
            try {
                await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

                var row = await db.Set<OpcClientOption>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.OPTION_CODE == OptionCode && x.IsEnabled, cancellationToken);

                var next = new OpcClientOptionDto();

                if (row != null && !string.IsNullOrWhiteSpace(row.OPTION_JSON)) {
                    next = JsonSerializer.Deserialize<OpcClientOptionDto>(
                        row.OPTION_JSON,
                        JsonOptions()) ?? new OpcClientOptionDto();

                    _logger.LogInformation("OPC Client 옵션 DB 로드 완료");
                } else {
                    _logger.LogInformation("OPC Client 옵션 DB 저장값 없음. 기본값 사용");
                }

                Normalize(next);

                lock (_lock) {
                    _current = next;
                }

                return;
            } catch (Exception ex) when (IsTransient(ex) && attempt < maxAttempts) {
                lastException = ex;

                _logger.LogWarning(
                    ex,
                    "OPC Client 옵션 DB 로드 재시도 | Attempt={Attempt}/{MaxAttempts}",
                    attempt,
                    maxAttempts);

                await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
            } catch (Exception ex) {
                lastException = ex;
                break;
            }
        }

        throw new InvalidOperationException("OPC Client 옵션 DB 로드를 완료하지 못했습니다.", lastException);
    }

    private static OpcClientOptionDto Clone(OpcClientOptionDto source) {
        var json = JsonSerializer.Serialize(source, JsonOptions());
        return JsonSerializer.Deserialize<OpcClientOptionDto>(json, JsonOptions()) ?? new OpcClientOptionDto();
    }

    private static void Normalize(OpcClientOptionDto options) {
        options.OperationTimeout = NormalizeTimeout(options.OperationTimeout, 6000000);
        options.DefaultSessionTimeout = NormalizeTimeout(options.DefaultSessionTimeout, -1);
        options.MinSubscriptionLifetime = NormalizeTimeout(options.MinSubscriptionLifetime, -1);
        options.SessionTimeout = NormalizeTimeout(options.SessionTimeout, 60000);

        options.MaxStringLength = options.MaxStringLength <= 0 ? int.MaxValue : options.MaxStringLength;
        options.MaxByteStringLength = options.MaxByteStringLength <= 0 ? int.MaxValue : options.MaxByteStringLength;
        options.MaxArrayLength = options.MaxArrayLength <= 0 ? 65535 : options.MaxArrayLength;
        options.MaxMessageSize = options.MaxMessageSize <= 0 ? 419430400 : options.MaxMessageSize;
        options.MaxBufferSize = options.MaxBufferSize <= 0 ? 65535 : options.MaxBufferSize;

        options.PublishingInterval = Math.Max(100, options.PublishingInterval);
        options.LifetimeCount = options.LifetimeCount == 0 ? 60u : options.LifetimeCount;
        options.KeepAliveCount = options.KeepAliveCount == 0 ? 10u : options.KeepAliveCount;
        options.QueueSize = options.QueueSize == 0 ? 1u : options.QueueSize;
        options.SamplingInterval = Math.Max(100, options.SamplingInterval);
    }

    private static bool IsTransient(Exception ex) {
        return ex is TimeoutException
            or NpgsqlException
            or InvalidOperationException
            || ex.InnerException is TimeoutException
            or NpgsqlException;
    }

    private static int NormalizeTimeout(int value, int defaultValue) {
        if (value == -1)
            return -1;

        if (value <= 0)
            return defaultValue;

        return value;
    }

    private static JsonSerializerOptions JsonOptions() {
        return new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }
}
