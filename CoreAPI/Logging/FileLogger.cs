using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

namespace CoreAPI.Logging
{
    public class FileLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly string _logDirectory;
        private readonly CustomConsoleFormatter _formatter;
        private readonly ConcurrentQueue<string> _logQueue = new();
        private readonly int _maxQueueSize = 100;
        private readonly object _lockObject = new();
        private readonly Timer _processQueueTimer;
        private bool _isProcessing = false;

        public FileLogger(string categoryName, string logDirectory, CustomConsoleFormatter formatter)
        {
            _categoryName = categoryName;
            _logDirectory = logDirectory;
            _formatter = formatter;

            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }

            _processQueueTimer = new Timer(ProcessQueue, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            using var stringWriter = new StringWriter();
            
            var logEntry = new LogEntry<TState>(logLevel, _categoryName, eventId, state, exception, formatter);
            
            _formatter.Write(logEntry, null, stringWriter);
            var formattedText = stringWriter.ToString();
            
            _logQueue.Enqueue(formattedText);
            
            if (_logQueue.Count > _maxQueueSize)
            {
                ProcessQueue(null);
            }
        }

        private void ProcessQueue(object? state)
        {
            if (_isProcessing || _logQueue.IsEmpty)
            {
                return;
            }

            lock (_lockObject)
            {
                if (_isProcessing)
                {
                    return;
                }
                
                _isProcessing = true;
                
                try
                {
                    var logFilePath = Path.Combine(_logDirectory, $"log_{DateTime.Now:yyyy-MM-dd}.txt");
                    var logBuilder = new StringBuilder();
                    
                    while (_logQueue.TryDequeue(out var logMessage))
                    {
                        // Remove ANSI escape sequences before writing to file
                        var cleanedMessage = StripAnsiEscapeCodes(logMessage);
                        logBuilder.Append(cleanedMessage);
                    }

                    if (logBuilder.Length > 0)
                    {
                        File.AppendAllText(logFilePath, logBuilder.ToString(), Encoding.UTF8);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Erro ao escrever logs em arquivo: {ex.Message}");
                }
                finally
                {
                    _isProcessing = false;
                }
            }
        }

        private static string StripAnsiEscapeCodes(string input)
        {
            return Regex.Replace(input, "\\u001b\\[[^m]*m", "");
        }
    }
}
