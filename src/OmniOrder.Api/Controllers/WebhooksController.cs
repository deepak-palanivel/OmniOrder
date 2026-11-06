using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using OmniOrder.Api.Middleware;
using OmniOrder.Application.DTOs;
using OmniOrder.Application.Interfaces;

namespace OmniOrder.Api.Controllers;

[ApiController]
[Route("api/v1/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly IWebhookService _webhookService;
    private readonly IConfiguration _configuration;

    public WebhooksController(IWebhookService webhookService, IConfiguration configuration)
    {
        _webhookService = webhookService;
        _configuration = configuration;
    }

    /// <summary>
    /// Receives a signed payment webhook. Verifies HMAC-SHA256 signature
    /// and processes PAYMENT_CAPTURED or PAYMENT_FAILED events exactly once.
    /// </summary>
    [HttpPost("payment")]
    [Idempotent]
    public async Task<IActionResult> PaymentWebhook(
        [FromBody] WebhookPaymentRequest request,
        [FromHeader(Name = "X-Webhook-Signature")] string signature,
        CancellationToken ct)
    {
        await _webhookService.ProcessPaymentWebhookAsync(request, signature, ct);
        return Ok(new { status = "processed", eventId = request.EventId });
    }

    /// <summary>
    /// Utility endpoint to generate a signed webhook payload for testing.
    /// </summary>
    [HttpPost("generate-signed-payload")]
    public IActionResult GenerateSignedPayload([FromBody] WebhookPaymentRequest request)
    {
        var secret = _configuration["Webhook:Secret"] ?? "omni-webhook-secret-key-for-hmac-sha256";
        var payload = JsonSerializer.Serialize(request);

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var signatureHex = Convert.ToHexString(hash).ToLowerInvariant();

        return Ok(new
        {
            payload = request,
            signature = signatureHex,
            header = "X-Webhook-Signature",
            idempotencyHeader = "X-Idempotency-Key"
        });
    }
}
