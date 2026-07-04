using JetBrains.Annotations;
using Sparkitect.Modding;
using Sparkitect.Utils.DU;

namespace Sparkitect.Settings.Sources;

/// <summary>
/// The readonly CLI setting source. Parses the process arguments as strict unix-style long options
/// (<c>--key=value</c>, bare <c>--flag</c>, <c>--no-flag</c> negation, multi-values by repetition) and
/// feeds a setting only when it explicitly declares the matching CLI option (no name-derived binding).
/// Malformed tokens fail loud at parse time; values malformed for their declared setting fail loud at
/// pull time. Writes are refused.
/// </summary>
[PublicAPI]
public sealed class CliSettingsSource : ISettingSource
{
    private const string NegationPrefix = "no-";

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
    /// <remarks>
    /// Negation is resolved here, with the declaration in hand: both <c>option</c> and
    /// <c>no-{option}</c> are looked up. An option present but malformed for its declared setting
    /// (repetition pulled by a scalar setting, a bare flag on a non-bool, an unparseable value) throws
    /// rather than falling through to a lower-precedence source.
    /// </remarks>
    public bool TryGet(Identification id, out object? value)
    {
        value = null;

        // A setting is fed only through its explicitly declared CLI option.
        if (_declarationProvider(id)?.CliOption is not { } option)
        {
            return false;
        }

        var declaration = _declarationProvider(id)!;

        if (_arguments.TryGetValue(option, out var argument))
        {
            var raw = argument switch
            {
                CliArgValue.Flag => "true",
                CliArgValue.Single single => single.Value,
                CliArgValue.Multi => throw new InvalidOperationException(
                    $"CLI option '--{option}' was given multiple times but feeds a single-value setting."),
            };
            if (!declaration.TryParseScalar(raw, out value))
            {
                throw new InvalidOperationException(argument is CliArgValue.Flag
                    ? $"CLI option '--{option}' is a bare flag but does not feed a boolean setting."
                    : $"CLI option '--{option}={raw}' does not parse to the setting's declared type.");
            }

            return true;
        }

        if (_arguments.ContainsKey(NegationPrefix + option))
        {
            if (!declaration.TryParseScalar("false", out value))
            {
                throw new InvalidOperationException(
                    $"CLI option '--{NegationPrefix}{option}' negates a non-boolean setting.");
            }

            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public Result<SetError> Write(Identification id, object? value) => new SetError.SourceReadonly(Identification.Empty);

    /// <summary>
    /// Parses raw process arguments as strict unix-style long options. Every token must start with
    /// <c>--</c>; <c>--key=value</c> stores a value (a <c>;</c> is literal content), bare <c>--flag</c>
    /// stores a flag (including <c>no-</c>-prefixed keys — negation is resolved at pull time), and
    /// repetition accumulates values. Non-<c>--</c> tokens, empty keys, a value on a negated form, and a
    /// <c>--foo</c>/<c>--no-foo</c> conflict throw. Unknown option names are retained for later
    /// consumers. Shared with the pre-container logger read so CLI parsing has a single source of truth.
    /// </summary>
    /// <param name="args">The raw process arguments (excluding the executable path).</param>
    internal static Dictionary<string, CliArgValue> ParseArguments(IReadOnlyList<string> args)
    {
        var arguments = new Dictionary<string, CliArgValue>(StringComparer.Ordinal);
        foreach (var arg in args)
        {
            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"CLI argument '{arg}' is not a '--' long option.");
            }

            var token = arg[2..];
            if (token.Length == 0)
            {
                throw new ArgumentException("CLI argument '--' carries no option name.");
            }

            var equalsIndex = token.IndexOf('=');
            if (equalsIndex < 0)
            {
                arguments.TryAdd(token, new CliArgValue.Flag());
                continue;
            }

            var key = token[..equalsIndex];
            if (key.Length == 0)
            {
                throw new ArgumentException($"CLI argument '{arg}' carries no option name.");
            }

            if (key.StartsWith(NegationPrefix, StringComparison.Ordinal))
            {
                throw new ArgumentException($"CLI argument '{arg}' is a negated form and takes no value.");
            }

            var value = token[(equalsIndex + 1)..];
            arguments[key] = arguments.TryGetValue(key, out var existing)
                ? existing switch
                {
                    CliArgValue.Flag => new CliArgValue.Single(value),
                    CliArgValue.Single single => new CliArgValue.Multi(new List<string> { single.Value, value }),
                    CliArgValue.Multi multi => AppendValue(multi, value),
                }
                : new CliArgValue.Single(value);
        }

        foreach (var key in arguments.Keys)
        {
            if (!key.StartsWith(NegationPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            var option = key[NegationPrefix.Length..];
            if (arguments.ContainsKey(option))
            {
                throw new ArgumentException($"CLI arguments '--{option}' and '--{key}' conflict.");
            }
        }

        return arguments;
    }

    private static CliArgValue.Multi AppendValue(CliArgValue.Multi multi, string value)
    {
        ((List<string>)multi.Values).Add(value);
        return multi;
    }
}
