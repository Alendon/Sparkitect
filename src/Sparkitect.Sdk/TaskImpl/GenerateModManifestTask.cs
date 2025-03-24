using System.Text.Json;
using Microsoft.Build.Framework;
using Semver;
using Sparkitect.Sdk.Tasks.Models;

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
    public string ModIdentifier { get; set; } = string.Empty;
    
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
            string[] authorsList = ModAuthor.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.Trim())
                .ToArray();

            // Get assembly name
            string assemblyFileName = Path.GetFileName(AssemblyPath);

            // Create the manifest model
            var manifest = new ModManifestModel(
                ModIdentifier,
                ModName,
                ModDescription,
                semVersion,
                authorsList,
                Array.Empty<ModRelationshipModel>(), // Empty relationships for now
                assemblyFileName
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