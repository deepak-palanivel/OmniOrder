using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OmniOrder.Application.Interfaces;

namespace OmniOrder.Infrastructure.BackgroundWorkers;

public class ExpiredReservationReaperService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpiredReservationReaperService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(15);

    public ExpiredReservationReaperService(
        IServiceScopeFactory scopeFactory,
        ILogger<ExpiredReservationReaperService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ExpiredReservationReaperService started — scanning every {Interval}s", _interval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var inventoryService = scope.ServiceProvider.GetRequiredService<IInventoryService>();
                await inventoryService.ReleaseExpiredReservationsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExpiredReservationReaperService");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("ExpiredReservationReaperService stopped");
    }
}
