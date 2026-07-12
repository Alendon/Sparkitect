import org.apache.tools.ant.taskdefs.condition.Os
import org.jetbrains.intellij.platform.gradle.Constants
import java.io.ByteArrayOutputStream
import java.util.Properties

plugins {
    id("java")
    alias(libs.plugins.kotlinJvm)
    id("org.jetbrains.intellij.platform") version "2.11.0" // https://github.com/JetBrains/intellij-platform-gradle-plugin/releases
}

// Consumable configuration exposing the Rider platform's rider-model.jar (SolutionModel / IdeRoot)
// to the :protocol subproject, whose rdgen Ext(SolutionModel.Solution) republish model compiles
// against it. rider-model is NOT a Maven artifact — it lives in the downloaded SDK's lib/rd/ — so
// the protocol project cannot resolve it directly; it consumes this artifact instead (the pattern
// resharper-unity uses). platformPath is only valid after the platform is initialized.
val riderModel: Configuration by configurations.creating {
    isCanBeConsumed = true
    isCanBeResolved = false
}

// The riderModel artifact (which file backs this configuration) is wired below, after the run-mode
// vals (useLocalRider / explicitRiderHome / ProductVersion) are in scope — see "rdgen model source".

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

// --- rdgen model source: rider-model.jar (SolutionModel / IdeRoot definitions) ---
// The :protocol rdgen Ext(SolutionModel.Solution) compiles against rider-model.jar, which ships in the
// DOWNLOADED Rider SDK's lib/rd/ — NOT in a runtime IDE install. Resolution depends on run mode:
//   * Default (download): platformPath IS the downloaded SDK, so the jar is right there.
//   * Local run-IDE (rider-spark / useLocalRider): platformPath is the local runtime install, which
//     ships rd-gen.jar but not rider-model.jar. Download just the Rider distribution zip on demand
//     (same coordinate + repos the plugin uses for `rider(...)`, so a prior default build's cached zip
//     is reused; otherwise it auto-downloads) and extract lib/rd/rider-model.jar from it.
if (useLocalRider || explicitRiderHome != null) {
    val riderSdkForModel = configurations.create("riderSdkForModel") {
        isCanBeConsumed = false
        isCanBeResolved = true
    }
    dependencies {
        add("riderSdkForModel", "com.jetbrains.intellij.rider:riderRD:$ProductVersion@zip")
    }
    val extractRiderModel = tasks.register("extractRiderModel", Copy::class) {
        // singleFile triggers resolution → reuses the cached riderRD zip, or auto-downloads it.
        from(provider { zipTree(riderSdkForModel.singleFile) })
        include("lib/rd/rider-model.jar")
        into(layout.buildDirectory.dir("rider-model"))
    }
    artifacts {
        add(riderModel.name, layout.buildDirectory.file("rider-model/lib/rd/rider-model.jar")) {
            builtBy(extractRiderModel)
        }
    }
} else {
    artifacts {
        add(riderModel.name, provider {
            intellijPlatform.platformPath.resolve("lib/rd/rider-model.jar").toFile().also {
                check(it.isFile) { "rider-model.jar not found at $it (download-mode platform path)" }
            }
        }) {
            builtBy(Constants.Tasks.INITIALIZE_INTELLIJ_PLATFORM_PLUGIN)
        }
    }
}

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
        // Generated rd debug-channel frontend stubs. :protocol:rdgen emits the shared data
        // library Kotlin (sparkitect.debug.protocol.*) into protocol/build/generated/kotlin and the
        // Solution-Ext republish Kotlin (com.jetbrains.rd.ide.model.DebugToolWindowModel) into
        // protocol/build/generated/kotlin-ext. The Ext binds against the full Rider platform runtime,
        // so — unlike the standalone :protocol subproject, which only compile-checks the rd-framework
        // library Kotlin — the PLUGIN build is where the Ext Kotlin is compiled. Both
        // dirs are git-ignored build output regenerated on model change.
        kotlin.srcDir("protocol/build/generated/kotlin")
        kotlin.srcDir("protocol/build/generated/kotlin-ext")
        resources.srcDir("src/rider/main/resources")
    }
}

tasks.compileKotlin {
    compilerOptions { jvmTarget.set(org.jetbrains.kotlin.gradle.dsl.JvmTarget.JVM_21) }
    // The generated frontend/Ext stubs must exist before the plugin's Kotlin compiles.
    dependsOn(":protocol:rdgen")
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
    // The generated C# backend stubs (game channel + shared library + Solution-Ext republish) must
    // exist before msbuild runs: Directory.Build.props / the Rider csproj compile them in.
    dependsOn(":protocol:rdgen")
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
