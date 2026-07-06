namespace FileConverter.Core.Models;

public sealed class AppSettings
{
    public string OutputDirectory { get; set; } = string.Empty;
    public string DefaultImageFormat { get; set; } = "png";
    public string DefaultAudioFormat { get; set; } = "mp3";
    public string DefaultVideoFormat { get; set; } = "mp4";
    public int MaxParallelConversions { get; set; } = 2;
    public bool OverwriteExistingFiles { get; set; }
    public string? FfmpegPath { get; set; }
    public AppTheme Theme { get; set; } = AppTheme.Dark;
    public bool UseMicaBackdrop { get; set; } = true;
}
