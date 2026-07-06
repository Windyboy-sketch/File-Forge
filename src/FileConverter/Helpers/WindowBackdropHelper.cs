using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using FileConverter.Core.Interfaces;
using FileConverter.Core.Models;
using Microsoft.Win32;

namespace FileConverter.Helpers;

/// <summary>
/// Applies Windows 11 Mica/Acrylic backdrops and immersive dark mode to WPF windows.
/// </summary>
public static class WindowBackdropHelper
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaSystemBackdropType = 38;
    private const int DwmwaMicaEffect = 2;
    private const int DwmwaAcrylicEffect = 3;

    public static void Apply(Window window, bool useMica, bool isDarkTheme)
    {
        window.SourceInitialized += (_, _) =>
        {
            var handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero)
                return;

            ApplyImmersiveDarkMode(handle, isDarkTheme);

            if (useMica && OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
            {
                var backdrop = DwmwaMicaEffect;
                _ = DwmSetWindowAttribute(handle, DwmwaSystemBackdropType, ref backdrop, sizeof(int));
            }
        };

        window.Background = Brushes.Transparent;
    }

    public static bool IsSystemDarkTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int intValue && intValue == 0;
        }
        catch
        {
            return false;
        }
    }

    private static void ApplyImmersiveDarkMode(IntPtr handle, bool isDark)
    {
        var useDark = isDark ? 1 : 0;
        _ = DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkMode, ref useDark, sizeof(int));
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
}
