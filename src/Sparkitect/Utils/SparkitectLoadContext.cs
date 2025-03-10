using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;

namespace Sparkitect.Utils;

internal class SparkitectLoadContext : AssemblyLoadContext
{
    private readonly Dictionary<string, WeakReference<Assembly>> _sharedAssemblies = new();
    private readonly SparkitectLoadContext? _parentContext;

    public SparkitectLoadContext(SparkitectLoadContext? parentContext) : base(true)
    {
        _parentContext = parentContext;
    }

    /// <inheritdoc />
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (_sharedAssemblies.TryGetValue(assemblyName.FullName, out var weakRef) &&
            weakRef.TryGetTarget(out var assembly))
        {
            return assembly;
        }

        return null;
    }

    private bool TryLoadCachedAssembly(AssemblyName assemblyName, [MaybeNullWhen(false)] out Assembly assembly)
    {
        if (_parentContext is not null && _parentContext.TryLoadCachedAssembly(assemblyName, out assembly))
        {
            return true;
        }

        if (_sharedAssemblies.TryGetValue(assemblyName.FullName, out var weakRef) && weakRef.TryGetTarget(out assembly))
        {
            return true;
        }

        assembly = null;
        return false;
    }

    public Assembly CachedLoadFromStream(Stream dllStream, Stream? pdbStream = null)
    {
        using var moduleMetadata = ModuleMetadata.CreateFromStream(dllStream);
        var assemblyName = moduleMetadata.GetMetadataReader().GetAssemblyDefinition().GetAssemblyName();

        if (TryLoadCachedAssembly(assemblyName, out var assembly))
        {
            return assembly;
        }

        assembly = LoadFromStream(dllStream, pdbStream);
        _sharedAssemblies[assemblyName.FullName] = new WeakReference<Assembly>(assembly);
        return assembly;
    }
}