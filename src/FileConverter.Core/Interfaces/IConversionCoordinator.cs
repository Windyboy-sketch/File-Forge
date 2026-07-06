using FileConverter.Core.Models;

namespace FileConverter.Core.Interfaces;

public interface IConversionCoordinator
{
    event EventHandler<ConversionJob>? JobUpdated;
    IReadOnlyList<ConversionJob> Jobs { get; }
    void Enqueue(ConversionJob job);
    void EnqueueRange(IEnumerable<ConversionJob> jobs);
    void Remove(Guid jobId);
    void ClearCompleted();
    void ClearAll();
    Task StartAsync(CancellationToken cancellationToken = default);
    Task CancelAsync();
}
