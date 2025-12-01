using System.IO.Compression;
using Microsoft.Build.Framework;

namespace Sparkitect.Sdk.TaskImpl;

/// <summary>
/// MSBuild task that creates a mod archive (.sparkmod)
/// </summary>
public class CreateModArchive : Microsoft.Build.Utilities.Task
{
    /// <summary>
    /// The output path for the archive
    /// </summary>
    [Required]
    public string OutputArchivePath { get; set; } = string.Empty;

    /// <summary>
    /// The path to the manifest file
    /// </summary>
    [Required]
    public string ManifestPath { get; set; } = string.Empty;

    /// <summary>
    /// The path to the mod's main assembly
    /// </summary>
    [Required]
    public string AssemblyPath { get; set; } = string.Empty;

    /// <summary>
    /// Directory containing resource files to include in the archive under resources/{subdir}/{file}
    /// </summary>
    public string? ResourceDirectory { get; set; }

    /// <summary>
    /// List of required assemblies (semicolon-separated)
    /// </summary>
    public string RequiredAssemblies { get; set; } = string.Empty;

    /// <summary>
    /// Executes the task to create the archive
    /// </summary>
    public override bool Execute()
    {
        try
        {
            Log.LogMessage(MessageImportance.Normal, "Creating mod archive...");

            // Create output directory if needed
            var outputDir = Path.GetDirectoryName(OutputArchivePath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // Delete existing archive if it exists
            if (File.Exists(OutputArchivePath))
            {
                File.Delete(OutputArchivePath);
            }

            // Create archive
            using var archive = ZipFile.Open(OutputArchivePath, ZipArchiveMode.Create);


            // Add manifest
            AddFileToArchive(archive, ManifestPath, "manifest.json");

            // Add main assembly
            var assemblyFileName = Path.GetFileName(AssemblyPath);
            AddFileToArchive(archive, AssemblyPath, assemblyFileName);

            // Add PDB if it exists
            var pdbPath = Path.ChangeExtension(AssemblyPath, ".pdb");
            if (File.Exists(pdbPath))
            {
                var pdbFileName = Path.GetFileName(pdbPath);
                AddFileToArchive(archive, pdbPath, pdbFileName);
            }

            // Add required assemblies
            if (!string.IsNullOrEmpty(RequiredAssemblies))
            {
                PackDependencies(archive);
            }

            // Add resource files
            if (!string.IsNullOrWhiteSpace(ResourceDirectory) && Directory.Exists(ResourceDirectory))
            {
                PackResourceDirectory(archive);
            }

            Log.LogMessage(MessageImportance.High, $"Created mod archive at: {OutputArchivePath}");
            return true;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, showStackTrace: true);
            return false;
        }
    }

    private void PackResourceDirectory(ZipArchive archive)
    {
        var resourceDir = Path.GetFullPath(ResourceDirectory!);
        var files = Directory.GetFiles(resourceDir, "*", SearchOption.AllDirectories);

        foreach (var filePath in files)
        {
            var relativePath = Path.GetRelativePath(resourceDir, filePath);
            var archivePath = $"resources/{relativePath.Replace('\\', '/')}";

            AddFileToArchive(archive, filePath, archivePath);
            Log.LogMessage(MessageImportance.Normal, $"Packed resource: {archivePath}");
        }
    }

    private void PackDependencies(ZipArchive archive)
    {
        var requiredAssemblies = RequiredAssemblies.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var assembly in requiredAssemblies)
        {
            var assemblyFileName = Path.GetFileName(assembly);
            var assemblyPath = Path.Combine(Path.GetDirectoryName(AssemblyPath)!, assemblyFileName);

            if (File.Exists(assemblyPath))
            {
                // Create the lib directory in the archive
                AddFileToArchive(archive, assemblyPath, $"lib/{assemblyFileName}");

                // Add PDB if it exists
                var reqPdbPath = Path.ChangeExtension(assemblyPath, ".pdb");
                if (!File.Exists(reqPdbPath)) continue;

                var pdbFileName = Path.GetFileName(reqPdbPath);
                AddFileToArchive(archive, reqPdbPath, $"lib/{pdbFileName}");
            }
            else
            {
                Log.LogWarning($"Required assembly not found: {assemblyPath}");
            }
        }
    }

    private void AddFileToArchive(ZipArchive archive, string sourcePath, string entryName)
    {
        if (!File.Exists(sourcePath))
        {
            Log.LogError($"File not found: {sourcePath}");
            throw new FileNotFoundException($"The file {sourcePath} could not be found.");
        }

        Log.LogMessage(MessageImportance.Low, $"Adding {entryName} to archive");
        var entry = archive.CreateEntry(entryName, CompressionLevel.NoCompression);
        using var entryStream = entry.Open();
        using var fileStream = File.OpenRead(sourcePath);
        fileStream.CopyTo(entryStream);
    }
}