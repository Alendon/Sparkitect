pluginManagement {
    // rd version drives the rdgen Gradle plugin, the rd-gen/rd-framework artifacts, and
    // the engine-side JetBrains.RdFramework NuGet. Pinned to the bundled Rider platform
    // line (ProductVersion in gradle.properties) so the wire ABI matches end to end.
    val rdVersion: String by settings

    repositories {
        maven { setUrl("https://cache-redirector.jetbrains.com/plugins.gradle.org") }
        maven { setUrl("https://cache-redirector.jetbrains.com/intellij-dependencies") }
        maven { setUrl("https://cache-redirector.jetbrains.com/maven-central") }
    }

    plugins {
        id("com.jetbrains.rdgen") version rdVersion
    }

    resolutionStrategy {
        eachPlugin {
            // Gradle maps a plugin id to '{id}:{id}.gradle.plugin:{version}', which does
            // not resolve for rdgen. Point it at the real rd-gen coordinates instead.
            // Reference rdVersion directly: subprojects apply the plugin without a version
            // (it comes from the plugins{} default above), so requested.version is null here.
            if (requested.id.id == "com.jetbrains.rdgen") {
                useModule("com.jetbrains.rd:rd-gen:$rdVersion")
            }
        }
    }
}

rootProject.name = "sparkitect"

// rdgen cross-process channel model (58.1). The game/backend channel is a standalone
// Root() model; a Solution-scoped Ext republish arrives in a later plan.
include(":protocol")
