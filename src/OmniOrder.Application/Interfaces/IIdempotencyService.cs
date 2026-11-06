using OmniOrder.Domain.Entities;

namespace OmniOrder.Application.Interfaces;

public interface IIdempotencyService
{
    Task<IdempotencyRecord?> GetByKeyAsync(string idempotencyKey, CancellationToken ct = default);
    Task SaveAsync(IdempotencyRecord record, CancellationToken ct = default);
}
