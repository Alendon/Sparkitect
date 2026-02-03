using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Build.Framework;

namespace Sparkitect.Sdk.TaskImpl;

/// <summary>
/// MSBuild task that parses dependency files to find direct dependencies
/// </summary>
public class ParseDependencyFile : Microsoft.Build.Utilities.Task
{
    /// <summary>
    /// Path to the dependency file (.deps.json)
    /// </summary>
    [Required]
    public string DependencyFilePath { get; set; } = string.Empty;

    [Required] public string AssemblyName { get; set; } = string.Empty;


    /// <summary>
    /// Target framework (e.g. net9.0)
    /// </summary>
    [Required]
    public string TargetFramework { get; set; } = string.Empty;

    /// <summary>
    /// Project output directory
    /// </summary>
    [Required]
    public string OutputDirectory { get; set; } = string.Empty;

    [Required] public string AssemblyVersion { get; set; } = string.Empty;

    /// <summary>
    /// ModProjectDependency items from the project file.
    /// Used to determine which dependencies are mods (should not pack their DLLs).
    /// </summary>
    public ITaskItem[] ModProjectDependencies { get; set; } = Array.Empty<ITaskItem>();

    /// <summary>
    /// The project directory for resolving relative paths in ModProjectDependency items.
    /// </summary>
    public string ProjectDirectory { get; set; } = "";

    /// <summary>
    /// Semicolon separated list of detected direct dependencies
    /// </summary>
    [Output]
    public string DirectDependencies { get; set; } = string.Empty;

    /// <summary>
    /// Executes the task to parse dependency files
    /// </summary>
    public override bool Execute()
    {
        try
        {
            Log.LogMessage(MessageImportance.Normal, "Parsing dependency file...");

            if (!File.Exists(DependencyFilePath))
            {
                Log.LogError($"Dependency file not found: {DependencyFilePath}");
                return false;
            }

            // Read and parse the json file
            string dependencyJson = File.ReadAllText(DependencyFilePath);
            using JsonDocument document = JsonDocument.Parse(dependencyJson);


            if (!document.RootElement.TryGetProperty("targets", out var targets) ||
                !targets.TryGetProperty($".NETCoreApp,Version=v{TargetFramework.Replace("net", "")}",
                    out var frameworkTargets) ||
                !frameworkTargets.TryGetProperty($"{AssemblyName}/{AssemblyVersion}", out var projectInfo) ||
                !projectInfo.TryGetProperty("dependencies", out var projectDependencies))
            {
                Log.LogError(
                    $"Malformed dependency file: missing targets or dependencies section. ({AssemblyName}/{AssemblyVersion})");
                return false;
            }


            // Build sets of mod assembly names from ModProjectDependency items
            // These are mods, so we skip packing their DLLs (the mod brings its own)
            // We use assembly names (not ModId) because deps.json uses assembly names
            var modAssemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var optionalModAssemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in ModProjectDependencies)
            {
                var projectPath = item.ItemSpec;
                var absoluteProjectPath = Path.IsPathRooted(projectPath)
                    ? projectPath
                    : Path.GetFullPath(Path.Combine(ProjectDirectory, projectPath));

                if (!File.Exists(absoluteProjectPath))
                {
                    Log.LogWarning($"ModProjectDependency '{projectPath}' not found at '{absoluteProjectPath}', skipping");
                    continue;
                }

                try
                {
                    var csprojDoc = XDocument.Load(absoluteProjectPath);

                    // Extract assembly name - use explicit AssemblyName or derive from project file name
                    var assemblyName = csprojDoc.Descendants("AssemblyName").FirstOrDefault()?.Value
                                    ?? Path.GetFileNameWithoutExtension(absoluteProjectPath);

                    // Also get ModId for logging
                    var modId = csprojDoc.Descendants("ModId").FirstOrDefault()?.Value
                             ?? csprojDoc.Descendants("ModIdentifier").FirstOrDefault()?.Value;

                    if (!string.IsNullOrEmpty(assemblyName))
                    {
                        modAssemblyNames.Add(assemblyName);
                        Log.LogMessage(MessageImportance.Normal, $"Detected mod dependency: {modId} (assembly: {assemblyName})");

                        // Track if this mod dependency is optional
                        if (string.Equals(item.GetMetadata("IsOptional"), "true", StringComparison.OrdinalIgnoreCase))
                        {
                            optionalModAssemblyNames.Add(assemblyName);
                            Log.LogMessage(MessageImportance.Normal, $"  Mod '{modId}' is optional - will include its transitive deps");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.LogWarning($"Failed to read ModProjectDependency '{projectPath}': {ex.Message}");
                }
            }

            List<(string name, string version, bool excludeFromArchive, bool traverseChildren)> directDependencies = [];

            foreach (var (name, version) in ParseDependencies(projectDependencies))
            {
                // Matching by assembly name since deps.json uses assembly names
                var isMod = modAssemblyNames.Contains(name);
                var isOptionalMod = isMod && optionalModAssemblyNames.Contains(name);

                // ALL mod DLLs are excluded from the archive (mods bring their own)
                var excludeFromArchive = isMod;

                // For required mods: don't traverse children (mod brings everything)
                // For optional mods: DO traverse children (mod might not load, need fallback deps)
                // For non-mods: DO traverse children (pack all transitive deps)
                var traverseChildren = !isMod || isOptionalMod;

                if (isMod)
                {
                    Log.LogMessage(MessageImportance.Normal, $"Mod '{name}' will be excluded from archive");
                    if (isOptionalMod)
                        Log.LogMessage(MessageImportance.Normal, $"  (optional - will include its non-mod transitive deps)");
                    else
                        Log.LogMessage(MessageImportance.Normal, $"  (required - will also exclude its transitive deps)");
                }

                directDependencies.Add((name, version, excludeFromArchive, traverseChildren));
            }

            HashSet<(string name, string version)> visited =
                new(directDependencies.Select(x => (x.name, Version: AssemblyVersion)));
            HashSet<(string name, string version)> toInclude = [];
            HashSet<(string name, string version)> assumeIncluded = [];

            Queue<(string name, string version, bool excludeFromArchive, bool traverseChildren)> toProcess = new(directDependencies);

            while (toProcess.TryDequeue(out var dependency))
            {
                // Handle the current dependency's inclusion/exclusion
                if (dependency.excludeFromArchive)
                {
                    assumeIncluded.Add((dependency.name, dependency.version));
                    toInclude.Remove((dependency.name, dependency.version));
                }
                else if (!assumeIncluded.Contains((dependency.name, dependency.version)))
                {
                    toInclude.Add((dependency.name, dependency.version));
                }

                // Only traverse children if flagged to do so
                if (!dependency.traverseChildren)
                    continue;

                if (!frameworkTargets.TryGetProperty($"{dependency.name}/{dependency.version}", out projectInfo))
                {
                    Log.LogError(
                        $"Malformed dependency file: missing targets entry. ({dependency.name}/{dependency.version})");
                    return false;
                }

                if (!projectInfo.TryGetProperty("dependencies", out projectDependencies))
                {
                    Log.LogMessage($"No additional dependencies for {dependency.name} found.");
                    continue;
                }

                foreach (var (name, version) in ParseDependencies(projectDependencies))
                {
                    if (!visited.Add((name, version))) continue;

                    // Key insight: When traversing from optional mod, children that are ALSO mods
                    // must be excluded, but we continue traversing their children too
                    var childIsMod = modAssemblyNames.Contains(name);
                    var childIsOptionalMod = childIsMod && optionalModAssemblyNames.Contains(name);

                    // Child mod DLLs are always excluded
                    var childExcludeFromArchive = childIsMod;
                    // Continue traversing if: not a required mod
                    var childTraverseChildren = !childIsMod || childIsOptionalMod;

                    toProcess.Enqueue((name, version, childExcludeFromArchive, childTraverseChildren));
                }
            }


            Log.LogMessage(MessageImportance.Normal,
                $"Found {toInclude.Count} dependencies to include in mod archive: {string.Join(", ", toInclude)}");


            // Step 2: Find the corresponding DLLs for direct dependencies

            List<string> dependencyFiles = [];
            foreach (var includeAssembly in toInclude)
            {
                if (!frameworkTargets.TryGetProperty($"{includeAssembly.name}/{includeAssembly.version}",
                        out var includeTargetEntry)
                    || !includeTargetEntry.TryGetProperty("runtime", out var runtimeTargets))
                {
                    Log.LogError($"Dependency {includeAssembly.name} not found in dependency file.");
                    continue;
                }

                if (runtimeTargets.GetPropertyCount() != 1)
                {
                    Log.LogError(
                        $"Dependency {includeAssembly.name} has multiple runtime targets. Currently not supported.");
                    continue;
                }

                var localPath = runtimeTargets.EnumerateObject().Single().Name;
                var dllName = Path.GetFileName(localPath);
                var dllPath = Path.Combine(OutputDirectory, dllName);

                if (!File.Exists(dllPath))
                {
                    Log.LogWarning($"Dependency {includeAssembly.name} not found in output directory: {dllPath}");
                    continue;
                }

                dependencyFiles.Add(dllPath);
            }


            DirectDependencies = string.Join(";", dependencyFiles);

            Log.LogMessage(MessageImportance.Normal,
                $"Detected {dependencyFiles.Count} direct and transitive dependencies: {DirectDependencies}");
            return true;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, showStackTrace: true);
            return false;
        }
    }

    private IEnumerable<(string name, string version)> ParseDependencies(JsonElement dependencies)
    {
        foreach (var dependencyEntry in dependencies.EnumerateObject())
        {
            if (dependencyEntry.Value.ValueKind != JsonValueKind.String)
            {
                Log.LogError($"Invalid dependency entry: {dependencyEntry.Name}. Expected a string value.");
                continue;
            }

            string dependencyName = dependencyEntry.Name;
            string? dependencyVersion = dependencyEntry.Value.GetString();

            if (dependencyVersion is null)
            {
                Log.LogError($"Invalid dependency version for {dependencyName}. Expected a string value.");
                continue;
            }

            yield return (dependencyName, dependencyVersion);
        }
    }
}