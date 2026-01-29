using System.Text.Json;
using Semver;
using Sparkitect.Modding;
using Sparkitect.Utils;

namespace Sparkitect.Tests.Modding;

public class ModManifestTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new SemVersionJsonConverter(),
            new SemVersionRangeJsonConverter()
        }
    };

    #region IsRootMod Tests

    [Test]
    public async Task ModManifest_DefaultIsRootMod_IsFalse()
    {
        // Arrange & Act
        var manifest = new ModManifest(
            Id: "test_mod",
            Name: "Test Mod",
            Description: "A test mod",
            Version: SemVersion.Parse("1.0.0", SemVersionStyles.Any),
            Authors: ["Test Author"],
            ModPath: null,
            Relationships: [],
            ModAssembly: "TestMod.dll",
            RequiredAssemblies: []);

        // Assert
        await Assert.That(manifest.IsRootMod).IsFalse();
    }

    [Test]
    public async Task ModManifest_WithIsRootModTrue_ReturnsTrue()
    {
        // Arrange & Act
        var manifest = new ModManifest(
            Id: "test_mod",
            Name: "Test Mod",
            Description: "A test mod",
            Version: SemVersion.Parse("1.0.0", SemVersionStyles.Any),
            Authors: ["Test Author"],
            ModPath: null,
            Relationships: [],
            ModAssembly: "TestMod.dll",
            RequiredAssemblies: [],
            IsRootMod: true);

        // Assert
        await Assert.That(manifest.IsRootMod).IsTrue();
    }

    [Test]
    public async Task ModManifest_JsonDeserialize_WithIsRootMod_ParsesCorrectly()
    {
        // Arrange
        const string json = """
        {
            "id": "my_mod",
            "name": "My Mod",
            "description": "A mod",
            "version": "1.0.0",
            "authors": ["Author"],
            "relationships": [],
            "modAssembly": "MyMod.dll",
            "requiredAssemblies": [],
            "isRootMod": true
        }
        """;

        // Act
        var manifest = JsonSerializer.Deserialize<ModManifest>(json, JsonOptions);

        // Assert
        await Assert.That(manifest).IsNotNull();
        await Assert.That(manifest!.IsRootMod).IsTrue();
    }

    [Test]
    public async Task ModManifest_JsonDeserialize_WithoutIsRootMod_DefaultsToFalse()
    {
        // Arrange
        const string json = """
        {
            "id": "my_mod",
            "name": "My Mod",
            "description": "A mod",
            "version": "1.0.0",
            "authors": ["Author"],
            "relationships": [],
            "modAssembly": "MyMod.dll",
            "requiredAssemblies": []
        }
        """;

        // Act
        var manifest = JsonSerializer.Deserialize<ModManifest>(json, JsonOptions);

        // Assert
        await Assert.That(manifest).IsNotNull();
        await Assert.That(manifest!.IsRootMod).IsFalse();
    }

    [Test]
    public async Task ModManifest_JsonSerialize_IncludesIsRootMod()
    {
        // Arrange
        var manifest = new ModManifest(
            Id: "test_mod",
            Name: "Test Mod",
            Description: "A test mod",
            Version: SemVersion.Parse("1.0.0", SemVersionStyles.Any),
            Authors: ["Test Author"],
            ModPath: null,
            Relationships: [],
            ModAssembly: "TestMod.dll",
            RequiredAssemblies: [],
            IsRootMod: true);

        // Act
        var json = JsonSerializer.Serialize(manifest, JsonOptions);

        // Assert
        await Assert.That(json).Contains("\"IsRootMod\":true");
    }

    #endregion
}
