using System.Threading.Tasks;

namespace Sparkitect.Generator.Tests;

public sealed class ModBuildSettingsTests
{
    private static ModBuildSettings CreateSettings(string sgOutputNamespace = "MyMod.CompilerGenerated")
        => new("TestMod", "test", "TestMod", false, sgOutputNamespace);

    [Test]
    public async Task ComputeOutputNamespace_NullSuffix_ReturnsSgOutputNamespace()
    {
        var settings = CreateSettings();

        var result = settings.ComputeOutputNamespace(null);

        await Assert.That(result).IsEqualTo("MyMod.CompilerGenerated");
    }

    [Test]
    public async Task ComputeOutputNamespace_NoArgument_ReturnsSgOutputNamespace()
    {
        var settings = CreateSettings();

        var result = settings.ComputeOutputNamespace();

        await Assert.That(result).IsEqualTo("MyMod.CompilerGenerated");
    }

    [Test]
    public async Task ComputeOutputNamespace_WithSuffix_ReturnsSgOutputNamespaceDotSuffix()
    {
        var settings = CreateSettings();

        var result = settings.ComputeOutputNamespace("Registrations");

        await Assert.That(result).IsEqualTo("MyMod.CompilerGenerated.Registrations");
    }

    [Test]
    public async Task ComputeOutputNamespace_WithNestedSuffix_ReturnsSgOutputNamespaceDotNestedSuffix()
    {
        var settings = CreateSettings();

        var result = settings.ComputeOutputNamespace("CompilerGenerated.DI");

        await Assert.That(result).IsEqualTo("MyMod.CompilerGenerated.CompilerGenerated.DI");
    }

    [Test]
    public async Task ComputeOutputNamespace_EmptyStringSuffix_ReturnsSgOutputNamespace()
    {
        var settings = CreateSettings();

        var result = settings.ComputeOutputNamespace("");

        await Assert.That(result).IsEqualTo("MyMod.CompilerGenerated");
    }
}
