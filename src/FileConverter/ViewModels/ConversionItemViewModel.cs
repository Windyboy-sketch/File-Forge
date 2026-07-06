using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using FileConverter.Core.Models;

namespace FileConverter.ViewModels;

/// <summary>
/// View model wrapper for a queued conversion job displayed in the UI.
/// </summary>
public sealed partial class ConversionItemViewModel : ObservableObject
{
    public ConversionItemViewModel(ConversionJob job)
    {
        Job = job;
    }

    public ConversionJob Job { get; }

    public string FileName => Job.Operation switch
    {
        ConversionOperation.MergePdf when Job.AdditionalSourcePaths is { Count: > 0 } extras
            => $"Merge {extras.Count + 1} PDFs",
        _ => Path.GetFileName(Job.SourcePath)
    };

    public string SourcePath => Job.SourcePath;

    public string FileSizeText => GetFileSizeText(Job.SourcePath);

    public string CategoryLabel => Job.Category.ToString();

    public string TargetFormat
    {
        get => Job.TargetFormat;
        set
        {
            if (Job.TargetFormat != value)
            {
                Job.TargetFormat = value;
                OnPropertyChanged();
            }
        }
    }

    public ConversionStatus Status
    {
        get => Job.Status;
        set
        {
            if (Job.Status != value)
            {
                Job.Status = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(IsActive));
            }
        }
    }

    public double Progress
    {
        get => Job.Progress;
        set
        {
            if (Math.Abs(Job.Progress - value) > 0.01)
            {
                Job.Progress = value;
                OnPropertyChanged();
            }
        }
    }

    public string? ErrorMessage
    {
        get => Job.ErrorMessage;
        set
        {
            if (Job.ErrorMessage != value)
            {
                Job.ErrorMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusText));
            }
        }
    }

    public MediaCategory Category => Job.Category;

    public bool IsActive => Status is ConversionStatus.Processing or ConversionStatus.Pending;

    public string StatusText => Status switch
    {
        ConversionStatus.Pending => "Pending",
        ConversionStatus.Processing => $"Processing ({Progress:F0}%)",
        ConversionStatus.Completed => "Completed",
        ConversionStatus.Failed => ErrorMessage ?? "Failed",
        ConversionStatus.Cancelled => "Cancelled",
        _ => Status.ToString()
    };

    public void Refresh()
    {
        OnPropertyChanged(nameof(FileName));
        OnPropertyChanged(nameof(SourcePath));
        OnPropertyChanged(nameof(FileSizeText));
        OnPropertyChanged(nameof(CategoryLabel));
        OnPropertyChanged(nameof(TargetFormat));
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(Progress));
        OnPropertyChanged(nameof(ErrorMessage));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(Category));
        OnPropertyChanged(nameof(IsActive));
    }

    private static string GetFileSizeText(string path)
    {
        try
        {
            if (!File.Exists(path))
                return Directory.Exists(path) ? "Folder" : "—";

            var length = new FileInfo(path).Length;
            return length switch
            {
                < 1024 => $"{length} B",
                < 1024 * 1024 => $"{length / 1024.0:F1} KB",
                < 1024 * 1024 * 1024 => $"{length / (1024.0 * 1024):F1} MB",
                _ => $"{length / (1024.0 * 1024 * 1024):F1} GB"
            };
        }
        catch
        {
            return "—";
        }
    }
}
