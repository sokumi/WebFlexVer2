using System.Threading.Channels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using WebFlex.UI.Data;

namespace WebFlex.UI.Controllers.Main;

[ApiController]
[Route("api/currentvalue")]
public class CurrentValueController : ControllerBase {
    private readonly TsdReadDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CurrentValueController> _logger;

    public CurrentValueController(
        TsdReadDbContext db,
        IConfiguration configuration,
        ILogger<CurrentValueController> logger) {
        _db = db;
        _configuration = configuration;
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

        var connectionString = _configuration.GetConnectionString("WebFlexTsd");

        if (string.IsNullOrWhiteSpace(connectionString)) {
            Response.StatusCode = StatusCodes.Status500InternalServerError;
            await WriteSseAsync("error", "WebFlexTsd connection string is empty", cancellationToken);
            return;
        }

        var channel = Channel.CreateUnbounded<string>();

        await using var connection = new NpgsqlConnection(connectionString);

        try {
            await connection.OpenAsync(cancellationToken);

            connection.Notification += (_, e) => {
                channel.Writer.TryWrite(e.Payload);
            };

            await using (var command = new NpgsqlCommand("LISTEN currentvalue_changed;", connection)) {
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await WriteSseAsync("connected", "{}", cancellationToken);

            var listenTask = Task.Run(async () => {
                try {
                    while (!cancellationToken.IsCancellationRequested) {
                        await connection.WaitAsync(cancellationToken);
                    }
                } catch (OperationCanceledException) {
                    // 정상 종료
                } catch (Exception ex) {
                    _logger.LogError(ex, "PostgreSQL LISTEN 대기 중 오류");
                    channel.Writer.TryComplete(ex);
                }
            }, cancellationToken);

            var heartbeatTask = Task.Run(async () => {
                try {
                    while (!cancellationToken.IsCancellationRequested) {
                        await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
                        channel.Writer.TryWrite("__heartbeat__");
                    }
                } catch (OperationCanceledException) {
                    // 정상 종료
                }
            }, cancellationToken);

            await foreach (var payload in channel.Reader.ReadAllAsync(cancellationToken)) {
                if (payload == "__heartbeat__") {
                    await Response.WriteAsync(": heartbeat\n\n", cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                    continue;
                }

                await WriteSseAsync("currentvalue", payload, cancellationToken);
            }

            await Task.WhenAny(listenTask, heartbeatTask);
        } catch (OperationCanceledException) {
            // 브라우저가 페이지 이동/새로고침해서 연결 끊은 정상 케이스
        } catch (Exception ex) {
            _logger.LogError(ex, "currentvalue LISTEN/NOTIFY stream error");

            if (!Response.HasStarted) {
                Response.StatusCode = StatusCodes.Status500InternalServerError;
            }
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