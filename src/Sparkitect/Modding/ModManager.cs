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
    public required ICliArgumentHandler CliArgumentHandler { private get; init; }
    public required IIdentificationManager IdentificationManager { private get; init; }
    public required IModDIService ModDiService { private get; init; }
    public required IResourceManager ResourceManager { private get; init; }

    private const string AddModDirsArgument = "addModDirs";

    public ModManager()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var name = assembly.GetName().Name;
            if (string.IsNullOrEmpty(name)) continue;

            _preLoadedAssemblies[name] = assembly;
        }
    }

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
        if (CliArgumentHandler.TryGetArgumentValues(AddModDirsArgument, out var additionalModDirs))
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

    private void ValidateModDependencies(ReadOnlySpan<string> modIdsToLoad)
    {
        var allAvailableMods = new HashSet<string>(_loadedMods.Keys);
        foreach (var modId in modIdsToLoad)
        {
            allAvailableMods.Add(modId);
        }

        foreach (var modId in modIdsToLoad)
        {
            var modManifest = _discoveredArchives.FirstOrDefault(m => m.Id == modId);
            if (modManifest is null) continue;

            foreach (var relationship in modManifest.Relationships)
            {
                if (relationship.RelationshipType == ModRelationshipType.Dependency)
                {
                    if (!allAvailableMods.Contains(relationship.Id))
                    {
                        throw new InvalidOperationException(
                            $"Mod '{modId}' requires dependency '{relationship.Id}' which is not loaded or available.");
                    }

                    var dependencyManifest = _discoveredArchives.FirstOrDefault(m => m.Id == relationship.Id)
                        ?? _loadedMods.GetValueOrDefault(relationship.Id)?.Manifest;

                    if (dependencyManifest != null && !relationship.VersionRange.Contains(dependencyManifest.Version))
                    {
                        throw new InvalidOperationException(
                            $"Mod '{modId}' requires '{relationship.Id}' version {relationship.VersionRange}, but found version {dependencyManifest.Version}.");
                    }
                }
                else if (relationship.RelationshipType == ModRelationshipType.Incompatible)
                {
                    if (allAvailableMods.Contains(relationship.Id))
                    {
                        var incompatibleManifest = _discoveredArchives.FirstOrDefault(m => m.Id == relationship.Id)
                            ?? _loadedMods.GetValueOrDefault(relationship.Id)?.Manifest;

                        if (incompatibleManifest != null && relationship.VersionRange.Contains(incompatibleManifest.Version))
                        {
                            throw new InvalidOperationException(
                                $"Mod '{modId}' is incompatible with '{relationship.Id}' version {incompatibleManifest.Version}.");
                        }
                    }
                }
            }
        }

        Log.Debug("Mod dependency validation completed successfully for {Count} mods", modIdsToLoad.Length);
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
            Id: Constants.VirtualSparkitectModId,
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
        ValidateModDependencies(modIds);

        _loadedModGroups.TryPeek(out var parentModGroup);
        var loadContext = new SparkitectLoadContext(parentModGroup?.LoadContextHandle.Target as SparkitectLoadContext,
            _preLoadedAssemblies);

        var newLoadedMods = new List<LoadedMod>(modIds.Length);

        foreach (var modId in modIds)
        {
            var modManifest = _discoveredArchives.FirstOrDefault(m => m.Id == modId);
            IdentificationManager.RegisterMod(modId);

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
            ZipArchive? archive = null;
            try
            {
                archive = ZipFile.OpenRead(modManifest.ModPath);

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
                archive = null; // Transfer ownership to LoadedMod, prevent disposal
            }
            finally
            {
                archive?.Dispose(); // Only disposes if exception occurred (archive not transferred)
            }
        }

        foreach (var newLoadedMod in newLoadedMods)
        {
            _loadedMods.Add(newLoadedMod.Manifest.Id, newLoadedMod);
            ResourceManager.OnModLoaded(newLoadedMod.Manifest.Id, newLoadedMod.Archive);
        }

        var modGroup = new LoadedModGroup()
        {
            LoadContextHandle = GCHandle.Alloc(loadContext, GCHandleType.Normal),
            ModIds = modIds.ToArray()
        };

        _loadedModGroups.Push(modGroup);

        // Notify ModDIService of newly loaded assemblies
        var modAssemblies = newLoadedMods.ToDictionary(
            m => m.Manifest.Id,
            m => m.Assembly);
        ModDiService.RegisterModAssemblies(modAssemblies);

        Log.Information("Loaded {ModCount} mods", _loadedMods.Count);
    }

    public IReadOnlyList<string> UnloadLastModGroup()
    {
        if (_loadedModGroups.Count == 0)
        {
            Log.Warning("Attempted to unload mod group but no groups are loaded");
            return Array.Empty<string>();
        }

        var group = _loadedModGroups.Pop();
        var modIds = group.ModIds;

        foreach (var modId in modIds)
        {
            if (_loadedMods.TryGetValue(modId, out var loadedMod))
            {
                ResourceManager.OnModUnloaded(modId);
                loadedMod.Archive?.Dispose();
                _loadedMods.Remove(modId);
            }
        }

        if (group.LoadContextHandle.Target is SparkitectLoadContext loadContext)
        {
            loadContext.Unload();
        }
        group.LoadContextHandle.Free();

        // Notify ModDIService of unloaded mods
        ModDiService.UnregisterMods(modIds);

        Log.Information("Unloaded {ModCount} mods from group", modIds.Length);
        return modIds;
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

    private class LoadedModGroup
    {
        public required GCHandle LoadContextHandle { get; init; }
        public required string[] ModIds { get; init; }
    }

    private class LoadedMod
    {
        public required ModManifest Manifest { get; init; }
        public required Assembly Assembly { get; init; }
        public ZipArchive? Archive { get; init; } // Nullable as virtual mods don't have an archive
    }
}
