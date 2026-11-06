using OmniOrder.Application.DTOs;

namespace OmniOrder.Application.Interfaces;

public interface IWebhookService
{
    Task ProcessPaymentWebhookAsync(WebhookPaymentRequest request, string signature, CancellationToken ct = default);
    bool VerifySignature(string payload, string signature, string secret);
}
