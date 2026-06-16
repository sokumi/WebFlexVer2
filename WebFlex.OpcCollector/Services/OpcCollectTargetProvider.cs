using Microsoft.EntityFrameworkCore;
using WebFlex.OpcCollector.Data;
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

        var devices = await db.OpcDevices
            .AsNoTracking()
            .Include(x => x.Tags)
            .Where(x =>
                x.IsEnabled &&
                x.IsCollectEnabled)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.DeviceName)
            .ToListAsync(cancellationToken);

        var targets = devices
            .Select(device => new OpcCollectTargetDto {
                DeviceId = device.Id,
                DeviceCode = device.DeviceCode,
                DeviceName = device.DeviceName,
                DeviceAddress = device.DeviceAddress,
                Port = device.Port,
                EndpointUrl = string.IsNullOrWhiteSpace(device.EndpointUrl)
                    ? $"opc.tcp://{device.DeviceAddress}:{device.Port}"
                    : device.EndpointUrl,
                UseSecurity = device.UseSecurity,
                SecurityPolicy = device.SecurityPolicy,
                SecurityMode = device.SecurityMode,
                UseAnonymous = device.UseAnonymous,
                UserName = device.UserName,
                Password = device.Password,
                PublishingIntervalMs = device.PublishingIntervalMs,
                SamplingIntervalMs = device.SamplingIntervalMs,
                QueueSize = device.QueueSize,
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
                        SamplingIntervalMs = tag.SamplingIntervalMs ?? device.SamplingIntervalMs,
                        QueueSize = tag.QueueSize ?? device.QueueSize,
                        SaveToDatabase = tag.SaveToDatabase
                    })
                    .ToList()
            })
            .Where(x => x.Tags.Count > 0)
            .ToList();

        _logger.LogInformation(
            "수집 대상 조회 완료 | DeviceCount={DeviceCount} | TagCount={TagCount}",
            targets.Count,
            targets.Sum(x => x.Tags.Count));

        return targets;
    }
}