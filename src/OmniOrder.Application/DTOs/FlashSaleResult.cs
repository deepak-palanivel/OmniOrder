namespace OmniOrder.Application.DTOs;

public record FlashSaleResult(
    int TotalAttempts,
    int SuccessfulOrders,
    int FailedAttempts,
    int InsufficientStockErrors,
    int ConcurrencyConflicts,
    List<FlashSaleAttemptResult> Results,
    double TotalElapsedMs
);

public record FlashSaleAttemptResult(
    int AttemptNumber,
    bool Success,
    string? OrderId,
    string? ErrorCode,
    string? ErrorMessage,
    double ElapsedMs
);
