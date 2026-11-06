using OmniOrder.Application.DTOs;

namespace OmniOrder.Application.Interfaces;

public interface IAnalyticsService
{
    Task<DashboardMetrics> GetDashboardMetricsAsync(CancellationToken ct = default);
}
