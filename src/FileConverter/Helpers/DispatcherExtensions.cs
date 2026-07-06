using System.Windows;
using System.Windows.Threading;

namespace FileConverter.Helpers;

/// <summary>
/// Dispatcher helpers that keep the UI thread responsive.
/// </summary>
public static class DispatcherExtensions
{
    public static void BeginInvokeSafe(this Dispatcher dispatcher, Action action)
    {
        if (dispatcher.CheckAccess())
            action();
        else
            dispatcher.BeginInvoke(action, DispatcherPriority.Background);
    }
}
