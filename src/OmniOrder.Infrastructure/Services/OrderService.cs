using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniOrder.Application.DTOs;
using OmniOrder.Application.Events;
using OmniOrder.Application.Interfaces;
using OmniOrder.Domain.Entities;
using OmniOrder.Domain.Exceptions;
using OmniOrder.Infrastructure.Channels;
using OmniOrder.Infrastructure.Data;

namespace OmniOrder.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly OmniOrderDbContext _context;
    private readonly OrderAuditChannel _auditChannel;
    private readonly IConcurrencyTracker _concurrencyTracker;
    private readonly ILogger<OrderService> _logger;
    private static int _orderCounter;

    public OrderService(
        OmniOrderDbContext context,
        OrderAuditChannel auditChannel,
        IConcurrencyTracker concurrencyTracker,
        ILogger<OrderService> logger)
    {
        _context = context;
        _auditChannel = auditChannel;
        _concurrencyTracker = concurrencyTracker;
        _logger = logger;
    }

    public async Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct = default)
    {
        var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Interlocked.Increment(ref _orderCounter):D5}";

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            var order = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = orderNumber,
                CustomerId = request.CustomerId,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5)
            };

            decimal totalAmount = 0;

            foreach (var itemReq in request.Items)
            {
                var product = await _context.Products.FindAsync(new object[] { itemReq.ProductId }, ct)
                    ?? throw new DomainException("PRODUCT_NOT_FOUND", $"Product {itemReq.ProductId} not found");

                if (product.AvailableStock < itemReq.Quantity)
                {
                    throw new InsufficientStockException(product.Id, itemReq.Quantity, product.AvailableStock);
                }

                product.ReservedStock += itemReq.Quantity;
                product.UpdatedAt = DateTime.UtcNow;

                var orderItem = new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    ProductId = product.Id,
                    Quantity = itemReq.Quantity,
                    UnitPrice = product.Price
                };

                var reservation = new InventoryReservation
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    ProductId = product.Id,
                    Quantity = itemReq.Quantity,
                    ExpiresAt = order.ExpiresAt!.Value,
                    Status = ReservationStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };

                totalAmount += product.Price * itemReq.Quantity;
                order.Items.Add(orderItem);
                order.Reservations.Add(reservation);
            }

            order.TotalAmount = totalAmount;
            _context.Orders.Add(order);

            var outboxMsg = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = "OrderCreated",
                Payload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    order.Id,
                    order.OrderNumber,
                    order.TotalAmount,
                    order.Status,
                    order.CreatedAt,
                    ItemCount = order.Items.Count
                }),
                OccurredOnUtc = DateTime.UtcNow
            };
            _context.OutboxMessages.Add(outboxMsg);

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            await _auditChannel.Writer.WriteAsync(new OrderAuditEvent(
                order.Id, order.OrderNumber, "OrderCreated",
                $"Order created with {order.Items.Count} items, total: {order.TotalAmount:C}",
                DateTime.UtcNow), ct);

            _logger.LogInformation("Order {OrderNumber} created successfully", order.OrderNumber);

            return MapToResponse(order);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync(ct);
            _concurrencyTracker.RecordConflict();
            _logger.LogWarning(ex, "Concurrency conflict while creating order");
            throw new ConcurrencyConflictException("Another transaction modified the stock. Please retry.");
        }

    }

    public async Task<OrderResponse?> GetOrderByIdAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        return order == null ? null : MapToResponse(order);
    }

    public async Task<List<OrderResponse>> GetAllOrdersAsync(CancellationToken ct = default)
    {
        var orders = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .OrderByDescending(o => o.CreatedAt)
            .Take(100)
            .ToListAsync(ct);

        return orders.Select(MapToResponse).ToList();
    }

    public async Task<FlashSaleResult> SimulateFlashSaleAsync(Guid productId, int totalBuyers, int stockAvailable, CancellationToken ct = default)
    {
        _concurrencyTracker.Reset();

        // Reset the flash sale product stock for the simulation
        var product = await _context.Products.FindAsync(new object[] { productId }, ct)
            ?? throw new DomainException("PRODUCT_NOT_FOUND", $"Product {productId} not found");

        product.TotalStock = stockAvailable;
        product.ReservedStock = 0;
        product.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        var sw = Stopwatch.StartNew();
        var results = new List<FlashSaleAttemptResult>();
        var tasks = new List<Task<FlashSaleAttemptResult>>();

        for (int i = 0; i < totalBuyers; i++)
        {
            var attemptNumber = i + 1;
            tasks.Add(SimulateSinglePurchaseAsync(productId, attemptNumber, ct));
        }

        var attemptResults = await Task.WhenAll(tasks);
        results.AddRange(attemptResults.OrderBy(r => r.AttemptNumber));

        sw.Stop();

        var successCount = results.Count(r => r.Success);
        var failedCount = results.Count(r => !r.Success);
        var insufficientStockErrors = results.Count(r => r.ErrorCode == "INSUFFICIENT_STOCK");
        var concurrencyConflicts = results.Count(r => r.ErrorCode == "CONCURRENCY_CONFLICT");

        var result = new FlashSaleResult(
            TotalAttempts: totalBuyers,
            SuccessfulOrders: successCount,
            FailedAttempts: failedCount,
            InsufficientStockErrors: insufficientStockErrors,
            ConcurrencyConflicts: concurrencyConflicts,
            Results: results,
            TotalElapsedMs: sw.Elapsed.TotalMilliseconds
        );

        _logger.LogInformation(
            "Flash sale simulation complete: {Success}/{Total} succeeded in {Elapsed}ms",
            successCount, totalBuyers, sw.Elapsed.TotalMilliseconds);

        return result;
    }

    private async Task<FlashSaleAttemptResult> SimulateSinglePurchaseAsync(Guid productId, int attemptNumber, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            // Each attempt gets its own DbContext scope via a new scope
            var optionsBuilder = new DbContextOptionsBuilder<OmniOrderDbContext>();

            // Extract connection string from current context
            var connectionString = _context.Database.GetConnectionString();
            optionsBuilder.UseSqlite(connectionString);

            await using var scopedContext = new OmniOrderDbContext(optionsBuilder.Options);

            await using var transaction = await scopedContext.Database.BeginTransactionAsync(ct);
            try
            {
                var product = await scopedContext.Products.FindAsync(new object[] { productId }, ct)
                    ?? throw new DomainException("PRODUCT_NOT_FOUND", $"Product {productId} not found");

                if (product.AvailableStock < 1)
                {
                    throw new InsufficientStockException(product.Id, 1, product.AvailableStock);
                }

                product.ReservedStock += 1;
                product.UpdatedAt = DateTime.UtcNow;

                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    OrderNumber = $"FLASH-{DateTime.UtcNow:yyyyMMddHHmmss}-{attemptNumber:D4}",
                    CustomerId = Guid.NewGuid(),
                    Status = OrderStatus.Confirmed,
                    TotalAmount = product.Price,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                    Items = new List<OrderItem>
                    {
                        new()
                        {
                            Id = Guid.NewGuid(),
                            ProductId = product.Id,
                            Quantity = 1,
                            UnitPrice = product.Price
                        }
                    },
                    Reservations = new List<InventoryReservation>
                    {
                        new()
                        {
                            Id = Guid.NewGuid(),
                            ProductId = product.Id,
                            Quantity = 1,
                            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                            Status = ReservationStatus.Active,
                            CreatedAt = DateTime.UtcNow
                        }
                    }
                };

                scopedContext.Orders.Add(order);
                await scopedContext.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                sw.Stop();
                return new FlashSaleAttemptResult(attemptNumber, true, order.Id.ToString(), null, null, sw.Elapsed.TotalMilliseconds);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }
        catch (InsufficientStockException ex)
        {
            sw.Stop();
            return new FlashSaleAttemptResult(attemptNumber, false, null, ex.Code, ex.Message, sw.Elapsed.TotalMilliseconds);
        }
        catch (DbUpdateConcurrencyException)
        {
            sw.Stop();
            _concurrencyTracker.RecordConflict();
            return new FlashSaleAttemptResult(attemptNumber, false, null, "CONCURRENCY_CONFLICT",
                "Optimistic concurrency conflict — another buyer locked the stock first", sw.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new FlashSaleAttemptResult(attemptNumber, false, null, "ERROR", ex.Message, sw.Elapsed.TotalMilliseconds);
        }
    }

    private static OrderResponse MapToResponse(Order order)
    {
        return new OrderResponse(
            order.Id,
            order.OrderNumber,
            order.CustomerId,
            order.Status.ToString(),
            order.TotalAmount,
            order.CreatedAt,
            order.ExpiresAt,
            order.Items.Select(i => new OrderItemResponse(
                i.ProductId,
                i.Product?.Name ?? "N/A",
                i.Quantity,
                i.UnitPrice,
                i.Quantity * i.UnitPrice
            )).ToList()
        );
    }
}
