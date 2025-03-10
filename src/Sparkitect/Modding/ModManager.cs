using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using DryIoc;
using JetBrains.Annotations;
using Sparkitect.DI;
using Sparkitect.Utils;

namespace Sparkitect.Modding;

/// <summary>
/// Implementation of the IModManager interface for managing mods
/// </summary>
internal class ModManager : IModManager
{
    private readonly Dictionary<string, LoadedMod> _loadedMods = [];
    private readonly List<ModManifest> _discoveredArchives = [];

    private readonly Stack<LoadedModGroup> _loadedModGroups = new();
    private readonly IContainer _baseCoreContainer;

    public ModManager(IContainer baseCoreContainer)
    {
        _baseCoreContainer = baseCoreContainer;
    }


    public IContainer CurrentCoreContainer =>
        _loadedModGroups.Count > 0 ? _loadedModGroups.Peek().CoreContainer : _baseCoreContainer;

    /// <summary>
    /// Gets a collection of all loaded mods
    /// </summary>
    public IReadOnlyCollection<string> LoadedMods => _loadedMods.Keys;

    /// <summary>
    /// Discovers all available mods from the mods folder
    /// </summary>
    public void DiscoverMods()
    {
        // Create the mods directory if it doesn't exist
        var modsFolder = Path.Combine(AppContext.BaseDirectory, "mods");
        if (!Directory.Exists(modsFolder))
        {
            Directory.CreateDirectory(modsFolder);
        }

        // Find all .sparkmod files in the mods folder
        var modFiles = Directory.GetFiles(modsFolder, "*.sparkmod", SearchOption.TopDirectoryOnly);

        // Clear any previously discovered archives
        _discoveredArchives.Clear();

        // Process each mod file
        foreach (var modFile in modFiles)
        {
            try
            {
                // Open the zip archive
                using var archive = ZipFile.OpenRead(modFile);

                // Read the mod manifest
                var manifestEntry = archive.GetEntry("manifest.json");
                if (manifestEntry is null)
                {
                    Console.WriteLine($"Warning: Mod {modFile} does not contain a manifest.json file");
                    continue;
                }

                using var manifestStream = manifestEntry.Open();
                var manifest = JsonSerializer.Deserialize<ModManifest>(manifestStream);

                if (manifest is null)
                {
                    Console.WriteLine($"Warning: Failed to read manifest.json for mod {modFile}");
                    continue;
                }

                manifest = manifest with { ModPath = modFile };

                // For now, just store the archive for later loading
                _discoveredArchives.Add(manifest);

                Console.WriteLine($"Discovered mod: {Path.GetFileName(modFile)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing mod file {modFile}: {ex.Message}");
            }
        }

        Console.WriteLine($"Discovered {_discoveredArchives.Count} mods");
    }

    /// <summary>
    /// Loads all discovered mods
    /// </summary>
    public void LoadMods(params ReadOnlySpan<string> modIds)
    {
        //TODO Resolve and validate mod relations
        //TODO Load External Dependencies (Non Mod dotnet assemblies)

        _loadedModGroups.TryPeek(out var parentModGroup);
        var loadContext = new SparkitectLoadContext(parentModGroup?.LoadContextHandle.Target as SparkitectLoadContext);
        var coreContainer = _baseCoreContainer.CreateChild();

        var modGroup = new LoadedModGroup()
        {
            LoadContextHandle = GCHandle.Alloc(loadContext, GCHandleType.Normal),
            ModIds = modIds.ToArray(),
            CoreContainer = coreContainer
        };

        var newLoadedMods = new List<LoadedMod>(modIds.Length);

        foreach (var modId in modIds)
        {
            var modManifest = _discoveredArchives.FirstOrDefault(m => m.Id == modId);

            if (modManifest is null)
            {
                //TODO result based error handling
                throw new InvalidOperationException($"Mod {modId} not found");
            }

            var archive = ZipFile.OpenRead(modManifest.ModPath);

            var modAssemblyEntry = archive.GetEntry(modManifest.ModAssembly);
            if (modAssemblyEntry is null)
            {
                //TODO result based error handling
                throw new InvalidOperationException(
                    $"Mod {modId} does not contain the specified assembly {modManifest.ModAssembly}");
            }

            using var assemblyStream = modAssemblyEntry.Open();
            using var pdbStream = archive.GetEntry(Path.ChangeExtension(modManifest.ModAssembly, ".pdb"))?.Open();

            var assembly = loadContext.CachedLoadFromStream(assemblyStream, pdbStream);
            newLoadedMods.Add(new LoadedMod
            {
                Archive = archive,
                Assembly = assembly,
                Manifest = modManifest
            });
        }
        
        //TODO Populate core container with mod services


        _loadedModGroups.Push(modGroup);
        foreach (var newLoadedMod in newLoadedMods)
        {
            _loadedMods.Add(newLoadedMod.Manifest.Id, newLoadedMod);
        }
        
        Console.WriteLine($"Loaded {_loadedMods.Count} mods");
    }

    [MustDisposeResource]
    public IContainer CreateConfigurationContainer<T>(bool trackDisposeTransients) where T : BaseConfigurationEntrypoint
    {
        if (CurrentCoreContainer is null)
        {
            throw new InvalidOperationException("Core container has not been initialized");
        }

        var rules = trackDisposeTransients ? Rules.Default.WithTrackingDisposableTransients() : Rules.Default;

        var container = CurrentCoreContainer.CreateChild(newRules: rules);

        return CreateConfigurationContainer<T>(container);
    }

    public IContainer CreateConfigurationContainer<T>(IContainer configurationContainer)
        where T : BaseConfigurationEntrypoint
    {
        foreach (var (_, mod) in _loadedMods)
        {
            var entrypointAttribute = T.EntrypointAttributeType;

            var entrypointTypes = mod.Assembly.GetTypes()
                .Where(t => t.GetCustomAttributes(false).Any(a => a.GetType() == entrypointAttribute))
                .Where(t => typeof(T).IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false });

            //TODO Future, when mod dependencies are implemented, we will need to have ordered entrypoints

            foreach (var entrypointType in entrypointTypes)
            {
                configurationContainer.Register(typeof(T), entrypointType, Reuse.Transient);
            }
        }

        return configurationContainer;
    }

    private class LoadedModGroup
    {
        public required GCHandle LoadContextHandle { get; init; }
        public required string[] ModIds { get; init; }
        public required IContainer CoreContainer { get; init; }
    }

    private class LoadedMod
    {
        public required ModManifest Manifest { get; init; }
        public required Assembly Assembly { get; init; }
        public required ZipArchive Archive { get; init; }
    }
}