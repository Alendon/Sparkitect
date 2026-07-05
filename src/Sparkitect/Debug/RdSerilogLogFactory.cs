using System;
using System.Linq;
using JetBrains.Diagnostics;
using Serilog.Events;
using Serilog.Parsing;

namespace Sparkitect.Debug;

/// <summary>
/// Routes the rd library's own diagnostics (<see cref="JetBrains.Diagnostics.Log" />) through the
/// engine's Serilog pipeline instead of rd's console default, so debug-channel wire logs land in the
/// same sinks as everything else. Installed once when the channel starts.
/// </summary>
internal sealed class RdSerilogLogFactory : ILogFactory
{
    public static void Install() => Log.DefaultFactory = new RdSerilogLogFactory();

    public ILog GetLog(string category) => new RdSerilogLog(category);

    private sealed class RdSerilogLog : ILog
    {
        private readonly Serilog.ILogger _logger;

        public RdSerilogLog(string category)
        {
            Category = category;
            _logger = Serilog.Log.ForContext("SourceContext", category);
        }

        public string Category { get; }

        public bool IsEnabled(LoggingLevel level) =>
            level != LoggingLevel.OFF && _logger.IsEnabled(Map(level));

        public void Log(LoggingLevel level, string? message, Exception? exception = null)
        {
            if (level == LoggingLevel.OFF)
                return;

            // Not a user log statement: emit through the non-template LogEvent API — outside the
            // log-enrichment interceptor's scope by design — and carry the rd text literally so brace
            // characters in wire diagnostics never hit the template parser.
            var text = message ?? exception?.Message ?? string.Empty;
            var template = new MessageTemplate(new MessageTemplateToken[] { new TextToken(text) });
            _logger.Write(new LogEvent(
                DateTimeOffset.Now, Map(level), exception, template, Enumerable.Empty<LogEventProperty>()));
        }

        private static LogEventLevel Map(LoggingLevel level) => level switch
        {
            LoggingLevel.FATAL => LogEventLevel.Fatal,
            LoggingLevel.ERROR => LogEventLevel.Error,
            LoggingLevel.WARN => LogEventLevel.Warning,
            LoggingLevel.INFO => LogEventLevel.Information,
            LoggingLevel.VERBOSE => LogEventLevel.Debug,
            _ => LogEventLevel.Verbose,
        };
    }
}
