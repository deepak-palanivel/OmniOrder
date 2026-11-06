namespace OmniOrder.Domain.Entities;

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Paid = 2,
    Shipped = 3,
    Delivered = 4,
    Cancelled = 5,
    Expired = 6
}
