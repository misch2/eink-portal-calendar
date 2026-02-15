using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using System.Text;

namespace PortalCalendarServer.Logging;

public class CustomConsoleFormatterOptions : ConsoleFormatterOptions
{
    public bool IncludeScopes { get; set; } = false;
}

public class CustomConsoleFormatter : ConsoleFormatter
{
    private readonly IDisposable? _optionsReloadToken;
    private CustomConsoleFormatterOptions _formatterOptions;

    public CustomConsoleFormatter(IOptionsMonitor<CustomConsoleFormatterOptions> options)
        : base("custom")
    {
        _optionsReloadToken = options.OnChange(ReloadLoggerOptions);
        _formatterOptions = options.CurrentValue;
    }

    private void ReloadLoggerOptions(CustomConsoleFormatterOptions options)
    {
        _formatterOptions = options;
    }

    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter)
    {
        string message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception) ?? string.Empty;

        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        var logLevel = logEntry.LogLevel switch
        {
            LogLevel.Trace => "trce",
            LogLevel.Debug => "dbug",
            LogLevel.Information => "info",
            LogLevel.Warning => "warn",
            LogLevel.Error => "fail",
            LogLevel.Critical => "crit",
            _ => "none"
        };

        var categoryName = logEntry.Category;
        var eventId = logEntry.EventId.Id;

        // Format: [info] Message text [Category[EventId]]
        var sb = new StringBuilder();
        sb.Append('[');
        sb.Append(logLevel);
        sb.Append("] ");
        sb.Append(message);

        // Add category and event ID at the end
        sb.Append(" [");
        sb.Append(categoryName);
        if (eventId != 0)
        {
            sb.Append('[');
            sb.Append(eventId);
            sb.Append(']');
        }
        sb.Append(']');

        textWriter.WriteLine(sb.ToString());

        // Write exception if present
        if (logEntry.Exception != null)
        {
            textWriter.WriteLine(logEntry.Exception.ToString());
        }
    }

    public void Dispose()
    {
        _optionsReloadToken?.Dispose();
    }
}
