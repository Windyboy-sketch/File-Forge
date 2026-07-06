using FileConverter.Core.Interfaces;
using FileConverter.Core.Models;
using FileConverter.Core.Services;
using Microsoft.Extensions.Logging;

namespace FileConverter.Infrastructure.Services;

/// <summary>
/// Builds and enqueues conversion jobs from user-selected files and folders.
/// </summary>
public sealed class ConversionQueueService : IConversionQueueService
{
    private readonly IConversionCoordinator _coordinator;
    private readonly ILogger<ConversionQueueService> _logger;

    public ConversionQueueService(IConversionCoordinator coordinator, ILogger<ConversionQueueService> logger)
    {
        _coordinator = coordinator;
        _logger = logger;
    }

    public IReadOnlyList<ConversionJob> EnqueueFromPaths(
        IEnumerable<string> paths,
        MediaCategory selectedCategory,
        string selectedFormat,
        ConversionOperation selectedOperation,
        string? outputDirectory)
    {
        if (selectedOperation == ConversionOperation.MergePdf)
            return EnqueueMergeBatch(paths, selectedCategory, outputDirectory);

        var added = new List<ConversionJob>();

        foreach (var path in paths)
        {
            if (Directory.Exists(path))
            {
                foreach (var file in FormatRegistry.EnumerateSupportedFiles(path))
                {
                    var job = TryBuildStandardJob(file, selectedCategory, selectedFormat, selectedOperation, outputDirectory);
                    if (job is not null)
                        added.Add(EnqueueJob(job));
                }
            }
            else if (File.Exists(path))
            {
                var job = TryBuildStandardJob(path, selectedCategory, selectedFormat, selectedOperation, outputDirectory);
                if (job is not null)
                    added.Add(EnqueueJob(job));
            }
        }

        return added;
    }

    public ConversionJob EnqueueJob(ConversionJob job)
    {
        _coordinator.Enqueue(job);
        _logger.LogDebug("Queued job {JobId} for {Source}.", job.Id, job.SourcePath);
        return job;
    }

    private static ConversionJob? TryBuildStandardJob(
        string filePath,
        MediaCategory selectedCategory,
        string selectedFormat,
        ConversionOperation selectedOperation,
        string? outputDirectory)
    {
        var category = FormatRegistry.GetCategory(filePath);
        if (category == MediaCategory.Unknown || category != selectedCategory)
            return null;

        var operation = selectedOperation;
        var targetFormat = selectedFormat;

        if (operation is ConversionOperation.MergePdf or ConversionOperation.SplitPdf)
        {
            if (!filePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                return null;
            targetFormat = "pdf";
        }

        return ConversionJobBuilder.Create(filePath, targetFormat, category, operation, outputDirectory);
    }

    private List<ConversionJob> EnqueueMergeBatch(
        IEnumerable<string> paths,
        MediaCategory selectedCategory,
        string? outputDirectory)
    {
        if (selectedCategory != MediaCategory.Document)
            return [];

        var pdfs = CollectPdfFiles(paths).OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
        if (pdfs.Count == 0)
            return [];

        IReadOnlyList<string>? additional = pdfs.Count > 1 ? pdfs.Skip(1).ToList() : null;
        var outputPath = string.IsNullOrWhiteSpace(outputDirectory)
            ? null
            : Path.Combine(outputDirectory, $"merged_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

        var job = ConversionJobBuilder.Create(
            pdfs[0],
            "pdf",
            MediaCategory.Document,
            ConversionOperation.MergePdf,
            outputDirectory,
            additional);

        if (outputPath is not null)
            job.OutputPath = outputPath;

        _logger.LogInformation(
            "Queued PDF merge job with {Count} file(s) -> {Output}",
            pdfs.Count,
            job.OutputPath ?? "(output pending)");

        return [EnqueueJob(job)];
    }

    private static IEnumerable<string> CollectPdfFiles(IEnumerable<string> paths)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in paths)
        {
            foreach (var file in FormatRegistry.EnumerateSupportedFiles(path))
            {
                if (!file.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (FormatRegistry.GetCategory(file) != MediaCategory.Document)
                    continue;

                if (seen.Add(file))
                    yield return file;
            }
        }
    }
}
