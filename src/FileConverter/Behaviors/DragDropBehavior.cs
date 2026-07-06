using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace FileConverter.Behaviors;

/// <summary>
/// Enables drag-and-drop file ingestion with visual highlight feedback.
/// </summary>
public static class DragDropBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(DragDropBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static readonly DependencyProperty DropCommandProperty =
        DependencyProperty.RegisterAttached(
            "DropCommand",
            typeof(ICommand),
            typeof(DragDropBehavior),
            new PropertyMetadata(null));

    public static readonly DependencyProperty IsDragOverProperty =
        DependencyProperty.RegisterAttached(
            "IsDragOver",
            typeof(bool),
            typeof(DragDropBehavior),
            new PropertyMetadata(false));

    public static readonly DependencyProperty HighlightBrushProperty =
        DependencyProperty.RegisterAttached(
            "HighlightBrush",
            typeof(Brush),
            typeof(DragDropBehavior),
            new PropertyMetadata(null));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    public static ICommand? GetDropCommand(DependencyObject obj) => (ICommand?)obj.GetValue(DropCommandProperty);

    public static void SetDropCommand(DependencyObject obj, ICommand? value) => obj.SetValue(DropCommandProperty, value);

    public static bool GetIsDragOver(DependencyObject obj) => (bool)obj.GetValue(IsDragOverProperty);

    public static void SetIsDragOver(DependencyObject obj, bool value) => obj.SetValue(IsDragOverProperty, value);

    public static Brush? GetHighlightBrush(DependencyObject obj) => (Brush?)obj.GetValue(HighlightBrushProperty);

    public static void SetHighlightBrush(DependencyObject obj, Brush? value) => obj.SetValue(HighlightBrushProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element)
            return;

        if ((bool)e.NewValue)
        {
            element.AllowDrop = true;
            element.DragEnter += OnDragEnter;
            element.DragLeave += OnDragLeave;
            element.DragOver += OnDragOver;
            element.Drop += OnDrop;
        }
        else
        {
            element.AllowDrop = false;
            element.DragEnter -= OnDragEnter;
            element.DragLeave -= OnDragLeave;
            element.DragOver -= OnDragOver;
            element.Drop -= OnDrop;
        }
    }

    private static void OnDragEnter(object sender, DragEventArgs e)
    {
        if (HasFiles(e))
            SetIsDragOver((DependencyObject)sender, true);
    }

    private static void OnDragLeave(object sender, DragEventArgs e)
    {
        SetIsDragOver((DependencyObject)sender, false);
    }

    private static void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = HasFiles(e) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private static void OnDrop(object sender, DragEventArgs e)
    {
        SetIsDragOver((DependencyObject)sender, false);

        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            return;

        var paths = (string[])e.Data.GetData(DataFormats.FileDrop)!;
        if (sender is DependencyObject element)
        {
            var command = GetDropCommand(element);
            if (command?.CanExecute(paths) == true)
                command.Execute(paths);
        }

        e.Handled = true;
    }

    private static bool HasFiles(DragEventArgs e) => e.Data.GetDataPresent(DataFormats.FileDrop);
}
