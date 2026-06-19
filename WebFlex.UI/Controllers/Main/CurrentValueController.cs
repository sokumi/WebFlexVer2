using System.Threading.Channels;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFlex.UI.Data;
using WebFlex.UI.Services.CurrentValue;

namespace WebFlex.UI.Controllers.Main;

[ApiController]
[Route("api/currentvalue")]
public class CurrentValueController : ControllerBase {
    private readonly TsdReadDbContext _db;
    private readonly CurrentValueNotifyService _notifyService;
    private readonly ILogger<CurrentValueController> _logger;

    public CurrentValueController(
        TsdReadDbContext db,
        CurrentValueNotifyService notifyService,
        ILogger<CurrentValueController> logger) {
        _db = db;
        _notifyService = notifyService;
        _logger = logger;
    }

    [HttpGet("list")]
    public async Task<IActionResult> List(CancellationToken cancellationToken) {
        var data = await _db.CurrentValues
            .AsNoTracking()
            .OrderBy(x => x.EndpointUrl)
            .ThenBy(x => x.NodeId)
            .Select(x => new CurrentValueDto {
                EndpointUrl = x.EndpointUrl,
                NodeId = x.NodeId,
                Value = x.Value,
                Status = x.Status,
                SourceTimestamp = x.SourceTimestamp,
                ReceivedAt = x.ReceivedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(data);
    }

    [HttpGet("stream")]
    public async Task Stream(CancellationToken cancellationToken) {
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.Append("X-Accel-Buffering", "no");

        HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

        var channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions {
            SingleReader = true,
            SingleWriter = false
        });

        var clientId = _notifyService.Subscribe(channel);

        try {
            await WriteSseAsync("connected", "{}", cancellationToken);

            var heartbeatTask = Task.Run(async () => {
                try {
                    while (!cancellationToken.IsCancellationRequested) {
                        await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);

                        await Response.WriteAsync(": heartbeat\n\n", cancellationToken);
                        await Response.Body.FlushAsync(cancellationToken);
                    }
                } catch (OperationCanceledException) {
                    // 정상 종료
                } catch (Exception ex) {
                    _logger.LogDebug(ex, "CurrentValue SSE heartbeat stopped.");
                }
            }, cancellationToken);

            await foreach (var payload in channel.Reader.ReadAllAsync(cancellationToken)) {
                await WriteSseAsync("currentvalue", payload, cancellationToken);
            }

            await heartbeatTask;
        } catch (OperationCanceledException) {
            // 브라우저 새로고침/페이지 이동으로 연결이 끊긴 정상 케이스
        } catch (Exception ex) {
            _logger.LogError(ex, "CurrentValue SSE stream error.");

            if (!Response.HasStarted) {
                Response.StatusCode = StatusCodes.Status500InternalServerError;
            }
        } finally {
            _notifyService.Unsubscribe(clientId);
        }
    }

    private async Task WriteSseAsync(
        string eventName,
        string data,
        CancellationToken cancellationToken) {
        await Response.WriteAsync($"event: {eventName}\n", cancellationToken);

        var lines = data.Replace("\r\n", "\n").Split('\n');

        foreach (var line in lines) {
            await Response.WriteAsync($"data: {line}\n", cancellationToken);
        }

        await Response.WriteAsync("\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }

    public class CurrentValueDto {
        public string EndpointUrl { get; set; } = "";
        public string NodeId { get; set; } = "";
        public string? Value { get; set; }
        public string? Status { get; set; }
        public DateTime? SourceTimestamp { get; set; }
        public DateTime ReceivedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}