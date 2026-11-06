namespace OmniOrder.Domain.Entities;

public class Product
{
    public Guid Id { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int TotalStock { get; set; }
    public int ReservedStock { get; set; }
    public int AvailableStock => TotalStock - ReservedStock;
    public byte[] RowVersion { get; set; } = Guid.NewGuid().ToByteArray();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<InventoryReservation> Reservations { get; set; } = new List<InventoryReservation>();
}
