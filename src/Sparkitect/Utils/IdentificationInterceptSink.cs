using Serilog.Core;
using Serilog.Events;
using Sparkitect.Modding;

namespace Sparkitect.Utils;

internal sealed class IdentificationInterceptSink : ILogEventSink
{
    private static IIdentificationManager? _instance;
    private static bool _isInitialized;
    private static readonly object _lock = new();

    public static IIdentificationManager? Instance
    {
        private get => _instance;
        set
        {
            if (_isInitialized) return;
            lock (_lock)
            {
                if (_isInitialized) return;
                _instance = value;
                _isInitialized = true;
            }
        }
    }

    private readonly ILogEventSink _wrappedSink;

    public IdentificationInterceptSink(ILogEventSink wrappedSink)
    {
        _wrappedSink = wrappedSink;
    }

    public void Emit(LogEvent logEvent)
    {
        var transformedEvent = TransformEvent(logEvent);
        _wrappedSink.Emit(transformedEvent);
    }

    private static LogEvent TransformEvent(LogEvent original)
    {
        if (_instance is null)
            return original;

        var needsTransform = false;
        foreach (var prop in original.Properties.Values)
        {
            if (ContainsIdentification(prop))
            {
                needsTransform = true;
                break;
            }
        }

        if (!needsTransform)
            return original;

        var transformedProperties = new List<LogEventProperty>();
        foreach (var kvp in original.Properties)
        {
            var transformedValue = TransformValue(kvp.Value);
            transformedProperties.Add(new LogEventProperty(kvp.Key, transformedValue));
        }

        return new LogEvent(
            original.Timestamp,
            original.Level,
            original.Exception,
            original.MessageTemplate,
            transformedProperties);
    }

    private static bool ContainsIdentification(LogEventPropertyValue value)
    {
        return value switch
        {
            ScalarValue { Value: Identification } => true,
            SequenceValue seq => seq.Elements.Any(ContainsIdentification),
            StructureValue str => str.Properties.Any(p => ContainsIdentification(p.Value)),
            DictionaryValue dict => dict.Elements.Any(e => ContainsIdentification(e.Key) || ContainsIdentification(e.Value)),
            _ => false
        };
    }

    private static LogEventPropertyValue TransformValue(LogEventPropertyValue value)
    {
        return value switch
        {
            ScalarValue { Value: Identification id } => new ScalarValue(FormatIdentification(id)),
            SequenceValue seq => new SequenceValue(seq.Elements.Select(TransformValue)),
            StructureValue str => new StructureValue(
                str.Properties.Select(p => new LogEventProperty(p.Name, TransformValue(p.Value))),
                str.TypeTag),
            DictionaryValue dict => new DictionaryValue(
                dict.Elements.Select(e => new KeyValuePair<ScalarValue, LogEventPropertyValue>(
                    TransformValue(e.Key) as ScalarValue ?? e.Key,
                    TransformValue(e.Value)))),
            _ => value
        };
    }

    private static string FormatIdentification(Identification id)
    {
        if (_instance is null)
            return $"{id.ModId}:{id.CategoryId}:{id.ItemId}";

        _instance.TryResolveIdentification(id, out var mod, out var cat, out var item);
        return $"{mod ?? id.ModId.ToString()}:{cat ?? id.CategoryId.ToString()}:{item ?? id.ItemId.ToString()}";
    }
}
