package model.rider

import com.jetbrains.rd.generator.nova.*
import com.jetbrains.rd.generator.nova.PredefinedType.*
import com.jetbrains.rider.model.nova.ide.SolutionModel
import model.lib.DebugLibrary

// The backend -> frontend republish (D-13, Pitfall 5). An Ext on the Solution model, bound by the
// Rider platform over the existing Solution-scoped rd link: the backend (sole game-channel client)
// pushes its cached snapshot to the frontend tool window, which cannot reach the game socket.
// Generated to C# (backend) + Kotlin (frontend), referencing the SAME DebugLibrary structdefs as
// the game channel so there is one data design across all endpoints.
@Suppress("unused")
object DebugToolWindowModel : Ext(SolutionModel.Solution) {

    // A discovered Sparkitect process the selector offers (D-07). The backend's DiscoveryWatcher owns
    // the shared-temp discovery files (pid/port/version); it republishes the live list here for the
    // frontend selector, which has no discovery access of its own. The port stays backend-only (the
    // backend is the sole game-channel client, Pitfall 5) — the frontend shows pid + version.
    val ProcessInfo = structdef {
        field("pid", int)
        field("engineVersion", string)
    }

    // Which source target a tree row navigates to (D-10): double-click resolves the type declaration,
    // the context menu resolves the registration site. Both route through the backend reverse lookup.
    val NavigationTarget = enum {
        +"TypeDeclaration"
        +"RegistrationSite"
    }

    // A frontend tree-row navigation request. Carries the same (mod, category, item) NAME triple the
    // snapshot rows already hold (DebugLibrary.IdName — single data design), plus which target to
    // resolve. The backend answers via DebugNavigation (string triple -> generated leaf -> source).
    val NavigationRequest = structdef {
        field("id", DebugLibrary.IdName)
        field("target", NavigationTarget)
    }

    init {
        // The republished snapshot the frontend renders. Nullable = nothing selected / not yet
        // received. Same DebugSnapshot struct the game channel publishes (single data design).
        property("snapshot", DebugLibrary.DebugSnapshot.nullable)

        // The process (pid) whose channel the window currently shows (D-07). Nullable = no process
        // selected; when a debug session is active the backend defaults it to that process.
        property("selectedProcess", int.nullable)

        // The live discovered-process list the selector renders (D-07). Backend -> frontend: the
        // frontend cannot watch the discovery dir through the Rider protocol sandbox, so the backend
        // (which does watch it) republishes the current set here.
        property("processes", immutableList(ProcessInfo))

        // Row navigation (D-10). The frontend calls with a (row-id, target); the backend runs the
        // reverse lookup + editor navigation and answers true iff a source target resolved (so the
        // frontend can no-op loudly on a miss rather than silently doing nothing).
        call("navigate", NavigationRequest, bool)
    }
}
