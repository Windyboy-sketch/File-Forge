# File-Forge
Created by Grey box Softworks

Windows desktop app for batch file conversion. Drag files in, pick a format, convert.

Built with C# / .NET 9 and WPF.

## What it converts

| Type | Formats |
|------|---------|
| Images | PNG, JPG, WEBP, BMP, TIFF, GIF, ICO |
| Audio | MP3, WAV, FLAC, OGG, AAC, M4A |
| Video | MP4, MKV, AVI, MOV, WEBM, GIF |
| Documents | PDF to/from images, merge PDFs, split PDFs |

## Requirements

- Windows 10 or later (64-bit)
- For development: [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

Audio and video need FFmpeg. The Windows installer bundles it. Running from source will download FFmpeg on first use if it is not already on your system.

## Install (Windows)

Download the latest `FileConverter-Setup-*.exe` from [Releases](https://github.com/Windyboy-sketch/File-Forge/releases).

To build the installer from source:

```powershell
powershell -ExecutionPolicy Bypass -File installer\build-installer.ps1
```

The setup exe lands in `installer\output\`. You need [Inno Setup 6](https://jrsoftware.org/isinfo.php) installed.

## Run from source

```powershell
git clone https://github.com/YOUR_USERNAME/File-Converter.git
cd File-Converter
dotnet run --project src/FileConverter
```

## How to use

1. Pick a category and output format.
2. Set an output folder (saved between sessions).
3. Drop files or folders on the window, or use **Add Files**.
4. Click **Start Conversion**.

For PDFs, change **Operation** to merge or split instead of convert.

- **Merge** — add all PDFs at once. One job, one output file.
- **Split** — one PDF per page in the output folder (`name_page001.pdf`, etc.).

## FFmpeg

Lookup order:

1. `ffmpegPath` in settings
2. `ffmpeg\` next to the installed exe (setup includes this)
3. System PATH
4. Auto-download to `%AppData%\FileConverter\ffmpeg`

Manual override in `%AppData%\FileConverter\settings.json`:

```json
{
  "ffmpegPath": "C:\\tools\\ffmpeg\\bin"
}
```

Needs `ffmpeg.exe` and `ffprobe.exe` in that folder.

## Settings

File: `%AppData%\FileConverter\settings.json`

Logs: `%AppData%\FileConverter\logs\`

## Project layout

```
FileConverter.sln
├── src/FileConverter/                 WPF UI
├── src/FileConverter.Core/          models, interfaces
├── src/FileConverter.Infrastructure/    converters, queue, settings
├── installer/                       Inno Setup + build script
├── tools/FfmpegBootstrap/           downloads FFmpeg for the installer
└── scripts/                         utility scripts
```

## Dependencies

- [SixLabors.ImageSharp](https://github.com/SixLabors/ImageSharp)
- [FFMpegCore](https://github.com/rosenbjerg/FFMpegCore)
- [PdfSharpCore](https://github.com/ststeiger/PdfSharpCore)
- [PDFtoImage](https://github.com/sungam3r/PDFtoImage)
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)

## Installer build notes

Skip re-downloading FFmpeg if you already have it in `installer\redist\ffmpeg\`:

```powershell
powershell -ExecutionPolicy Bypass -File installer\build-installer.ps1 -SkipFfmpeg
```
