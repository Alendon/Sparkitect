using System.Linq;
using System.Reflection;
using Sparkitect.Modding;
using Sparkitect.RenderGraph;

namespace Sparkitect.Tests.RenderGraph;

public class RenderPassRegistryTests
{
    [Test]
    public async Task Class_HasRegistryAttribute_WithIdentifierRenderPass()
    {
        var attrs = typeof(RenderPassRegistry).GetCustomAttributes(typeof(RegistryAttribute), inherit: false);
        await Assert.That(attrs).HasCount().EqualTo(1);
        var registry = (RegistryAttribute)attrs[0];
        await Assert.That(registry.Identifier).IsEqualTo("render_pass");
    }

    [Test]
    public async Task StaticIdentifier_ReturnsRenderPass()
    {
        await Assert.That(RenderPassRegistry.Identifier).IsEqualTo("render_pass");
    }

    [Test]
    public async Task ImplementsIRegistry()
    {
        await Assert.That(typeof(IRegistry).IsAssignableFrom(typeof(RenderPassRegistry))).IsTrue();
    }

    [Test]
    public async Task RegisterPass_HasRegistryMethodAndKeyedFactoryMarkerAttributes()
    {
        var method = typeof(RenderPassRegistry).GetMethod(
            "RegisterPass",
            BindingFlags.Public | BindingFlags.Instance);
        await Assert.That(method).IsNotNull();

        var attrNames = method!.GetCustomAttributes(inherit: false)
            .Select(a => a.GetType().Name)
            .ToList();

        await Assert.That(attrNames).Contains("RegistryMethodAttribute");
        // KeyedFactoryGenerationMarkerAttribute<IPass> — generic-attribute name carries arity suffix `1
        await Assert.That(attrNames.Any(n => n.StartsWith("KeyedFactoryGenerationMarkerAttribute"))).IsTrue();
    }

    [Test]
    public async Task RegisterPass_GenericConstraintsMatchDummyRegistryShape()
    {
        var method = typeof(RenderPassRegistry).GetMethod(
            "RegisterPass",
            BindingFlags.Public | BindingFlags.Instance)!;
        var typeParam = method.GetGenericArguments().Single();
        var constraints = typeParam.GetGenericParameterConstraints().Select(c => c.Name).ToList();
        await Assert.That(constraints).Contains("IPass");
        await Assert.That(constraints).Contains("IHasIdentification");
        var hasClassConstraint =
            (typeParam.GenericParameterAttributes & System.Reflection.GenericParameterAttributes.ReferenceTypeConstraint) != 0;
        await Assert.That(hasClassConstraint).IsTrue();
    }

    [Test]
    public async Task KeyedFactoryGenerator_RecordedMarkerTBaseInRegistryMetadata()
    {
        // Per 49.2-02-SUMMARY.md: the generator emits a per-method KeyedFactoryMarkerTBase
        // const field on the inner method-metadata class. Unlike the per-(registry, method)
        // configurator + attribute (which are emitted only once at least one concrete provider
        // is registered, per RegistryGenerator.Output.cs:384), the metadata field is emitted
        // unconditionally whenever the marker is present on the registry method — so it is the
        // correct walking-skeleton-time proof that the [KeyedFactoryGenerationMarker<IPass>]
        // marker drove the 49.2 generator before any concrete pass type ships (Wave 3+).
        var asm = typeof(RenderPassRegistry).Assembly;
        var metadata = asm.GetType(
            "Sparkitect.CompilerGenerated.RenderPassRegistry_Metadata",
            throwOnError: false);
        await Assert.That(metadata).IsNotNull();

        var registerPassMeta = metadata!.GetNestedType("RegisterPass", BindingFlags.Public);
        await Assert.That(registerPassMeta).IsNotNull();

        var markerField = registerPassMeta!.GetField(
            "KeyedFactoryMarkerTBase",
            BindingFlags.Public | BindingFlags.Static);
        await Assert.That(markerField).IsNotNull();

        var markerValue = (string?)markerField!.GetRawConstantValue();
        await Assert.That(markerValue).IsEqualTo("global::Sparkitect.RenderGraph.IPass");
    }
}
