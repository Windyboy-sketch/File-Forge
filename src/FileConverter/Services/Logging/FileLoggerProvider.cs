using System.Collections.Concurrent;
using System.IO;
using Microsoft.Extensions.Logging;

namespace FileConverter.Services.Logging;

/// <summary>
/// Writes log entries to a rolling daily log file under AppData.
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _logDirectory;
    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();
    private readonly object _writeLock = new();

    public FileLoggerProvider(string logDirectory)
    {
        _logDirectory = logDirectory;
        Directory.CreateDirectory(_logDirectory);
    }

    public ILogger CreateLogger(string categoryName) =>
        _loggers.GetOrAdd(categoryName, name => new FileLogger(name, _logDirectory, _writeLock));

    public void Dispose()
    {
        _loggers.Clear();
    }

    private sealed class FileLogger : ILogger
    {
        private readonly string _category;
        private readonly string _logDirectory;
        private readonly object _writeLock;

        public FileLogger(string category, string logDirectory, object writeLock)
        {
            _category = category;
            _logDirectory = logDirectory;
            _writeLock = writeLock;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Debug;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var message = formatter(state, exception);
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{logLevel,-5}] {_category}: {message}";
            if (exception is not null)
                line += Environment.NewLine + exception;

            var logPath = Path.Combine(_logDirectory, $"fileconverter-{DateTime.Now:yyyyMMdd}.log");

            lock (_writeLock)
            {
                File.AppendAllText(logPath, line + Environment.NewLine);
            }
        }
    }
}
