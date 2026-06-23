using Sparkitect.Graphing.Moments;
using Sparkitect.Modding;
using static Sparkitect.Tests.Graphing.SyntheticDomain;

namespace Sparkitect.Tests.Graphing;

/// <summary>
/// Moment registration rides the existing RegistryGenerator: a registration binds one
/// <see cref="Identification"/> to a <see cref="MomentDefinition{T}"/> conveying the moment's resource
/// type — name + resource type only, never backing/position/producer. These assert the carrier conveys
/// the resource type and the registry method stores exactly one moment per id (the SG emits the
/// Identification property; the registry method is the registration sink it drives).
/// </summary>
public class MomentRegistrationTests
{
    private static readonly Identification TargetBuffer = Identification.Create(7, 1, 1);

    [Test]
    public async Task MomentDefinition_ConveysTheResourceTypeThroughTheCarrier()
    {
        MomentDefinition definition = new MomentDefinition<SyntheticBuffer>();

        // The carrier conveys T at registration without the caller knowing the generic argument.
        await Assert.That(definition.ResourceType).IsEqualTo(typeof(SyntheticBuffer));
    }

    [Test]
    public async Task RegisterMoment_YieldsOneRegistrationCarryingTheResourceType()
    {
        var store = new GraphMomentStore();
        var registry = new GraphMomentRegistry(store);

        registry.RegisterMoment(TargetBuffer, new MomentDefinition<SyntheticBuffer>());

        // Exactly one moment, keyed by its Identification, carrying its resource type.
        await Assert.That(store.RegisteredMoments).HasCount().EqualTo(1);
        await Assert.That(store.TryGetMoment(TargetBuffer, out var definition)).IsTrue();
        await Assert.That(definition.ResourceType).IsEqualTo(typeof(SyntheticBuffer));
    }

    [Test]
    public async Task RegistryCategory_IsGraphMoment()
    {
        // The category id the SG keys the emitted Identification properties under.
        await Assert.That(GraphMomentRegistry.Identifier).IsEqualTo("graph_moment");
    }
}
