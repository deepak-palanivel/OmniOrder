using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniOrder.Domain.Entities;

namespace OmniOrder.Infrastructure.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OmniOrderDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<OmniOrderDbContext>>();

        try
        {
            await context.Database.EnsureCreatedAsync();

            // Enable WAL mode for SQLite
            if (context.Database.IsSqlite())
            {
                await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
                await context.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=5000;");
                logger.LogInformation("SQLite WAL mode enabled");
            }

            if (await context.Products.AnyAsync())
            {
                logger.LogInformation("Database already seeded");
                return;
            }

            var products = GetSeedProducts();
            context.Products.AddRange(products);
            await context.SaveChangesAsync();

            var orders = GetSeedOrders(products);
            context.Orders.AddRange(orders);
            await context.SaveChangesAsync();

            logger.LogInformation("Database seeded with {ProductCount} products and {OrderCount} historical orders",
                products.Count, orders.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error seeding database");
            throw;
        }
    }

    private static List<Product> GetSeedProducts()
    {
        return new List<Product>
        {
            new()
            {
                Id = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                SKU = "GPU-RTX5090",
                Name = "⚡ Flash Sale GPU - RTX 5090 Ti",
                Description = "Ultra-limited flash sale GPU. Only 5 units available! High-performance graphics card with 32GB GDDR7.",
                Price = 1999.99m,
                TotalStock = 5,
                ReservedStock = 0,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901"),
                SKU = "CPU-R9-9950X",
                Name = "AMD Ryzen 9 9950X Processor",
                Description = "16-core, 32-thread desktop processor with 5.7 GHz boost clock and 170W TDP.",
                Price = 649.99m,
                TotalStock = 50,
                ReservedStock = 3,
                CreatedAt = DateTime.UtcNow.AddDays(-60),
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.Parse("c3d4e5f6-a7b8-9012-cdef-123456789012"),
                SKU = "RAM-DDR5-64",
                Name = "Corsair Dominator DDR5 64GB Kit",
                Description = "64GB (2x32GB) DDR5-6400 memory kit with RGB lighting and aluminum heat spreader.",
                Price = 289.99m,
                TotalStock = 120,
                ReservedStock = 7,
                CreatedAt = DateTime.UtcNow.AddDays(-45),
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.Parse("d4e5f6a7-b8c9-0123-defa-234567890123"),
                SKU = "SSD-NVM-4TB",
                Name = "Samsung 990 Pro 4TB NVMe SSD",
                Description = "PCIe Gen 5.0 NVMe M.2 SSD with 14,000 MB/s sequential read speeds.",
                Price = 449.99m,
                TotalStock = 80,
                ReservedStock = 2,
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.Parse("e5f6a7b8-c9d0-1234-efab-345678901234"),
                SKU = "MB-Z890-MAX",
                Name = "ASUS ROG Maximus Z890 Extreme",
                Description = "Intel Z890 chipset motherboard with Wi-Fi 7, 10GbE LAN, and Thunderbolt 5.",
                Price = 799.99m,
                TotalStock = 25,
                ReservedStock = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.Parse("f6a7b8c9-d0e1-2345-fabc-456789012345"),
                SKU = "PSU-1600W",
                Name = "Seasonic Prime TX-1600 PSU",
                Description = "1600W 80+ Titanium fully modular power supply with 12-year warranty.",
                Price = 549.99m,
                TotalStock = 35,
                ReservedStock = 0,
                CreatedAt = DateTime.UtcNow.AddDays(-40),
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.Parse("a7b8c9d0-e1f2-3456-abcd-567890123456"),
                SKU = "CASE-O11D-EVO",
                Name = "Lian Li O11 Dynamic EVO 2",
                Description = "Mid-tower ATX case with dual-chamber design, tempered glass panels, and USB-C front I/O.",
                Price = 179.99m,
                TotalStock = 60,
                ReservedStock = 4,
                CreatedAt = DateTime.UtcNow.AddDays(-25),
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.Parse("b8c9d0e1-f2a3-4567-bcde-678901234567"),
                SKU = "COOL-AIO-420",
                Name = "NZXT Kraken Elite 420mm AIO Cooler",
                Description = "420mm AIO liquid cooler with 2.36\" LCD display and Asetek Gen 8 pump.",
                Price = 329.99m,
                TotalStock = 40,
                ReservedStock = 2,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow
            }
        };
    }

    private static List<Order> GetSeedOrders(List<Product> products)
    {
        var customerId1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var customerId2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var customerId3 = Guid.Parse("33333333-3333-3333-3333-333333333333");

        return new List<Order>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-2026-0001",
                CustomerId = customerId1,
                Status = OrderStatus.Paid,
                TotalAmount = 2649.98m,
                CreatedAt = DateTime.UtcNow.AddDays(-7),
                UpdatedAt = DateTime.UtcNow.AddDays(-7),
                Items = new List<OrderItem>
                {
                    new() { Id = Guid.NewGuid(), ProductId = products[1].Id, Quantity = 1, UnitPrice = products[1].Price },
                    new() { Id = Guid.NewGuid(), ProductId = products[0].Id, Quantity = 1, UnitPrice = products[0].Price }
                }
            },
            new()
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-2026-0002",
                CustomerId = customerId2,
                Status = OrderStatus.Paid,
                TotalAmount = 1129.98m,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow.AddDays(-5),
                Items = new List<OrderItem>
                {
                    new() { Id = Guid.NewGuid(), ProductId = products[2].Id, Quantity = 2, UnitPrice = products[2].Price },
                    new() { Id = Guid.NewGuid(), ProductId = products[3].Id, Quantity = 1, UnitPrice = products[3].Price }
                }
            },
            new()
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-2026-0003",
                CustomerId = customerId3,
                Status = OrderStatus.Shipped,
                TotalAmount = 799.99m,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                UpdatedAt = DateTime.UtcNow.AddDays(-2),
                Items = new List<OrderItem>
                {
                    new() { Id = Guid.NewGuid(), ProductId = products[4].Id, Quantity = 1, UnitPrice = products[4].Price }
                }
            },
            new()
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-2026-0004",
                CustomerId = customerId1,
                Status = OrderStatus.Expired,
                TotalAmount = 329.99m,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddHours(-20),
                Items = new List<OrderItem>
                {
                    new() { Id = Guid.NewGuid(), ProductId = products[7].Id, Quantity = 1, UnitPrice = products[7].Price }
                }
            },
            new()
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-2026-0005",
                CustomerId = customerId2,
                Status = OrderStatus.Cancelled,
                TotalAmount = 549.99m,
                CreatedAt = DateTime.UtcNow.AddHours(-12),
                UpdatedAt = DateTime.UtcNow.AddHours(-10),
                Items = new List<OrderItem>
                {
                    new() { Id = Guid.NewGuid(), ProductId = products[5].Id, Quantity = 1, UnitPrice = products[5].Price }
                }
            }
        };
    }
}
