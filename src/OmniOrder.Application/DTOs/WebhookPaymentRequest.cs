namespace OmniOrder.Application.DTOs;

public record WebhookPaymentRequest(
    string EventId,
    string EventType,
    Guid OrderId,
    decimal Amount,
    string Currency,
    DateTime Timestamp
);
