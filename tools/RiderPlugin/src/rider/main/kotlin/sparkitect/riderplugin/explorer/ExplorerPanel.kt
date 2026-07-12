package sparkitect.riderplugin.explorer

import com.intellij.openapi.Disposable
import com.intellij.openapi.project.Project
import com.intellij.openapi.ui.ComboBox
import com.intellij.openapi.util.Disposer
import com.intellij.openapi.wm.ToolWindow
import com.intellij.openapi.wm.ex.ToolWindowManagerListener
import com.intellij.ui.ColoredTreeCellRenderer
import com.intellij.ui.DoubleClickListener
import com.intellij.ui.JBSplitter
import com.intellij.ui.PopupHandler
import com.intellij.ui.SimpleTextAttributes
import com.intellij.ui.TreeSpeedSearch
import com.intellij.ui.components.JBLabel
import com.intellij.ui.components.JBScrollPane
import com.intellij.ui.treeStructure.Tree
import com.intellij.util.ui.JBUI
import com.jetbrains.rd.framework.RdTaskResult
import com.jetbrains.rd.ide.model.ExplorerEntry
import com.jetbrains.rd.ide.model.ExplorerModel
import com.jetbrains.rd.ide.model.ModExplorerData
import com.jetbrains.rd.ide.model.ModItem
import com.jetbrains.rd.ide.model.NavigationRequest
import com.jetbrains.rd.ide.model.NavigationTarget
import com.jetbrains.rd.ide.model.explorerModel
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.lifetime.LifetimeDefinition
import com.jetbrains.rider.projectView.solution
import sparkitect.debug.protocol.IdName
import sparkitect.riderplugin.explorer.detail.ShaderSourceDetailPanel
import java.awt.BorderLayout
import java.awt.CardLayout
import java.awt.event.MouseEvent
import javax.swing.DefaultComboBoxModel
import javax.swing.JComponent
import javax.swing.JMenuItem
import javax.swing.JPanel
import javax.swing.JPopupMenu
import javax.swing.tree.DefaultMutableTreeNode
import javax.swing.tree.DefaultTreeModel

/**
 * The Identification-structure explorer left pane. Binds the Solution-scoped [ExplorerModel]
 * Ext: a mod selector (each pane inspects ONE mod at a time, mirroring the Game State process
 * selector) and a 2-level category→entry tree of the selected mod's generated Identifications.
 * Double-click and a context menu fire the shared `navigate` call to source through the identical
 * backend reverse-lookup the Game State window uses.
 *
 * Master-detail: the tree is the left master of a [JBSplitter]; the right detail is a
 * `JPanel(CardLayout)` keyed by a `Map<category, DetailPanelFactory>`. Selecting an entry shows its
 * category's registered viewer (only the shader-module demo is built; every other category maps to the
 * default/empty card — the extensible slot is designed for them but none is built, not an IntelliJ EP).
 */
class ExplorerPanel(private val project: Project, toolWindow: ToolWindow) : JPanel(BorderLayout()) {

    private val lifetime: Lifetime
    private val model: ExplorerModel

    private val modSelector = ComboBox<ModItem>()
    private val treeRoot = DefaultMutableTreeNode()
    private val treeModel = DefaultTreeModel(treeRoot)
    private val tree = Tree(treeModel)
    private val treeScroll = JBScrollPane(tree)
    private val messageLabel = JBLabel().apply { border = JBUI.Borders.empty(12) }

    // Detail (right) pane: a card per category, swapped on tree selection. The Map is the extensible slot
    // (in-repo categories, NOT an IntelliJ EP); only the shader-module demo factory is registered.
    private val detailLayout = CardLayout()
    private val detailPane = JPanel(detailLayout)
    private val detailFactories = LinkedHashMap<String, DetailPanelFactory>()
    private val splitter = JBSplitter(false, 0.4f).apply {
        firstComponent = treeScroll
        secondComponent = detailPane
    }

    // Local frontend state: the last fetched per-mod tree and the current selection.
    // No round-trip to the backend for a mod switch — both are purely local.
    private var currentMods: List<ModExplorerData> = emptyList()
    private var selectedModId: String? = null

    init {
        val lifetimeDef = LifetimeDefinition()
        Disposer.register(toolWindow.disposable, Disposable { lifetimeDef.terminate() })
        lifetime = lifetimeDef.lifetime

        model = project.solution.explorerModel

        tree.isRootVisible = false
        tree.showsRootHandles = true
        tree.cellRenderer = RowRenderer()
        TreeSpeedSearch.installOn(tree) // Standard speed-search; default scope is the selected mod.

        add(buildTopBar(), BorderLayout.NORTH)
        add(splitter, BorderLayout.CENTER)

        buildDetailPane()
        wireSelector()
        wireNavigation()
        wireDetail()

        // Re-fetch on the backend's payload-less invalidation trigger (the fresh tree is pulled, not
        // pushed), and whenever this tool window becomes visible again (covers a change that happened
        // while it was hidden -- Invalidated only matters while something is listening for it).
        model.invalidated.advise(lifetime) { fetch() }
        project.messageBus.connect(toolWindow.disposable).subscribe(
            ToolWindowManagerListener.TOPIC,
            object : ToolWindowManagerListener {
                override fun toolWindowShown(shownWindow: ToolWindow) {
                    if (shownWindow.id == toolWindow.id) fetch()
                }
            },
        )

        showMessage("Loading Sparkitect mods…")
        fetch()
    }

    private fun buildTopBar(): JComponent {
        val bar = JPanel(BorderLayout())
        bar.border = JBUI.Borders.empty(4)
        modSelector.renderer = ModCellRenderer()
        bar.add(JBLabel("Mod: "), BorderLayout.WEST)
        bar.add(modSelector, BorderLayout.CENTER)
        return bar
    }

    private fun wireSelector() {
        // Purely local: no model.selectedMod round-trip. Re-renders from the already-fetched tree.
        modSelector.addActionListener {
            selectedModId = (modSelector.selectedItem as? ModItem)?.modId
            renderCurrent()
        }
    }

    // Pulls the full per-mod tree on demand. The backend stores nothing -- every call is a fresh walk.
    // Every terminal outcome ends loading: success renders the tree, fault/cancel render a visible
    // non-loading message distinct from the empty-success state, and a retry (re-invalidation or the
    // window becoming visible again) can still recover into a normal tree.
    private fun fetch() {
        model.fetch.start(lifetime, Unit).result.advise(lifetime) { taskResult ->
            when (taskResult) {
                is RdTaskResult.Success -> onFetched(taskResult.value)
                is RdTaskResult.Fault -> showMessage("Failed to load Sparkitect mods: ${taskResult.error.reasonAsText}")
                is RdTaskResult.Cancelled -> showMessage("Loading Sparkitect mods was cancelled.")
            }
        }
    }

    private fun onFetched(mods: List<ModExplorerData>) {
        currentMods = mods
        // Preserve the current local selection if its mod still exists in the fresh result, else default
        // to the first mod.
        if (selectedModId == null || mods.none { it.mod.modId == selectedModId }) {
            selectedModId = mods.firstOrNull()?.mod?.modId
        }
        syncSelectorToLocal()
        renderCurrent()
    }

    private fun syncSelectorToLocal() {
        modSelector.model = DefaultComboBoxModel(currentMods.map { it.mod }.toTypedArray())
        modSelector.selectedItem = currentMods.firstOrNull { it.mod.modId == selectedModId }?.mod
    }

    private fun renderCurrent() {
        if (currentMods.isEmpty()) {
            showMessage("No Sparkitect mods found in this solution.")
            return
        }
        val selected = currentMods.firstOrNull { it.mod.modId == selectedModId }
        if (selected == null) {
            showMessage("No Sparkitect mods found in this solution.")
            return
        }
        render(selected.entries)
    }

    private fun render(entries: List<ExplorerEntry>) {
        if (entries.isEmpty()) {
            val modName = (modSelector.selectedItem as? ModItem)?.displayName ?: "this mod"
            showMessage("No identifications found in $modName.")
            return
        }
        rebuildTree(entries)
        showTree()
    }

    private fun rebuildTree(entries: List<ExplorerEntry>) {
        treeRoot.removeAllChildren()
        // 2-level category→entry: group the flat entry list by category, then list its items.
        val byCategory = entries.groupBy { it.category }.toSortedMap()
        for ((category, items) in byCategory) {
            val categoryNode = DefaultMutableTreeNode(Row("$category (${items.size})", RowKind.CATEGORY))
            for (entry in items.sortedBy { it.id.item }) {
                categoryNode.add(DefaultMutableTreeNode(Row(entry.id.item, RowKind.ENTRY, navId = entry.id)))
            }
            treeRoot.add(categoryNode)
        }
        treeModel.reload()
    }

    private fun wireNavigation() {
        // Double-click -> type declaration, mirroring the Game State panel's row navigation.
        object : DoubleClickListener() {
            override fun onDoubleClick(event: MouseEvent): Boolean {
                val id = selectedNavId() ?: return false
                navigate(id, NavigationTarget.TypeDeclaration)
                return true
            }
        }.installOn(tree)

        val contextMenu = buildContextMenu()
        tree.addMouseListener(object : PopupHandler() {
            override fun invokePopup(comp: java.awt.Component, x: Int, y: Int) {
                val path = tree.getClosestPathForLocation(x, y) ?: return
                tree.selectionPath = path
                if (selectedNavId() != null) {
                    contextMenu.show(comp, x, y)
                }
            }
        })
    }

    private fun buildContextMenu(): JPopupMenu {
        val menu = JPopupMenu()
        val toDeclaration = JMenuItem("Go to Type Declaration").apply {
            addActionListener { selectedNavId()?.let { navigate(it, NavigationTarget.TypeDeclaration) } }
        }
        menu.add(toDeclaration)
        return menu
    }

    private fun navigate(id: IdName, target: NavigationTarget) {
        // Fire the shared rd call; the backend runs the reverse lookup + editor jump (no new logic).
        model.navigate.start(lifetime, NavigationRequest(id, target))
    }

    private fun selectedNavId(): IdName? {
        val node = tree.lastSelectedPathComponent as? DefaultMutableTreeNode ?: return null
        return (node.userObject as? Row)?.navId
    }

    private fun buildDetailPane() {
        // The default/empty card, shown for a category with no registered viewer (all but the demo).
        detailPane.add(
            JBLabel("Select a shader-module entry to view its source.").apply {
                border = JBUI.Borders.empty(12)
            },
            EMPTY_CARD,
        )
        // The one demo viewer occupies the extensible slot for the shader-module category; all other
        // categories fall through to the empty card above (no viewer built).
        registerDetail(ShaderSourceDetailPanel(project, model, lifetime))
        detailLayout.show(detailPane, EMPTY_CARD)
    }

    // Registers a per-category detail viewer into the extensible slot.
    private fun registerDetail(factory: DetailPanelFactory) {
        detailFactories[factory.category] = factory
        detailPane.add(factory.component, factory.category)
    }

    private fun wireDetail() {
        tree.addTreeSelectionListener {
            val node = tree.lastSelectedPathComponent as? DefaultMutableTreeNode
            showDetailFor((node?.userObject as? Row)?.navId)
        }
    }

    private fun showDetailFor(id: IdName?) {
        val factory = id?.let { detailFactories[it.category] }
        if (factory == null) {
            detailLayout.show(detailPane, EMPTY_CARD)
            return
        }
        factory.show(id)
        detailLayout.show(detailPane, factory.category)
    }

    private fun showMessage(text: String) {
        messageLabel.text = "<html>" + text.replace("\n", "<br/>") + "</html>"
        remove(splitter)
        add(messageLabel, BorderLayout.CENTER)
        revalidate()
        repaint()
    }

    private fun showTree() {
        remove(messageLabel)
        add(splitter, BorderLayout.CENTER)
        revalidate()
        repaint()
    }

    private companion object {
        // CardLayout key of the default/empty detail card.
        const val EMPTY_CARD = "__empty__"
    }

    private class ModCellRenderer : com.intellij.ui.SimpleListCellRenderer<ModItem>() {
        override fun customize(
            list: javax.swing.JList<out ModItem>,
            value: ModItem?,
            index: Int,
            selected: Boolean,
            hasFocus: Boolean,
        ) {
            text = value?.displayName ?: "No mod selected"
        }
    }

    private enum class RowKind { CATEGORY, ENTRY }

    private class Row(
        val label: String,
        val kind: RowKind,
        val navId: IdName? = null,
    )

    private class RowRenderer : ColoredTreeCellRenderer() {
        override fun customizeCellRenderer(
            tree: javax.swing.JTree,
            value: Any?,
            selected: Boolean,
            expanded: Boolean,
            leaf: Boolean,
            row: Int,
            hasFocus: Boolean,
        ) {
            val node = value as? DefaultMutableTreeNode ?: return
            val data = node.userObject as? Row ?: return
            val attributes = when (data.kind) {
                RowKind.CATEGORY -> SimpleTextAttributes.REGULAR_BOLD_ATTRIBUTES
                RowKind.ENTRY -> SimpleTextAttributes.REGULAR_ATTRIBUTES
            }
            append(data.label, attributes)
        }
    }
}

/**
 * A per-category detail viewer for the explorer's right pane. One card per [category]; [component]
 * is added to the detail [java.awt.CardLayout] under that key and [show] refreshes it for the selected
 * entry. The extensible slot is this in-repo interface — categories are known at build time, so a
 * `Map<category, DetailPanelFactory>` is used rather than an IntelliJ extension point.
 */
interface DetailPanelFactory {
    /** The registration category this viewer serves (e.g. `shader_module`). */
    val category: String

    /** The card shown when an entry of [category] is selected. */
    val component: JComponent

    /** Refresh the card for the selected entry's id triple. */
    fun show(id: IdName)
}
