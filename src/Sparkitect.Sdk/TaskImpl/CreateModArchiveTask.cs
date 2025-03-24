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
    /// Additional files to include in the archive (semicolon-separated list of archive_path=file_path entries)
    /// </summary>
    public string? AdditionalFiles { get; set; }

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
            string assemblyFileName = Path.GetFileName(AssemblyPath);
            AddFileToArchive(archive, AssemblyPath, assemblyFileName);

            // Add PDB if it exists
            string pdbPath = Path.ChangeExtension(AssemblyPath, ".pdb");
            if (File.Exists(pdbPath))
            {
                string pdbFileName = Path.GetFileName(pdbPath);
                AddFileToArchive(archive, pdbPath, pdbFileName);
            }

            // Add additional files
            if (!string.IsNullOrWhiteSpace(AdditionalFiles))
            {
                var additionalFileEntries = AdditionalFiles.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var entry in additionalFileEntries)
                {
                    var parts = entry.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        string archivePath = parts[0].Trim();
                        string filePath = parts[1].Trim();
                        
                        if (File.Exists(filePath))
                        {
                            AddFileToArchive(archive, filePath, archivePath);
                        }
                        else
                        {
                            Log.LogWarning($"Additional file not found: {filePath}");
                        }
                    }
                }
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