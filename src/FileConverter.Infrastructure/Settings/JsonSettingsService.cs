using System.Text.Json;
using FileConverter.Core.Interfaces;
using FileConverter.Core.Models;

namespace FileConverter.Infrastructure.Settings;

public sealed class JsonSettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _settingsPath;

    public AppSettings Current { get; private set; } = new();

    public JsonSettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var folder = Path.Combine(appData, "FileConverter");
        Directory.CreateDirectory(folder);
        _settingsPath = Path.Combine(folder, "settings.json");

        if (string.IsNullOrWhiteSpace(Current.OutputDirectory))
            Current.OutputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FileConverter Output");
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_settingsPath))
        {
            await SaveAsync(cancellationToken);
            return;
        }

        await using var stream = File.OpenRead(_settingsPath);
        var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions, cancellationToken);
        Current = settings ?? new AppSettings();
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await using var stream = File.Create(_settingsPath);
        await JsonSerializer.SerializeAsync(stream, Current, JsonOptions, cancellationToken);
    }
}
