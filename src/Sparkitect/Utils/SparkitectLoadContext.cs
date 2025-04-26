using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;

namespace Sparkitect.Utils;

internal class SparkitectLoadContext : AssemblyLoadContext
{
    private readonly SparkitectLoadContext? _parentContext;
    private readonly Dictionary<string, Assembly> _loadedAssemblies;

    public SparkitectLoadContext(SparkitectLoadContext? parentContext, Dictionary<string, Assembly> loadedAssemblies) :
        base(true)
    {
        _parentContext = parentContext;
        _loadedAssemblies = loadedAssemblies;
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
        using var moduleMetadata = ModuleMetadata.CreateFromStream(dllStream, PEStreamOptions.LeaveOpen);
        dllStream.Seek(0, SeekOrigin.Begin);

        var assemblyName = moduleMetadata.GetMetadataReader().GetAssemblyDefinition().GetAssemblyName();

        if (TryLoadCachedAssembly(assemblyName, out var assembly))
        {
            return assembly;
        }

        assembly = LoadFromStream(dllStream, pdbStream);

        if (assemblyName.Name is not null)
            _loadedAssemblies[assemblyName.Name] = assembly;
        
        return assembly;
    }
}