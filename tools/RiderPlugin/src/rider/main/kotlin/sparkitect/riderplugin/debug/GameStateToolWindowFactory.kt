package sparkitect.riderplugin.debug

import com.intellij.openapi.project.DumbAware
import com.intellij.openapi.project.Project
import com.intellij.openapi.wm.ToolWindow
import com.intellij.openapi.wm.ToolWindowFactory
import com.intellij.ui.content.ContentFactory

/**
 * The "Sparkitect Game State" tool window (D-08): always available, registered in plugin.xml with no
 * availability condition so its explanatory empty states are reachable whether or not a game is running.
 * It builds a single [GameStateTreePanel] that binds the republished `DebugToolWindowModel` Ext — the
 * backend is the sole game-channel client (Pitfall 5), so everything the window renders arrives over the
 * Solution-scoped protocol.
 */
class GameStateToolWindowFactory : ToolWindowFactory, DumbAware {
    override fun createToolWindowContent(project: Project, toolWindow: ToolWindow) {
        val panel = GameStateTreePanel(project, toolWindow)
        val content = ContentFactory.getInstance().createContent(panel, null, false)
        toolWindow.contentManager.addContent(content)
    }
}
