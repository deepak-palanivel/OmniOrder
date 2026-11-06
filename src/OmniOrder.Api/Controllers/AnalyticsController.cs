using Microsoft.AspNetCore.Mvc;
using OmniOrder.Application.Interfaces;

namespace OmniOrder.Api.Controllers;

[ApiController]
[Route("api/v1/analytics")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    /// <summary>
    /// Returns real-time dashboard metrics using dual ORM strategy
    /// (EF Core for writes comparison + Dapper for high-speed reads).
    /// </summary>
    [HttpGet("dashboard-metrics")]
    public async Task<IActionResult> GetDashboardMetrics(CancellationToken ct)
    {
        var metrics = await _analyticsService.GetDashboardMetricsAsync(ct);
        return Ok(metrics);
    }
}
