package model.rider

import com.jetbrains.rd.generator.nova.*
import com.jetbrains.rd.generator.nova.PredefinedType.*
import com.jetbrains.rider.model.nova.ide.SolutionModel
import model.lib.DebugLibrary

// The static Identification-structure explorer's transport. A THIRD solution-scoped model, sibling to
// DebugToolWindowModel and wholly separate from the game channel: it NEVER touches SocketWire /
// SparkitectDebugModel. The backend host is stateless: `fetch` computes the full per-mod category->entry
// tree fresh from the live resolved compilation on every call (nothing cached), and `invalidated` is a
// payload-less trigger the frontend uses to know when to re-fetch. Generated to C# (backend) + Kotlin
// (frontend); references DebugLibrary.IdName (single data design) rather than redefining the navigation
// name triple.
@Suppress("unused")
object ExplorerModel : Ext(SolutionModel.Solution) {

    // A mod project offered in the explorer's mod selector: its ModId plus a human display name.
    val ModItem = structdef {
        field("modId", string)
        field("displayName", string)
    }

    // One Identification entry for the selected mod: its category (registry key) and the (mod, category,
    // item) name triple the backend reverse lookup navigates on.
    val ExplorerEntry = structdef {
        field("category", string)
        field("id", DebugLibrary.IdName)
    }

    // One mod's full structure: its selector identity plus its flat category-tagged entry list (the
    // frontend groups by category into the tree). fetch returns ALL mods in one round-trip; mod
    // switching is local frontend filtering.
    val ModExplorerData = structdef {
        field("mod", ModItem)
        field("entries", immutableList(ExplorerEntry))
    }

    init {
        // On-demand full-tree pull: every mod + its category->entry tree, freshly walked from the live
        // resolved compilation on each call. The backend stores nothing between calls.
        call("fetch", void, immutableList(ModExplorerData))

        // Backend -> frontend trigger only (no payload, nothing stored): fires on the pinned
        // resolved-compilation-change signal, debounced onto the protocol thread. The frontend re-fetches
        // on receipt.
        signal("invalidated", void)

        // Row navigation: the frontend calls with (row-id, target); the backend runs the reverse lookup +
        // editor navigation and answers true iff a source target resolved (loud no-op on a miss). Shares
        // DebugToolWindowModel's NavigationRequest/NavigationTarget vocabulary (single data design) so
        // explorer navigation routes through the identical backend DebugNavigation path.
        call("navigate", DebugToolWindowModel.NavigationRequest, bool)

        // Detail-pane supply for the ONE demo deep-inspector: given a shader-module entry's (mod, category,
        // item) triple, the backend resolves its registration source file and returns the text (nullable =
        // unresolved). Strictly scoped to the shader-module category; every other category's detail viewer
        // is SEEDED (the extensible frontend slot is designed for them but none are built here).
        call("loadShaderSource", DebugLibrary.IdName, string.nullable)
    }
}
