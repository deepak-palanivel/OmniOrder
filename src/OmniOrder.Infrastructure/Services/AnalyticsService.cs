using System.Data;
using System.Diagnostics;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OmniOrder.Application.DTOs;
using OmniOrder.Application.Interfaces;
using OmniOrder.Infrastructure.Data;

namespace OmniOrder.Infrastructure.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly OmniOrderDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IConcurrencyTracker _concurrencyTracker;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(
        OmniOrderDbContext context,
        IConfiguration configuration,
        IConcurrencyTracker concurrencyTracker,
        ILogger<AnalyticsService> logger)
    {
        _context = context;
        _configuration = configuration;
        _concurrencyTracker = concurrencyTracker;
        _logger = logger;
    }

    public async Task<DashboardMetrics> GetDashboardMetricsAsync(CancellationToken ct = default)
    {
        // ── EF Core Query (for comparison) ──
        var efSw = Stopwatch.StartNew();
        var efTotalOrders = await _context.Orders.CountAsync(ct);
        var efPaidOrders = await _context.Orders
            .Where(o => o.Status == Domain.Entities.OrderStatus.Paid || o.Status == Domain.Entities.OrderStatus.Shipped || o.Status == Domain.Entities.OrderStatus.Delivered)
            .Select(o => o.TotalAmount)
            .ToListAsync(ct);
        var efTotalGMV = efPaidOrders.Sum();
        efSw.Stop();
        var efQueryTimeMs = efSw.Elapsed.TotalMilliseconds;

        // ── Dapper Query (high-speed analytics) ──
        var dapperSw = Stopwatch.StartNew();
        var connectionString = _context.Database.GetConnectionString()!;

        using IDbConnection connection = new SqliteConnection(connectionString);
        connection.Open();

        var orderStats = await connection.QueryFirstAsync<OrderStatsDto>(
            @"SELECT
                COUNT(*) AS TotalOrders,
                COALESCE(SUM(CASE WHEN Status IN ('Paid','Shipped','Delivered') THEN TotalAmount ELSE 0 END), 0) AS TotalGMV,
                COALESCE(SUM(CASE WHEN Status = 'Pending' THEN 1 ELSE 0 END), 0) AS PendingOrders,
                COALESCE(SUM(CASE WHEN Status = 'Paid' THEN 1 ELSE 0 END), 0) AS PaidOrders,
                COALESCE(SUM(CASE WHEN Status = 'Expired' THEN 1 ELSE 0 END), 0) AS ExpiredOrders,
                COALESCE(SUM(CASE WHEN Status = 'Cancelled' THEN 1 ELSE 0 END), 0) AS CancelledOrders
            FROM Orders");

        var stockInfos = (await connection.QueryAsync<ProductStockInfoDto>(
            @"SELECT Id AS ProductId, Name, SKU, TotalStock, ReservedStock, (TotalStock - ReservedStock) AS AvailableStock
            FROM Products ORDER BY Name")).ToList();

        var totalLockedStock = Convert.ToInt32(await connection.ExecuteScalarAsync<object>(
            "SELECT COALESCE(SUM(ReservedStock), 0) FROM Products") ?? 0);

        // Calculate order velocity (orders per minute in last hour)
        var oneHourAgo = DateTime.UtcNow.AddHours(-1).ToString("o");
        var recentOrderCount = Convert.ToInt32(await connection.ExecuteScalarAsync<object>(
            "SELECT COUNT(*) FROM Orders WHERE CreatedAt >= @Since", new { Since = oneHourAgo }) ?? 0);
        var orderVelocity = recentOrderCount / 60.0m;

        dapperSw.Stop();
        var dapperQueryTimeMs = dapperSw.Elapsed.TotalMilliseconds;

        _logger.LogDebug("Analytics: EF Core={EfMs}ms, Dapper={DapperMs}ms", efQueryTimeMs, dapperQueryTimeMs);

        return new DashboardMetrics(
            TotalGMV: Convert.ToDecimal(orderStats.TotalGMV),
            TotalOrders: Convert.ToInt32(orderStats.TotalOrders),
            PendingOrders: Convert.ToInt32(orderStats.PendingOrders),
            PaidOrders: Convert.ToInt32(orderStats.PaidOrders),
            ExpiredOrders: Convert.ToInt32(orderStats.ExpiredOrders),
            CancelledOrders: Convert.ToInt32(orderStats.CancelledOrders),
            OrderVelocityPerMinute: orderVelocity,
            TotalLockedStock: totalLockedStock,
            ConcurrencyConflictsBlocked: _concurrencyTracker.ConflictsBlocked,
            EfCoreQueryTimeMs: Math.Round(efQueryTimeMs, 2),
            DapperQueryTimeMs: Math.Round(dapperQueryTimeMs, 2),
            ProductStockInfos: stockInfos.Select(s => new ProductStockInfo(
                Guid.Parse(s.ProductId.ToString()), s.Name, s.SKU, (int)s.TotalStock, (int)s.ReservedStock, (int)s.AvailableStock
            )).ToList()
        );
    }

    private class OrderStatsDto
    {
        public long TotalOrders { get; set; }
        public double TotalGMV { get; set; }
        public long PendingOrders { get; set; }
        public long PaidOrders { get; set; }
        public long ExpiredOrders { get; set; }
        public long CancelledOrders { get; set; }
    }

    private class ProductStockInfoDto
    {
        public string ProductId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public long TotalStock { get; set; }
        public long ReservedStock { get; set; }
        public long AvailableStock { get; set; }
    }
}
