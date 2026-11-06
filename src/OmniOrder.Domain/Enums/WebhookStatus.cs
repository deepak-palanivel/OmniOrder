namespace OmniOrder.Domain.Entities;

public enum WebhookStatus
{
    Received = 0,
    Processing = 1,
    Processed = 2,
    Failed = 3,
    Duplicate = 4
}
