using FileConverter.Core.Interfaces;
using FileConverter.Core.Models;
using FileConverter.Core.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Formats.Webp;

namespace FileConverter.Infrastructure.Converters;

public sealed class ImageConversionService : IConversionService
{
    private static readonly HashSet<string> SupportedExtensions =
    [
        ".png", ".jpg", ".jpeg", ".webp", ".bmp", ".tif", ".tiff", ".gif", ".ico"
    ];

    public MediaCategory Category => MediaCategory.Image;

    public bool CanHandle(string filePath, ConversionOperation operation)
    {
        if (operation != ConversionOperation.Convert)
            return false;

        return SupportedExtensions.Contains(Path.GetExtension(filePath));
    }

    public async Task ConvertAsync(ConversionJob job, IProgress<double> progress, CancellationToken cancellationToken)
    {
        var outputPath = job.OutputPath ?? throw new InvalidOperationException("Output path is required.");
        var targetFormat = job.TargetFormat.ToLowerInvariant();

        progress.Report(10);

        await using var inputStream = File.OpenRead(job.SourcePath);
        using var image = await Image.LoadAsync(inputStream, cancellationToken);

        progress.Report(50);

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        await using var outputStream = File.Create(outputPath);
        var encoder = GetEncoder(targetFormat);
        await image.SaveAsync(outputStream, encoder, cancellationToken);

        progress.Report(100);
    }

    private static IImageEncoder GetEncoder(string format) => format switch
    {
        "png" => new PngEncoder(),
        "jpg" or "jpeg" => new JpegEncoder { Quality = 90 },
        "webp" => new WebpEncoder { Quality = 90 },
        "bmp" => new BmpEncoder(),
        "tiff" or "tif" => new TiffEncoder(),
        "gif" => new GifEncoder(),
        "ico" => new PngEncoder(),
        _ => throw new NotSupportedException($"Image format '{format}' is not supported.")
    };
}
