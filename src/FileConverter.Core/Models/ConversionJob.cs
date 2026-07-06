namespace FileConverter.Core.Models;

public sealed class ConversionJob
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string SourcePath { get; init; }
    public string? OutputPath { get; set; }
    public required string TargetFormat { get; set; }
    public ConversionOperation Operation { get; init; } = ConversionOperation.Convert;
    public MediaCategory Category { get; init; }
    public ConversionStatus Status { get; set; } = ConversionStatus.Pending;
    public double Progress { get; set; }
    public string? ErrorMessage { get; set; }
    public IReadOnlyList<string>? AdditionalSourcePaths { get; init; }
}
