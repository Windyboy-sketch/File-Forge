using FFMpegCore;
using FFMpegCore.Extensions.Downloader;
using FFMpegCore.Extensions.Downloader.Enums;
using FileConverter.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace FileConverter.Infrastructure.Ffmpeg;

/// <summary>
/// Locates, downloads if needed, and configures FFmpeg binaries for FFMpegCore.
/// </summary>
public sealed class FfmpegLocator
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<FfmpegLocator> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _configured;

    public FfmpegLocator(ISettingsService settingsService, ILogger<FfmpegLocator> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    public async Task EnsureConfiguredAsync(CancellationToken cancellationToken = default)
    {
        if (_configured)
            return;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_configured)
                return;

            var binaryFolder = await ResolveBinaryFolderAsync(cancellationToken);
            _logger.LogInformation("Using FFmpeg binaries from: {Folder}", binaryFolder);

            GlobalFFOptions.Configure(options =>
            {
                options.BinaryFolder = binaryFolder;
                options.TemporaryFilesFolder = Path.Combine(Path.GetTempPath(), "FileConverter");
                Directory.CreateDirectory(options.TemporaryFilesFolder);
            });

            _configured = true;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<string> ResolveBinaryFolderAsync(CancellationToken cancellationToken)
    {
        var configuredPath = _settingsService.Current.FfmpegPath;
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            if (HasBinaries(configuredPath))
            {
                _logger.LogDebug("Found FFmpeg in configured path: {Path}", configuredPath);
                return configuredPath;
            }

            _logger.LogWarning("Configured FFmpeg path does not contain ffmpeg.exe/ffprobe.exe: {Path}", configuredPath);
        }

        var bundledFolder = Path.Combine(AppContext.BaseDirectory, "ffmpeg");
        if (HasBinaries(bundledFolder))
        {
            _logger.LogDebug("Using FFmpeg bundled with the application: {Path}", bundledFolder);
            return bundledFolder;
        }

        var onPath = FindOnSystemPath();
        if (onPath is not null)
        {
            _logger.LogDebug("Found FFmpeg on system PATH: {Path}", onPath);
            return onPath;
        }

        var appDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FileConverter",
            "ffmpeg");

        if (HasBinaries(appDataFolder))
        {
            _logger.LogDebug("Using cached FFmpeg from: {Path}", appDataFolder);
            return appDataFolder;
        }

        Directory.CreateDirectory(appDataFolder);
        _logger.LogInformation("FFmpeg not found. Downloading binaries to {Folder}...", appDataFolder);

        GlobalFFOptions.Configure(options => options.BinaryFolder = appDataFolder);

        try
        {
            var downloaded = await FFMpegDownloader.DownloadBinaries(
                FFMpegVersions.LatestAvailable,
                FFMpegBinaries.FFMpeg | FFMpegBinaries.FFProbe);

            _logger.LogDebug("Downloaded FFmpeg files: {Files}", string.Join(", ", downloaded));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download FFmpeg binaries.");
            throw new InvalidOperationException(
                "FFmpeg is not installed and automatic download failed. " +
                "Install FFmpeg from https://ffmpeg.org/download.html, add it to PATH, " +
                "or set ffmpegPath in %AppData%\\FileConverter\\settings.json to the folder containing ffmpeg.exe.",
                ex);
        }

        if (!HasBinaries(appDataFolder))
        {
            throw new InvalidOperationException(
                $"FFmpeg download completed but binaries were not found in '{appDataFolder}'. " +
                "Install FFmpeg manually and set ffmpegPath in settings.");
        }

        _logger.LogInformation("FFmpeg downloaded successfully.");
        return appDataFolder;
    }

    private static bool HasBinaries(string folder)
    {
        if (!Directory.Exists(folder))
            return false;

        return File.Exists(Path.Combine(folder, "ffmpeg.exe"))
            && File.Exists(Path.Combine(folder, "ffprobe.exe"));
    }

    private static string? FindOnSystemPath()
    {
        var pathVariable = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathVariable))
            return null;

        foreach (var entry in pathVariable.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (HasBinaries(entry))
                return entry;
        }

        return null;
    }
}
