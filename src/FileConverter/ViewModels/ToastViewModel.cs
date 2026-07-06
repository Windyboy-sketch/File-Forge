using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using FileConverter.Core.Interfaces;

namespace FileConverter.ViewModels;

/// <summary>
/// Represents a single toast notification shown in the overlay host.
/// </summary>
public sealed partial class ToastViewModel : ObservableObject
{
    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private ToastSeverity _severity = ToastSeverity.Info;

    [ObservableProperty]
    private bool _isVisible;

    public DispatcherTimer? DismissTimer { get; set; }
}
