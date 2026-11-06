using Microsoft.EntityFrameworkCore;
using OmniOrder.Application.Interfaces;
using OmniOrder.Domain.Entities;
using OmniOrder.Infrastructure.Data;

namespace OmniOrder.Infrastructure.Services;

public class IdempotencyService : IIdempotencyService
{
    private readonly OmniOrderDbContext _context;

    public IdempotencyService(OmniOrderDbContext context)
    {
        _context = context;
    }

    public async Task<IdempotencyRecord?> GetByKeyAsync(string idempotencyKey, CancellationToken ct = default)
    {
        return await _context.IdempotencyRecords
            .FirstOrDefaultAsync(r => r.IdempotencyKey == idempotencyKey, ct);
    }

    public async Task SaveAsync(IdempotencyRecord record, CancellationToken ct = default)
    {
        _context.IdempotencyRecords.Add(record);
        await _context.SaveChangesAsync(ct);
    }
}
