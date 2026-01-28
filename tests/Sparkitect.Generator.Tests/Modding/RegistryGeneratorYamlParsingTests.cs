using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Moq;
using Sparkitect.Generator.Modding;

namespace Sparkitect.Generator.Tests.Modding;

/// <summary>
/// Tests for RegistryGenerator.ParseResourceYaml edge cases.
/// These tests verify the YAML parser handles invalid input gracefully.
/// </summary>
public class RegistryGeneratorYamlParsingTests
{
    private static AdditionalText CreateMockAdditionalText(string path, string? content)
    {
        var mock = new Mock<AdditionalText>();
        mock.Setup(t => t.Path).Returns(path);
        mock.Setup(t => t.GetText(It.IsAny<CancellationToken>()))
            .Returns(content is null ? null : SourceText.From(content));
        return mock.Object;
    }

    [Test]
    public async Task ParseResourceYaml_EmptyFile_ReturnsEmptyArray(CancellationToken token)
    {
        // Arrange - empty content
        var text = CreateMockAdditionalText("test.sparkres.yaml", "");

        // Act
        var result = RegistryGenerator.ParseResourceYaml(text, token);

        // Assert - empty content returns empty array
        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ParseResourceYaml_NullContent_ReturnsEmptyArray(CancellationToken token)
    {
        // Arrange - null content (simulates file read failure)
        var text = CreateMockAdditionalText("test.sparkres.yaml", null);

        // Act
        var result = RegistryGenerator.ParseResourceYaml(text, token);

        // Assert - null content returns empty array
        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ParseResourceYaml_ValidYaml_SingleEntry_ReturnsParsedEntry(CancellationToken token)
    {
        // Arrange - valid single entry
        var yaml = """
            TestRegistry.RegisterFile:
              - my_item: "file.txt"
            """;
        var text = CreateMockAdditionalText("test.sparkres.yaml", yaml);

        // Act
        var result = RegistryGenerator.ParseResourceYaml(text, token);

        // Assert - should parse successfully
        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].Id).IsEqualTo("my_item");
        await Assert.That(result[0].RegistryClass).IsEqualTo("TestRegistry");
        await Assert.That(result[0].MethodName).IsEqualTo("RegisterFile");
    }

    [Test]
    public async Task ParseResourceYaml_ValidYaml_MultiFile_ReturnsParsedEntry(CancellationToken token)
    {
        // Arrange - valid multi-file entry
        var yaml = """
            TestRegistry.RegisterFile:
              - stone_block:
                  albedo: "stone.png"
                  normal: "stone_normal.png"
            """;
        var text = CreateMockAdditionalText("test.sparkres.yaml", yaml);

        // Act
        var result = RegistryGenerator.ParseResourceYaml(text, token);

        // Assert - should parse successfully with multiple files
        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].Id).IsEqualTo("stone_block");
        await Assert.That(result[0].Files.Count).IsEqualTo(2);
        await Assert.That(Enumerable.Any(result[0].Files, f => f.fileId == "albedo" && f.fileName == "stone.png")).IsTrue();
        await Assert.That(Enumerable.Any(result[0].Files, f => f.fileId == "normal" && f.fileName == "stone_normal.png")).IsTrue();
    }

    [Test]
    public async Task ParseResourceYaml_IdNotSnakeCase_SkipsEntry(CancellationToken token)
    {
        // Arrange - ID is PascalCase, not snake_case
        var yaml = """
            TestRegistry.RegisterFile:
              - MyItem: "file.txt"
            """;
        var text = CreateMockAdditionalText("test.sparkres.yaml", yaml);

        // Act
        var result = RegistryGenerator.ParseResourceYaml(text, token);

        // Assert - "MyItem" is not snake_case, should be skipped
        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ParseResourceYaml_IdWithHyphen_SkipsEntry(CancellationToken token)
    {
        // Arrange - ID uses kebab-case instead of snake_case
        var yaml = """
            TestRegistry.RegisterFile:
              - my-item: "file.txt"
            """;
        var text = CreateMockAdditionalText("test.sparkres.yaml", yaml);

        // Act
        var result = RegistryGenerator.ParseResourceYaml(text, token);

        // Assert - "my-item" is kebab-case, not snake_case, should be skipped
        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ParseResourceYaml_IdUpperCase_SkipsEntry(CancellationToken token)
    {
        // Arrange - ID is SCREAMING_SNAKE_CASE
        var yaml = """
            TestRegistry.RegisterFile:
              - MY_ITEM: "file.txt"
            """;
        var text = CreateMockAdditionalText("test.sparkres.yaml", yaml);

        // Act
        var result = RegistryGenerator.ParseResourceYaml(text, token);

        // Assert - "MY_ITEM" has uppercase, not valid snake_case, should be skipped
        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ParseResourceYaml_MissingMethodDelimiter_SkipsEntry(CancellationToken token)
    {
        // Arrange - registry key has no dot separator
        var yaml = """
            TestRegistryRegisterFile:
              - my_item: "file.txt"
            """;
        var text = CreateMockAdditionalText("test.sparkres.yaml", yaml);

        // Act
        var result = RegistryGenerator.ParseResourceYaml(text, token);

        // Assert - no dot in registry key, should be skipped
        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ParseResourceYaml_EmptyId_SkipsEntry(CancellationToken token)
    {
        // Arrange - empty string ID
        var yaml = """
            TestRegistry.RegisterFile:
              - "": "file.txt"
            """;
        var text = CreateMockAdditionalText("test.sparkres.yaml", yaml);

        // Act
        var result = RegistryGenerator.ParseResourceYaml(text, token);

        // Assert - empty ID should be skipped
        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ParseResourceYaml_ValidIdWithNumbers_ReturnsParsedEntry(CancellationToken token)
    {
        // Arrange - snake_case ID with numbers
        var yaml = """
            TestRegistry.RegisterFile:
              - block_type_1: "block1.txt"
            """;
        var text = CreateMockAdditionalText("test.sparkres.yaml", yaml);

        // Act
        var result = RegistryGenerator.ParseResourceYaml(text, token);

        // Assert - "block_type_1" is valid snake_case, should parse
        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].Id).IsEqualTo("block_type_1");
    }

    [Test]
    public async Task ParseResourceYaml_SimpleIdNoUnderscores_ReturnsParsedEntry(CancellationToken token)
    {
        // Arrange - simple lowercase ID without underscores
        var yaml = """
            TestRegistry.RegisterFile:
              - stone: "stone.txt"
            """;
        var text = CreateMockAdditionalText("test.sparkres.yaml", yaml);

        // Act
        var result = RegistryGenerator.ParseResourceYaml(text, token);

        // Assert - "stone" is valid snake_case (no underscores required), should parse
        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].Id).IsEqualTo("stone");
    }

    [Test]
    public async Task ParseResourceYaml_MultipleEntries_ParsesAllValid(CancellationToken token)
    {
        // Arrange - mix of valid and invalid entries
        var yaml = """
            TestRegistry.RegisterFile:
              - valid_item: "valid.txt"
              - InvalidItem: "invalid.txt"
              - another_valid: "another.txt"
            """;
        var text = CreateMockAdditionalText("test.sparkres.yaml", yaml);

        // Act
        var result = RegistryGenerator.ParseResourceYaml(text, token);

        // Assert - should only parse valid snake_case entries
        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(Enumerable.Any(result, e => e.Id == "valid_item")).IsTrue();
        await Assert.That(Enumerable.Any(result, e => e.Id == "another_valid")).IsTrue();
    }
}
