using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OmniOrder.Infrastructure.Data;

namespace OmniOrder.Infrastructure.BackgroundWorkers;

public class OutboxProcessorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessorService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(5);

    public OutboxProcessorService(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxProcessorService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxProcessorService started — polling every {Interval}s", _interval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<OmniOrderDbContext>();

                var pendingMessages = await context.OutboxMessages
                    .Where(m => m.ProcessedOnUtc == null)
                    .OrderBy(m => m.OccurredOnUtc)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                if (pendingMessages.Count > 0)
                {
                    // Try to get the SignalR hub — it may not be registered in test scenarios
                    var hubContext = scope.ServiceProvider.GetService<IHubContext<OrderHub>>();

                    foreach (var message in pendingMessages)
                    {
                        try
                        {
                            if (hubContext != null)
                            {
                                await hubContext.Clients.All.SendAsync(
                                    "OrderUpdate",
                                    new { message.Type, message.Payload, message.OccurredOnUtc },
                                    stoppingToken);
                            }

                            message.ProcessedOnUtc = DateTime.UtcNow;
                            _logger.LogDebug("Processed outbox message {Id}: {Type}", message.Id, message.Type);
                        }
                        catch (Exception ex)
                        {
                            message.Error = ex.Message;
                            message.ProcessedOnUtc = DateTime.UtcNow;
                            _logger.LogError(ex, "Failed to process outbox message {Id}", message.Id);
                        }
                    }

                    await context.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Processed {Count} outbox messages", pendingMessages.Count);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OutboxProcessorService");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("OutboxProcessorService stopped");
    }
}

// SignalR Hub — defined here so Infrastructure can reference it for the outbox processor
public class OrderHub : Hub
{
    private readonly ILogger<OrderHub> _logger;

    public OrderHub(ILogger<OrderHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
