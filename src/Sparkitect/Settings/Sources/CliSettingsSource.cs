using JetBrains.Annotations;
using Sparkitect.Modding;
using Sparkitect.Utils.DU;

namespace Sparkitect.Settings.Sources;

/// <summary>
/// The readonly CLI setting source. Parses the process arguments with the same
/// <c>-key=value</c> / <c>;</c>-multi / flag semantics the retired <c>CliArgumentHandler</c> produced,
/// and feeds a setting only when it explicitly declares the matching CLI option (no name-derived
/// binding, D-11). Writes are refused.
/// </summary>
[PublicAPI]
public sealed class CliSettingsSource : ISettingSource
{
    private readonly Dictionary<string, CliArgValue> _arguments;
    private readonly Func<Identification, ISettingDeclaration?> _declarationProvider;
    private readonly Func<IReadOnlyList<SettingSourceOrder>>? _orderBefore;
    private readonly Func<IReadOnlyList<SettingSourceOrder>>? _orderAfter;

    /// <summary>Creates the CLI source over <paramref name="args"/>.</summary>
    /// <param name="args">The raw process arguments (excluding the executable path).</param>
    /// <param name="declarationProvider">Resolves a setting's declaration (its CLI option and scalar parser).</param>
    /// <param name="orderBefore">Sources this source outranks; evaluated when the registration pass is
    /// processed, so targets may reference generated ids assigned later in the same pass.</param>
    /// <param name="orderAfter">Sources that outrank this source; same deferred evaluation.</param>
    public CliSettingsSource(
        IReadOnlyList<string> args,
        Func<Identification, ISettingDeclaration?> declarationProvider,
        Func<IReadOnlyList<SettingSourceOrder>>? orderBefore = null,
        Func<IReadOnlyList<SettingSourceOrder>>? orderAfter = null)
    {
        _declarationProvider = declarationProvider;
        _orderBefore = orderBefore;
        _orderAfter = orderAfter;
        _arguments = ParseArguments(args);
    }

    /// <inheritdoc/>
    public string SourceId => "cli";

    /// <inheritdoc/>
    public bool CanWrite => false;

    /// <inheritdoc/>
    public IReadOnlyList<SettingSourceOrder> OrderBefore => _orderBefore?.Invoke() ?? [];

    /// <inheritdoc/>
    public IReadOnlyList<SettingSourceOrder> OrderAfter => _orderAfter?.Invoke() ?? [];

    /// <inheritdoc/>
    public bool TryGet(Identification id, out object? value)
    {
        value = null;

        // A setting is fed only through its explicitly declared CLI option (D-11).
        if (_declarationProvider(id)?.CliOption is not { } option)
        {
            return false;
        }

        if (!_arguments.TryGetValue(option, out var argument))
        {
            return false;
        }

        var declaration = _declarationProvider(id)!;
        return argument switch
        {
            CliArgValue.Flag => declaration.TryParseScalar("true", out value),
            CliArgValue.Single single => declaration.TryParseScalar(single.Value, out value),
            CliArgValue.Multi multi => multi.Values.Count > 0 && declaration.TryParseScalar(multi.Values[0], out value),
        };
    }

    /// <inheritdoc/>
    public Result<SetError> Write(Identification id, object? value) => new SetError.SourceReadonly(Identification.Empty);

    /// <summary>
    /// Parses raw process arguments into keyed values with the <c>-key=value</c> / <c>;</c>-multi / flag
    /// semantics the retired <c>CliArgumentHandler</c> produced. Shared with the pre-container logger read
    /// (D-16) so CLI parsing has a single source of truth.
    /// </summary>
    /// <param name="args">The raw process arguments (excluding the executable path).</param>
    internal static Dictionary<string, CliArgValue> ParseArguments(IReadOnlyList<string> args)
    {
        var arguments = new Dictionary<string, CliArgValue>(StringComparer.OrdinalIgnoreCase);
        foreach (var arg in args)
        {
            if (!arg.StartsWith('-'))
            {
                continue;
            }

            var trimmed = arg.TrimStart('-');
            var equalsIndex = trimmed.IndexOf('=');
            if (equalsIndex <= 0)
            {
                arguments[trimmed] = new CliArgValue.Flag();
                continue;
            }

            var key = trimmed[..equalsIndex].Trim();
            var value = trimmed[(equalsIndex + 1)..].Trim();
            var values = value.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();

            if (!arguments.TryGetValue(key, out var existing))
            {
                arguments[key] = values.Count == 1 ? new CliArgValue.Single(values[0]) : new CliArgValue.Multi(values);
                continue;
            }

            switch (existing)
            {
                case CliArgValue.Flag:
                    arguments[key] = values.Count == 1 ? new CliArgValue.Single(values[0]) : new CliArgValue.Multi(values);
                    break;
                case CliArgValue.Single single:
                    var merged = new List<string> { single.Value };
                    merged.AddRange(values);
                    arguments[key] = new CliArgValue.Multi(merged);
                    break;
                case CliArgValue.Multi multi:
                    ((List<string>)multi.Values).AddRange(values);
                    break;
            }
        }

        return arguments;
    }
}
