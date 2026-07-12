using JetBrains.Annotations;
using Sparkitect.Modding;
using Sparkitect.Utils.DU;
using YamlDotNet.RepresentationModel;

namespace Sparkitect.Settings.Sources;

/// <summary>
/// The readonly engine-config source. Reads the human-editable <c>Sparkitect.yaml</c> via YAML node
/// traversal (never POCO/reflection auto-bind) and supplies each mapped scalar parsed against the
/// declared setting type. Window size is deliberately not carried here. Writes are refused.
/// </summary>
[PublicAPI]
public sealed class EngineSettingsSource : ISettingSource
{
    /// <summary>The engine-config file name resolved from the engine working directory.</summary>
    public const string ConfigFileName = "Sparkitect.yaml";

    private readonly Dictionary<string, string> _scalars;
    private readonly Func<Identification, string?> _keyProvider;
    private readonly Func<Identification, ISettingDeclaration?> _declarationProvider;

    /// <summary>Creates the source over an in-memory YAML document.</summary>
    /// <param name="yamlText">The YAML document text, or null when no config is present.</param>
    /// <param name="keyProvider">Maps a setting id to its YAML key (its registration name), or null when unmapped.</param>
    /// <param name="declarationProvider">Resolves a setting's declaration (its scalar parser).</param>
    /// <param name="orderBefore">Sources this source outranks.</param>
    /// <param name="orderAfter">Sources that outrank this source.</param>
    public EngineSettingsSource(
        string? yamlText,
        Func<Identification, string?> keyProvider,
        Func<Identification, ISettingDeclaration?> declarationProvider,
        IReadOnlyList<SettingSourceOrder>? orderBefore = null,
        IReadOnlyList<SettingSourceOrder>? orderAfter = null)
    {
        _keyProvider = keyProvider;
        _declarationProvider = declarationProvider;
        _scalars = ParseScalars(yamlText);
        OrderBefore = orderBefore ?? [];
        OrderAfter = orderAfter ?? [];
    }

    /// <summary>
    /// Builds a source reading <see cref="ConfigFileName"/> from the engine working directory. A missing
    /// file yields a source that supplies nothing (every setting falls through to lower sources).
    /// </summary>
    /// <param name="keyProvider">Maps a setting id to its YAML key.</param>
    /// <param name="declarationProvider">Resolves a setting's declaration.</param>
    /// <param name="orderBefore">Sources this source outranks.</param>
    /// <param name="orderAfter">Sources that outrank this source.</param>
    public static EngineSettingsSource FromWorkingDirectory(
        Func<Identification, string?> keyProvider,
        Func<Identification, ISettingDeclaration?> declarationProvider,
        IReadOnlyList<SettingSourceOrder>? orderBefore = null,
        IReadOnlyList<SettingSourceOrder>? orderAfter = null)
    {
        return new EngineSettingsSource(ReadWorkingDirectoryText(), keyProvider, declarationProvider, orderBefore, orderAfter);
    }

    /// <summary>
    /// Reads the working-directory <see cref="ConfigFileName"/> and returns its root scalar entries via the
    /// same node traversal the source uses. Shared with the pre-container logger read so engine-config
    /// parsing has a single source of truth. A missing file yields an empty map.
    /// </summary>
    internal static IReadOnlyDictionary<string, string> ReadWorkingDirectoryScalars() =>
        ParseScalars(ReadWorkingDirectoryText());

    private static string? ReadWorkingDirectoryText()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), ConfigFileName);
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }

    /// <inheritdoc/>
    public string SourceId => "engine_config";

    /// <inheritdoc/>
    public bool CanWrite => false;

    /// <inheritdoc/>
    public IReadOnlyList<SettingSourceOrder> OrderBefore { get; }

    /// <inheritdoc/>
    public IReadOnlyList<SettingSourceOrder> OrderAfter { get; }

    /// <inheritdoc/>
    public bool TryGet(Identification id, out object? value)
    {
        value = null;

        if (_keyProvider(id) is not { } key)
        {
            return false;
        }

        if (!_scalars.TryGetValue(key, out var raw))
        {
            return false;
        }

        return _declarationProvider(id) is { } declaration && declaration.TryParseScalar(raw, out value);
    }

    /// <inheritdoc/>
    public Result<SetError> Write(Identification id, object? value) => new SetError.SourceReadonly(Identification.Empty);

    // Node traversal only: read the root mapping's scalar->scalar children. Nested structures are ignored
    // (settings are primitives); there is no DeserializerBuilder/POCO binding.
    private static Dictionary<string, string> ParseScalars(string? yamlText)
    {
        var scalars = new Dictionary<string, string>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(yamlText))
        {
            return scalars;
        }

        var stream = new YamlStream();
        using var reader = new StringReader(yamlText);
        stream.Load(reader);
        if (stream.Documents.Count == 0 || stream.Documents[0].RootNode is not YamlMappingNode root)
        {
            return scalars;
        }

        foreach (var entry in root.Children)
        {
            if (entry.Key is YamlScalarNode { Value: { } key } && entry.Value is YamlScalarNode { Value: { } raw })
            {
                scalars[key] = raw;
            }
        }

        return scalars;
    }
}
