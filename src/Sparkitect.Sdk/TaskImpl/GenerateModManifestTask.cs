using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Semver;
using Sparkitect.Sdk.TaskImpl.Models;

namespace Sparkitect.Sdk.TaskImpl;

/// <summary>
/// MSBuild task that generates a manifest.json file for a mod
/// </summary>
public class GenerateModManifest : Microsoft.Build.Utilities.Task
{
    /// <summary>
    /// The unique identifier for the mod
    /// </summary>
    [Required]
    public string ModId { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the mod
    /// </summary>
    [Required]
    public string ModName { get; set; } = string.Empty;

    /// <summary>
    /// The description of the mod
    /// </summary>
    [Required]
    public string ModDescription { get; set; } = string.Empty;

    /// <summary>
    /// The version of the mod
    /// </summary>
    [Required]
    public string ModVersion { get; set; } = string.Empty;

    /// <summary>
    /// The authors of the mod (semicolon-separated)
    /// </summary>
    [Required]
    public string ModAuthor { get; set; } = string.Empty;

    /// <summary>
    /// The path to the mod's main assembly
    /// </summary>
    [Required]
    public string AssemblyPath { get; set; } = string.Empty;

    /// <summary>
    /// The output path for the manifest file
    /// </summary>
    [Required]
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>
    /// List of required assemblies (semicolon-separated)
    /// </summary>
    public string RequiredAssemblies { get; set; } = string.Empty;

    /// <summary>
    /// ModProjectDependency items from the project file.
    /// Each item's ItemSpec is the relative path to the referenced .csproj file.
    /// </summary>
    public ITaskItem[] ModProjectDependencies { get; set; } = Array.Empty<ITaskItem>();

    // TODO: ModDependency (package-style) processing deferred to future phase.
    // When implemented, add: public ITaskItem[] ModDependencies { get; set; } = Array.Empty<ITaskItem>();

    /// <summary>
    /// Whether this mod can be loaded as a root mod by the bootstrapper.
    /// </summary>
    public bool IsRootMod { get; set; } = false;

    /// <summary>
    /// The build configuration (Debug/Release) for finding referenced manifest files.
    /// </summary>
    public string Configuration { get; set; } = "Debug";

    /// <summary>
    /// The target framework for finding referenced manifest files.
    /// </summary>
    public string TargetFramework { get; set; } = "";

    /// <summary>
    /// The project directory for resolving relative paths in ModProjectDependency items.
    /// </summary>
    public string ProjectDirectory { get; set; } = "";

    /// <summary>
    /// Executes the task to generate the manifest
    /// </summary>
    public override bool Execute()
    {
        try
        {
            Log.LogMessage(MessageImportance.Normal, "Generating mod manifest...");

            // Parse version to SemVersion
            if (!SemVersion.TryParse(ModVersion, SemVersionStyles.Any, out var semVersion))
            {
                Log.LogError($"Invalid version format: {ModVersion}. Must be a valid semantic version.");
                return false;
            }

            // Split authors string
            string[] authorsList = ModAuthor.Split([';', ','], StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.Trim())
                .ToArray();

            // Get assembly name
            string assemblyFileName = Path.GetFileName(AssemblyPath);

            // Parse required assemblies
            string[] requiredAssembliesList = string.IsNullOrEmpty(RequiredAssemblies)
                ? []
                : RequiredAssemblies.Split([';'], StringSplitOptions.RemoveEmptyEntries)
                    .Select(a => Path.GetFileName(a.Trim()))
                    .ToArray();

            // Process ModProjectDependency items to build relationships list
            // Read mod metadata directly from referenced .csproj files (no build order dependency)
            var relationships = new List<ModRelationshipModel>();
            foreach (var item in ModProjectDependencies)
            {
                var projectPath = item.ItemSpec;
                var isOptional = string.Equals(item.GetMetadata("IsOptional"), "true", StringComparison.OrdinalIgnoreCase);

                // Resolve the path relative to ProjectDirectory
                var absoluteProjectPath = Path.IsPathRooted(projectPath)
                    ? projectPath
                    : Path.GetFullPath(Path.Combine(ProjectDirectory, projectPath));

                if (!File.Exists(absoluteProjectPath))
                {
                    Log.LogError($"Could not find ModProjectDependency '{projectPath}' at '{absoluteProjectPath}'");
                    continue;
                }

                // Read mod metadata directly from the .csproj file
                string? referencedModId;
                string? referencedModVersion;
                XDocument csprojDoc;
                try
                {
                    csprojDoc = XDocument.Load(absoluteProjectPath);
                    // Try ModId first, fall back to ModIdentifier for compatibility
                    referencedModId = csprojDoc.Descendants("ModId").FirstOrDefault()?.Value
                                   ?? csprojDoc.Descendants("ModIdentifier").FirstOrDefault()?.Value;
                    referencedModVersion = csprojDoc.Descendants("ModVersion").FirstOrDefault()?.Value;
                }
                catch (Exception ex)
                {
                    Log.LogError($"Failed to parse .csproj for ModProjectDependency '{projectPath}': {ex.Message}");
                    continue;
                }

                if (string.IsNullOrEmpty(referencedModId))
                {
                    Log.LogError($"ModProjectDependency '{projectPath}' does not have a ModId property. " +
                                 "Ensure the referenced project is configured as a mod project.");
                    continue;
                }

                // Validate no self-reference
                if (string.Equals(referencedModId, ModId, StringComparison.OrdinalIgnoreCase))
                {
                    Log.LogError($"Mod '{ModId}' cannot depend on itself via ModProjectDependency");
                    continue;
                }

                // Determine version range: explicit VersionRange wins, else infer from ModVersion, else *
                var versionRangeStr = item.GetMetadata("VersionRange");
                SemVersionRange versionRange;
                if (!string.IsNullOrEmpty(versionRangeStr))
                {
                    // Explicit VersionRange wins
                    if (!SemVersionRange.TryParse(versionRangeStr, out versionRange!))
                    {
                        Log.LogError($"Invalid VersionRange '{versionRangeStr}' for ModProjectDependency '{projectPath}'");
                        continue;
                    }
                }
                else if (!string.IsNullOrEmpty(referencedModVersion) &&
                         SemVersion.TryParse(referencedModVersion, SemVersionStyles.Any, out var refVersion))
                {
                    // Infer ^x.y.z from referenced ModVersion
                    versionRange = SemVersionRange.Parse($"^{refVersion}");
                    Log.LogMessage(MessageImportance.Normal,
                        $"Inferred VersionRange '^{refVersion}' from ModProjectDependency '{projectPath}'");
                }
                else
                {
                    // Fall back to *
                    versionRange = SemVersionRange.All;
                    Log.LogMessage(MessageImportance.Low,
                        $"Using VersionRange '*' for ModProjectDependency '{projectPath}' (no ModVersion found)");
                }

                relationships.Add(new ModRelationshipModel(referencedModId, versionRange, IsOptional: isOptional));

                Log.LogMessage(MessageImportance.Normal, $"Resolved ModProjectDependency '{projectPath}' to mod '{referencedModId}'");
            }

            // Create the manifest model
            var manifest = new ModManifestModel(
                ModId,
                ModName,
                ModDescription,
                semVersion,
                authorsList,
                relationships,
                assemblyFileName,
                requiredAssembliesList,
                IsRootMod
            );

            // Serialize to JSON
            string manifestContent = JsonSerializer.Serialize(manifest);

            // Create output directory if needed
            var outputDir = Path.GetDirectoryName(OutputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // Write manifest to file
            File.WriteAllText(OutputPath, manifestContent);

            Log.LogMessage(MessageImportance.High, $"Generated mod manifest at: {OutputPath}");
            return true;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, showStackTrace: true);
            return false;
        }
    }
}