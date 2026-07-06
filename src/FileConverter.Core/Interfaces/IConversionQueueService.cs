using FileConverter.Core.Models;

namespace FileConverter.Core.Interfaces;

/// <summary>
/// Manages the in-memory conversion queue exposed to the UI.
/// </summary>
public interface IConversionQueueService
{
    IReadOnlyList<ConversionJob> EnqueueFromPaths(IEnumerable<string> paths, MediaCategory selectedCategory, string selectedFormat, ConversionOperation selectedOperation, string? outputDirectory);
    ConversionJob EnqueueJob(ConversionJob job);
}
