using FileConverter.Core.Interfaces;

namespace FileConverter.Services;

/// <summary>
/// Publishes toast messages for the UI host to display.
/// </summary>
public sealed class ToastNotificationService : IToastService
{
    public event EventHandler<ToastMessage>? ToastRequested;

    public void ShowInfo(string message) => Publish(message, ToastSeverity.Info);

    public void ShowSuccess(string message) => Publish(message, ToastSeverity.Success);

    public void ShowWarning(string message) => Publish(message, ToastSeverity.Warning);

    public void ShowError(string message) => Publish(message, ToastSeverity.Error);

    private void Publish(string message, ToastSeverity severity)
    {
        ToastRequested?.Invoke(this, new ToastMessage
        {
            Text = message,
            Severity = severity
        });
    }
}
