using Semver;
using Sparkitect.Modding;

namespace Sparkitect.Tests.Modding;

public class RootModConfigurationTests
{
    private string _tempDir = null!;

    [Before(Test)]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"RootModConfigTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [After(Test)]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    #region LoadConfig Tests

    [Test]
    public async Task LoadConfig_FileDoesNotExist_ReturnsNull()
    {
        // Arrange
        var path = Path.Combine(_tempDir, "nonexistent.json");

        // Act
        var result = RootModConfiguration.LoadConfig(path);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task LoadConfig_ValidJsonWithVersions_ReturnsConfig()
    {
        // Arrange
        var path = Path.Combine(_tempDir, "roots.json");
        const string json = """
        {
            "rootMods": [
                { "id": "my_game_mod", "version": "1.0.0" },
                { "id": "base_content", "version": "2.1.0" }
            ]
        }
        """;
        File.WriteAllText(path, json);

        // Act
        var result = RootModConfiguration.LoadConfig(path);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.RootMods).HasCount().EqualTo(2);

        await Assert.That(result.RootMods[0].Id).IsEqualTo("my_game_mod");
        await Assert.That(result.RootMods[0].Version).IsNotNull();
        await Assert.That(result.RootMods[0].Version!.Major).IsEqualTo(1);

        await Assert.That(result.RootMods[1].Id).IsEqualTo("base_content");
        await Assert.That(result.RootMods[1].Version).IsNotNull();
        await Assert.That(result.RootMods[1].Version!.Major).IsEqualTo(2);
    }

    [Test]
    public async Task LoadConfig_ValidJsonWithoutVersions_ReturnsConfigWithNullVersions()
    {
        // Arrange
        var path = Path.Combine(_tempDir, "roots.json");
        const string json = """
        {
            "rootMods": [
                { "id": "my_game_mod" },
                { "id": "base_content" }
            ]
        }
        """;
        File.WriteAllText(path, json);

        // Act
        var result = RootModConfiguration.LoadConfig(path);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.RootMods).HasCount().EqualTo(2);

        await Assert.That(result.RootMods[0].Id).IsEqualTo("my_game_mod");
        await Assert.That(result.RootMods[0].Version).IsNull();

        await Assert.That(result.RootMods[1].Id).IsEqualTo("base_content");
        await Assert.That(result.RootMods[1].Version).IsNull();
    }

    [Test]
    public async Task LoadConfig_MixedVersionsAndNoVersions_ReturnsCorrectConfig()
    {
        // Arrange
        var path = Path.Combine(_tempDir, "roots.json");
        const string json = """
        {
            "rootMods": [
                { "id": "specific_mod", "version": "1.2.3" },
                { "id": "latest_mod" }
            ]
        }
        """;
        File.WriteAllText(path, json);

        // Act
        var result = RootModConfiguration.LoadConfig(path);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.RootMods[0].Version).IsNotNull();
        await Assert.That(result.RootMods[1].Version).IsNull();
    }

    [Test]
    public async Task LoadConfig_EmptyRootMods_ReturnsConfigWithEmptyList()
    {
        // Arrange
        var path = Path.Combine(_tempDir, "roots.json");
        const string json = """
        {
            "rootMods": []
        }
        """;
        File.WriteAllText(path, json);

        // Act
        var result = RootModConfiguration.LoadConfig(path);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.RootMods).HasCount().EqualTo(0);
    }

    [Test]
    public async Task LoadConfig_CaseInsensitivePropertyNames_Works()
    {
        // Arrange
        var path = Path.Combine(_tempDir, "roots.json");
        const string json = """
        {
            "RootMods": [
                { "Id": "my_mod", "Version": "1.0.0" }
            ]
        }
        """;
        File.WriteAllText(path, json);

        // Act
        var result = RootModConfiguration.LoadConfig(path);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.RootMods[0].Id).IsEqualTo("my_mod");
    }

    [Test]
    public async Task LoadConfig_PrereleaseVersion_ParsesCorrectly()
    {
        // Arrange
        var path = Path.Combine(_tempDir, "roots.json");
        const string json = """
        {
            "rootMods": [
                { "id": "beta_mod", "version": "2.0.0-alpha.1" }
            ]
        }
        """;
        File.WriteAllText(path, json);

        // Act
        var result = RootModConfiguration.LoadConfig(path);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.RootMods[0].Version!.ToString()).IsEqualTo("2.0.0-alpha.1");
    }

    #endregion

    #region RootModEntry Tests

    [Test]
    public async Task RootModEntry_DefaultVersion_IsNull()
    {
        // Act
        var entry = new RootModEntry("test_mod");

        // Assert
        await Assert.That(entry.Id).IsEqualTo("test_mod");
        await Assert.That(entry.Version).IsNull();
    }

    [Test]
    public async Task RootModEntry_WithVersion_HasVersion()
    {
        // Arrange
        var version = SemVersion.Parse("1.2.3", SemVersionStyles.Any);

        // Act
        var entry = new RootModEntry("test_mod", version);

        // Assert
        await Assert.That(entry.Id).IsEqualTo("test_mod");
        await Assert.That(entry.Version).IsEqualTo(version);
    }

    #endregion
}
