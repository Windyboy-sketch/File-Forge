using System.Drawing;
using FFMpegCore;
using FFMpegCore.Enums;
using FileConverter.Core.Interfaces;
using FileConverter.Core.Models;
using FileConverter.Infrastructure.Ffmpeg;
using Microsoft.Extensions.Logging;

namespace FileConverter.Infrastructure.Converters;

public sealed class VideoConversionService : IConversionService
{
    private static readonly HashSet<string> SupportedExtensions =
    [
        ".mp4", ".mkv", ".avi", ".mov", ".webm", ".gif"
    ];

    private readonly ISettingsService _settingsService;
    private readonly FfmpegLocator _ffmpegLocator;
    private readonly ILogger<VideoConversionService> _logger;

    public VideoConversionService(
        ISettingsService settingsService,
        FfmpegLocator ffmpegLocator,
        ILogger<VideoConversionService> logger)
    {
        _settingsService = settingsService;
        _ffmpegLocator = ffmpegLocator;
        _logger = logger;
    }

    public MediaCategory Category => MediaCategory.Video;

    public bool CanHandle(string filePath, ConversionOperation operation)
    {
        return operation == ConversionOperation.Convert
            && SupportedExtensions.Contains(Path.GetExtension(filePath));
    }

    public async Task ConvertAsync(ConversionJob job, IProgress<double> progress, CancellationToken cancellationToken)
    {
        var outputPath = job.OutputPath ?? throw new InvalidOperationException("Output path is required.");
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        var targetFormat = job.TargetFormat.ToLowerInvariant();
        _logger.LogInformation(
            "Starting video conversion: {Source} -> {Output} (format: {Format})",
            job.SourcePath, outputPath, targetFormat);

        progress.Report(5);
        await _ffmpegLocator.EnsureConfiguredAsync(cancellationToken);
        progress.Report(10);

        if (targetFormat == "gif")
        {
            _logger.LogDebug("Using GIF snapshot conversion.");
            await FFMpeg.GifSnapshotAsync(job.SourcePath, outputPath, new Size(480, -1), TimeSpan.Zero);
            progress.Report(100);
            return;
        }

        var (videoCodec, container) = GetCodecAndContainer(targetFormat);

        var arguments = FFMpegArguments
            .FromFileInput(job.SourcePath)
            .OutputToFile(outputPath, overwrite: _settingsService.Current.OverwriteExistingFiles, options =>
            {
                options.WithVideoCodec(videoCodec);
                options.WithAudioCodec(AudioCodec.Aac);
                options.ForceFormat(container);
            });

        await FfmpegConversionRunner.ExecuteAsync(
            arguments,
            job.SourcePath,
            progress,
            _logger,
            cancellationToken);

        _logger.LogInformation("Video conversion completed: {Output}", outputPath);
    }

    private static (Codec VideoCodec, string Container) GetCodecAndContainer(string format) => format switch
    {
        "mp4" => (VideoCodec.LibX264, "mp4"),
        "mkv" => (VideoCodec.LibX264, "matroska"),
        "avi" => (VideoCodec.LibX264, "avi"),
        "mov" => (VideoCodec.LibX264, "mov"),
        "webm" => (VideoCodec.LibVpx, "webm"),
        _ => throw new NotSupportedException($"Video format '{format}' is not supported.")
    };
}
