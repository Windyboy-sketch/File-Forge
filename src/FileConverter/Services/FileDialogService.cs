using FileConverter.Core.Interfaces;
using Microsoft.Win32;

namespace FileConverter.Services;

public sealed class FileDialogService : IFileDialogService
{
    public string? PickOutputFolder(string? initialPath = null)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select output folder",
            InitialDirectory = initialPath ?? string.Empty
        };

        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }

    public IReadOnlyList<string> PickFiles(string filter, bool multiSelect = true)
    {
        var dialog = new OpenFileDialog
        {
            Filter = filter,
            Multiselect = multiSelect,
            CheckFileExists = true
        };

        return dialog.ShowDialog() == true
            ? dialog.FileNames
            : [];
    }
}
