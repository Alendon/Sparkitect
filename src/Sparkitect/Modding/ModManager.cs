using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using OneOf.Types;
using Sparkitect.DI;
using Sparkitect.Utils;
using OneOf;
using Semver;
using Serilog;
using Sparkitect.DI.Container;
using Sparkitect.DI.GeneratorAttributes;

namespace Sparkitect.Modding;

/// <summary>
/// Implementation of the IModManager interface for managing mods
/// </summary>
[CreateServiceFactory<IModManager>]
internal class ModManager : IModManager
{
    private readonly Dictionary<string, LoadedMod> _loadedMods = [];
    private readonly List<ModManifest> _discoveredArchives = [];

    private readonly Dictionary<string, Assembly> _preLoadedAssemblies = [];

    private readonly Stack<LoadedModGroup> _loadedModGroups = new();
    private readonly ICliArgumentHandler _cliArgumentHandler;
    private readonly IIdentificationManager _identificationManager;

    private const string AddModDirsArgument = "addModDirs";
    public const string VirtualSparkitectModId = "sparkitect.core";

    public ModManager(ICliArgumentHandler cliArgumentHandler,
        IIdentificationManager identificationManager)
    {
        _cliArgumentHandler = cliArgumentHandler;
        _identificationManager = identificationManager;

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var name = assembly.GetName().Name;
            if (string.IsNullOrEmpty(name)) continue;

            _preLoadedAssemblies[name] = assembly;
        }
    }


    public ICoreContainer CurrentCoreContainer =>
        _loadedModGroups.Count > 0 ? _loadedModGroups.Peek().CoreContainer : BaseCoreContainer;

    internal ICoreContainer BaseCoreContainer
    {
        get => field ?? throw new InvalidOperationException("Base Core Container not set");
        set;
    } = null!;

    /// <summary>
    /// Gets a collection of all loaded mods
    /// </summary>
    public IReadOnlyCollection<string> LoadedMods => _loadedMods.Keys;

    public IReadOnlyList<IReadOnlyList<string>> LoadedModsPerGroup =>
        _loadedModGroups.Select(g => g.ModIds).ToList();

    public IReadOnlyList<ModManifest> DiscoveredArchives => _discoveredArchives;

    /// <summary>
    /// Discovers all available mods from the mods folder and from any additional directories specified by the addModDirs argument
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

        // Check for additional mod directories specified in command line arguments
        var additionalModFiles = new List<string>();
        if (_cliArgumentHandler.TryGetArgumentValues(AddModDirsArgument, out var additionalModDirs))
        {
            ProcessAdditionalModDirs(additionalModDirs, additionalModFiles);
        }

        // Combine all mod files from default location and additional directories
        var allModFiles = modFiles.Concat(additionalModFiles).ToArray();

        // Process each mod file
        foreach (var modFile in allModFiles)
        {
            DiscoverModArchive(modFile);
        }

        // Add the virtual Sparkitect mod
        var sparkitectModManifest = CreateSparkitectModManifest();
        _discoveredArchives.Add(sparkitectModManifest);
        Log.Information("Discovered mod: {ModId} (virtual)", sparkitectModManifest.Id);

        Log.Information("Discovered {ModCount} mods", _discoveredArchives.Count);
    }

    private void DiscoverModArchive(string modFile)
    {
        // Open the zip archive
        using var archive = ZipFile.OpenRead(modFile);

        // Read the mod manifest
        var manifestEntry = archive.GetEntry("manifest.json");
        if (manifestEntry is null)
        {
            Log.Warning("Mod {ModFile} does not contain a manifest.json file", modFile);
            return;
        }

        using var manifestStream = manifestEntry.Open();
        var manifest = JsonSerializer.Deserialize<ModManifest>(manifestStream);

        if (manifest is null)
        {
            Log.Warning("Failed to read manifest.json for mod {ModFile}", modFile);
            return;
        }

        manifest = manifest with { ModPath = modFile };

        // For now, just store the archive for later loading
        _discoveredArchives.Add(manifest);

        Log.Information("Discovered mod: {ModFile}", Path.GetFileName(modFile));
    }

    private static void ProcessAdditionalModDirs(IReadOnlyList<string> additionalModDirs,
        List<string> additionalModFiles)
    {
        foreach (var dirPath in additionalModDirs)
        {
            if (Directory.Exists(dirPath))
            {
                // Add all .sparkmod files in the specified directory
                additionalModFiles.AddRange(
                    Directory.GetFiles(dirPath, "*.sparkmod", SearchOption.TopDirectoryOnly));
                Log.Information("Added mod directory: {DirPath}", dirPath);
            }
            else if (File.Exists(dirPath) &&
                     Path.GetExtension(dirPath).Equals(".sparkmod", StringComparison.OrdinalIgnoreCase))
            {
                // Add a single .sparkmod file if it exists
                additionalModFiles.Add(dirPath);
                Log.Information("Added mod file: {FilePath}", dirPath);
            }
            else
            {
                Log.Warning("Specified mod path does not exist or is not a .sparkmod file: {Path}", dirPath);
            }
        }
    }

    /// <summary>
    /// Creates a virtual manifest for the Sparkitect core mod
    /// </summary>
    /// <returns>A ModManifest representing the Sparkitect core</returns>
    private ModManifest CreateSparkitectModManifest()
    {
        // This method will be implemented by the user later
        return new ModManifest
        (
            Id: VirtualSparkitectModId,
            Name: "Sparkitect Core",
            Version: SemVersion.Parse("1.0.0"),
            Description: "Core engine functionality",
            ModAssembly: "Sparkitect",
            ModPath: null!, // No physical path for virtual mod
            Relationships: [],
            Authors: [],
            RequiredAssemblies: []
        );
    }

    /// <summary>
    /// Loads all discovered mods
    /// </summary>
    public void LoadMods(params ReadOnlySpan<string> modIds)
    {
        //TODO Resolve and validate mod relations
        //TODO Load External Dependencies (Non Mod dotnet assemblies)

        _loadedModGroups.TryPeek(out var parentModGroup);
        var loadContext = new SparkitectLoadContext(parentModGroup?.LoadContextHandle.Target as SparkitectLoadContext,
            _preLoadedAssemblies);

        var newLoadedMods = new List<LoadedMod>(modIds.Length);

        foreach (var modId in modIds)
        {
            var modManifest = _discoveredArchives.FirstOrDefault(m => m.Id == modId);
            _identificationManager.RegisterMod(modId);

            if (modManifest is null)
            {
                //TODO result based error handling
                throw new InvalidOperationException($"Mod {modId} not found");
            }

            // Check if it is a virtual mod (e.g. Sparkitect core)
            if (modManifest.ModPath is null)
            {
                if (!_preLoadedAssemblies.TryGetValue(modManifest.ModAssembly, out var preLoadedAssembly))
                {
                    Log.Warning("Virtual mod {ModId} assembly {Assembly} not found", modId, modManifest.ModAssembly);
                    continue;
                }

                newLoadedMods.Add(new LoadedMod
                {
                    Archive = null, // No archive for virtual mod
                    Assembly = preLoadedAssembly,
                    Manifest = modManifest
                });

                Log.Information("Loaded virtual mod: {ModId}", modId);
                continue;
            }

            // Handle regular mods with archives
            var archive = ZipFile.OpenRead(modManifest.ModPath);

            // Load required assemblies first
            LoadModDependencies(modManifest, archive, modId, loadContext);


            // Load the main mod assembly
            var assembly = LoadModAssembly(archive, modManifest, modId, loadContext);
            newLoadedMods.Add(new LoadedMod
            {
                Archive = archive,
                Assembly = assembly,
                Manifest = modManifest
            });
        }

        foreach (var newLoadedMod in newLoadedMods)
        {
            _loadedMods.Add(newLoadedMod.Manifest.Id, newLoadedMod);
        }
        
        
        using var configurationContainer = CreateEntrypointContainer<CoreConfigurator>(modIds.ToArray());
        var configurators = configurationContainer.ResolveMany();
        var coreContainerBuilder = new CoreContainerBuilder(CurrentCoreContainer);

        foreach (var coreConfigurator in configurators)
        {
            coreConfigurator.ConfigureIoc(coreContainerBuilder);
        }

        var modGroup = new LoadedModGroup()
        {
            LoadContextHandle = GCHandle.Alloc(loadContext, GCHandleType.Normal),
            ModIds = modIds.ToArray(),
            CoreContainer = coreContainerBuilder.Build()
        };

        _loadedModGroups.Push(modGroup);
        

        Log.Information("Loaded {ModCount} mods", _loadedMods.Count);
    }

    private static Assembly LoadModAssembly(ZipArchive archive, ModManifest modManifest, string modId,
        SparkitectLoadContext loadContext)
    {
        var modAssemblyEntry = archive.GetEntry(modManifest.ModAssembly);
        if (modAssemblyEntry is null)
        {
            //TODO result based error handling
            throw new InvalidOperationException(
                $"Mod {modId} does not contain the specified assembly {modManifest.ModAssembly}");
        }

        using var assemblyStream = modAssemblyEntry.Open();
        using var pdbStream = archive.GetEntry(Path.ChangeExtension(modManifest.ModAssembly, ".pdb"))?.Open();

        using var assemblyMemoryStream = new MemoryStream();
        assemblyStream.CopyTo(assemblyMemoryStream);
        assemblyMemoryStream.Seek(0, SeekOrigin.Begin);

        MemoryStream? pdbMemoryStream = pdbStream is not null ? new MemoryStream() : null;
        pdbStream?.CopyTo(pdbMemoryStream!);
        pdbMemoryStream?.Seek(0, SeekOrigin.Begin);

        var assembly = loadContext.CachedLoadFromStream(assemblyMemoryStream, pdbMemoryStream);
        return assembly;
    }

    private static void LoadModDependencies(ModManifest modManifest, ZipArchive archive, string modId,
        SparkitectLoadContext loadContext)
    {
        foreach (var requiredAssembly in modManifest.RequiredAssemblies)
        {
            var assemblyEntry = archive.GetEntry($"lib/{requiredAssembly}");
            if (assemblyEntry == null)
            {
                Log.Warning("Required assembly {Assembly} not found in mod {ModId}", requiredAssembly, modId);
                continue;
            }

            using var assemblyStream = assemblyEntry.Open();
            using var pdbStream = archive.GetEntry($"lib/{Path.ChangeExtension(requiredAssembly, ".pdb")}")?.Open();

            using var assemblyMemoryStream = new MemoryStream();
            assemblyStream.CopyTo(assemblyMemoryStream);
            assemblyMemoryStream.Seek(0, SeekOrigin.Begin);

            MemoryStream? pdbMemoryStream = pdbStream is not null ? new MemoryStream() : null;
            pdbStream?.CopyTo(pdbMemoryStream!);
            pdbMemoryStream?.Seek(0, SeekOrigin.Begin);

            try
            {
                loadContext.CachedLoadFromStream(assemblyMemoryStream, pdbMemoryStream);
                Log.Debug("Loaded required assembly: {Assembly} for mod {ModId}", requiredAssembly, modId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading required assembly {Assembly} for mod {ModId}", requiredAssembly, modId);
            }
        }
    }

    public IEntrypointContainer<T> CreateEntrypointContainer<T>(OneOf<All, IEnumerable<string>> modsToInclude)
        where T : class, IBaseConfigurationEntrypoint
    {
        var entrypointAttribute = T.EntrypointAttributeType;
        var instances = new List<T>();

        var modIds = modsToInclude.Match(
            _ => _loadedMods.Keys,
            ids => ids);

        foreach (var modId in modIds)
        {
            var mod = _loadedMods[modId];

            // Find all types marked with the entrypoint attribute and assignable to T
            var candidateTypes = mod.Assembly.GetTypes()
                .Where(t => t.GetCustomAttributes(false).Any(a => a.GetType() == entrypointAttribute))
                .Where(t => typeof(T).IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false })
                .ToArray();

            // Order entrypoints deterministically (stub for now)
            var orderedTypes = OrderEntrypoints<T>(candidateTypes);

            foreach (var type in orderedTypes)
            {
                // Only support parameterless constructors for configuration entrypoints
                var ctor = type.GetConstructor(Type.EmptyTypes);
                if (ctor is null)
                {
                    Log.Warning("Skipping entrypoint type {Type} without parameterless constructor", type.FullName);
                    continue;
                }

                if (Activator.CreateInstance(type) is T instance)
                {
                    instances.Add(instance);
                }
                else
                {
                    Log.Warning("Failed to instantiate entrypoint type {Type}", type.FullName);
                }
            }
        }

        return new EntrypointContainer<T>(instances);
    }

    // TODO: When mod dependencies and ordering semantics are implemented,
    //       this method should apply a deterministic ordering for entrypoint execution.
    //       For now, it returns the input unchanged.
    private static IEnumerable<Type> OrderEntrypoints<T>(IEnumerable<Type> types) where T : class, IBaseConfigurationEntrypoint
        => types;

    private class LoadedModGroup
    {
        public required GCHandle LoadContextHandle { get; init; }
        public required string[] ModIds { get; init; }
        public required ICoreContainer CoreContainer { get; init; }
    }

    private class LoadedMod
    {
        public required ModManifest Manifest { get; init; }
        public required Assembly Assembly { get; init; }
        public ZipArchive? Archive { get; init; } // Nullable as virtual mods don't have an archive
    }
}
