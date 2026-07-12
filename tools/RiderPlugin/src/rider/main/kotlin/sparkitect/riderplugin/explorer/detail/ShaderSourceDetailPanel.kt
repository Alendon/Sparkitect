package sparkitect.riderplugin.explorer.detail

import com.intellij.openapi.fileTypes.PlainTextFileType
import com.intellij.openapi.project.Project
import com.intellij.ui.EditorTextField
import com.intellij.util.ui.JBUI
import com.jetbrains.rd.framework.RdTaskResult
import com.jetbrains.rd.ide.model.ExplorerModel
import com.jetbrains.rd.util.lifetime.Lifetime
import sparkitect.debug.protocol.IdName
import sparkitect.riderplugin.explorer.DetailPanelFactory
import java.awt.BorderLayout
import javax.swing.JComponent
import javax.swing.JPanel

/**
 * The ONE demo deep-inspector: renders a shader-module entry's registration source in a read-only
 * editor, proving the extensible detail-pane slot end-to-end (backend rd supply -> frontend viewer). Every
 * other category's viewer is not built. Read-only via [EditorTextField.setViewer]; no dependency on
 * `org.intellij.images` (the illustrative FUTURE image/texture case for the slot).
 */
class ShaderSourceDetailPanel(
    project: Project,
    private val model: ExplorerModel,
    private val lifetime: Lifetime,
) : DetailPanelFactory {

    override val category = "shader_module"

    private val editor = EditorTextField("", project, PlainTextFileType.INSTANCE).apply {
        setViewer(true)
        setOneLineMode(false)
    }

    private val root = JPanel(BorderLayout()).apply {
        border = JBUI.Borders.empty(4)
        add(editor, BorderLayout.CENTER)
    }

    override val component: JComponent get() = root

    override fun show(id: IdName) {
        editor.text = "Loading shader source…"
        // Backend resolves the entry's registration source file and returns its text (null = unresolved).
        model.loadShaderSource.start(lifetime, id).result.advise(lifetime) { result ->
            editor.text = (result as? RdTaskResult.Success)?.value
                ?: "No source available for ${id.item}."
        }
    }
}
