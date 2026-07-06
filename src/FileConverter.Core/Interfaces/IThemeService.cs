using FileConverter.Core.Models;

namespace FileConverter.Core.Interfaces;

/// <summary>
/// Applies and persists application light/dark themes.
/// </summary>
public interface IThemeService
{
    AppTheme CurrentTheme { get; }
    bool IsDarkTheme { get; }
    event EventHandler? ThemeChanged;
    void ApplyTheme(AppTheme theme);
    void ApplySavedTheme();
}
