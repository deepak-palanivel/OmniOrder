using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OmniOrder.Application.DTOs;
using OmniOrder.Application.Interfaces;
using OmniOrder.Domain.Entities;
using OmniOrder.Domain.Exceptions;
using OmniOrder.Infrastructure.Data;

namespace OmniOrder.Infrastructure.Services;

public class WebhookService : IWebhookService
{
    private readonly OmniOrderDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(
        OmniOrderDbContext context,
        IConfiguration configuration,
        ILogger<WebhookService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task ProcessPaymentWebhookAsync(WebhookPaymentRequest request, string signature, CancellationToken ct = default)
    {
        var secret = _configuration["Webhook:Secret"] ?? "omni-webhook-secret-key-for-hmac-sha256";
        var payload = JsonSerializer.Serialize(request);

        if (!VerifySignature(payload, signature, secret))
        {
            throw new InvalidWebhookSignatureException();
        }

        // Check for duplicate webhook (idempotency)
        var existingEvent = await _context.WebhookEventLogs
            .FirstOrDefaultAsync(e => e.EventId == request.EventId, ct);

        if (existingEvent != null)
        {
            existingEvent.Status = WebhookStatus.Duplicate;
            await _context.SaveChangesAsync(ct);
            _logger.LogWarning("Duplicate webhook event {EventId} — already processed", request.EventId);
            throw new DuplicateWebhookException(request.EventId);
        }

        var webhookLog = new WebhookEventLog
        {
            Id = Guid.NewGuid(),
            EventId = request.EventId,
            EventType = request.EventType,
            ProviderSignature = signature,
            Payload = payload,
            Status = WebhookStatus.Processing,
            ReceivedAt = DateTime.UtcNow
        };
        _context.WebhookEventLogs.Add(webhookLog);

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            var order = await _context.Orders
                .Include(o => o.Reservations)
                .ThenInclude(r => r.Product)
                .FirstOrDefaultAsync(o => o.Id == request.OrderId, ct)
                ?? throw new OrderNotFoundException(request.OrderId);

            switch (request.EventType.ToUpperInvariant())
            {
                case "PAYMENT_CAPTURED":
                case "PAYMENT_SUCCESS":
                    if (order.Status == OrderStatus.Pending || order.Status == OrderStatus.Confirmed)
                    {
                        order.Status = OrderStatus.Paid;
                        order.UpdatedAt = DateTime.UtcNow;
                        order.ExpiresAt = null; // No longer expires

                        foreach (var reservation in order.Reservations.Where(r => r.Status == ReservationStatus.Active))
                        {
                            reservation.Status = ReservationStatus.Confirmed;
                        }

                        _logger.LogInformation("Order {OrderNumber} marked as Paid via webhook {EventId}",
                            order.OrderNumber, request.EventId);
                    }
                    break;

                case "PAYMENT_FAILED":
                    if (order.Status == OrderStatus.Pending || order.Status == OrderStatus.Confirmed)
                    {
                        order.Status = OrderStatus.Cancelled;
                        order.UpdatedAt = DateTime.UtcNow;

                        foreach (var reservation in order.Reservations.Where(r => r.Status == ReservationStatus.Active))
                        {
                            reservation.Status = ReservationStatus.Released;
                            reservation.Product.ReservedStock -= reservation.Quantity;
                            reservation.Product.UpdatedAt = DateTime.UtcNow;
                        }

                        _logger.LogInformation("Order {OrderNumber} cancelled due to payment failure via webhook {EventId}",
                            order.OrderNumber, request.EventId);
                    }
                    break;

                default:
                    _logger.LogWarning("Unknown webhook event type: {EventType}", request.EventType);
                    break;
            }

            webhookLog.Status = WebhookStatus.Processed;
            webhookLog.ProcessedAt = DateTime.UtcNow;

            _context.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = $"Webhook_{request.EventType}",
                Payload = JsonSerializer.Serialize(new
                {
                    request.EventId,
                    request.EventType,
                    request.OrderId,
                    order.OrderNumber,
                    order.Status,
                    ProcessedAt = DateTime.UtcNow
                }),
                OccurredOnUtc = DateTime.UtcNow
            });

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (Exception ex) when (ex is not DuplicateWebhookException and not OrderNotFoundException and not InvalidWebhookSignatureException)
        {
            await transaction.RollbackAsync(ct);
            webhookLog.Status = WebhookStatus.Failed;
            webhookLog.Error = ex.Message;
            await _context.SaveChangesAsync(ct);
            throw;
        }
    }

    public bool VerifySignature(string payload, string signature, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var computedSignature = Convert.ToHexString(computedHash).ToLowerInvariant();
        return string.Equals(computedSignature, signature, StringComparison.OrdinalIgnoreCase);
    }
}
