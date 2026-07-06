using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using FileConverter.Core.Models;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;

namespace FileConverter.Converters;

public sealed class CategoryToIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not MediaCategory category)
            return Application.Current.FindResource("IconDocument");

        var key = category switch
        {
            MediaCategory.Image => "IconImage",
            MediaCategory.Audio => "IconAudio",
            MediaCategory.Video => "IconVideo",
            MediaCategory.Document => "IconDocument",
            _ => "IconDocument"
        };

        return Application.Current.FindResource(key);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class CategoryToAccentBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not MediaCategory category)
            return Application.Current.FindResource("AccentBrush");

        return category switch
        {
            MediaCategory.Image => new SolidColorBrush(Color.FromRgb(156, 220, 254)),
            MediaCategory.Audio => new SolidColorBrush(Color.FromRgb(108, 203, 95)),
            MediaCategory.Video => new SolidColorBrush(Color.FromRgb(255, 107, 107)),
            MediaCategory.Document => new SolidColorBrush(Color.FromRgb(252, 225, 0)),
            _ => Application.Current.FindResource("AccentBrush")
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
