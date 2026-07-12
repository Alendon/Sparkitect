package model.lib

import com.jetbrains.rd.generator.nova.*
import com.jetbrains.rd.generator.nova.PredefinedType.*
import com.jetbrains.rd.generator.nova.csharp.CSharp50Generator
import com.jetbrains.rd.generator.nova.kotlin.Kotlin11Generator

// The single shared snapshot data design. Declared as an `isLibrary` Root so
// its structdefs are generated once and REFERENCED (not re-emitted) by both the game channel
// (SparkitectDebugModel) and the Solution republish (DebugToolWindowModel) — one data design
// across every endpoint. Carries only plain, consumer-agnostic data (string ids, origin enums,
// counts, ints); no Rider/PSI concepts on the wire, so a future standalone analyzer can speak it.
@Suppress("unused")
object DebugLibrary : Root() {

    init {
        setting(Kotlin11Generator.Namespace, "sparkitect.debug.protocol")
        setting(CSharp50Generator.Namespace, "Sparkitect.Debug.Protocol")
    }

    // Marks this Root as a shared library: consuming models reference these structdefs instead
    // of re-generating them, and the C# side is emitted `symmetric` (usable both asis+reversed).
    override val isLibrary = true

    // The four provenance badges. Every composed module carries exactly one origin.
    val ModuleOrigin = enum {
        +"AddedDirect"
        +"AddedTransitive"
        +"InheritedFromParent"
        +"AutoActivatedIntegration"
    }

    // How a StatelessFunction is scheduled for a frame, mirroring the engine's three
    // per-frame runtime lists (PerFrameMethods / TransitionEnterMethods / TransitionExitMethods).
    val StatelessFunctionKind = enum {
        +"PerFrame"
        +"TransitionEnter"
        +"TransitionExit"
    }

    // The string form of a runtime Identification: the plugin cannot map runtime
    // numeric ids, so every navigable row rides the (mod, category, item) NAME triple that the
    // engine resolves via IdentificationManager before publishing.
    val IdName = structdef {
        field("mod", string)
        field("category", string)
        field("item", string)
    }

    // One module in a frame's complete composed set: its id, its origin badge, and the one-hop
    // requirers behind expansion. Requirers are the direct edges only — no full chains.
    val ModuleEntry = structdef {
        field("id", IdName)
        field("origin", ModuleOrigin)
        field("requirers", immutableList(IdName))
    }

    // One StatelessFunction active for a frame, from runtime truth: its id + schedule kind.
    val StatelessFunctionEntry = structdef {
        field("id", IdName)
        field("kind", StatelessFunctionKind)
    }

    // A single stack frame: the complete composed module set (delta-annotated via origin), the
    // per-frame SF sets, the mods added at this frame ("Mods added"), and summary counts for
    // the enriched header. `modules` is the COMPLETE set, never just the delta.
    val StateFrame = structdef {
        field("stateId", IdName)
        field("modules", immutableList(ModuleEntry))
        field("statelessFunctions", immutableList(StatelessFunctionEntry))
        field("addedMods", immutableList(string))
        field("moduleCount", int)
        field("modCount", int)
    }

    // The published snapshot: a loud protocol version marker for the base handshake, plus the
    // ordered frames, top-of-stack first. Pushed on connect and on every composition change.
    val DebugSnapshot = structdef {
        field("protocolVersion", int)
        field("frames", immutableList(StateFrame))
    }

    // Generic floor: a string-keyed, self-describing entry for schema-less consumers (the debug
    // window rendering arbitrary entries, drive-by third parties). A mod can emit these without a
    // typed model; unknown entries are simply ignored, keeping the channel forward-tolerant.
    val DebugEntryField = structdef {
        field("key", string)
        field("value", string)
    }

    val GenericDebugEntry = structdef {
        field("entryType", string)
        field("fields", immutableList(DebugEntryField))
    }
}
