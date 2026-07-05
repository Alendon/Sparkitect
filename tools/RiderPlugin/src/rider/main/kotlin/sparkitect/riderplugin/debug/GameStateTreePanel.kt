package sparkitect.riderplugin.debug

import com.intellij.openapi.Disposable
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.Disposer
import com.intellij.openapi.wm.ToolWindow
import com.intellij.ui.ColoredTreeCellRenderer
import com.intellij.ui.DoubleClickListener
import com.intellij.ui.PopupHandler
import com.intellij.ui.SimpleTextAttributes
import com.intellij.ui.TreeSpeedSearch
import com.intellij.ui.components.JBLabel
import com.intellij.ui.components.JBScrollPane
import com.intellij.ui.treeStructure.Tree
import com.intellij.util.ui.JBUI
import com.jetbrains.rd.ide.model.DebugToolWindowModel
import com.jetbrains.rd.ide.model.NavigationRequest
import com.jetbrains.rd.ide.model.NavigationTarget
import com.jetbrains.rd.ide.model.ProcessInfo
import com.jetbrains.rd.ide.model.debugToolWindowModel
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.lifetime.LifetimeDefinition
import com.jetbrains.rider.projectView.solution
import sparkitect.debug.protocol.DebugSnapshot
import sparkitect.debug.protocol.IdName
import sparkitect.debug.protocol.ModuleEntry
import sparkitect.debug.protocol.ModuleOrigin
import sparkitect.debug.protocol.StateFrame
import sparkitect.debug.protocol.StatelessFunctionEntry
import sparkitect.debug.protocol.StatelessFunctionKind
import java.awt.BorderLayout
import java.awt.event.MouseEvent
import javax.swing.JComponent
import javax.swing.JMenuItem
import javax.swing.JPanel
import javax.swing.JPopupMenu
import javax.swing.tree.DefaultMutableTreeNode
import javax.swing.tree.DefaultTreeModel
import javax.swing.tree.TreePath
import com.intellij.openapi.ui.ComboBox
import javax.swing.DefaultComboBoxModel

/**
 * The Game State Stack inspector (D-01..D-12). Binds the republished [DebugToolWindowModel] Ext: a
 * process selector (D-07), the composed state stack rendered top-of-stack-first with enriched headers
 * (D-04), per-frame Modules with origin badges + expandable one-hop requirers (D-01/D-02), a "Mods added"
 * section (D-03) and per-frame StatelessFunctions (D-05), standard tree speed-search only (D-12), and row
 * navigation to source through the backend reverse lookup (D-10). Version drift degrades loudly: a
 * mismatched protocol marker replaces the whole tree with a banner (D-09) — never a half-rendered tree.
 *
 * The frontend cannot socket the game (Pitfall 5); every value here arrives over the Solution protocol.
 */
class GameStateTreePanel(project: Project, toolWindow: ToolWindow) : JPanel(BorderLayout()) {

    private companion object {
        /** The debug-view protocol version this plugin speaks (D-09). Must match the engine's
         *  DebugSnapshotBuilder.ProtocolVersion; a mismatch is surfaced loudly, never half-rendered. */
        const val PLUGIN_PROTOCOL_VERSION = 1
    }

    private val lifetime: Lifetime
    private val model: DebugToolWindowModel

    private val processSelector = ComboBox<ProcessItem>()
    private val treeRoot = DefaultMutableTreeNode()
    private val treeModel = DefaultTreeModel(treeRoot)
    private val tree = Tree(treeModel)
    private val treeScroll = JBScrollPane(tree)
    private val messageLabel = JBLabel().apply { border = JBUI.Borders.empty(12) }

    // Guards the selector<->model echo so syncing the combo from the model does not re-fire selection.
    private var syncingSelection = false

    init {
        val lifetimeDef = LifetimeDefinition()
        Disposer.register(toolWindow.disposable, Disposable { lifetimeDef.terminate() })
        lifetime = lifetimeDef.lifetime

        model = project.solution.debugToolWindowModel

        tree.isRootVisible = false
        tree.showsRootHandles = true
        tree.cellRenderer = RowRenderer()
        TreeSpeedSearch.installOn(tree) // D-12: standard speed-search only, no custom filter/export.

        add(buildTopBar(), BorderLayout.NORTH)
        add(treeScroll, BorderLayout.CENTER)

        wireSelector()
        wireNavigation()

        // D-07: keep the selector populated with the backend's discovered-process list.
        model.processes.advise(lifetime) { processes -> onProcessesChanged(processes) }
        // Reflect a backend-driven default selection (auto-select of the active process) into the combo.
        model.selectedProcess.advise(lifetime) { pid -> syncSelectorTo(pid) }
        // D-06: the cached snapshot is always current (push-on-change); re-render on every publish.
        model.snapshot.advise(lifetime) { snapshot -> render(snapshot) }

        renderEmptyOrCurrent()
    }

    private fun buildTopBar(): JComponent {
        val bar = JPanel(BorderLayout())
        bar.border = JBUI.Borders.empty(4)
        processSelector.renderer = ProcessCellRenderer()
        bar.add(JBLabel("Process: "), BorderLayout.WEST)
        bar.add(processSelector, BorderLayout.CENTER)
        return bar
    }

    private fun wireSelector() {
        processSelector.addActionListener {
            if (syncingSelection) return@addActionListener
            val pid = (processSelector.selectedItem as? ProcessItem)?.pid
            model.selectedProcess.set(pid)
        }
    }

    private fun onProcessesChanged(processes: List<ProcessInfo>) {
        syncingSelection = true
        try {
            val items = processes.map { ProcessItem(it.pid, it.engineVersion) }
            processSelector.model = DefaultComboBoxModel(items.toTypedArray())
            val current = model.selectedProcess.value
            processSelector.selectedItem = items.firstOrNull { it.pid == current }
        } finally {
            syncingSelection = false
        }
        renderEmptyOrCurrent()
    }

    private fun syncSelectorTo(pid: Int?) {
        syncingSelection = true
        try {
            val items = (0 until processSelector.itemCount).map { processSelector.getItemAt(it) }
            processSelector.selectedItem = items.firstOrNull { it.pid == pid }
        } finally {
            syncingSelection = false
        }
        renderEmptyOrCurrent()
    }

    private fun renderEmptyOrCurrent() {
        val snapshot = model.snapshot.value
        if (snapshot != null) {
            render(snapshot)
        } else if (processSelector.itemCount == 0) {
            // D-08: nothing discovered.
            showMessage("No Sparkitect process found.\nStart a game with --debug-channel to inspect its Game State Stack.")
        } else {
            // D-08: a process exists but no channel snapshot has arrived (module off / pre-58.1 engine).
            showMessage("No debug channel on this process (module disabled or pre-58.1 engine).")
        }
    }

    private fun render(snapshot: DebugSnapshot?) {
        if (snapshot == null) {
            renderEmptyOrCurrent()
            return
        }
        // D-09 fail-loud: version drift replaces the tree with a banner — never a half-rendered tree.
        if (snapshot.protocolVersion != PLUGIN_PROTOCOL_VERSION) {
            showMessage(
                "Engine debug view v${snapshot.protocolVersion} not supported (plugin speaks v$PLUGIN_PROTOCOL_VERSION).\n" +
                    "Update the Sparkitect plugin or engine so their debug protocol versions match."
            )
            return
        }
        rebuildTree(snapshot)
        showTree()
    }

    private fun rebuildTree(snapshot: DebugSnapshot) {
        treeRoot.removeAllChildren()
        // D-04: frames arrive top-of-stack first; the first is the active frame.
        snapshot.frames.forEachIndexed { index, frame ->
            treeRoot.add(buildFrameNode(frame, active = index == 0))
        }
        treeModel.reload()
        expandTopFrame()
    }

    private fun buildFrameNode(frame: StateFrame, active: Boolean): DefaultMutableTreeNode {
        val marker = if (active) "▶ " else "" // active frame visually marked (D-04)
        val header = "$marker${frame.stateId.item} — ${frame.moduleCount} modules, ${frame.modCount} mods"
        val node = DefaultMutableTreeNode(Row(header, RowKind.FRAME))

        // D-03: sibling "Modules" and "Mods added" sections, plus per-frame StatelessFunctions (D-05).
        node.add(buildModulesSection(frame))
        node.add(buildModsSection(frame))
        node.add(buildStatelessFunctionsSection(frame))
        return node
    }

    private fun buildModulesSection(frame: StateFrame): DefaultMutableTreeNode {
        val section = DefaultMutableTreeNode(Row("Modules (${frame.modules.size})", RowKind.SECTION))
        // D-01: the COMPLETE composed set (Pitfall 4), each badged by origin.
        for (module in frame.modules) {
            section.add(buildModuleNode(module))
        }
        return section
    }

    private fun buildModuleNode(module: ModuleEntry): DefaultMutableTreeNode {
        val node = DefaultMutableTreeNode(
            Row(module.id.item, RowKind.MODULE, navId = module.id, badge = originBadge(module.origin))
        )
        // D-02: one-hop requirers behind an expandable detail node (not inline).
        if (module.requirers.isNotEmpty()) {
            val requirers = DefaultMutableTreeNode(Row("Required by (${module.requirers.size})", RowKind.SECTION))
            for (requirer in module.requirers) {
                requirers.add(DefaultMutableTreeNode(Row(requirer.item, RowKind.REQUIRER, navId = requirer)))
            }
            node.add(requirers)
        }
        return node
    }

    private fun buildModsSection(frame: StateFrame): DefaultMutableTreeNode {
        val section = DefaultMutableTreeNode(Row("Mods added (${frame.addedMods.size})", RowKind.SECTION))
        for (mod in frame.addedMods) {
            section.add(DefaultMutableTreeNode(Row(mod, RowKind.MOD)))
        }
        return section
    }

    private fun buildStatelessFunctionsSection(frame: StateFrame): DefaultMutableTreeNode {
        val section = DefaultMutableTreeNode(
            Row("Stateless Functions (${frame.statelessFunctions.size})", RowKind.SECTION)
        )
        // D-05: runtime-truth SF sets, grouped by schedule kind under the state frame.
        for (kind in StatelessFunctionKind.values()) {
            val ofKind = frame.statelessFunctions.filter { it.kind == kind }
            if (ofKind.isEmpty()) continue
            val group = DefaultMutableTreeNode(Row("${kindLabel(kind)} (${ofKind.size})", RowKind.SECTION))
            for (sf in ofKind) {
                group.add(DefaultMutableTreeNode(Row(sf.id.item, RowKind.STATELESS_FUNCTION, navId = sf.id)))
            }
            section.add(group)
        }
        return section
    }

    private fun expandTopFrame() {
        if (treeRoot.childCount == 0) return
        val firstFrame = treeRoot.getChildAt(0) as DefaultMutableTreeNode
        tree.expandPath(TreePath(arrayOf<Any>(treeRoot, firstFrame)))
    }

    private fun wireNavigation() {
        // D-10 double-click -> type declaration.
        object : DoubleClickListener() {
            override fun onDoubleClick(event: MouseEvent): Boolean {
                val id = selectedNavId() ?: return false
                navigate(id, NavigationTarget.TypeDeclaration)
                return true
            }
        }.installOn(tree)

        // D-10 context menu -> registration site (+ type declaration). A PopupHandler drives a plain
        // Swing menu so no ActionManager registration is needed; it handles cross-platform triggers.
        val contextMenu = buildContextMenu()
        tree.addMouseListener(object : PopupHandler() {
            override fun invokePopup(comp: java.awt.Component, x: Int, y: Int) {
                val path = tree.getClosestPathForLocation(x, y) ?: return
                tree.selectionPath = path
                if (selectedNavId() != null) contextMenu.show(comp, x, y)
            }
        })
    }

    private fun buildContextMenu(): JPopupMenu {
        // Registration-site navigation is intentionally not offered: declaration and registration are
        // directly tied for every row kind, so one target suffices.
        val menu = JPopupMenu()
        val toDeclaration = JMenuItem("Go to Type Declaration").apply {
            addActionListener { selectedNavId()?.let { navigate(it, NavigationTarget.TypeDeclaration) } }
        }
        menu.add(toDeclaration)
        return menu
    }

    private fun navigate(id: IdName, target: NavigationTarget) {
        // Fire-and-forget over the Ext; the backend runs the reverse lookup + editor jump and answers
        // whether a target resolved (a miss is logged loudly backend-side, D-11).
        model.navigate.start(lifetime, NavigationRequest(id, target))
    }

    private fun selectedNavId(): IdName? {
        val node = tree.lastSelectedPathComponent as? DefaultMutableTreeNode ?: return null
        return (node.userObject as? Row)?.navId
    }

    private fun showMessage(text: String) {
        messageLabel.text = "<html>" + text.replace("\n", "<br/>") + "</html>"
        remove(treeScroll)
        add(messageLabel, BorderLayout.CENTER)
        revalidate()
        repaint()
    }

    private fun showTree() {
        remove(messageLabel)
        add(treeScroll, BorderLayout.CENTER)
        revalidate()
        repaint()
    }

    private fun originBadge(origin: ModuleOrigin): String = when (origin) {
        ModuleOrigin.AddedDirect -> "direct"
        ModuleOrigin.AddedTransitive -> "transitive"
        ModuleOrigin.InheritedFromParent -> "inherited"
        ModuleOrigin.AutoActivatedIntegration -> "auto-activated"
    }

    private fun kindLabel(kind: StatelessFunctionKind): String = when (kind) {
        StatelessFunctionKind.PerFrame -> "Per frame"
        StatelessFunctionKind.TransitionEnter -> "On enter"
        StatelessFunctionKind.TransitionExit -> "On exit"
    }

    private data class ProcessItem(val pid: Int, val engineVersion: String) {
        override fun toString(): String = "pid $pid — $engineVersion"
    }

    private class ProcessCellRenderer : com.intellij.ui.SimpleListCellRenderer<ProcessItem>() {
        override fun customize(
            list: javax.swing.JList<out ProcessItem>,
            value: ProcessItem?,
            index: Int,
            selected: Boolean,
            hasFocus: Boolean,
        ) {
            text = value?.toString() ?: "No process selected"
        }
    }

    private enum class RowKind { FRAME, SECTION, MODULE, MOD, STATELESS_FUNCTION, REQUIRER }

    private class Row(
        val label: String,
        val kind: RowKind,
        val navId: IdName? = null,
        val badge: String? = null,
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
                RowKind.FRAME -> SimpleTextAttributes.REGULAR_BOLD_ATTRIBUTES
                RowKind.SECTION -> SimpleTextAttributes.GRAYED_ATTRIBUTES
                else -> SimpleTextAttributes.REGULAR_ATTRIBUTES
            }
            append(data.label, attributes)
            data.badge?.let { append("  [$it]", SimpleTextAttributes.GRAY_ITALIC_ATTRIBUTES) }
        }
    }
}
