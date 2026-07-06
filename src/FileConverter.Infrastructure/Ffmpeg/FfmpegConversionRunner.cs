using FFMpegCore;
using Microsoft.Extensions.Logging;

namespace FileConverter.Infrastructure.Ffmpeg;

/// <summary>
/// Runs FFmpeg conversions with logging, cancellation, and progress reporting.
/// </summary>
public static class FfmpegConversionRunner
{
    public static async Task ExecuteAsync(
        FFMpegArgumentProcessor processor,
        string sourcePath,
        IProgress<double> progress,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Analysing media: {Source}", sourcePath);

        var mediaInfo = await FFProbe.AnalyseAsync(sourcePath, cancellationToken: cancellationToken);
        var streamCount = mediaInfo.VideoStreams.Count + mediaInfo.AudioStreams.Count;
        logger.LogDebug(
            "Media info — duration: {Duration}, format: {Format}, streams: {Streams}",
            mediaInfo.Duration,
            mediaInfo.Format.FormatName,
            streamCount);

        progress.Report(15);

        var lastLoggedPercent = -1;
        await processor
            .NotifyOnProgress(percent =>
            {
                var mapped = 15 + percent * 0.8;
                progress.Report(mapped);

                var rounded = (int)percent;
                if (rounded >= lastLoggedPercent + 10)
                {
                    lastLoggedPercent = rounded;
                    logger.LogDebug("FFmpeg progress: {Percent:F0}%", percent);
                }
            }, mediaInfo.Duration)
            .NotifyOnError(error => logger.LogWarning("FFmpeg stderr: {Error}", error))
            .NotifyOnOutput(output => logger.LogTrace("FFmpeg stdout: {Output}", output))
            .CancellableThrough(cancellationToken)
            .ProcessAsynchronously();

        progress.Report(100);
        logger.LogDebug("FFmpeg process completed for {Source}", sourcePath);
    }
}
