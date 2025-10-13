using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace CoreAPI.Logging
{
    [ProviderAlias("File")]
    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly IDisposable? _onChangeToken;
        private FileLoggerOptions _currentConfig;
        private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();
        private readonly CustomConsoleFormatter _formatter;

        public FileLoggerProvider(
            IOptionsMonitor<FileLoggerOptions> config,
            IOptionsMonitor<CustomConsoleFormatterOptions> formatterOptions)
        {
            _currentConfig = config.CurrentValue;
            _onChangeToken = config.OnChange(updatedConfig => _currentConfig = updatedConfig);
            _formatter = new CustomConsoleFormatter(formatterOptions);
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new FileLogger(
                name,
                _currentConfig.LogDirectory,
                _formatter));
        }

        public void Dispose()
        {
            _loggers.Clear();
            _onChangeToken?.Dispose();
        }
    }

    public class FileLoggerOptions
    {
        public string LogDirectory { get; set; } = "Logs";
    }
}
