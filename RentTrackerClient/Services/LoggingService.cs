using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace RentTrackerClient.Services
{
    /// <summary>
    /// Custom logging service for client-side logging with enhanced capabilities
    /// </summary>
    public class LoggingService : ILogger
    {
        private readonly string _categoryName;
        private readonly LogLevel _minimumLogLevel;

        public LoggingService(string categoryName, LogLevel minimumLogLevel = LogLevel.Debug)
        {
            _categoryName = categoryName ?? throw new ArgumentNullException(nameof(categoryName));
            _minimumLogLevel = minimumLogLevel;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            // Return a no-op disposable instead of null
            return new NoopDisposable();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= _minimumLogLevel;
        }

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
            var logEntry = new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = logLevel,
                CategoryName = _categoryName,
                Message = message,
                EventId = eventId,
                Exception = exception
            };

            // Use a more cross-platform logging method
            LogMessage(logEntry);
        }

        private void LogMessage(LogEntry entry)
        {
            // Use a more browser-friendly logging approach
            string logMessage = $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] [{entry.Level}] [{entry.CategoryName}]: {entry.Message}";
            
            // Use console.log for browser compatibility
            Console.WriteLine(logMessage);

            if (entry.Exception != null)
            {
                Console.WriteLine($"Exception: {entry.Exception.GetType().Name}");
                Console.WriteLine($"Message: {entry.Exception.Message}");
                Console.WriteLine($"Stack Trace: {entry.Exception.StackTrace}");
            }
        }

        // Placeholder for cross-platform color mapping if needed
        private string GetLogLevelColor(LogLevel level)
        {
            return level switch
            {
                LogLevel.Trace => "gray",
                LogLevel.Debug => "blue",
                LogLevel.Information => "green",
                LogLevel.Warning => "yellow",
                LogLevel.Error => "red",
                LogLevel.Critical => "darkred",
                _ => "black"
            };
        }

        // Simple no-op disposable to replace null
        private class NoopDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }

    /// <summary>
    /// Represents a log entry with detailed information
    /// </summary>
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public EventId EventId { get; set; }
        public Exception? Exception { get; set; }
    }

    /// <summary>
    /// Logger provider for creating LoggingService instances
    /// </summary>
    public class ClientLoggerProvider : ILoggerProvider
    {
        private readonly LogLevel _minimumLogLevel;
        private readonly Dictionary<string, LoggingService> _loggers = new();

        public ClientLoggerProvider(LogLevel minimumLogLevel = LogLevel.Debug)
        {
            _minimumLogLevel = minimumLogLevel;
        }

        public ILogger CreateLogger(string categoryName)
        {
            if (!_loggers.TryGetValue(categoryName, out var logger))
            {
                logger = new LoggingService(categoryName, _minimumLogLevel);
                _loggers[categoryName] = logger;
            }
            return logger;
        }

        public void Dispose()
        {
            _loggers.Clear();
            GC.SuppressFinalize(this);
        }
    }
}