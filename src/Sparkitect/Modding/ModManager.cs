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
using Sparkitect.GameState;

namespace Sparkitect.Modding;

/// <summary>
/// Implementation of the IModManager interface for managing mods
/// </summary>
[StateService<IModManager, CoreModule>]
internal class ModManager : IModManager
{
    private readonly Dictionary<string, LoadedMod> _loadedMods = [];
    private readonly List<ModManifest> _discoveredArchives = [];

    private readonly Dictionary<string, Assembly> _preLoadedAssemblies = [];

    private readonly Stack<LoadedModGroup> _loadedModGroups = new();
    public required ICliArgumentHandler CliArgumentHandler { private get; init; }
    public required IIdentificationManager IdentificationManager { private get; init; }
    public required IDIService ModDiService { private get; init; }
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
    /// Gets a collection of all loaded mods with their file identifiers (ID + Version).
    /// </summary>
    public IReadOnlyCollection<ModFileIdentifier> LoadedMods =>
        _loadedMods.Values.Select(m => new ModFileIdentifier(m.Manifest.Id, m.Manifest.Version)).ToList();

    public IReadOnlyList<IReadOnlyList<ModFileIdentifier>> LoadedModsPerGroup =>
        _loadedModGroups.Select(g => g.ModIdentifiers).ToList();

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

        // Clear any previously discovered archives and duplicate tracking
        _discoveredArchives.Clear();
        _seenModIdentifiers.Clear();
        _modIdentifierPaths.Clear();

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

    // Track seen mod identifiers for duplicate detection during discovery
    private readonly HashSet<ModFileIdentifier> _seenModIdentifiers = [];
    // Track first-seen path for each identifier for duplicate warning messages
    private readonly Dictionary<ModFileIdentifier, string> _modIdentifierPaths = new();

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

        // Check for exact duplicate (same ID AND same Version)
        var identifier = new ModFileIdentifier(manifest.Id, manifest.Version);
        if (!_seenModIdentifiers.Add(identifier))
        {
            // Exact duplicate - warn and skip (same ID and same version)
            var existingPath = _modIdentifierPaths.GetValueOrDefault(identifier, "unknown");
            Log.Warning("Duplicate mod discovered: {Id}@{Version} (already found at {ExistingPath})",
                manifest.Id, manifest.Version, existingPath);
            return;
        }

        // Track the path for this identifier (for future duplicate warnings)
        _modIdentifierPaths[identifier] = modFile;

        // Store the archive for later loading
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

    private ValidationResult ValidateModDependencies(ReadOnlySpan<string> modIdsToLoad)
    {
        var errors = new List<ValidationError>();

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
                // Check for self-reference (Pitfall 5)
                if (relationship.Id == modId)
                {
                    errors.Add(new ValidationError.SelfReference(modId));
                    continue;
                }

                if (relationship.IsIncompatible)
                {
                    // Incompatibility check - error if incompatible mod is present
                    if (allAvailableMods.Contains(relationship.Id))
                    {
                        var incompatibleManifest = _discoveredArchives.FirstOrDefault(m => m.Id == relationship.Id)
                            ?? _loadedMods.GetValueOrDefault(relationship.Id)?.Manifest;

                        if (incompatibleManifest != null && relationship.VersionRange.Contains(incompatibleManifest.Version))
                        {
                            errors.Add(new ValidationError.IncompatibleMod(
                                modId,
                                relationship.Id,
                                incompatibleManifest.Version.ToString()));
                        }
                    }
                }
                else if (!relationship.IsOptional)
                {
                    // Required dependency - error if missing
                    if (!allAvailableMods.Contains(relationship.Id))
                    {
                        errors.Add(new ValidationError.MissingDependency(modId, relationship.Id));
                        continue;
                    }

                    // Version check for required dependencies
                    var dependencyManifest = _discoveredArchives.FirstOrDefault(m => m.Id == relationship.Id)
                        ?? _loadedMods.GetValueOrDefault(relationship.Id)?.Manifest;

                    if (dependencyManifest != null && !relationship.VersionRange.Contains(dependencyManifest.Version))
                    {
                        errors.Add(new ValidationError.VersionMismatch(
                            modId,
                            relationship.Id,
                            relationship.VersionRange.ToString(),
                            dependencyManifest.Version.ToString()));
                    }
                }
                // Optional dependencies (IsOptional = true, IsIncompatible = false): no validation needed
            }
        }

        if (errors.Count > 0)
        {
            Log.Debug("Mod dependency validation found {ErrorCount} errors for {Count} mods", errors.Count, modIdsToLoad.Length);
            return ValidationResult.Failure(errors);
        }

        Log.Debug("Mod dependency validation completed successfully for {Count} mods", modIdsToLoad.Length);
        return ValidationResult.Success;
    }

    /// <summary>
    /// Creates a virtual manifest for the Sparkitect core mod
    /// </summary>
    /// <returns>A ModManifest representing the Sparkitect core</returns>
    private ModManifest CreateSparkitectModManifest()
    {
        // Virtual manifest for Sparkitect engine - values mirror Sparkitect.csproj properties
        return new ModManifest
        (
            Id: Constants.VirtualSparkitectModId,  // "sparkitect" - matches ModIdentifier in csproj
            Name: "Sparkitect",                     // Matches ModName in csproj
            Version: SemVersion.Parse(Constants.VirtualSparkitectVersion),     // Matches ModVersion in csproj
            Description: "Core engine functionality",
            ModAssembly: "Sparkitect",
            ModPath: null!, // No physical path for virtual mod
            Relationships: [],
            Authors: [],
            RequiredAssemblies: []
        );
    }

    /// <summary>
    /// Loads the specified mods by their file identifiers.
    /// </summary>
    /// <param name="identifiers">The mod file identifiers (ID + Version) to load.</param>
    public void LoadMods(params ReadOnlySpan<ModFileIdentifier> identifiers)
    {
        // Validation uses IDs only (dependencies are ID-based)
        var modIds = identifiers.ToArray().Select(x => x.Id).ToArray();
        var validationResult = ValidateModDependencies(modIds);

        if (!validationResult.IsValid)
        {
            var errorMessages = string.Join(Environment.NewLine, validationResult.Errors.Select(e => $"  - {e.Message}"));
            throw new InvalidOperationException($"Mod dependency validation failed:{Environment.NewLine}{errorMessages}");
        }

        _loadedModGroups.TryPeek(out var parentModGroup);
        var loadContext = new SparkitectLoadContext(parentModGroup?.LoadContextHandle.Target as SparkitectLoadContext,
            _preLoadedAssemblies);

        var newLoadedMods = new List<LoadedMod>(identifiers.Length);

        foreach (var identifier in identifiers)
        {
            // Find manifest by BOTH Id AND Version for unambiguous selection
            var modManifest = _discoveredArchives.FirstOrDefault(m => m.Id == identifier.Id && m.Version == identifier.Version);
            IdentificationManager.RegisterMod(identifier.Id);

            if (modManifest is null)
            {
                //TODO result based error handling
                throw new InvalidOperationException($"Mod {identifier} not found");
            }

            // Check if it is a virtual mod (e.g. Sparkitect core)
            if (modManifest.ModPath is null)
            {
                if (!_preLoadedAssemblies.TryGetValue(modManifest.ModAssembly, out var preLoadedAssembly))
                {
                    Log.Warning("Virtual mod {ModId} assembly {Assembly} not found", identifier.Id, modManifest.ModAssembly);
                    continue;
                }

                newLoadedMods.Add(new LoadedMod
                {
                    Archive = null, // No archive for virtual mod
                    Assembly = preLoadedAssembly,
                    Manifest = modManifest
                });

                Log.Information("Loaded virtual mod: {ModId}", identifier.Id);
                continue;
            }

            // Handle regular mods with archives
            ZipArchive? archive = null;
            try
            {
                archive = ZipFile.OpenRead(modManifest.ModPath);

                // Load required assemblies first
                LoadModDependencies(modManifest, archive, identifier.Id, loadContext);

                // Load the main mod assembly
                var assembly = LoadModAssembly(archive, modManifest, identifier.Id, loadContext);
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
            ModIdentifiers = identifiers.ToArray()
        };

        _loadedModGroups.Push(modGroup);

        // Notify ModDIService of newly loaded assemblies
        var modAssemblies = newLoadedMods.ToDictionary(
            m => m.Manifest.Id,
            m => m.Assembly);
        ModDiService.RegisterModAssemblies(modAssemblies);

        Log.Information("Loaded {ModCount} mods", _loadedMods.Count);
    }

    public IReadOnlyList<ModFileIdentifier> UnloadLastModGroup()
    {
        if (_loadedModGroups.Count == 0)
        {
            Log.Warning("Attempted to unload mod group but no groups are loaded");
            return Array.Empty<ModFileIdentifier>();
        }

        var group = _loadedModGroups.Pop();
        var identifiers = group.ModIdentifiers;

        foreach (var identifier in identifiers)
        {
            if (_loadedMods.TryGetValue(identifier.Id, out var loadedMod))
            {
                ResourceManager.OnModUnloaded(identifier.Id);
                loadedMod.Archive?.Dispose();
                _loadedMods.Remove(identifier.Id);
            }
        }

        if (group.LoadContextHandle.Target is SparkitectLoadContext loadContext)
        {
            loadContext.Unload();
        }
        group.LoadContextHandle.Free();

        // Notify ModDIService of unloaded mods (uses IDs)
        var modIds = identifiers.Select(i => i.Id).ToArray();
        ModDiService.UnregisterMods(modIds);

        Log.Information("Unloaded {ModCount} mods from group", identifiers.Length);
        return identifiers;
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

        using MemoryStream? pdbMemoryStream = pdbStream is not null ? new MemoryStream() : null;
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

            using MemoryStream? pdbMemoryStream = pdbStream is not null ? new MemoryStream() : null;
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
        public required ModFileIdentifier[] ModIdentifiers { get; init; }
    }

    private class LoadedMod
    {
        public required ModManifest Manifest { get; init; }
        public required Assembly Assembly { get; init; }
        public ZipArchive? Archive { get; init; } // Nullable as virtual mods don't have an archive
    }
}
