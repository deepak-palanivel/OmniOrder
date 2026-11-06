using OmniOrder.Application.DTOs;

namespace OmniOrder.Application.Interfaces;

public interface IOrderService
{
    Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct = default);
    Task<OrderResponse?> GetOrderByIdAsync(Guid orderId, CancellationToken ct = default);
    Task<List<OrderResponse>> GetAllOrdersAsync(CancellationToken ct = default);
    Task<FlashSaleResult> SimulateFlashSaleAsync(Guid productId, int totalBuyers, int stockAvailable, CancellationToken ct = default);
}
