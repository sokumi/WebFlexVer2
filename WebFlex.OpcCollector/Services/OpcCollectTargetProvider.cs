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
                DeviceAddress = device.DEVICE_ADDRESS,
                Port = device.PORT,
                EndpointUrl = string.IsNullOrWhiteSpace(device.ENDPOINT_URL)
                    ? $"opc.tcp://{device.DEVICE_ADDRESS}:{device.PORT}"
                    : device.ENDPOINT_URL,
                UseSecurity = device.USESECURITY,
                SecurityMode = device.SECURITYMODE,
                UseAnonymous = device.USE_ANONYMOUS,
                PublishingIntervalMs = device.PUBLISHINGINTERVALMS,
                SamplingIntervalMs = device.SAMPLINGINTERVALMS,
                Tags = device.Tags
                    .Where(tag =>
                        tag.IsEnabled &&
                        tag.IsCollectEnabled)
                    .OrderBy(tag => tag.SortOrder)
                    .ThenBy(tag => tag.DisplayName)
                    .Select(tag => new OpcCollectTargetTagDto {
                        TagId = tag.Id,
                        TagCode = tag.TagCode,
                        NodeId = tag.NodeId,
                        DisplayName = tag.DisplayName,
                        SamplingIntervalMs = tag.SamplingIntervalMs ?? device.SAMPLINGINTERVALMS,
                        SaveToDatabase = tag.SaveToDatabase
                    })
                    .ToList()
            })
            .Where(x => x.Tags.Count > 0)
            .ToList();


        return targets;
    }
}