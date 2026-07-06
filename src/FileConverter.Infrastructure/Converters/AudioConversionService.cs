using FFMpegCore;
using FFMpegCore.Enums;
using FileConverter.Core.Interfaces;
using FileConverter.Core.Models;
using FileConverter.Infrastructure.Ffmpeg;
using Microsoft.Extensions.Logging;

namespace FileConverter.Infrastructure.Converters;

public sealed class AudioConversionService : IConversionService
{
    private static readonly HashSet<string> SupportedExtensions =
    [
        ".mp3", ".wav", ".flac", ".ogg", ".aac", ".m4a"
    ];

    private readonly ISettingsService _settingsService;
    private readonly FfmpegLocator _ffmpegLocator;
    private readonly ILogger<AudioConversionService> _logger;

    public AudioConversionService(
        ISettingsService settingsService,
        FfmpegLocator ffmpegLocator,
        ILogger<AudioConversionService> logger)
    {
        _settingsService = settingsService;
        _ffmpegLocator = ffmpegLocator;
        _logger = logger;
    }

    public MediaCategory Category => MediaCategory.Audio;

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
            "Starting audio conversion: {Source} -> {Output} (format: {Format})",
            job.SourcePath, outputPath, targetFormat);

        progress.Report(5);

        await _ffmpegLocator.EnsureConfiguredAsync(cancellationToken);
        progress.Report(10);

        var arguments = FFMpegArguments
            .FromFileInput(job.SourcePath)
            .OutputToFile(outputPath, overwrite: _settingsService.Current.OverwriteExistingFiles, options =>
            {
                switch (targetFormat)
                {
                    case "mp3":
                        options.WithAudioCodec(AudioCodec.LibMp3Lame);
                        options.ForceFormat("mp3");
                        break;
                    case "wav":
                        options.WithAudioCodec("pcm_s16le");
                        options.ForceFormat("wav");
                        break;
                    case "flac":
                        options.WithAudioCodec("flac");
                        options.ForceFormat("flac");
                        break;
                    case "ogg":
                        options.WithAudioCodec(AudioCodec.LibVorbis);
                        options.ForceFormat("ogg");
                        break;
                    case "aac":
                        options.WithAudioCodec(AudioCodec.Aac);
                        options.ForceFormat("adts");
                        break;
                    case "m4a":
                        options.WithAudioCodec(AudioCodec.Aac);
                        options.ForceFormat("ipod");
                        break;
                    default:
                        throw new NotSupportedException($"Audio format '{targetFormat}' is not supported.");
                }
            });

        await FfmpegConversionRunner.ExecuteAsync(
            arguments,
            job.SourcePath,
            progress,
            _logger,
            cancellationToken);

        _logger.LogInformation("Audio conversion completed: {Output}", outputPath);
    }
}
