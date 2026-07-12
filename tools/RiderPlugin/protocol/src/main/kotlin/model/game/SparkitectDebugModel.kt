package model.game

import com.jetbrains.rd.generator.nova.*
import com.jetbrains.rd.generator.nova.PredefinedType.*
import com.jetbrains.rd.generator.nova.csharp.CSharp50Generator
import com.jetbrains.rd.generator.nova.kotlin.Kotlin11Generator
import model.lib.DebugLibrary

// The game channel: a standalone Root() the engine hosts over a SocketWire.Server and the
// ReSharper backend connects to as a client. Generated to C# twice from this one definition —
// asis for the backend endpoint, reversed for the engine endpoint — with identical wire ids by
// construction. NOT rooted on SolutionModel: the frontend cannot socket the game directly;
// the backend republishes to the frontend via DebugToolWindowModel.
//
// It references the shared DebugLibrary structdefs (single data design) rather than declaring its
// own, and layers the typed GSM view over the generic entry floor so a mod can later bind its
// own typed model / emit schema-less entries onto the same extensible protocol.
@Suppress("unused")
object SparkitectDebugModel : Root() {

    init {
        setting(Kotlin11Generator.Namespace, "sparkitect.debug.protocol.game")
        setting(CSharp50Generator.Namespace, "Sparkitect.Debug.Protocol.Game")
    }

    init {
        // Typed GSM view: the current composition snapshot, pushed on connect and on every
        // composition change. Nullable = not yet published (pre-first-compose).
        property("snapshot", DebugLibrary.DebugSnapshot.nullable)

        // Generic self-describing floor: a string-keyed set of entries for schema-less
        // consumers. Unknown keys are simply ignored, keeping the channel forward-tolerant without
        // a contract version. A mod contributes its own entries here (or binds its own Root).
        map("entries", string, DebugLibrary.GenericDebugEntry)
    }
}
