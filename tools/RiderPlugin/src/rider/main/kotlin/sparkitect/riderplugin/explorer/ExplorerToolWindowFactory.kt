package sparkitect.riderplugin.explorer

import com.intellij.openapi.project.DumbAware
import com.intellij.openapi.project.Project
import com.intellij.openapi.wm.ToolWindow
import com.intellij.openapi.wm.ToolWindowFactory
import com.intellij.ui.content.ContentFactory

/**
 * The "Sparkitect Explorer" tool window: always available (no availability condition) so its
 * explanatory empty states are reachable whether or not a solution has mods. It builds a single
 * [ExplorerPanel] that binds the Solution-scoped `ExplorerModel` Ext (backend-populated PSI walk);
 * a sibling of the Game State window, sharing nothing but the tree/selector/navigate patterns.
 */
class ExplorerToolWindowFactory : ToolWindowFactory, DumbAware {
    override fun createToolWindowContent(project: Project, toolWindow: ToolWindow) {
        val panel = ExplorerPanel(project, toolWindow)
        val content = ContentFactory.getInstance().createContent(panel, null, false)
        toolWindow.contentManager.addContent(content)
    }
}
