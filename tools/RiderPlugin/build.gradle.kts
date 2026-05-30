import org.apache.tools.ant.taskdefs.condition.Os
import java.io.ByteArrayOutputStream
import java.util.Properties

plugins {
    id("java")
    alias(libs.plugins.kotlinJvm)
    id("org.jetbrains.intellij.platform") version "2.11.0" // https://github.com/JetBrains/intellij-platform-gradle-plugin/releases
}

val isWindows = Os.isFamily(Os.FAMILY_WINDOWS)

// Per-developer override (git-ignored, see local.user.properties.example).
// Opt in with `useLocalRider=true` to use a locally-installed Rider for :runIde
// instead of downloading the IDE. The install location is taken from the
// RIDER_LOCAL_HOME environment variable, or from an explicit `riderLocalHome`
// property when you want to point at a specific install. Default => download.
val localUserProperties = Properties().apply {
    val f = rootProject.file("local.user.properties")
    if (f.exists()) f.inputStream().use { load(it) }
}
val useLocalRider = localUserProperties.getProperty("useLocalRider")?.toBoolean() == true
val explicitRiderHome = localUserProperties.getProperty("riderLocalHome")?.trim()?.takeIf { it.isNotEmpty() }
val envRiderHome = providers.environmentVariable("RIDER_LOCAL_HOME").orNull

val DotnetSolution: String by project
val BuildConfiguration: String by project
val ProductVersion: String by project
val DotnetPluginId: String by project
val PluginVersion: String by project

version = PluginVersion

allprojects {
    repositories {
        maven { setUrl("https://cache-redirector.jetbrains.com/maven-central") }
    }
}

repositories {
    intellijPlatform {
        defaultRepositories()
        jetbrainsRuntime()
    }
}

tasks.wrapper {
    gradleVersion = "8.13"
    distributionType = Wrapper.DistributionType.ALL
    distributionUrl = "https://cache-redirector.jetbrains.com/services.gradle.org/distributions/gradle-${gradleVersion}-all.zip"
}

sourceSets {
    main {
        java.srcDir("src/rider/main/java")
        kotlin.srcDir("src/rider/main/kotlin")
        resources.srcDir("src/rider/main/resources")
    }
}

tasks.compileKotlin {
    compilerOptions { jvmTarget.set(org.jetbrains.kotlin.gradle.dsl.JvmTarget.JVM_21) }
}

val setBuildTool by tasks.registering {
    doLast {
        extra["executable"] = "dotnet"
        val args = mutableListOf("msbuild")
        args.add(DotnetSolution)
        args.add("/p:Configuration=${BuildConfiguration}")
        args.add("/p:HostFullIdentifier=")
        extra["args"] = args
    }
}

val compileDotNet by tasks.registering {
    dependsOn(setBuildTool)
    doLast {
        val executable: String by setBuildTool.get().extra
        val arguments = (setBuildTool.get().extra["args"] as List<String>).toMutableList()
        arguments.add("/t:Restore;Rebuild")
        exec {
            executable(executable)
            args(arguments)
            workingDir(rootDir)
        }
    }
}

dependencies {
    intellijPlatform {
        when {
            // Explicit path override: use exactly this locally-installed Rider home.
            explicitRiderHome != null            -> local(file(explicitRiderHome))
            // Opt-in local install, location from the RIDER_LOCAL_HOME environment variable.
            useLocalRider && envRiderHome != null -> local(file(envRiderHome))
            useLocalRider                         -> error("useLocalRider=true but neither riderLocalHome nor RIDER_LOCAL_HOME is set")
            // Default: download the IDE and its matching runtime (portable for all contributors).
            else                                  -> { rider(ProductVersion, useInstaller = false); jetbrainsRuntime() }
        }

        // Bundled Rider plugin that ships JetBrains.ReSharper.Plugins.Yaml.dll
        // (rider-unity/dotnet/). The YAML PSI types used by the .sparkres.yaml
        // reference provider are not part of the ReSharper SDK NuGet metapackage;
        // the backend csproj references the DLL from this plugin's install folder.
        bundledPlugin("com.intellij.resharper.unity")

        // Frontend YAML language plugin. The Sparkres file type borrows its
        // YAMLSyntaxHighlighterFactory and the plugin <depends> on it at runtime.
        bundledPlugin("org.jetbrains.plugins.yaml")
    }
}

tasks.runIde {
    // Match Rider's default heap size of 1.5Gb (the runIde default is 512Mb).
    maxHeapSize = "1500m"
}

// No searchable settings UI ships in this plugin, so skip the headless-IDE indexing step.
tasks.buildSearchableOptions {
    enabled = false
}

tasks.prepareSandbox {
    dependsOn(compileDotNet)

    val outputFolder = "${rootDir}/src/dotnet/${DotnetPluginId}/bin/${DotnetPluginId}.Rider/${BuildConfiguration}"
    val dllFiles = listOf(
        "$outputFolder/${DotnetPluginId}.dll",
        "$outputFolder/${DotnetPluginId}.pdb",
    )

    dllFiles.forEach { f ->
        from(file(f)) { into("${rootProject.name}/dotnet") }
    }

    doLast {
        dllFiles.forEach { f ->
            val file = file(f)
            if (!file.exists()) throw RuntimeException("File $file does not exist")
        }
    }
}
