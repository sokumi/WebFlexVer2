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
            .FirstOrDefaultAsync(x => x.OpcDeviceId == deviceId, cancellationToken);

        if (status == null) {
            status = new OpcCollectRuntimeStatus {
                Id = deviceId,
                OpcDeviceId = deviceId,
                EndpointUrl = endpointUrl,
                CreatedAt = nowUtc,
                IsEnabled = true
            };

            db.Set<OpcCollectRuntimeStatus>().Add(status);
        }

        status.EndpointUrl = endpointUrl;
        status.IsConnected = true;
        status.SubscribedCount = subscribedCount;
        status.LastConnectedAt ??= nowUtc;
        status.LastErrorMessage = null;
        status.StatusUpdatedAt = nowUtc;
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
            .FirstOrDefaultAsync(x => x.OpcDeviceId == deviceId, cancellationToken);

        if (status == null) {
            status = new OpcCollectRuntimeStatus {
                Id = deviceId,
                OpcDeviceId = deviceId,
                EndpointUrl = endpointUrl,
                CreatedAt = nowUtc,
                IsEnabled = true
            };

            db.Set<OpcCollectRuntimeStatus>().Add(status);
        }

        status.EndpointUrl = endpointUrl;
        status.IsConnected = true;
        status.SubscribedCount = subscribedCount;
        status.LastReceivedAt = nowUtc;
        status.LastErrorMessage = null;
        status.StatusUpdatedAt = nowUtc;
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
            .FirstOrDefaultAsync(x => x.OpcDeviceId == deviceId, cancellationToken);

        if (status == null) {
            status = new OpcCollectRuntimeStatus {
                Id = deviceId,
                OpcDeviceId = deviceId,
                EndpointUrl = endpointUrl,
                CreatedAt = nowUtc,
                IsEnabled = true
            };

            db.Set<OpcCollectRuntimeStatus>().Add(status);
        }

        status.EndpointUrl = endpointUrl;
        status.IsConnected = false;
        status.SubscribedCount = 0;
        status.LastDisconnectedAt = nowUtc;
        status.LastErrorMessage = errorMessage;
        status.StatusUpdatedAt = nowUtc;
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
            .FirstOrDefaultAsync(x => x.OpcDeviceId == deviceId, cancellationToken);

        if (status == null) {
            status = new OpcCollectRuntimeStatus {
                Id = deviceId,
                OpcDeviceId = deviceId,
                EndpointUrl = endpointUrl,
                CreatedAt = nowUtc,
                IsEnabled = true
            };

            db.Set<OpcCollectRuntimeStatus>().Add(status);
        }

        status.EndpointUrl = endpointUrl;
        status.IsConnected = false;
        status.LastErrorMessage = errorMessage;
        status.StatusUpdatedAt = nowUtc;
        status.UpdatedAt = nowUtc;

        await db.SaveChangesAsync(cancellationToken);

        _logger.LogError(
            "OPC Runtime 상태 오류 저장 | DeviceId={DeviceId} | Endpoint={EndpointUrl} | Error={Error}",
            deviceId,
            endpointUrl,
            errorMessage);
    }
}