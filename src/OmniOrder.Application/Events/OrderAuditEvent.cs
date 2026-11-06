namespace OmniOrder.Application.Events;

public record OrderAuditEvent(
    Guid OrderId,
    string OrderNumber,
    string EventType,
    string Details,
    DateTime OccurredAt
);
