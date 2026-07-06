using FFMpegCore;
using FFMpegCore.Extensions.Downloader;
using FFMpegCore.Extensions.Downloader.Enums;

var targetDir = args.Length > 0
    ? Path.GetFullPath(args[0])
    : Path.Combine(Environment.CurrentDirectory, "ffmpeg");

Directory.CreateDirectory(targetDir);

var ffmpegExe = Path.Combine(targetDir, "ffmpeg.exe");
var ffprobeExe = Path.Combine(targetDir, "ffprobe.exe");

if (File.Exists(ffmpegExe) && File.Exists(ffprobeExe))
{
    Console.WriteLine($"FFmpeg already present in {targetDir}");
    return 0;
}

Console.WriteLine($"Downloading FFmpeg to {targetDir}...");

GlobalFFOptions.Configure(options => options.BinaryFolder = targetDir);

var downloaded = await FFMpegDownloader.DownloadBinaries(
    FFMpegVersions.LatestAvailable,
    FFMpegBinaries.FFMpeg | FFMpegBinaries.FFProbe);

if (!File.Exists(ffmpegExe) || !File.Exists(ffprobeExe))
{
    Console.Error.WriteLine("Download finished but ffmpeg.exe or ffprobe.exe was not found.");
    return 1;
}

Console.WriteLine($"Downloaded {downloaded.Count} file(s): {string.Join(", ", downloaded)}");
return 0;
