using FileConverter.Core.Interfaces;
using FileConverter.Core.Models;
using FileConverter.Core.Services;
using Microsoft.Extensions.Logging;

namespace FileConverter.Infrastructure.Services;

/// <summary>
/// Processes conversion jobs asynchronously with configurable parallelism.
/// </summary>
public sealed class ConversionCoordinator : IConversionCoordinator
{
    private readonly IEnumerable<IConversionService> _conversionServices;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<ConversionCoordinator> _logger;
    private readonly List<ConversionJob> _jobs = [];
    private readonly object _lock = new();
    private CancellationTokenSource? _processingCts;
    private Task? _processingTask;

    public ConversionCoordinator(
        IEnumerable<IConversionService> conversionServices,
        ISettingsService settingsService,
        ILogger<ConversionCoordinator> logger)
    {
        _conversionServices = conversionServices;
        _settingsService = settingsService;
        _logger = logger;
    }

    public event EventHandler<ConversionJob>? JobUpdated;

    public IReadOnlyList<ConversionJob> Jobs
    {
        get
        {
            lock (_lock)
                return _jobs.ToList();
        }
    }

    public void Enqueue(ConversionJob job)
    {
        lock (_lock)
            _jobs.Add(job);

        JobUpdated?.Invoke(this, job);
    }

    public void EnqueueRange(IEnumerable<ConversionJob> jobs)
    {
        foreach (var job in jobs)
            Enqueue(job);
    }

    public void Remove(Guid jobId)
    {
        lock (_lock)
            _jobs.RemoveAll(j => j.Id == jobId);
    }

    public void ClearCompleted()
    {
        lock (_lock)
            _jobs.RemoveAll(j => j.Status is ConversionStatus.Completed or ConversionStatus.Failed or ConversionStatus.Cancelled);
    }

    public void ClearAll()
    {
        lock (_lock)
            _jobs.Clear();
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_processingTask is { IsCompleted: false })
            return _processingTask;

        _processingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _processingTask = ProcessQueueAsync(_processingCts.Token);
        return _processingTask;
    }

    public async Task CancelAsync()
    {
        if (_processingCts is null)
            return;

        await _processingCts.CancelAsync();

        if (_processingTask is not null)
        {
            try
            {
                await _processingTask;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Conversion batch was cancelled.");
            }
        }
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        var maxParallel = Math.Max(1, _settingsService.Current.MaxParallelConversions);
        using var semaphore = new SemaphoreSlim(maxParallel, maxParallel);

        var pendingJobs = GetPendingJobs();
        _logger.LogInformation("Starting conversion of {Count} job(s) with parallelism {Parallelism}.", pendingJobs.Count, maxParallel);

        var tasks = pendingJobs.Select(job => ProcessJobAsync(job, semaphore, cancellationToken)).ToArray();
        await Task.WhenAll(tasks);
    }

    private List<ConversionJob> GetPendingJobs()
    {
        lock (_lock)
            return _jobs.Where(j => j.Status == ConversionStatus.Pending).ToList();
    }

    private async Task ProcessJobAsync(ConversionJob job, SemaphoreSlim semaphore, CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            UpdateJob(job, ConversionStatus.Processing, 0, null);

            var service = _conversionServices.FirstOrDefault(s => s.CanHandle(job.SourcePath, job.Operation))
                ?? throw new InvalidOperationException($"No converter available for '{job.SourcePath}'.");

            EnsureOutputPath(job);

            var progress = new Progress<double>(value => UpdateJob(job, ConversionStatus.Processing, value, null));

            await service.ConvertAsync(job, progress, cancellationToken);

            UpdateJob(job, ConversionStatus.Completed, 100, null);
            _logger.LogInformation("Completed conversion: {Source} -> {Output}", job.SourcePath, job.OutputPath);
        }
        catch (OperationCanceledException)
        {
            UpdateJob(job, ConversionStatus.Cancelled, job.Progress, "Conversion was cancelled.");
            _logger.LogWarning("Cancelled conversion: {Source}", job.SourcePath);
        }
        catch (Exception ex)
        {
            UpdateJob(job, ConversionStatus.Failed, job.Progress, ex.Message);
            _logger.LogError(ex, "Failed conversion: {Source}", job.SourcePath);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private void EnsureOutputPath(ConversionJob job)
    {
        if (!string.IsNullOrWhiteSpace(job.OutputPath))
            return;

        var outputDir = _settingsService.Current.OutputDirectory;
        Directory.CreateDirectory(outputDir);

        job.OutputPath = ConversionJobBuilder.ResolveOutputPath(
            outputDir,
            job.SourcePath,
            job.TargetFormat,
            job.Operation,
            job.Category);
    }

    private void UpdateJob(ConversionJob job, ConversionStatus status, double progress, string? error)
    {
        job.Status = status;
        job.Progress = progress;
        job.ErrorMessage = error;
        JobUpdated?.Invoke(this, job);
    }
}
