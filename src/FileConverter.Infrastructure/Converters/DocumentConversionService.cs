using FileConverter.Core.Interfaces;
using FileConverter.Core.Models;
using Microsoft.Extensions.Logging;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using PDFtoImage;
using SixLabors.ImageSharp;

namespace FileConverter.Infrastructure.Converters;

public sealed class DocumentConversionService : IConversionService
{
    private readonly ILogger<DocumentConversionService> _logger;

    public DocumentConversionService(ILogger<DocumentConversionService> logger)
    {
        _logger = logger;
    }
    private static readonly HashSet<string> ImageExtensions =
    [
        ".png", ".jpg", ".jpeg", ".webp", ".bmp", ".tif", ".tiff", ".gif"
    ];

    public MediaCategory Category => MediaCategory.Document;

    public bool CanHandle(string filePath, ConversionOperation operation)
    {
        var extension = Path.GetExtension(filePath);

        return operation switch
        {
            ConversionOperation.MergePdf => extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase),
            ConversionOperation.SplitPdf => extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase),
            ConversionOperation.Convert => extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase)
                || ImageExtensions.Contains(extension),
            _ => false
        };
    }

    public async Task ConvertAsync(ConversionJob job, IProgress<double> progress, CancellationToken cancellationToken)
    {
        switch (job.Operation)
        {
            case ConversionOperation.MergePdf:
                await MergePdfsAsync(job, progress, cancellationToken);
                break;
            case ConversionOperation.SplitPdf:
                await SplitPdfAsync(job, progress, cancellationToken);
                break;
            case ConversionOperation.Convert:
                await ConvertDocumentAsync(job, progress, cancellationToken);
                break;
            default:
                throw new NotSupportedException($"Operation '{job.Operation}' is not supported for documents.");
        }
    }

    private static async Task ConvertDocumentAsync(ConversionJob job, IProgress<double> progress, CancellationToken cancellationToken)
    {
        var sourceExtension = Path.GetExtension(job.SourcePath);
        var targetFormat = job.TargetFormat.ToLowerInvariant();

        if (sourceExtension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            await PdfToImagesAsync(job, progress, cancellationToken);
        }
        else if (targetFormat == "pdf")
        {
            await ImagesToPdfAsync(job, progress, cancellationToken);
        }
        else
        {
            throw new NotSupportedException("Document conversion only supports PDF ↔ images.");
        }
    }

    private static async Task PdfToImagesAsync(ConversionJob job, IProgress<double> progress, CancellationToken cancellationToken)
    {
        var outputDir = job.OutputPath ?? throw new InvalidOperationException("Output path is required.");
        Directory.CreateDirectory(outputDir);

        var targetFormat = job.TargetFormat.ToLowerInvariant();
        var baseName = Path.GetFileNameWithoutExtension(job.SourcePath);
        var renderOptions = new RenderOptions(Dpi: 300);

        await Task.Run(() =>
        {
            using var pdfStream = File.OpenRead(job.SourcePath);
            var pageCount = Conversion.GetPageCount(pdfStream, leaveOpen: true);

            for (var i = 0; i < pageCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var outputFile = Path.Combine(outputDir, $"{baseName}_page{i + 1}.{targetFormat}");
                pdfStream.Position = 0;

                SavePdfPage(pdfStream, i, outputFile, targetFormat, renderOptions);

                progress.Report((i + 1) * 100.0 / pageCount);
            }
        }, cancellationToken);
    }

    private static void SavePdfPage(Stream pdfStream, int pageIndex, string outputFile, string format, RenderOptions options)
    {
        switch (format)
        {
            case "png":
                Conversion.SavePng(outputFile, pdfStream, pageIndex, leaveOpen: true, password: null, options);
                break;
            case "jpg" or "jpeg":
                Conversion.SaveJpeg(outputFile, pdfStream, pageIndex, leaveOpen: true, password: null, options);
                break;
            case "webp":
                Conversion.SaveWebp(outputFile, pdfStream, pageIndex, leaveOpen: true, password: null, options);
                break;
            default:
                Conversion.SavePng(outputFile, pdfStream, pageIndex, leaveOpen: true, password: null, options);
                break;
        }
    }

    private static async Task ImagesToPdfAsync(ConversionJob job, IProgress<double> progress, CancellationToken cancellationToken)
    {
        var outputPath = job.OutputPath ?? throw new InvalidOperationException("Output path is required.");
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        var imagePaths = new List<string> { job.SourcePath };
        if (job.AdditionalSourcePaths is not null)
            imagePaths.AddRange(job.AdditionalSourcePaths);

        await Task.Run(() =>
        {
            using var document = new PdfDocument();

            for (var i = 0; i < imagePaths.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var imagePath = imagePaths[i];
                var page = document.AddPage();

                using var image = Image.Load(imagePath);
                var horizontalDpi = image.Metadata.HorizontalResolution > 0 ? image.Metadata.HorizontalResolution : 96;
                var verticalDpi = image.Metadata.VerticalResolution > 0 ? image.Metadata.VerticalResolution : 96;

                page.Width = image.Width * 72.0 / horizontalDpi;
                page.Height = image.Height * 72.0 / verticalDpi;

                using var gfx = PdfSharpCore.Drawing.XGraphics.FromPdfPage(page);
                using var stream = File.OpenRead(imagePath);
                var xImage = PdfSharpCore.Drawing.XImage.FromStream(() => stream);
                gfx.DrawImage(xImage, 0, 0, page.Width, page.Height);

                progress.Report((i + 1) * 100.0 / imagePaths.Count);
            }

            document.Save(outputPath);
        }, cancellationToken);

        progress.Report(100);
    }

    private async Task MergePdfsAsync(ConversionJob job, IProgress<double> progress, CancellationToken cancellationToken)
    {
        var outputPath = job.OutputPath ?? throw new InvalidOperationException("Output path is required.");

        if (Directory.Exists(outputPath))
            throw new InvalidOperationException($"Output path is a folder, expected a file: '{outputPath}'.");

        var outputDirectory = Path.GetDirectoryName(outputPath)
            ?? throw new InvalidOperationException($"Invalid output path: '{outputPath}'.");

        Directory.CreateDirectory(outputDirectory);

        var sources = new List<string> { job.SourcePath };
        if (job.AdditionalSourcePaths is not null)
            sources.AddRange(job.AdditionalSourcePaths);

        _logger.LogInformation("Merging {Count} PDF(s) into {Output}", sources.Count, outputPath);

        await Task.Run(() =>
        {
            using var outputDocument = new PdfDocument();

            for (var i = 0; i < sources.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var sourcePath = sources[i];
                if (!File.Exists(sourcePath))
                    throw new FileNotFoundException($"PDF not found: '{sourcePath}'", sourcePath);

                _logger.LogDebug("Adding PDF {Index}/{Total}: {Source}", i + 1, sources.Count, sourcePath);

                try
                {
                    using var inputDocument = PdfReader.Open(sourcePath, PdfDocumentOpenMode.Import);
                    for (var pageIndex = 0; pageIndex < inputDocument.PageCount; pageIndex++)
                        outputDocument.AddPage(inputDocument.Pages[pageIndex]);

                    _logger.LogDebug("Added {Pages} page(s) from {Source}", inputDocument.PageCount, sourcePath);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to read PDF '{sourcePath}': {ex.Message}", ex);
                }

                progress.Report((i + 1) * 90.0 / sources.Count);
            }

            if (outputDocument.PageCount == 0)
                throw new InvalidOperationException("No pages were merged. Check that the input PDFs are valid.");

            if (File.Exists(outputPath))
                File.Delete(outputPath);

            outputDocument.Save(outputPath);
            _logger.LogInformation("Merged {Pages} page(s) into {Output}", outputDocument.PageCount, outputPath);
        }, cancellationToken);

        progress.Report(100);
    }

    private async Task SplitPdfAsync(ConversionJob job, IProgress<double> progress, CancellationToken cancellationToken)
    {
        var outputDir = job.OutputPath ?? throw new InvalidOperationException("Output path is required.");

        if (File.Exists(outputDir))
            throw new InvalidOperationException($"Output path must be a folder for split: '{outputDir}'.");

        Directory.CreateDirectory(outputDir);

        if (!File.Exists(job.SourcePath))
            throw new FileNotFoundException($"PDF not found: '{job.SourcePath}'", job.SourcePath);

        var prefix = GetSplitFilePrefix(job.SourcePath);
        _logger.LogInformation("Splitting PDF {Source} into folder {Output}", job.SourcePath, outputDir);

        await Task.Run(() =>
        {
            int pageCount;
            try
            {
                using var probe = PdfReader.Open(job.SourcePath, PdfDocumentOpenMode.Import);
                pageCount = probe.PageCount;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to read PDF '{job.SourcePath}': {ex.Message}", ex);
            }

            if (pageCount == 0)
                throw new InvalidOperationException($"PDF has no pages: '{job.SourcePath}'.");

            for (var i = 0; i < pageCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var outputFile = Path.Combine(outputDir, $"{prefix}_page{i + 1:D3}.pdf");

                if (File.Exists(outputFile))
                    File.Delete(outputFile);

                using var inputDocument = PdfReader.Open(job.SourcePath, PdfDocumentOpenMode.Import);
                using var outputDocument = new PdfDocument
                {
                    Version = inputDocument.Version
                };
                outputDocument.AddPage(inputDocument.Pages[i]);
                outputDocument.Save(outputFile);

                _logger.LogDebug("Saved split page {Page}/{Total} to {Output}", i + 1, pageCount, outputFile);
                progress.Report((i + 1) * 100.0 / pageCount);
            }

            _logger.LogInformation("Split {Source} into {Count} file(s)", job.SourcePath, pageCount);
        }, cancellationToken);
    }

    private static string GetSplitFilePrefix(string sourcePath)
    {
        var baseName = SanitizeFileName(Path.GetFileNameWithoutExtension(sourcePath));
        var parentName = Path.GetFileName(Path.GetDirectoryName(Path.GetFullPath(sourcePath)));

        if (string.IsNullOrWhiteSpace(parentName))
            return baseName;

        return $"{baseName}_{SanitizeFileName(parentName)}";
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var invalidChar in Path.GetInvalidFileNameChars())
            name = name.Replace(invalidChar, '_');

        return name;
    }
}
