namespace FileConverter.Core.Interfaces;

public interface IFileDialogService
{
    string? PickOutputFolder(string? initialPath = null);
    IReadOnlyList<string> PickFiles(string filter, bool multiSelect = true);
}
