namespace OmniOrder.Application.DTOs;

public record DashboardMetrics(
    decimal TotalGMV,
    int TotalOrders,
    int PendingOrders,
    int PaidOrders,
    int ExpiredOrders,
    int CancelledOrders,
    decimal OrderVelocityPerMinute,
    int TotalLockedStock,
    int ConcurrencyConflictsBlocked,
    double EfCoreQueryTimeMs,
    double DapperQueryTimeMs,
    List<ProductStockInfo> ProductStockInfos
);

public record ProductStockInfo(
    Guid ProductId,
    string Name,
    string SKU,
    int TotalStock,
    int ReservedStock,
    int AvailableStock
);
