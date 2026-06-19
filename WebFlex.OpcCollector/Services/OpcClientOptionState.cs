using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebFlex.OpcCollector.Data;
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
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var row = await db.OpcClientOptions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OptionCode == OptionCode && x.IsEnabled, cancellationToken);

        var next = new OpcClientOptionDto();

        if (row != null && !string.IsNullOrWhiteSpace(row.OptionJson)) {
            next = JsonSerializer.Deserialize<OpcClientOptionDto>(
                row.OptionJson,
                JsonOptions()) ?? new OpcClientOptionDto();

            _logger.LogInformation("OPC Client 옵션 DB 로드 완료");
        } else {
            _logger.LogInformation("OPC Client 옵션 DB 저장값 없음. 기본값 사용");
        }

        Normalize(next);

        lock (_lock) {
            _current = next;
        }
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