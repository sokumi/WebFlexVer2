using Microsoft.EntityFrameworkCore;
using WebFlex.OpcCollector.Data;
using WebFlex.Shared;
using WebFlex.Shared.Dtos.Opc;

namespace WebFlex.OpcCollector.Services;

public class OpcCollectTargetProvider {
    private readonly IDbContextFactory<WebFlexConfigDbContext> _contextFactory;
    private readonly ILogger<OpcCollectTargetProvider> _logger;

    public OpcCollectTargetProvider(
        IDbContextFactory<WebFlexConfigDbContext> contextFactory,
        ILogger<OpcCollectTargetProvider> logger) {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<List<OpcCollectTargetDto>> GetCollectTargetsAsync(
        CancellationToken cancellationToken = default) {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var devices = await db.Set<OpcDevice>()
            .AsNoTracking()
            .Include(x => x.Tags)
            .Where(x =>
                x.IsEnabled &&
                x.IS_COLLECTENABLED)
            .ToListAsync(cancellationToken);

        var targets = devices
            .Select(device => new OpcCollectTargetDto {
                DeviceId = device.ID,
                DeviceName = device.DEVICE_NAME,
                DeviceAddress = device.DEVICE_ADDRESS ?? "",
                Port = device.PORT ?? 0,
                EndpointUrl = string.IsNullOrWhiteSpace(device.ENDPOINT_URL)
                    ? $"opc.tcp://{device.DEVICE_ADDRESS}:{device.PORT}"
                    : device.ENDPOINT_URL,
                UseSecurity = device.USESECURITY,
                SecurityMode = device.SECURITYMODE,
                UseAnonymous = device.USE_ANONYMOUS,
                PublishingIntervalMs = device.PUBLISHINGINTERVALMS ?? 1000,
                SamplingIntervalMs = device.SAMPLINGINTERVALMS ?? 1000,
                Tags = (device.Tags ?? new())
                    .Where(tag =>
                        tag.IsEnabled &&
                        tag.IS_COLLECTENABLED)
                    .OrderBy(tag => tag.SORT_ORDER)
                    .ThenBy(tag => tag.TAG_NAME)
                    .Select(tag => new OpcCollectTargetTagDto {
                        TagId = tag.ID,
                        TagCode = "",
                        NodeId = tag.TAG_NAME ?? "",
                        DisplayName = tag.TAG_NAME ?? "",
                        SamplingIntervalMs = tag.SAMPLINGINTERVALMS ?? device.SAMPLINGINTERVALMS ?? 1000,
                        SaveToDatabase = tag.SAVE_TO_DATABASE
                    })
                    .ToList()
            })
            .Where(x => x.Tags.Count > 0)
            .ToList();


        return targets;
    }
}