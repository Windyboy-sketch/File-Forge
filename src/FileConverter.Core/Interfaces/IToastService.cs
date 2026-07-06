namespace FileConverter.Core.Interfaces;

/// <summary>
/// Displays transient toast notifications in the UI.
/// </summary>
public interface IToastService
{
    event EventHandler<ToastMessage>? ToastRequested;

    void ShowInfo(string message);
    void ShowSuccess(string message);
    void ShowWarning(string message);
    void ShowError(string message);
}

public enum ToastSeverity
{
    Info,
    Success,
    Warning,
    Error
}

public sealed class ToastMessage
{
    public required string Text { get; init; }
    public ToastSeverity Severity { get; init; } = ToastSeverity.Info;
}
