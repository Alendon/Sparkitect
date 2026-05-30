package sparkitect.riderplugin.yaml

import com.jetbrains.rider.ideaInterop.fileTypes.RiderLanguageFileTypeBase
import javax.swing.Icon

/**
 * Rider-backed file type for `*.sparkres.yaml`. Extending [RiderLanguageFileTypeBase] is what makes Rider
 * track the open file as a backend document, so the ReSharper backend's YAML PSI, registration
 * references and daemon highlightings are surfaced into the editor (go-to-declaration, highlighting)
 * instead of the file being opened by the stock frontend YAML editor that ignores the backend.
 */
object SparkresYamlFileType : RiderLanguageFileTypeBase(SparkresYamlLanguage) {
    override fun getName(): String = "SparkresYaml"
    override fun getDescription(): String = "Sparkitect resource file"
    override fun getDefaultExtension(): String = "sparkres.yaml"
    override fun getIcon(): Icon? = null
}
