using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using FileConverter.ViewModels;

namespace FileConverter.Views.Controls;

/// <summary>
/// Overlay host that animates toast notifications.
/// </summary>
public partial class ToastNotificationHost : UserControl
{
    private readonly System.Windows.Threading.DispatcherTimer _dismissTimer;

    public ToastNotificationHost()
    {
        InitializeComponent();

        _dismissTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(3.5) };
        _dismissTimer.Tick += (_, _) => HideToast();
    }

    public void ShowToast(ToastViewModel toast)
    {
        DataContext = toast;
        ToastBorder.Opacity = 0;
        ToastBorder.Visibility = Visibility.Visible;

        var fadeIn = (Storyboard)FindResource("FadeInStoryboard");
        ToastBorder.BeginStoryboard(fadeIn);

        _dismissTimer.Stop();
        _dismissTimer.Start();
    }

    private void HideToast()
    {
        _dismissTimer.Stop();

        var fadeOut = (Storyboard)FindResource("FadeOutStoryboard");
        fadeOut.Completed += OnFadeOutCompleted;
        ToastBorder.BeginStoryboard(fadeOut);
    }

    private void OnFadeOutCompleted(object? sender, EventArgs e)
    {
        if (sender is Storyboard storyboard)
            storyboard.Completed -= OnFadeOutCompleted;

        ToastBorder.Visibility = Visibility.Collapsed;

        if (DataContext is ToastViewModel toast)
            toast.IsVisible = false;
    }
}
