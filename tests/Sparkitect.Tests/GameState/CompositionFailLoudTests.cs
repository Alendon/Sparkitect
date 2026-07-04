using Sparkitect.GameState;
using Sparkitect.Modding;

namespace Sparkitect.Tests.GameState;

/// <summary>
/// Pins fail-loud behaviour: an unregistered required or directly-declared module
/// throws <see cref="InvalidOperationException"/> whose message names the ordered resolution chain
/// (state -> ... -> missing), not merely "throws".
/// </summary>
public class CompositionFailLoudTests
{
    private static readonly Identification StateId = Identification.Create(2, 9, 1);

    private static ModuleComposition Mod(Identification id, params Identification[] requires)
        => new(id, requires, []);

    private static Dictionary<Identification, ModuleComposition> Registry(params ModuleComposition[] mods)
        => mods.ToDictionary(m => m.Id);

    private static HashSet<Identification> EmptySeed() => [];

    private static InvalidOperationException? Capture(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (InvalidOperationException ex)
        {
            return ex;
        }
    }

    [Test]
    public async Task Compose_DirectModuleUnregistered_ThrowsNamingStateAndMissing()
    {
        var missing = Identification.Create(3, 9, 1);
        var registry = Registry(); // missing is NOT registered
        var state = new StateComposition(StateId, Identification.Empty, [missing]);

        var ex = Capture(() => StateComposer.Compose(state, EmptySeed(), registry));

        await Assert.That(ex).IsNotNull();
        await Assert.That(ex!.Message).Contains(StateId.ToString());
        await Assert.That(ex.Message).Contains(missing.ToString());
    }

    [Test]
    public async Task Compose_TransitiveRequireUnregistered_ThrowsNamingFullChain()
    {
        // State -> A -> B -> missingC. The message must contain the state, at least one intermediate
        // module, and the missing id, in resolution order.
        var a = Identification.Create(3, 9, 10);
        var b = Identification.Create(3, 9, 11);
        var missing = Identification.Create(3, 9, 12);
        var registry = Registry(Mod(a, b), Mod(b, missing)); // missing not registered
        var state = new StateComposition(StateId, Identification.Empty, [a]);

        var ex = Capture(() => StateComposer.Compose(state, EmptySeed(), registry));

        await Assert.That(ex).IsNotNull();
        // Chain names the state, an intermediate (a or b), and the missing id.
        await Assert.That(ex!.Message).Contains(StateId.ToString());
        await Assert.That(ex.Message).Contains(a.ToString());
        await Assert.That(ex.Message).Contains(b.ToString());
        await Assert.That(ex.Message).Contains(missing.ToString());

        // The chain is ordered: state precedes intermediate precedes missing. Inspect only the
        // "Resolution chain:" portion — the missing id also appears in the leading "Module X ..."
        // sentence, so ordering must be checked within the chain rendering itself.
        var chainStart = ex.Message.IndexOf("Resolution chain:", StringComparison.Ordinal);
        await Assert.That(chainStart >= 0).IsTrue();
        var chain = ex.Message[chainStart..];
        var idxState = chain.IndexOf(StateId.ToString(), StringComparison.Ordinal);
        var idxIntermediate = chain.IndexOf(b.ToString(), StringComparison.Ordinal);
        var idxMissing = chain.IndexOf(missing.ToString(), StringComparison.Ordinal);
        await Assert.That(idxState < idxIntermediate).IsTrue();
        await Assert.That(idxIntermediate < idxMissing).IsTrue();
    }
}
