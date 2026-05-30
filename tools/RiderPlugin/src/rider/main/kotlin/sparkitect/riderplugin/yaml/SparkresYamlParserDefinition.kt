package sparkitect.riderplugin.yaml

import com.intellij.lexer.DummyLexer
import com.intellij.lexer.Lexer
import com.intellij.openapi.project.Project
import com.intellij.psi.tree.IElementType
import com.jetbrains.rider.ideaInterop.fileTypes.RiderFileElementType
import com.jetbrains.rider.ideaInterop.fileTypes.RiderParserDefinitionBase

/**
 * Parser definition for [SparkresYamlLanguage]. The frontend deliberately does not parse these files —
 * a [DummyLexer] produces a single leaf so the frontend PSI is a stub and the ReSharper backend owns the
 * real structure, references and highlighting (streamed over the Rider markup/reference protocol). This
 * mirrors the Unity plugin's parser definition for its backend-owned YAML file types.
 */
class SparkresYamlParserDefinition :
    RiderParserDefinitionBase(SparkresYamlFileElementType, SparkresYamlFileType) {
    override fun createLexer(project: Project?): Lexer = DummyLexer(SparkresYamlFileElementType)

    companion object {
        private val SparkresYamlElementType: IElementType =
            IElementType("SPARKRES_YAML", SparkresYamlLanguage)

        val SparkresYamlFileElementType: RiderFileElementType =
            RiderFileElementType("SPARKRES_YAML_FILE", SparkresYamlLanguage, SparkresYamlElementType)
    }
}
