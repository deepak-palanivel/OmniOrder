using OmniOrder.Application.DTOs;

namespace OmniOrder.Application.Interfaces;

public interface IInventoryService
{
    Task<List<ProductResponse>> GetAllProductsAsync(CancellationToken ct = default);
    Task<ProductResponse?> GetProductByIdAsync(Guid productId, CancellationToken ct = default);
    Task ReleaseExpiredReservationsAsync(CancellationToken ct = default);
}
