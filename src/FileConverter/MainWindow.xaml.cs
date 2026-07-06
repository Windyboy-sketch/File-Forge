using System.Windows;
using System.Windows.Media.Animation;
using FileConverter.Core.Interfaces;
using FileConverter.Helpers;
using FileConverter.ViewModels;
using FileConverter.Views.Controls;

namespace FileConverter;

/// <summary>
/// Main application window with Fluent styling and Mica backdrop.
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly ISettingsService _settingsService;
    private readonly IThemeService _themeService;

    public MainWindow(MainViewModel viewModel, ISettingsService settingsService, IThemeService themeService)
    {
        _viewModel = viewModel;
        _settingsService = settingsService;
        _themeService = themeService;

        InitializeComponent();
        DataContext = viewModel;

        _themeService.ThemeChanged += (_, _) => ApplyBackdrop();
        Loaded += OnLoaded;
        Closed += (_, _) => viewModel.Dispose();
    }

    public void ShowToast() => ToastHost.ShowToast(_viewModel.Toast);

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyBackdrop();
        _viewModel.Toast.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(ToastViewModel.IsVisible) or nameof(ToastViewModel.Message))
                ShowToast();
        };

        Opacity = 0;
        BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(350))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        });
    }

    private void ApplyBackdrop()
    {
        WindowBackdropHelper.Apply(this, _settingsService.Current.UseMicaBackdrop, _themeService.IsDarkTheme);
    }
}
