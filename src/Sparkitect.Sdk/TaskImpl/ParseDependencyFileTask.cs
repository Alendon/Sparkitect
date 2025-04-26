using System.Net;
using System.Text.Json;
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


            List<(string name, string version, bool packRecursive)> directDependencies = [];

            foreach (var (name, version) in ParseDependencies(projectDependencies))
            {
                //TODO robust approach including mod relationships 
                var pack = name != "Sparkitect";

                directDependencies.Add((name, version, pack));
            }

            HashSet<(string name, string version)> visited =
                new(directDependencies.Select(x => (x.name, Version: AssemblyVersion)));
            HashSet<(string name, string version)> toInclude = [];
            HashSet<(string name, string version)> assumeIncluded = [];

            Queue<(string name, string version, bool packRecursive)> toProcess = new(directDependencies);

            while (toProcess.TryDequeue(out var dependency))
            {
                switch (dependency.packRecursive)
                {
                    case false:
                        assumeIncluded.Add((dependency.name, dependency.version));
                        toInclude.Remove((dependency.name, dependency.version));
                        break;
                    case true when !assumeIncluded.Contains((dependency.name, dependency.version)):
                        toInclude.Add((dependency.name, dependency.version));
                        break;
                }

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

                    directDependencies.Add((name, version, dependency.packRecursive));
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