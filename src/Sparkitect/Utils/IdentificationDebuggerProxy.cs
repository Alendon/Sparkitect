using System.Diagnostics;
using Sparkitect.Modding;

namespace Sparkitect.Utils;

internal sealed class IdentificationDebuggerProxy
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

    private readonly Identification _id;

    public IdentificationDebuggerProxy(Identification id)
    {
        _id = id;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public ushort ModId => _id.ModId;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public ushort CategoryId => _id.CategoryId;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public uint ItemId => _id.ItemId;

    public string? Mod => ResolveModString();
    public string? Category => ResolveCategoryString();
    public string? Item => ResolveItemString();
    public string FullId => FormatFullId();

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public string NumericId => $"{_id.ModId}:{_id.CategoryId}:{_id.ItemId}";

    private string? ResolveModString()
    {
        if (_instance is null) return null;
        return _instance.TryGetModId(_id.ModId, out var mod) ? mod : null;
    }

    private string? ResolveCategoryString()
    {
        if (_instance is null) return null;
        return _instance.TryGetCategoryId(_id.CategoryId, out var cat) ? cat : null;
    }

    private string? ResolveItemString()
    {
        if (_instance is null) return null;
        _instance.TryResolveIdentification(_id, out _, out _, out var item);
        return item;
    }

    private string FormatFullId()
    {
        var mod = Mod ?? _id.ModId.ToString();
        var cat = Category ?? _id.CategoryId.ToString();
        var item = Item ?? _id.ItemId.ToString();
        return $"{mod}:{cat}:{item}";
    }

    internal static string? FormatIdentification(Identification id)
    {
        if (_instance is null) return null;
        if (!_instance.TryResolveIdentification(id, out var mod, out var cat, out var item))
        {
            mod ??= id.ModId.ToString();
            cat ??= id.CategoryId.ToString();
            item ??= id.ItemId.ToString();
        }
        return $"{mod}:{cat}:{item}";
    }
}
