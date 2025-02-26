using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text;

namespace CoreAPI.Logging
{
    public sealed class CustomConsoleFormatter : ConsoleFormatter
    {
        private readonly IOptionsMonitor<CustomConsoleFormatterOptions> _options;
        private static readonly ConcurrentDictionary<string, string> _categoryCache = new();
        private const int TIME_COL_WIDTH = 8;
        private const int LEVEL_COL_WIDTH = 5;
        private const int CATEGORY_COL_WIDTH = 14;

        private static readonly Dictionary<string, string> _categoryMappings = new()
        {
            { "Microsoft.EntityFrameworkCore", "EF" },
            { "Microsoft.AspNetCore", "AspNet" },
            { "Microsoft.Extensions", "Extensions" },
            { "Microsoft.Hosting", "Host" },
            { "Business.Services", "Service" },
            { "CoreAPI.Controllers", "API" },
            { "Data.Repositories", "Repo" },
            { "Microsoft.Hosting.Lifetime", "Lifetime" },
            { "Microsoft.AspNetCore.Diagnostics", "Diagnostic" },
            { "Microsoft.AspNetCore.DataProtection", "DataProtect" },
            { "Microsoft.EntityFrameworkCore.Migrations", "Migrations" }
        };

        public CustomConsoleFormatter(IOptionsMonitor<CustomConsoleFormatterOptions> options)
            : base("CustomConsole")
        {
            _options = options;
        }

        public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
        {
            string? message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);
            if (message is null)
            {
                return;
            }

            var time = DateTime.Now.ToString("HH:mm:ss");
            var category = GetSimplifiedCategory(logEntry.Category);
            var logLevel = GetLogLevelString(logEntry.LogLevel);
            var levelColor = GetLogLevelColor(logLevel);
            
            // Alinhar as colunas
            var timeCol = $"{time} ".PadRight(TIME_COL_WIDTH); // Dois espaços após o horário
            var levelCol = logLevel == "ERROR" 
                ? $" {logLevel}" // No padding for ERROR
                : $" {logLevel} ".PadRight(LEVEL_COL_WIDTH); // Espaço antes e depois do nível para outros casos

            // Centralizar a categoria
            var categoryPadding = (CATEGORY_COL_WIDTH - category.Length) / 2;
            var categoryCol = category.Length > CATEGORY_COL_WIDTH 
                ? category[..(CATEGORY_COL_WIDTH-2)] + ".." 
                : new string(' ', categoryPadding) + category.PadRight(CATEGORY_COL_WIDTH - categoryPadding);

            // Resetar a cor no final
            var reset = "\u001b[0m";
            var dim = "\u001b[2m"; // Texto mais suave para os separadores

            // Destacar caixa do Swagger e URL
            if (message.Contains("EXTREME ERROR"))
            {
                var extremeColor = "\u001b[38;5;196m"; // Vermelho mais vivo
                var bgColor = "\u001b[48;5;52m";       // Fundo vermelho escuro
                var bold = "\u001b[1m";                // Negrito
                message = $"{extremeColor}{bgColor}{bold}{message}{reset}";
            }
            else if (message.Contains("Swagger UI:") || message.Contains("Hangfire Dashboard"))
            {
                message = $"\u001b[38;5;226m{message}\u001b[0m"; // Amarelo vibrante
            }

            // Processar a mensagem para juntar múltiplas linhas
            var processedMessage = ProcessMultilineMessage(message);

            textWriter.Write($"{timeCol}{dim}│{reset}{levelColor}{levelCol}{reset}{dim}│{reset} {categoryCol}{dim}│{reset} {processedMessage}");
            
            if (logEntry.Exception != null)
            {
                textWriter.WriteLine();
                textWriter.Write($"{new string(' ', TIME_COL_WIDTH)}{dim}│{reset} {new string(' ', LEVEL_COL_WIDTH)}{dim}│{reset} {new string(' ', CATEGORY_COL_WIDTH)}{dim}│{reset} {"\u001b[31m"}{logEntry.Exception}{reset}");
            }
            textWriter.WriteLine();
        }

        private static string ProcessMultilineMessage(string message)
        {
            // Substituir quebras de linha por espaços e remover espaços extras
            return string.Join(" ", message.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                        .Replace("    ", " ")
                        .Replace("   ", " ")
                        .Replace("  ", " ")
                        .Trim();
        }

        private static string GetSimplifiedCategory(string category)
        {
            return _categoryCache.GetOrAdd(category, cat =>
            {
                // Verificar mapeamentos conhecidos
                foreach (var mapping in _categoryMappings)
                {
                    if (cat.StartsWith(mapping.Key))
                    {
                        var remaining = cat[(mapping.Key.Length + 1)..];
                        var lastPart = remaining.Contains('.') ? remaining[(remaining.LastIndexOf('.') + 1)..] : remaining;
                        return string.IsNullOrEmpty(lastPart) ? mapping.Value : lastPart;
                    }
                }

                // Para outros casos, pegar apenas o último segmento
                var lastDot = cat.LastIndexOf('.');
                return lastDot >= 0 ? cat[(lastDot + 1)..] : cat;
            });
        }

        private static string GetLogLevelString(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Trace => "TRACE",
                LogLevel.Debug => "DEBUG",
                LogLevel.Information => "INFO",
                LogLevel.Warning => "WARN",
                LogLevel.Error => "ERROR",
                LogLevel.Critical => "CRIT",
                _ => logLevel.ToString().ToUpper()
            };
        }

        private static string GetLogLevelColor(string logLevel)
        {
            return logLevel switch
            {
                "TRACE" => "\u001b[37m",         // Branco
                "DEBUG" => "\u001b[36m",         // Ciano
                "INFO" => "\u001b[32m",          // Verde
                "WARN" => "\u001b[33m",          // Amarelo
                "ERROR" => "\u001b[31m",         // Vermelho
                "CRIT" => "\u001b[35m",          // Magenta
                _ => "\u001b[37m"                // Branco (default)
            };
        }
    }

    public class CustomConsoleFormatterOptions : ConsoleFormatterOptions
    {
    }
}
