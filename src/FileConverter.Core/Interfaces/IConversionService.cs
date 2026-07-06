using FileConverter.Core.Models;

namespace FileConverter.Core.Interfaces;

public interface IConversionService
{
    MediaCategory Category { get; }
    bool CanHandle(string filePath, ConversionOperation operation);
    Task ConvertAsync(ConversionJob job, IProgress<double> progress, CancellationToken cancellationToken);
}
