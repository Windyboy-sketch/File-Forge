using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileConverter.Core.Interfaces;
using FileConverter.Core.Models;
using FileConverter.Core.Services;
using FileConverter.Helpers;
using Microsoft.Extensions.Logging;

namespace FileConverter.ViewModels;

/// <summary>
/// Primary view model for the conversion workspace.
/// </summary>
public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly IConversionCoordinator _coordinator;
    private readonly IConversionQueueService _queueService;
    private readonly ISettingsService _settingsService;
    private readonly IFileDialogService _fileDialogService;
    private readonly IThemeService _themeService;
    private readonly IToastService _toastService;
    private readonly ILogger<MainViewModel> _logger;
    private readonly ToastViewModel _toast = new();
    private CancellationTokenSource? _conversionCts;
    private bool _disposed;

    public MainViewModel(
        IConversionCoordinator coordinator,
        IConversionQueueService queueService,
        ISettingsService settingsService,
        IFileDialogService fileDialogService,
        IThemeService themeService,
        IToastService toastService,
        ILogger<MainViewModel> logger)
    {
        _coordinator = coordinator;
        _queueService = queueService;
        _settingsService = settingsService;
        _fileDialogService = fileDialogService;
        _themeService = themeService;
        _toastService = toastService;
        _logger = logger;

        _coordinator.JobUpdated += OnJobUpdated;
        _toastService.ToastRequested += OnToastRequested;

        AvailableFormats = new ObservableCollection<string>(FormatRegistry.ImageFormats);
    }

    public ObservableCollection<ConversionItemViewModel> Queue { get; } = [];

    public ToastViewModel Toast => _toast;

    [ObservableProperty]
    private string _outputDirectory = string.Empty;

    [ObservableProperty]
    private string _selectedFormat = "png";

    [ObservableProperty]
    private MediaCategory _selectedCategory = MediaCategory.Image;

    [ObservableProperty]
    private bool _isConverting;

    [ObservableProperty]
    private string _statusMessage = "Drop files or folders to begin.";

    [ObservableProperty]
    private ConversionOperation _selectedOperation = ConversionOperation.Convert;

    public ObservableCollection<string> AvailableFormats { get; }

    public IReadOnlyList<MediaCategory> Categories { get; } =
    [
        MediaCategory.Image,
        MediaCategory.Audio,
        MediaCategory.Video,
        MediaCategory.Document
    ];

    public IReadOnlyList<ConversionOperation> DocumentOperations { get; } =
    [
        ConversionOperation.Convert,
        ConversionOperation.MergePdf,
        ConversionOperation.SplitPdf
    ];

    public async Task InitializeAsync()
    {
        await _settingsService.LoadAsync();
        OutputDirectory = _settingsService.Current.OutputDirectory;
        UpdateFormatsForCategory(SelectedCategory);
    }

    partial void OnSelectedCategoryChanged(MediaCategory value)
    {
        UpdateFormatsForCategory(value);
        SelectedOperation = ConversionOperation.Convert;
    }

    partial void OnOutputDirectoryChanged(string value)
    {
        if (_disposed || string.IsNullOrWhiteSpace(value))
            return;

        _settingsService.Current.OutputDirectory = value;
        _ = _settingsService.SaveAsync();
    }

    [RelayCommand]
    private void BrowseOutputFolder()
    {
        var folder = _fileDialogService.PickOutputFolder(OutputDirectory);
        if (!string.IsNullOrWhiteSpace(folder))
        {
            OutputDirectory = folder;
            _toastService.ShowInfo("Output folder updated.");
        }
    }

    [RelayCommand]
    private void AddFiles()
    {
        var filter = BuildFileFilter(SelectedCategory);
        var files = _fileDialogService.PickFiles(filter);
        AddPaths(files);
    }

    [RelayCommand]
    private void DropFiles(object? parameter)
    {
        if (parameter is string[] paths)
            AddPaths(paths);
        else if (parameter is IEnumerable<string> pathsEnumerable)
            AddPaths(pathsEnumerable);
    }

    [RelayCommand]
    private async Task StartConversionAsync()
    {
        if (Queue.Count == 0)
        {
            StatusMessage = "Add files to the queue first.";
            _toastService.ShowWarning(StatusMessage);
            return;
        }

        if (string.IsNullOrWhiteSpace(OutputDirectory))
        {
            StatusMessage = "Select an output folder.";
            _toastService.ShowWarning(StatusMessage);
            return;
        }

        IsConverting = true;
        StatusMessage = "Converting...";
        _conversionCts = new CancellationTokenSource();

        try
        {
            await _coordinator.StartAsync(_conversionCts.Token);
            StatusMessage = "Conversion complete.";
            _toastService.ShowSuccess("All conversions finished.");
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Conversion cancelled.";
            _toastService.ShowWarning(StatusMessage);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            _toastService.ShowError(ex.Message);
            _logger.LogError(ex, "Conversion batch failed.");
        }
        finally
        {
            IsConverting = false;
            _conversionCts?.Dispose();
            _conversionCts = null;
        }
    }

    [RelayCommand]
    private async Task CancelConversionAsync()
    {
        if (_conversionCts is not null)
            await _conversionCts.CancelAsync();

        await _coordinator.CancelAsync();
    }

    [RelayCommand]
    private void RemoveItem(ConversionItemViewModel? item)
    {
        if (item is null)
            return;

        _coordinator.Remove(item.Job.Id);
        Queue.Remove(item);
    }

    [RelayCommand]
    private void ClearCompleted()
    {
        _coordinator.ClearCompleted();
        var toRemove = Queue.Where(q => q.Status is ConversionStatus.Completed or ConversionStatus.Failed or ConversionStatus.Cancelled).ToList();
        foreach (var item in toRemove)
            Queue.Remove(item);

        if (toRemove.Count > 0)
            _toastService.ShowInfo($"Removed {toRemove.Count} completed item(s).");
    }

    [RelayCommand]
    private void ClearAll()
    {
        if (IsConverting)
            return;

        _coordinator.ClearAll();
        Queue.Clear();
        StatusMessage = "Queue cleared.";
    }

    public void AddPaths(IEnumerable<string> paths)
    {
        var added = _queueService.EnqueueFromPaths(
            paths,
            SelectedCategory,
            SelectedFormat,
            SelectedOperation,
            OutputDirectory);

        foreach (var job in added)
            Queue.Add(new ConversionItemViewModel(job));

        if (added.Count == 0)
        {
            _toastService.ShowWarning("No supported files were found for the selected category.");
            return;
        }

        StatusMessage = $"{Queue.Count} file(s) in queue.";
        _toastService.ShowSuccess($"Added {added.Count} file(s) to the queue.");
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _coordinator.JobUpdated -= OnJobUpdated;
        _toastService.ToastRequested -= OnToastRequested;
        _conversionCts?.Cancel();
        _conversionCts?.Dispose();
        _disposed = true;
    }

    private static string BuildFileFilter(MediaCategory category) => category switch
    {
        MediaCategory.Image => "Images|*.png;*.jpg;*.jpeg;*.webp;*.bmp;*.tif;*.tiff;*.gif;*.ico",
        MediaCategory.Audio => "Audio|*.mp3;*.wav;*.flac;*.ogg;*.aac;*.m4a",
        MediaCategory.Video => "Video|*.mp4;*.mkv;*.avi;*.mov;*.webm;*.gif",
        MediaCategory.Document => "Documents|*.pdf;*.png;*.jpg;*.jpeg;*.webp",
        _ => "All files|*.*"
    };

    private void UpdateFormatsForCategory(MediaCategory category)
    {
        AvailableFormats.Clear();
        foreach (var format in FormatRegistry.GetFormatsForCategory(category))
            AvailableFormats.Add(format);

        SelectedFormat = FormatRegistry.GetDefaultFormat(category);
    }

    private void OnJobUpdated(object? sender, ConversionJob job)
    {
        Application.Current.Dispatcher.BeginInvokeSafe(() =>
        {
            var item = Queue.FirstOrDefault(q => q.Job.Id == job.Id);
            item?.Refresh();
        });
    }

    private void OnToastRequested(object? sender, ToastMessage message)
    {
        Application.Current.Dispatcher.BeginInvokeSafe(() =>
        {
            _toast.Message = message.Text;
            _toast.Severity = message.Severity;
            _toast.IsVisible = true;
        });
    }
}
