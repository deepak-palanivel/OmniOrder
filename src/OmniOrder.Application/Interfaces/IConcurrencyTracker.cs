namespace OmniOrder.Application.Interfaces;

public interface IConcurrencyTracker
{
    int ConflictsBlocked { get; }
    void RecordConflict();
    void Reset();
}
