using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Serilog;

namespace Sparkitect.Modding;

/// <summary>
/// A collectible <see cref="AssemblyLoadContext"/> for a mod (group). Owns its own assembly cache that
/// dies with the context — no cache is shared across sibling contexts. Cross-ALC identity
/// must always be compared by <see cref="Assembly"/>/<see cref="Type"/> reference (<c>ReferenceEquals</c>),
/// never by name string: a name match across two different ALCs does not imply the same loaded instance,
/// and comparing by name is exactly the split-identity bug this contract guards against.
/// </summary>
internal class SparkitectLoadContext : AssemblyLoadContext
{
    private readonly SparkitectLoadContext? _parentContext;
    private readonly Dictionary<string, Assembly> _loadedAssemblies;
    private readonly IReadOnlySet<string>? _allowedAssemblyNames;
    private readonly string _ownerLabel;

    /// <param name="parentContext">The parent context to walk up to for scoped downward sharing.</param>
    /// <param name="allowedAssemblyNames">
    /// The resolve-safety set: simple assembly names this context is expected to resolve. A resolve
    /// outside this set is a warn-only observability signal (never enforced) — pass null to skip the check.
    /// </param>
    /// <param name="ownerLabel">A label naming the owner (mod/group) for resolve-safety warning messages.</param>
    public SparkitectLoadContext(SparkitectLoadContext? parentContext,
        IReadOnlySet<string>? allowedAssemblyNames = null, string ownerLabel = "") : base(true)
    {
        _parentContext = parentContext;
        _loadedAssemblies = new Dictionary<string, Assembly>(); // instance-owned, dies with this ALC
        _allowedAssemblyNames = allowedAssemblyNames;
        _ownerLabel = ownerLabel;

        // A collectible ALC that keeps strong references to its own loaded Assembly instances in a managed
        // field never unloads — the field roots the assemblies, which root this context, and the collectible
        // unload machinery cannot break that cycle while the strong refs live. Dropping them at Unload()
        // initiation (the Unloading event) is what actually lets the cache "die with its context" and
        // is the prerequisite that makes the mod DLL leave memory. Verified empirically: without
        // this clear the context stays alive across unbounded forced-GC drains; with it, it dies immediately.
        Unloading += _ => _loadedAssemblies.Clear();
    }

    /// <inheritdoc />
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        TryLoadCachedAssembly(assemblyName, out var assembly);
        return assembly;
    }

    private bool TryLoadCachedAssembly(AssemblyName assemblyName, [MaybeNullWhen(false)] out Assembly assembly)
    {
        if (assemblyName.Name is not null && _loadedAssemblies.TryGetValue(assemblyName.Name, out assembly))
        {
            return true;
        }

        if (_parentContext is not null && _parentContext.TryLoadCachedAssembly(assemblyName, out assembly))
        {
            return true;
        }

        assembly = null;
        return false;
    }

    public Assembly CachedLoadFromStream(Stream dllStream, Stream? pdbStream = null)
    {
        AssemblyName assemblyName;
        Assembly? assembly;

        using (var moduleMetadata = ModuleMetadata.CreateFromStream(dllStream, PEStreamOptions.LeaveOpen))
        {
            assemblyName = moduleMetadata.GetMetadataReader().GetAssemblyDefinition().GetAssemblyName();

            if (TryLoadCachedAssembly(assemblyName, out assembly))
            {
                return assembly;
            }
        }
        dllStream.Seek(0, SeekOrigin.Begin);

        assembly = LoadFromStream(dllStream, pdbStream);

        if (assemblyName.Name is not null)
        {
            _loadedAssemblies[assemblyName.Name] = assembly;

            // Warn-only resolve-safety net. A resolve outside the manifest-derived allowed set is
            // observed, never blocked — the load already happened above and proceeds normally either way.
            if (_allowedAssemblyNames is not null && !_allowedAssemblyNames.Contains(assemblyName.Name))
            {
                Log.Warning(
                    "Mod ALC {Owner} resolved assembly {Assembly} outside its declared dependency closure",
                    _ownerLabel, assemblyName.Name);
            }
        }

        return assembly;
    }
}