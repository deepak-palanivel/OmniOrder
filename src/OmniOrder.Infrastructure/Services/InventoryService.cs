using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniOrder.Application.DTOs;
using OmniOrder.Application.Interfaces;
using OmniOrder.Domain.Entities;
using OmniOrder.Infrastructure.Data;

namespace OmniOrder.Infrastructure.Services;

public class InventoryService : IInventoryService
{
    private readonly OmniOrderDbContext _context;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(OmniOrderDbContext context, ILogger<InventoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ProductResponse>> GetAllProductsAsync(CancellationToken ct = default)
    {
        var products = await _context.Products
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

        return products.Select(p => new ProductResponse(
            p.Id, p.SKU, p.Name, p.Description, p.Price,
            p.TotalStock, p.ReservedStock, p.AvailableStock, p.UpdatedAt
        )).ToList();
    }

    public async Task<ProductResponse?> GetProductByIdAsync(Guid productId, CancellationToken ct = default)
    {
        var p = await _context.Products.FindAsync(new object[] { productId }, ct);
        if (p == null) return null;

        return new ProductResponse(
            p.Id, p.SKU, p.Name, p.Description, p.Price,
            p.TotalStock, p.ReservedStock, p.AvailableStock, p.UpdatedAt
        );
    }

    public async Task ReleaseExpiredReservationsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var expiredOrders = await _context.Orders
            .Include(o => o.Reservations)
            .ThenInclude(r => r.Product)
            .Where(o => o.Status == OrderStatus.Pending && o.ExpiresAt != null && o.ExpiresAt <= now)
            .ToListAsync(ct);

        if (expiredOrders.Count == 0) return;

        foreach (var order in expiredOrders)
        {
            foreach (var reservation in order.Reservations.Where(r => r.Status == ReservationStatus.Active))
            {
                reservation.Status = ReservationStatus.Expired;
                reservation.Product.ReservedStock -= reservation.Quantity;
                reservation.Product.UpdatedAt = now;

                _logger.LogInformation(
                    "Released {Qty} units of {Product} from expired order {Order}",
                    reservation.Quantity, reservation.Product.Name, order.OrderNumber);
            }

            order.Status = OrderStatus.Expired;
            order.UpdatedAt = now;

            _context.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = "OrderExpired",
                Payload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    order.Id,
                    order.OrderNumber,
                    order.Status,
                    ExpiredAt = now
                }),
                OccurredOnUtc = now
            });
        }

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Released reservations for {Count} expired orders", expiredOrders.Count);
    }
}
