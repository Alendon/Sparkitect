using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Sparkitect.Generator.OptionalDependency;

namespace Sparkitect.Generator.Tests.OptionalDependency;

public class OptionalDependencyAnalyzerTests : AnalyzerTestBase<OptionalDependencyAnalyzer>
{
    /// <summary>
    /// Inline definition of the Sparkitect.Modding attributes for test compilation.
    /// </summary>
    private static readonly (string, object) ModdingAttributes = ("ModdingAttributes.cs", """
        namespace Sparkitect.Modding
        {
            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
            public sealed class OptionalModDependentAttribute : System.Attribute
            {
                public string ModId { get; }
                public OptionalModDependentAttribute(string modId) => ModId = modId;
            }

            [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
            public sealed class ModLoadedGuardAttribute : System.Attribute
            {
                public string ModId { get; }
                public ModLoadedGuardAttribute(string modId) => ModId = modId;
            }
        }
        """);

    /// <summary>
    /// Creates a mock optional mod assembly containing types that could trigger assembly load.
    /// The assembly name is what the analyzer sees as ContainingAssembly.Name.
    /// </summary>
    private static MetadataReference CreateMockOptionalModAssembly(string assemblyName)
    {
        var source = $$"""
            namespace {{assemblyName}}
            {
                public class ColorService
                {
                    public void DoSomething() { }
                }

                public interface IColorProvider
                {
                    string GetColor();
                }

                public class ColorConfig
                {
                    public string PrimaryColor { get; set; }
                }
            }
            """;

        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) };

        var compilation = CSharpCompilation.Create(
            assemblyName, // THIS is what the analyzer sees as ContainingAssembly.Name
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return compilation.ToMetadataReference();
    }

    /// <summary>
    /// Sets up test with optional mod dependencies using mod ID to assembly name pairs.
    /// </summary>
    /// <param name="mods">Pairs of (modId, assemblyName). The modId is used in attributes,
    /// the assemblyName is used for type checking.</param>
    private void SetupWithOptionalMods(params (string modId, string assemblyName)[] mods)
    {
        // Add inline attribute definitions
        TestSources.Add(ModdingAttributes);

        foreach (var (_, assemblyName) in mods)
        {
            // Create mock assembly and add as reference so types resolve with correct assembly name
            AdditionalReferences.Add(CreateMockOptionalModAssembly(assemblyName));
        }

        if (mods.Length > 0)
        {
            // Set both mod IDs (for attribute matching) and assembly names (for type checking)
            GlobalOptions["build_property.OptionalModIds"] = string.Join(";", mods.Select(m => m.modId));
            GlobalOptions["build_property.OptionalModAssemblies"] = string.Join(";", mods.Select(m => m.assemblyName));
        }
    }

    /// <summary>
    /// Legacy helper that uses assembly name as both mod ID and assembly name.
    /// Used for tests that don't care about the mod ID distinction.
    /// </summary>
    private void SetupWithOptionalAssemblies(params string[] assemblies)
    {
        // When mod ID equals assembly name, the fallback behavior works
        SetupWithOptionalMods(assemblies.Select(a => (a, a)).ToArray());
    }

    #region Configuration Tests (no optional assemblies)

    [Test]
    public async Task NoOptionalAssemblies_NoDiagnostics()
    {
        // When no optional assemblies configured, analyzer should not run
        TestSources.Add(("Test.cs", """
            public class Test
            {
                public string Field;
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task EmptyOptionalAssemblies_NoDiagnostics()
    {
        // Empty string should also be treated as no optional assemblies
        GlobalOptions["build_property.OptionalModAssemblies"] = "";

        TestSources.Add(("Test.cs", """
            public class Test
            {
                public string Field;
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task WhitespaceOptionalAssemblies_NoDiagnostics()
    {
        // Whitespace-only string should also be treated as no optional assemblies
        GlobalOptions["build_property.OptionalModAssemblies"] = "   ";

        TestSources.Add(("Test.cs", """
            public class Test
            {
                public string Field;
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    #endregion

    #region System Types Tests (no diagnostics expected)

    [Test]
    public async Task FieldTypeFromOptionalMod_NoDiagnosticForSystemTypes()
    {
        // Types from standard library are not from optional assemblies
        SetupWithOptionalAssemblies("ColorProviderMod");

        TestSources.Add(("Test.cs", """
            public class Test
            {
                // System.String is not from ColorProviderMod
                public string Field;
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task GenericTypeArgument_NoLeakageWithSystemTypes()
    {
        // List<string> should not be flagged - neither List nor string are from optional mod
        SetupWithOptionalAssemblies("ColorProviderMod");

        TestSources.Add(("Test.cs", """
            using System.Collections.Generic;

            public class Test
            {
                public List<string> Items;
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task BaseType_NoLeakageWithSystemTypes()
    {
        SetupWithOptionalAssemblies("ColorProviderMod");

        TestSources.Add(("Test.cs", """
            using System;

            public class Test : Exception
            {
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task MethodReturnType_NoLeakageWithSystemTypes()
    {
        SetupWithOptionalAssemblies("ColorProviderMod");

        TestSources.Add(("Test.cs", """
            public class Test
            {
                public string GetValue() => "";
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task MethodParameter_NoLeakageWithSystemTypes()
    {
        SetupWithOptionalAssemblies("ColorProviderMod");

        TestSources.Add(("Test.cs", """
            public class Test
            {
                public void Process(string value) { }
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task InterfaceImplementation_NoLeakageWithSystemTypes()
    {
        SetupWithOptionalAssemblies("ColorProviderMod");

        TestSources.Add(("Test.cs", """
            using System;

            public class Test : IDisposable
            {
                public void Dispose() { }
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task ArrayElementType_NoLeakageWithSystemTypes()
    {
        SetupWithOptionalAssemblies("ColorProviderMod");

        TestSources.Add(("Test.cs", """
            public class Test
            {
                public string[] Items;
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task PropertyType_NoLeakageWithSystemTypes()
    {
        SetupWithOptionalAssemblies("ColorProviderMod");

        TestSources.Add(("Test.cs", """
            public class Test
            {
                public string Name { get; set; }
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task TypeConstraint_NoLeakageWithSystemTypes()
    {
        SetupWithOptionalAssemblies("ColorProviderMod");

        TestSources.Add(("Test.cs", """
            using System;

            public class Test
            {
                public void Process<T>() where T : IDisposable
                {
                }
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task CompilerGeneratedMethods_AreSkipped()
    {
        SetupWithOptionalAssemblies("ColorProviderMod");

        // Record types generate compiler methods - they should be skipped
        TestSources.Add(("Test.cs", """
            public record Person(string Name, int Age);
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task NestedGenericType_NoLeakageWithSystemTypes()
    {
        SetupWithOptionalAssemblies("ColorProviderMod");

        TestSources.Add(("Test.cs", """
            using System.Collections.Generic;

            public class Test
            {
                public Dictionary<string, List<int>> NestedData;
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    #endregion

    #region Type Leakage Detection Tests (SPARK0601 MUST be reported)

    [Test]
    public async Task FieldTypeFromOptionalMod_ReportsDiagnostic()
    {
        SetupWithOptionalAssemblies("ColorProviderMod");

        TestSources.Add(("Test.cs", """
            public class Test
            {
                public ColorProviderMod.ColorService Service; // LEAK!
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0601", 1);
    }

    [Test]
    public async Task PropertyTypeFromOptionalMod_ReportsDiagnostic()
    {
        SetupWithOptionalAssemblies("ColorProviderMod");

        TestSources.Add(("Test.cs", """
            public class Test
            {
                public ColorProviderMod.ColorService Service { get; set; } // LEAK!
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0601", 1);
    }

    [Test]
    public async Task MethodReturnTypeFromOptionalMod_ReportsDiagnostic()
    {
        SetupWithOptionalAssemblies("ColorProviderMod");

        TestSources.Add(("Test.cs", """
            public class Test
            {
                public ColorProviderMod.ColorService GetService() => null; // LEAK!
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0601", 3);
    }

    [Test]
    public async Task MethodParameterFromOptionalMod_ReportsDiagnostic()
    {
        SetupWithOptionalAssemblies("ColorProviderMod");

        TestSources.Add(("Test.cs", """
            public class Test
            {
                public void Process(ColorProviderMod.ColorService service) { } // LEAK!
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0601", 1);
    }

    [Test]
    public async Task BaseTypeFromOptionalMod_ReportsDiagnostic()
    {
        SetupWithOptionalAssemblies("ColorProviderMod");

        TestSources.Add(("Test.cs", """
            public class Test : ColorProviderMod.ColorService // LEAK!
            {
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0601", 1);
    }

    [Test]
    public async Task InterfaceFromOptionalMod_ReportsDiagnostic()
    {
        SetupWithOptionalAssemblies("ColorProviderMod");

        TestSources.Add(("Test.cs", """
            public class Test : ColorProviderMod.IColorProvider // LEAK!
            {
                public string GetColor() => "";
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0601", 1);
    }

    [Test]
    public async Task GenericTypeArgumentFromOptionalMod_ReportsDiagnostic()
    {
        SetupWithOptionalAssemblies("ColorProviderMod");

        TestSources.Add(("Test.cs", """
            using System.Collections.Generic;

            public class Test
            {
                public List<ColorProviderMod.ColorService> Services; // LEAK!
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0601", 1);
    }

    [Test]
    public async Task ArrayElementTypeFromOptionalMod_ReportsDiagnostic()
    {
        SetupWithOptionalAssemblies("ColorProviderMod");

        TestSources.Add(("Test.cs", """
            public class Test
            {
                public ColorProviderMod.ColorService[] Services; // LEAK!
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0601", 1);
    }

    [Test]
    public async Task MultipleLeaksInSameClass_ReportsMultipleDiagnostics()
    {
        SetupWithOptionalAssemblies("ColorProviderMod");

        TestSources.Add(("Test.cs", """
            public class Test
            {
                public ColorProviderMod.ColorService ServiceField; // LEAK 1
                public ColorProviderMod.ColorConfig ConfigProp { get; set; } // LEAK 2
                public ColorProviderMod.IColorProvider GetProvider() => null; // LEAK 3
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0601", 5);
    }

    [Test]
    public async Task NestedGenericWithOptionalModType_ReportsDiagnostic()
    {
        SetupWithOptionalAssemblies("ColorProviderMod");

        TestSources.Add(("Test.cs", """
            using System.Collections.Generic;

            public class Test
            {
                public Dictionary<string, List<ColorProviderMod.ColorService>> Data; // LEAK!
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0601", 1);
    }

    #endregion

    #region Guard Attribute Tests (no diagnostics when guarded)

    [Test]
    public async Task FieldInOptionalModDependentClass_NoDiagnostic()
    {
        SetupWithOptionalAssemblies("ColorProviderMod");

        TestSources.Add(("Test.cs", """
            using Sparkitect.Modding;

            [OptionalModDependent("ColorProviderMod")]
            public class ColorIntegration
            {
                public ColorProviderMod.ColorService Service; // Allowed - class is guarded
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task PropertyInOptionalModDependentClass_NoDiagnostic()
    {
        SetupWithOptionalAssemblies("ColorProviderMod");

        TestSources.Add(("Test.cs", """
            using Sparkitect.Modding;

            [OptionalModDependent("ColorProviderMod")]
            public class ColorIntegration
            {
                public ColorProviderMod.ColorConfig Config { get; set; } // Allowed - class is guarded
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task BaseTypeInOptionalModDependentClass_NoDiagnostic()
    {
        SetupWithOptionalAssemblies("ColorProviderMod");

        TestSources.Add(("Test.cs", """
            using Sparkitect.Modding;

            [OptionalModDependent("ColorProviderMod")]
            public class ColorIntegration : ColorProviderMod.ColorService // Allowed - class is guarded
            {
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task InterfaceInOptionalModDependentClass_NoDiagnostic()
    {
        SetupWithOptionalAssemblies("ColorProviderMod");

        TestSources.Add(("Test.cs", """
            using Sparkitect.Modding;

            [OptionalModDependent("ColorProviderMod")]
            public class ColorIntegration : ColorProviderMod.IColorProvider // Allowed - class is guarded
            {
                public string GetColor() => "";
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task ReturnTypeInModLoadedGuardMethod_NoDiagnostic()
    {
        SetupWithOptionalAssemblies("ColorProviderMod");

        TestSources.Add(("Test.cs", """
            using Sparkitect.Modding;

            public class ColorBridge
            {
                [ModLoadedGuard("ColorProviderMod")]
                public ColorProviderMod.ColorService GetService() => null; // Allowed - method is guarded
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task ParameterInModLoadedGuardMethod_NoDiagnostic()
    {
        SetupWithOptionalAssemblies("ColorProviderMod");

        TestSources.Add(("Test.cs", """
            using Sparkitect.Modding;

            public class ColorBridge
            {
                [ModLoadedGuard("ColorProviderMod")]
                public void Configure(ColorProviderMod.ColorConfig config) { } // Allowed - method is guarded
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task ClassAttributeAllowsTypesInAllMembers()
    {
        // When class has [OptionalModDependent], all its members can use those types
        SetupWithOptionalAssemblies("ColorProviderMod");

        TestSources.Add(("Test.cs", """
            using Sparkitect.Modding;

            [OptionalModDependent("ColorProviderMod")]
            public class ModIntegration
            {
                public ColorProviderMod.ColorService Field;
                public ColorProviderMod.ColorConfig Property { get; set; }
                public ColorProviderMod.IColorProvider GetValue() => null;
                public void SetValue(ColorProviderMod.ColorService value) { }
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task MethodAttributeOnlyAllowsTypesInThatMethod()
    {
        // [ModLoadedGuard] on method allows types in that method
        // Field outside guarded method still reports
        SetupWithOptionalAssemblies("ColorProviderMod");

        TestSources.Add(("Test.cs", """
            using Sparkitect.Modding;

            public class ModBridge
            {
                // This field is NOT guarded - reports diagnostic
                public ColorProviderMod.ColorService UnguardedField; // LEAK!

                // This method is guarded - allowed to use ColorProviderMod types
                [ModLoadedGuard("ColorProviderMod")]
                public ColorProviderMod.ColorService GetModService() => null; // Allowed
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0601", 1);
    }

    #endregion

    #region Multiple Optional Mods Tests

    [Test]
    public async Task MultipleOptionalModAttributes_AllRespected()
    {
        SetupWithOptionalAssemblies("ModA", "ModB");

        // Need to create both mock assemblies with distinct types
        // SetupWithOptionalAssemblies already adds them as references

        TestSources.Add(("Test.cs", """
            using Sparkitect.Modding;

            [OptionalModDependent("ModA")]
            [OptionalModDependent("ModB")]
            public class MultiModIntegration
            {
                // Types from both ModA and ModB are allowed
                public ModA.ColorService ModAService;
                public ModB.ColorService ModBService;
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task PartialGuarding_ReportsForUnguardedMod()
    {
        SetupWithOptionalAssemblies("ModA", "ModB");

        TestSources.Add(("Test.cs", """
            using Sparkitect.Modding;

            [OptionalModDependent("ModA")] // Only guarded for ModA
            public class PartialIntegration
            {
                public ModA.ColorService ModAService; // Allowed - guarded
                public ModB.ColorService ModBService; // LEAK! - ModB not guarded
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0601", 1);
    }

    [Test]
    public async Task CaseInsensitiveModIdMatching()
    {
        // OptionalModAssemblies uses different case than attribute
        SetupWithOptionalAssemblies("COLORPROVIDERMOD");

        TestSources.Add(("Test.cs", """
            using Sparkitect.Modding;

            [OptionalModDependent("ColorProviderMod")] // Different case
            public class ColorIntegration
            {
                public COLORPROVIDERMOD.ColorService ColorField; // Should be allowed
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task CaseInsensitiveAssemblyMatching_Leak()
    {
        // Assembly name is case-insensitive for matching
        SetupWithOptionalAssemblies("COLORPROVIDERMOD");

        TestSources.Add(("Test.cs", """
            public class Test
            {
                public COLORPROVIDERMOD.ColorService Service; // LEAK - case doesn't matter
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0601", 1);
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task UnguardedMethodInGuardedClass_StillAllowed()
    {
        // Class-level guard covers all members, even unguarded methods
        SetupWithOptionalAssemblies("ColorProviderMod");

        TestSources.Add(("Test.cs", """
            using Sparkitect.Modding;

            [OptionalModDependent("ColorProviderMod")]
            public class ColorIntegration
            {
                // No [ModLoadedGuard] on this method, but class is guarded
                public ColorProviderMod.ColorService GetService() => null; // Allowed
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task GuardedMethodInUnguardedClass_AllowsMethodOnly()
    {
        // Method guard only covers that method, not class-level members
        SetupWithOptionalAssemblies("ColorProviderMod");

        TestSources.Add(("Test.cs", """
            using Sparkitect.Modding;

            public class PartialBridge
            {
                // Class-level field is not guarded - LEAK
                public ColorProviderMod.ColorService ClassField; // LEAK!

                // Method is guarded - allowed
                [ModLoadedGuard("ColorProviderMod")]
                public ColorProviderMod.ColorService GetService() => null; // Allowed
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0601", 1);
    }

    [Test]
    public async Task MultipleMethodGuards_AllowsMultipleMods()
    {
        SetupWithOptionalAssemblies("ModA", "ModB");

        TestSources.Add(("Test.cs", """
            using Sparkitect.Modding;

            public class MultiBridge
            {
                [ModLoadedGuard("ModA")]
                [ModLoadedGuard("ModB")]
                public void ProcessBoth(ModA.ColorService a, ModB.ColorService b) { } // Allowed
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    #endregion

    #region Mod ID to Assembly Name Mapping Tests

    [Test]
    public async Task ModIdInAttribute_MapsToAssemblyName_NoDiagnostic()
    {
        // The real-world scenario: mod ID is snake_case, assembly name is PascalCase
        // Attribute uses mod ID, but type checking uses assembly name
        SetupWithOptionalMods(("color_provider_mod", "ColorProviderMod"));

        TestSources.Add(("Test.cs", """
            using Sparkitect.Modding;

            [OptionalModDependent("color_provider_mod")] // Uses mod ID
            public class ColorIntegration
            {
                public ColorProviderMod.ColorService Service; // Type is from assembly name
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task ModIdInAttribute_MapsToAssemblyName_MethodGuard_NoDiagnostic()
    {
        SetupWithOptionalMods(("color_provider_mod", "ColorProviderMod"));

        TestSources.Add(("Test.cs", """
            using Sparkitect.Modding;

            public class ColorBridge
            {
                [ModLoadedGuard("color_provider_mod")] // Uses mod ID
                public ColorProviderMod.ColorService GetService() => null; // Type is from assembly name
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task WrongModIdInAttribute_StillReportsDiagnostic()
    {
        // Using wrong mod ID should still report leak
        SetupWithOptionalMods(("color_provider_mod", "ColorProviderMod"));

        TestSources.Add(("Test.cs", """
            using Sparkitect.Modding;

            [OptionalModDependent("wrong_mod_id")] // Wrong mod ID
            public class ColorIntegration
            {
                public ColorProviderMod.ColorService Service; // Type from ColorProviderMod leaks
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0601", 1);
    }

    [Test]
    public async Task MultipleModsWithDifferentIds_AllMapped()
    {
        SetupWithOptionalMods(
            ("mod_a", "ModA"),
            ("mod_b", "ModB"));

        TestSources.Add(("Test.cs", """
            using Sparkitect.Modding;

            [OptionalModDependent("mod_a")]
            [OptionalModDependent("mod_b")]
            public class MultiModIntegration
            {
                public ModA.ColorService ServiceA;
                public ModB.ColorService ServiceB;
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task PartialModIdGuarding_ReportsUnguardedMod()
    {
        SetupWithOptionalMods(
            ("mod_a", "ModA"),
            ("mod_b", "ModB"));

        TestSources.Add(("Test.cs", """
            using Sparkitect.Modding;

            [OptionalModDependent("mod_a")] // Only guarded for mod_a
            public class PartialIntegration
            {
                public ModA.ColorService ServiceA; // Allowed - guarded via mod_a
                public ModB.ColorService ServiceB; // LEAK! - mod_b not guarded
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0601", 1);
    }

    #endregion

    #region Gap Closure Tests (Field Initializers and Transitive Types)

    [Test]
    public async Task FieldInitializerWithOptionalModType_ReportsDiagnostic()
    {
        // Field declared as object, but initializer creates optional mod type
        SetupWithOptionalAssemblies("ColorProviderMod");

        TestSources.Add(("Test.cs", """
            public class Test
            {
                // Field type is object (allowed), but initializer uses optional mod type (LEAK!)
                private object _service = new ColorProviderMod.ColorService();
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        // May report 2x: once from operation block analysis, once from IObjectCreationOperation.Type
        await Assert.That(diagnostics.Count(d => d.Id == "SPARK0601")).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task FieldInitializerInGuardedClass_NoDiagnostic()
    {
        // Field initializer in [OptionalModDependent] class is allowed
        SetupWithOptionalAssemblies("ColorProviderMod");

        TestSources.Add(("Test.cs", """
            using Sparkitect.Modding;

            [OptionalModDependent("ColorProviderMod")]
            public class ColorIntegration
            {
                private object _service = new ColorProviderMod.ColorService(); // Allowed - class is guarded
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task FieldInitializerWithMethodCall_ReportsDiagnostic()
    {
        // Field initializer that calls method returning optional mod type
        SetupWithOptionalAssemblies("ColorProviderMod");

        TestSources.Add(("Test.cs", """
            public class Test
            {
                private object _config = GetConfig();

                private static ColorProviderMod.ColorConfig GetConfig() => null; // LEAK in signature
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        // GetConfig return type leaks (analyzed via AnalyzeMethod)
        await Assert.That(diagnostics.Count(d => d.Id == "SPARK0601")).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task LocalTypeWithOptionalModDependent_UsedInUnguardedContext_ReportsDiagnostic()
    {
        // A type in the SAME assembly with [OptionalModDependent] requires guards when used
        SetupWithOptionalAssemblies("ColorProviderMod");

        TestSources.Add(("ColorIntegration.cs", """
            using Sparkitect.Modding;

            [OptionalModDependent("ColorProviderMod")]
            public class ColorIntegration
            {
                public ColorProviderMod.ColorService Service; // Allowed inside guarded class
            }
            """));

        TestSources.Add(("Consumer.cs", """
            public class Consumer
            {
                // Using a type that depends on optional mod - should require guard!
                public void DoWork()
                {
                    var integration = new ColorIntegration(); // LEAK - ColorIntegration requires guard
                }
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        // May report multiple times from different analysis paths
        await Assert.That(diagnostics.Count(d => d.Id == "SPARK0601")).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task LocalTypeWithOptionalModDependent_UsedInGuardedContext_NoDiagnostic()
    {
        // Using [OptionalModDependent] type within properly guarded context
        SetupWithOptionalAssemblies("ColorProviderMod");

        TestSources.Add(("ColorIntegration.cs", """
            using Sparkitect.Modding;

            [OptionalModDependent("ColorProviderMod")]
            public class ColorIntegration
            {
                public ColorProviderMod.ColorService Service;
            }
            """));

        TestSources.Add(("Consumer.cs", """
            using Sparkitect.Modding;

            public class Consumer
            {
                [ModLoadedGuard("ColorProviderMod")]
                public void DoGuardedWork()
                {
                    var integration = new ColorIntegration(); // Allowed - method is guarded
                }
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    [Test]
    public async Task LocalTypeWithOptionalModDependent_AsFieldType_ReportsDiagnostic()
    {
        // Field type is a local [OptionalModDependent] type in unguarded class
        SetupWithOptionalAssemblies("ColorProviderMod");

        TestSources.Add(("ColorIntegration.cs", """
            using Sparkitect.Modding;

            [OptionalModDependent("ColorProviderMod")]
            public class ColorIntegration
            {
                public void Configure() { }
            }
            """));

        TestSources.Add(("Consumer.cs", """
            public class Consumer
            {
                // Field type is a guarded-required type - LEAK!
                public ColorIntegration Integration;
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertDiagnosticCount(diagnostics, "SPARK0601", 1);
    }

    [Test]
    public async Task LocalTypeWithOptionalModDependent_MultipleModIds_RequiresAllGuards()
    {
        // Type with multiple [OptionalModDependent] requires ALL mods to be guarded
        SetupWithOptionalMods(("mod_a", "ModA"), ("mod_b", "ModB"));

        TestSources.Add(("MultiIntegration.cs", """
            using Sparkitect.Modding;

            [OptionalModDependent("mod_a")]
            [OptionalModDependent("mod_b")]
            public class MultiIntegration
            {
                public void Configure() { }
            }
            """));

        TestSources.Add(("Consumer.cs", """
            using Sparkitect.Modding;

            public class Consumer
            {
                [ModLoadedGuard("mod_a")] // Only guarded for mod_a, not mod_b
                public void PartiallyGuarded()
                {
                    var integration = new MultiIntegration(); // LEAK - missing mod_b guard
                }
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        // May report multiple times from different analysis paths
        await Assert.That(diagnostics.Count(d => d.Id == "SPARK0601")).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task LocalTypeWithOptionalModDependent_AllModsGuarded_NoDiagnostic()
    {
        // Type with multiple [OptionalModDependent] - all mods guarded
        SetupWithOptionalMods(("mod_a", "ModA"), ("mod_b", "ModB"));

        TestSources.Add(("MultiIntegration.cs", """
            using Sparkitect.Modding;

            [OptionalModDependent("mod_a")]
            [OptionalModDependent("mod_b")]
            public class MultiIntegration
            {
                public void Configure() { }
            }
            """));

        TestSources.Add(("Consumer.cs", """
            using Sparkitect.Modding;

            public class Consumer
            {
                [ModLoadedGuard("mod_a")]
                [ModLoadedGuard("mod_b")]
                public void FullyGuarded()
                {
                    var integration = new MultiIntegration(); // Allowed - both mods guarded
                }
            }
            """));

        var diagnostics = await RunAnalyzerAsync();
        await AssertNoDiagnostics(diagnostics);
    }

    #endregion
}
