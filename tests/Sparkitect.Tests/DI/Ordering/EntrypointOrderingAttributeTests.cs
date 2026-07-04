using System.Reflection;
using Sparkitect.DI.Ordering;

namespace Sparkitect.Tests.DI.Ordering;

// Dummy types for generic attribute targets
public class TypeA;

public class TypeB;

public class EntrypointOrderingAttributeTests
{
    [Test]
    public async Task OrderBefore_Generic_AddsEdge_CurrentToTarget()
    {
        // Arrange
        var attr = new EntrypointOrderBeforeAttribute<TypeB>();
        var builder = new EntrypointOrderingBuilder();
        builder.SetCurrentType("Sparkitect.Tests.DI.Ordering.TypeA");

        // Act
        attr.ApplyOrdering(builder);

        // Assert
        await Assert.That(builder.Edges.Count).IsEqualTo(1);
        var edge = builder.Edges.First();
        await Assert.That(edge.From).IsEqualTo("Sparkitect.Tests.DI.Ordering.TypeA");
        await Assert.That(edge.To).IsEqualTo(typeof(TypeB).FullName);
    }

    [Test]
    public async Task OrderAfter_Generic_AddsEdge_TargetToCurrent()
    {
        // Arrange
        var attr = new EntrypointOrderAfterAttribute<TypeA>();
        var builder = new EntrypointOrderingBuilder();
        builder.SetCurrentType("Sparkitect.Tests.DI.Ordering.TypeB");

        // Act
        attr.ApplyOrdering(builder);

        // Assert
        await Assert.That(builder.Edges.Count).IsEqualTo(1);
        var edge = builder.Edges.First();
        await Assert.That(edge.From).IsEqualTo(typeof(TypeA).FullName);
        await Assert.That(edge.To).IsEqualTo("Sparkitect.Tests.DI.Ordering.TypeB");
    }

    [Test]
    public async Task OrderBefore_String_AddsEdge_CurrentToTarget()
    {
        // Arrange
        var attr = new EntrypointOrderBeforeAttribute("Some.Other.Type");
        var builder = new EntrypointOrderingBuilder();
        builder.SetCurrentType("MyType");

        // Act
        attr.ApplyOrdering(builder);

        // Assert
        await Assert.That(builder.Edges.Count).IsEqualTo(1);
        var edge = builder.Edges.First();
        await Assert.That(edge.From).IsEqualTo("MyType");
        await Assert.That(edge.To).IsEqualTo("Some.Other.Type");
    }

    [Test]
    public async Task OrderAfter_String_AddsEdge_TargetToCurrent()
    {
        // Arrange
        var attr = new EntrypointOrderAfterAttribute("Some.Other.Type");
        var builder = new EntrypointOrderingBuilder();
        builder.SetCurrentType("MyType");

        // Act
        attr.ApplyOrdering(builder);

        // Assert
        await Assert.That(builder.Edges.Count).IsEqualTo(1);
        var edge = builder.Edges.First();
        await Assert.That(edge.From).IsEqualTo("Some.Other.Type");
        await Assert.That(edge.To).IsEqualTo("MyType");
    }

    [Test]
    public async Task MultipleAttributes_AllApplied()
    {
        // Arrange
        var before1 = new EntrypointOrderBeforeAttribute<TypeB>();
        var before2 = new EntrypointOrderBeforeAttribute("Other.Type");
        var after1 = new EntrypointOrderAfterAttribute<TypeA>();
        var builder = new EntrypointOrderingBuilder();
        builder.SetCurrentType("Current.Type");

        // Act
        before1.ApplyOrdering(builder);
        before2.ApplyOrdering(builder);
        after1.ApplyOrdering(builder);

        // Assert
        await Assert.That(builder.Edges.Count).IsEqualTo(3);
    }

    [Test]
    public async Task AllowMultiple_CanApplyMultipleTimes()
    {
        // Verify all 4 attribute types have AllowMultiple = true
        var types = new[]
        {
            typeof(EntrypointOrderBeforeAttribute<>),
            typeof(EntrypointOrderBeforeAttribute),
            typeof(EntrypointOrderAfterAttribute<>),
            typeof(EntrypointOrderAfterAttribute),
        };

        foreach (var type in types)
        {
            var attributeUsage = type.GetCustomAttribute<AttributeUsageAttribute>();
            await Assert.That(attributeUsage).IsNotNull();
            await Assert.That(attributeUsage!.AllowMultiple).IsTrue();
        }
    }
}
