using FileConverter.Core.Models;

namespace FileConverter.Core.Services;

public static class FormatRegistry
{
    private static readonly Dictionary<string, MediaCategory> ExtensionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = MediaCategory.Image,
        [".jpg"] = MediaCategory.Image,
        [".jpeg"] = MediaCategory.Image,
        [".webp"] = MediaCategory.Image,
        [".bmp"] = MediaCategory.Image,
        [".tif"] = MediaCategory.Image,
        [".tiff"] = MediaCategory.Image,
        [".gif"] = MediaCategory.Image,
        [".ico"] = MediaCategory.Image,

        [".mp3"] = MediaCategory.Audio,
        [".wav"] = MediaCategory.Audio,
        [".flac"] = MediaCategory.Audio,
        [".ogg"] = MediaCategory.Audio,
        [".aac"] = MediaCategory.Audio,
        [".m4a"] = MediaCategory.Audio,

        [".mp4"] = MediaCategory.Video,
        [".mkv"] = MediaCategory.Video,
        [".avi"] = MediaCategory.Video,
        [".mov"] = MediaCategory.Video,
        [".webm"] = MediaCategory.Video,

        [".pdf"] = MediaCategory.Document
    };

    public static readonly string[] ImageFormats = ["png", "jpg", "webp", "bmp", "tiff", "gif", "ico"];
    public static readonly string[] AudioFormats = ["mp3", "wav", "flac", "ogg", "aac", "m4a"];
    public static readonly string[] VideoFormats = ["mp4", "mkv", "avi", "mov", "webm", "gif"];
    public static readonly string[] DocumentFormats = ["pdf", "png", "jpg", "webp"];

    public static MediaCategory GetCategory(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return ExtensionMap.TryGetValue(extension, out var category) ? category : MediaCategory.Unknown;
    }

    public static bool IsSupported(string filePath) => GetCategory(filePath) != MediaCategory.Unknown;

    public static IReadOnlyList<string> GetFormatsForCategory(MediaCategory category) => category switch
    {
        MediaCategory.Image => ImageFormats,
        MediaCategory.Audio => AudioFormats,
        MediaCategory.Video => VideoFormats,
        MediaCategory.Document => DocumentFormats,
        _ => []
    };

    public static string GetDefaultFormat(MediaCategory category) => category switch
    {
        MediaCategory.Image => "png",
        MediaCategory.Audio => "mp3",
        MediaCategory.Video => "mp4",
        MediaCategory.Document => "pdf",
        _ => "png"
    };

    public static IEnumerable<string> EnumerateSupportedFiles(string path)
    {
        if (File.Exists(path))
        {
            if (IsSupported(path))
                yield return path;
            yield break;
        }

        if (!Directory.Exists(path))
            yield break;

        foreach (var file in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
        {
            if (IsSupported(file))
                yield return file;
        }
    }
}
