using OmniOrder.Application.Interfaces;

namespace OmniOrder.Infrastructure.Services;

public class ConcurrencyTracker : IConcurrencyTracker
{
    private int _conflictsBlocked;

    public int ConflictsBlocked => _conflictsBlocked;

    public void RecordConflict()
    {
        Interlocked.Increment(ref _conflictsBlocked);
    }

    public void Reset()
    {
        Interlocked.Exchange(ref _conflictsBlocked, 0);
    }
}
