using FileConverter.Core.Models;

namespace FileConverter.Core.Services;

/// <summary>
/// Creates conversion jobs and resolves output paths consistently across the application.
/// </summary>
public static class ConversionJobBuilder
{
    public static ConversionJob Create(
        string sourcePath,
        string targetFormat,
        MediaCategory category,
        ConversionOperation operation,
        string? outputDirectory,
        IReadOnlyList<string>? additionalSourcePaths = null)
    {
        return new ConversionJob
        {
            SourcePath = sourcePath,
            TargetFormat = targetFormat,
            Category = category,
            Operation = operation,
            AdditionalSourcePaths = additionalSourcePaths,
            OutputPath = ResolveOutputPath(outputDirectory, sourcePath, targetFormat, operation, category)
        };
    }

    public static string? ResolveOutputPath(
        string? outputDirectory,
        string sourcePath,
        string targetFormat,
        ConversionOperation operation,
        MediaCategory category)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory))
            return null;

        return operation switch
        {
            ConversionOperation.SplitPdf => outputDirectory,
            ConversionOperation.MergePdf =>
                Path.Combine(outputDirectory, "merged.pdf"),
            ConversionOperation.Convert when category == MediaCategory.Document
                && sourcePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
                && !targetFormat.Equals("pdf", StringComparison.OrdinalIgnoreCase) =>
                Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(sourcePath)),
            _ => Path.Combine(outputDirectory, $"{Path.GetFileNameWithoutExtension(sourcePath)}.{targetFormat.ToLowerInvariant()}")
        };
    }
}
