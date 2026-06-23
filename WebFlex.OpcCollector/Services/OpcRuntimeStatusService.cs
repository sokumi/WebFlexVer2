using Microsoft.EntityFrameworkCore;
using WebFlex.OpcCollector.Data;
using WebFlex.Shared;

namespace WebFlex.OpcCollector.Services;

public class OpcRuntimeStatusService {
    private readonly IDbContextFactory<WebFlexConfigDbContext> _contextFactory;
    private readonly ILogger<OpcRuntimeStatusService> _logger;

    public OpcRuntimeStatusService(
        IDbContextFactory<WebFlexConfigDbContext> contextFactory,
        ILogger<OpcRuntimeStatusService> logger) {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task UpsertConnectedAsync(
        string deviceId,
        string endpointUrl,
        int subscribedCount,
        CancellationToken cancellationToken = default) {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var nowUtc = DateTime.UtcNow;

        var status = await db.Set<OpcCollectRuntimeStatus>()
            .FirstOrDefaultAsync(x => x.DEVICE_ID == deviceId, cancellationToken);

        if (status == null) {
            status = new OpcCollectRuntimeStatus {
                ID = deviceId,
                DEVICE_ID = deviceId,
                ENDPOINT_URL = endpointUrl,
                CreatedAt = nowUtc,
                IsEnabled = true
            };

            db.Set<OpcCollectRuntimeStatus>().Add(status);
        }

        status.ENDPOINT_URL = endpointUrl;
        status.IS_CONNECTED = true;
        status.SUBSCRIBED_COUNT = subscribedCount;
        status.LAST_CONNECTED_AT ??= nowUtc;
        status.LAST_ERROR_MESSAGE = null;
        status.STATUS_UPDATED_AT = nowUtc;
        status.UpdatedAt = nowUtc;

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpsertReceivedAsync(
        string deviceId,
        string endpointUrl,
        int subscribedCount,
        CancellationToken cancellationToken = default) {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var nowUtc = DateTime.UtcNow;

        var status = await db.Set<OpcCollectRuntimeStatus>()
            .FirstOrDefaultAsync(x => x.DEVICE_ID == deviceId, cancellationToken);

        if (status == null) {
            status = new OpcCollectRuntimeStatus {
                ID = deviceId,
                DEVICE_ID = deviceId,
                ENDPOINT_URL = endpointUrl,
                CreatedAt = nowUtc,
                IsEnabled = true
            };

            db.Set<OpcCollectRuntimeStatus>().Add(status);
        }

        status.ENDPOINT_URL = endpointUrl;
        status.IS_CONNECTED = true;
        status.SUBSCRIBED_COUNT = subscribedCount;
        status.LAST_RECEIVED_AT = nowUtc;
        status.LAST_ERROR_MESSAGE = null;
        status.STATUS_UPDATED_AT = nowUtc;
        status.UpdatedAt = nowUtc;

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpsertDisconnectedAsync(
        string deviceId,
        string endpointUrl,
        string? errorMessage = null,
        CancellationToken cancellationToken = default) {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var nowUtc = DateTime.UtcNow;

        var status = await db.Set<OpcCollectRuntimeStatus>()
            .FirstOrDefaultAsync(x => x.DEVICE_ID == deviceId, cancellationToken);

        if (status == null) {
            status = new OpcCollectRuntimeStatus {
                ID = deviceId,
                DEVICE_ID = deviceId,
                ENDPOINT_URL = endpointUrl,
                CreatedAt = nowUtc,
                IsEnabled = true
            };

            db.Set<OpcCollectRuntimeStatus>().Add(status);
        }

        status.ENDPOINT_URL = endpointUrl;
        status.IS_CONNECTED = false;
        status.SUBSCRIBED_COUNT = 0;
        status.LAST_DISCONNECTED_AT = nowUtc;
        status.LAST_ERROR_MESSAGE = errorMessage;
        status.STATUS_UPDATED_AT = nowUtc;
        status.UpdatedAt = nowUtc;

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpsertErrorAsync(
        string deviceId,
        string endpointUrl,
        string errorMessage,
        CancellationToken cancellationToken = default) {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var nowUtc = DateTime.UtcNow;

        var status = await db.Set<OpcCollectRuntimeStatus>()
            .FirstOrDefaultAsync(x => x.DEVICE_ID == deviceId, cancellationToken);

        if (status == null) {
            status = new OpcCollectRuntimeStatus {
                ID = deviceId,
                DEVICE_ID = deviceId,
                ENDPOINT_URL = endpointUrl,
                CreatedAt = nowUtc,
                IsEnabled = true
            };

            db.Set<OpcCollectRuntimeStatus>().Add(status);
        }

        status.ENDPOINT_URL = endpointUrl;
        status.IS_CONNECTED = false;
        status.LAST_ERROR_MESSAGE = errorMessage;
        status.STATUS_UPDATED_AT = nowUtc;
        status.UpdatedAt = nowUtc;

        await db.SaveChangesAsync(cancellationToken);

        _logger.LogError(
            "OPC Runtime 상태 오류 저장 | DeviceId={DeviceId} | Endpoint={EndpointUrl} | Error={Error}",
            deviceId,
            endpointUrl,
            errorMessage);
    }
}