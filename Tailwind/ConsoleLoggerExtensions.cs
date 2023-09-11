using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace Tailwind;

public static class ConsoleLoggerExtensions
{
    public static ILoggingBuilder AddCustomFormatter(
        this ILoggingBuilder builder,
        Action<CustomOptions> configure) =>
        builder.AddConsole(options => options.FormatterName = "cli")
            .AddConsoleFormatter<CustomFormatter, CustomOptions>(configure);
}
public sealed class CustomOptions : ConsoleFormatterOptions
{
}
public sealed class CustomFormatter : ConsoleFormatter, IDisposable
{
    private readonly IDisposable? _optionsReloadToken;
    private CustomOptions _formatterOptions;

    public CustomFormatter(IOptionsMonitor<CustomOptions> options)
        // Case insensitive
        : base("cli") =>
        (_optionsReloadToken, _formatterOptions) =
            (options.OnChange(ReloadLoggerOptions), options.CurrentValue);

    private void ReloadLoggerOptions(CustomOptions options) =>
        _formatterOptions = options;

    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter)
    {
        string? message =
            logEntry.Formatter?.Invoke(
                logEntry.State, logEntry.Exception);

        if (message is null)
        {
            return;
        }

        textWriter.WriteLine(message);
    }

    public void Dispose() => _optionsReloadToken?.Dispose();
}