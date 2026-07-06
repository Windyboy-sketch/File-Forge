using System.IO;
using System.Windows;
using FileConverter.Core.Interfaces;
using FileConverter.Infrastructure;
using FileConverter.Services;
using FileConverter.Services.Logging;
using FileConverter.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FileConverter;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FileConverter",
            "logs");

        _host = Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddDebug();
                logging.AddProvider(new FileLoggerProvider(logDirectory));
            })
            .ConfigureServices(services =>
            {
                services.AddInfrastructure();
                services.AddSingleton<IFileDialogService, FileDialogService>();
                services.AddSingleton<IThemeService, ThemeService>();
                services.AddSingleton<IToastService, ToastNotificationService>();
                services.AddSingleton<MainViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        await _host.StartAsync();

        var viewModel = _host.Services.GetRequiredService<MainViewModel>();
        await viewModel.InitializeAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            if (_host.Services.GetService<MainViewModel>() is IDisposable disposable)
                disposable.Dispose();

            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }
}
