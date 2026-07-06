using FileConverter.Core.Interfaces;
using FileConverter.Core.Models;

namespace FileConverter.Services;

/// <summary>
/// Applies the dark theme resource dictionary.
/// </summary>
public sealed class ThemeService : IThemeService
{
    public AppTheme CurrentTheme => AppTheme.Dark;

    public bool IsDarkTheme => true;

    public event EventHandler? ThemeChanged;

    public void ApplySavedTheme() => ApplyTheme(AppTheme.Dark);

    public void ApplyTheme(AppTheme theme) => ThemeChanged?.Invoke(this, EventArgs.Empty);
}
