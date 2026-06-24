using System.Collections.Concurrent;
using System.Threading.Channels;
using Npgsql;

namespace WebFlex.UI.Services;

public class CurrentValueNotifyService : BackgroundService {
    private readonly IConfiguration _configuration;
    private readonly ILogger<CurrentValueNotifyService> _logger;

    private readonly ConcurrentDictionary<Guid, Channel<string>> _clients = new();

    public CurrentValueNotifyService(
        IConfiguration configuration,
        ILogger<CurrentValueNotifyService> logger) {
        _configuration = configuration;
        _logger = logger;
    }

    public Guid Subscribe(Channel<string> channel) {
        var clientId = Guid.NewGuid();

        _clients.TryAdd(clientId, channel);

        _logger.LogInformation(
            "CurrentValue SSE client subscribed. ClientId={ClientId}, Count={Count}",
            clientId,
            _clients.Count);

        return clientId;
    }

    public void Unsubscribe(Guid clientId) {
        if (_clients.TryRemove(clientId, out var channel)) {
            channel.Writer.TryComplete();

            _logger.LogInformation(
                "CurrentValue SSE client unsubscribed. ClientId={ClientId}, Count={Count}",
                clientId,
                _clients.Count);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        var connectionString = _configuration.GetConnectionString("WebFlexTsd");

        if (string.IsNullOrWhiteSpace(connectionString)) {
            _logger.LogError("WebFlexTsd connection string is empty.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested) {
            try {
                await ListenLoopAsync(connectionString, stoppingToken);
            } catch (OperationCanceledException) {
                // 정상 종료
            } catch (Exception ex) {
                _logger.LogError(ex, "CurrentValue LISTEN loop error. Retry after 5 seconds.");

                try {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                } catch (OperationCanceledException) {
                    // 정상 종료
                }
            }
        }
    }

    // PostgreSQL LISTEN 채널
    private async Task ListenLoopAsync(
        string connectionString,
        CancellationToken stoppingToken) {
        await using var connection = new NpgsqlConnection(connectionString);

        await connection.OpenAsync(stoppingToken);

        connection.Notification += (_, e) => {
            Broadcast(e.Payload);
        };

        await using (var command = new NpgsqlCommand("LISTEN currentvalue_changed;", connection)) {
            await command.ExecuteNonQueryAsync(stoppingToken);
        }

        _logger.LogInformation("LISTEN currentvalue_changed started.");

        while (!stoppingToken.IsCancellationRequested) {
            await connection.WaitAsync(stoppingToken);
        }
    }

    private void Broadcast(string payload) {
        if (_clients.IsEmpty) {
            return;
        }

        foreach (var item in _clients) {
            var clientId = item.Key;
            var channel = item.Value;

            if (!channel.Writer.TryWrite(payload)) {
                Unsubscribe(clientId);
            }
        }
    }
}