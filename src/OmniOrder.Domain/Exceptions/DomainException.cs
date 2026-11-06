namespace OmniOrder.Domain.Exceptions;

public class DomainException : Exception
{
    public string Code { get; }
    
    public DomainException(string code, string message) : base(message)
    {
        Code = code;
    }
}

public class InsufficientStockException : DomainException
{
    public Guid ProductId { get; }
    public int RequestedQuantity { get; }
    public int AvailableStock { get; }
    
    public InsufficientStockException(Guid productId, int requested, int available)
        : base("INSUFFICIENT_STOCK", $"Product {productId}: requested {requested}, available {available}")
    {
        ProductId = productId;
        RequestedQuantity = requested;
        AvailableStock = available;
    }
}

public class ConcurrencyConflictException : DomainException
{
    public ConcurrencyConflictException(string message)
        : base("CONCURRENCY_CONFLICT", message) { }
}

public class OrderNotFoundException : DomainException
{
    public OrderNotFoundException(Guid orderId)
        : base("ORDER_NOT_FOUND", $"Order {orderId} not found") { }
}

public class DuplicateWebhookException : DomainException
{
    public DuplicateWebhookException(string eventId)
        : base("DUPLICATE_WEBHOOK", $"Webhook event {eventId} already processed") { }
}

public class InvalidWebhookSignatureException : DomainException
{
    public InvalidWebhookSignatureException()
        : base("INVALID_SIGNATURE", "Webhook HMAC-SHA256 signature verification failed") { }
}
