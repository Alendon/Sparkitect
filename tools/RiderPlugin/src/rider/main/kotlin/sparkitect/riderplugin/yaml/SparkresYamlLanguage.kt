package sparkitect.riderplugin.yaml

import com.intellij.lang.Language

/**
 * Frontend language for `.sparkres.yaml` resource files. Rider's frontend already has stock YAML
 * support, but that editor is fully frontend-owned and never consults the ReSharper backend, so the
 * backend-computed registration references and daemon highlightings stay invisible. Mapping these files
 * to a dedicated Rider-backed language (paired with [SparkresYamlFileType] and a dummy-lexer parser
 * definition) makes the frontend defer their PSI, references and highlighting to the backend — mirroring
 * how the Unity plugin handles `.meta`/`.asset` YAML. Plain `.yaml` files are unaffected; only the
 * `*.sparkres.yaml` pattern routes here.
 */
object SparkresYamlLanguage : Language("SparkresYaml")
