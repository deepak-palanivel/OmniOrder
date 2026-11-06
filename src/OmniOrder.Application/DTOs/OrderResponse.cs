using OmniOrder.Domain.Entities;

namespace OmniOrder.Application.DTOs;

public record OrderResponse(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    string Status,
    decimal TotalAmount,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    List<OrderItemResponse> Items
);

public record OrderItemResponse(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice
);
