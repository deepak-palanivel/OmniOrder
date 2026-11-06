namespace OmniOrder.Application.DTOs;

public record ProductResponse(
    Guid Id,
    string SKU,
    string Name,
    string Description,
    decimal Price,
    int TotalStock,
    int ReservedStock,
    int AvailableStock,
    DateTime UpdatedAt
);
