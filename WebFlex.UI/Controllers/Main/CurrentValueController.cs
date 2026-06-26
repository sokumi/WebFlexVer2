using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFlex.Shared;
using WebFlex.UI.Data;
using WebFlex.UI.Services;

namespace WebFlex.UI.Controllers.Api;

[ApiController]
[Route("api/currentvalue")]
public class CurrentValueController : ControllerBase {
    private readonly TsdReadDbContext _db;
    private readonly CurrentValueNotifyService _notifyService;

    public CurrentValueController(
        TsdReadDbContext db,
        CurrentValueNotifyService notifyService) {
        _db = db;
        _notifyService = notifyService;
    }

    [HttpGet("page")]
    public async Task<IActionResult> Page(
        int skip = 0,
        int take = 100,
        string? groupId = null,
        string? keyword = null,
        CancellationToken cancellationToken = default) {
        if (skip < 0) {
            skip = 0;
        }

        if (take <= 0) {
            take = 100;
        }

        if (take > 300) {
            take = 300;
        }

        var query = _db.Set<CurrentValue>()
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(groupId)) {
            query = query.Where(x => x.GROUP_ID == groupId);
        }

        if (!string.IsNullOrWhiteSpace(keyword)) {
            var q = keyword.Trim();

            query = query.Where(x =>
                x.TAG_ID.Contains(q) ||
                x.GROUP_ID.Contains(q) ||
                (x.VALUE != null && x.VALUE.Contains(q)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderBy(x => x.GROUP_ID)
            .ThenBy(x => x.TAG_ID)
            .Skip(skip)
            .Take(take)
            .Select(x => new CurrentValueRowDto {
                GroupId = x.GROUP_ID,
                TagId = x.TAG_ID,
                Value = x.VALUE,
                Status = x.STATUS == null ? null : (short?)x.STATUS,
                SourceTimestamp = x.SOURCETIMESTAMP,
                ReceivedAt = x.RECEIVEDAT,
                UpdatedAt = x.UPDATEDAT
            })
            .ToListAsync(cancellationToken);

        return Ok(new {
            success = true,
            data = new {
                totalCount,
                skip,
                take,
                rows
            }
        });
    }

    [HttpGet("groups")]
    public async Task<IActionResult> Groups(CancellationToken cancellationToken = default) {
        var groups = await _db.Set<CurrentValue>()
            .AsNoTracking()
            .GroupBy(x => x.GROUP_ID)
            .Select(x => new {
                groupId = x.Key,
                count = x.Count(),
                badCount = x.Count(v => v.STATUS != VaribaleStatusType.Good)
            })
            .OrderBy(x => x.groupId)
            .ToListAsync(cancellationToken);

        return Ok(new {
            success = true,
            data = groups
        });
    }

    // 기존 main.ts가 쓰던 stream endpoint 유지
    [HttpGet("stream")]
    public async Task Stream(CancellationToken cancellationToken) {
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.Headers.ContentType = "text/event-stream";

        var channel = Channel.CreateUnbounded<string>();
        var clientId = _notifyService.Subscribe(channel);

        try {
            await WriteEventAsync("connected", "{}", cancellationToken);

            await foreach (var payload in channel.Reader.ReadAllAsync(cancellationToken)) {
                await WriteEventAsync("currentvalue", payload, cancellationToken);
            }
        } finally {
            _notifyService.Unsubscribe(clientId);
        }
    }

    private async Task WriteEventAsync(
        string eventName,
        string data,
        CancellationToken cancellationToken) {
        await Response.WriteAsync($"event: {eventName}\n", cancellationToken);
        await Response.WriteAsync($"data: {data}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }

    private sealed class CurrentValueRowDto {
        public string? GroupId { get; set; }
        public string TagId { get; set; } = "";
        public string? Value { get; set; }
        public short? Status { get; set; }
        public DateTime? SourceTimestamp { get; set; }
        public DateTime ReceivedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}