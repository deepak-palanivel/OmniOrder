namespace OmniOrder.Domain.Entities;

public class WebhookEventLog
{
    public Guid Id { get; set; }
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string ProviderSignature { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public WebhookStatus Status { get; set; } = WebhookStatus.Received;
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public string? Error { get; set; }
}
