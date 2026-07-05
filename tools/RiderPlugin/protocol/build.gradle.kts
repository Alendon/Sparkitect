import com.jetbrains.rd.generator.gradle.RdGenTask

// rdgen cross-process debug channel (58.1-04). One shared structdef library (model.lib.DebugLibrary)
// carries the single snapshot data design; the game channel (model.game.SparkitectDebugModel : Root)
// generates to C# twice (asis = ReSharper backend endpoint, reversed = engine endpoint) and the
// Solution republish (model.rider.DebugToolWindowModel : Ext(SolutionModel.Solution)) generates to
// C# (backend) + Kotlin (frontend). All three reference the same library so there is one data design
// across every endpoint (resolves Pitfall 5 — the frontend cannot socket the game).
plugins {
    id("com.jetbrains.rdgen")
    alias(libs.plugins.kotlinJvm)
}

repositories {
    maven("https://cache-redirector.jetbrains.com/intellij-dependencies")
    maven("https://cache-redirector.jetbrains.com/maven-central")
}

// The full package set every generator scans. rd-gen runs a SINGLE global toplevel scan (there is
// no per-generator package scoping), so every generator must list the union or roots go undiscovered.
val modelPackages = "model.game,model.rider,model.lib"

// Generated-stub output roots.
//   backend  — ReSharper backend endpoint (plugin C#); consumes game(asis) + ext(reversed) + lib.
//   engine   — game/engine endpoint; lands in the engine source tree so Sparkitect.dll compiles it.
//   frontend — Rider frontend (Kotlin); consumes the ext republish + lib. NOT the game Root (Pitfall 5).
val repoRoot = rootDir.parentFile.parentFile   // gradle rootDir = tools/RiderPlugin → repo root
val backendCsOutDir = layout.buildDirectory.dir("generated/csharp-backend").get().asFile
val engineCsOutDir = repoRoot.resolve("src/Sparkitect/Debug/generated")
// Library + game-channel Kotlin bind against rd-framework alone and are compile-checked here.
val frontendKtOutDir = layout.buildDirectory.dir("generated/kotlin").get().asFile
// The Ext republish Kotlin binds against the full Rider platform (SolutionModel runtime), so it is
// emitted separately and compiled by the plugin build (plan 08), not this standalone subproject.
val frontendExtKtOutDir = layout.buildDirectory.dir("generated/kotlin-ext").get().asFile

sourceSets {
    // Model definitions (fed to rdgen); compile against the rd-gen generator API + rider-model.
    main {
        kotlin.srcDir("src/main/kotlin")
    }
    // The GENERATED frontend Kotlin stubs compile against the rd-framework runtime. Kept out of
    // main so rdgen (which consumes main) has no cycle with the stubs it produces. Only the game
    // channel + library Kotlin are compiled here (they bind against rd-framework alone); the Ext
    // republish Kotlin binds against the full Rider platform and is compiled by the plugin (plan 08).
    test {
        kotlin.srcDir(frontendKtOutDir)
    }
}

rdgen {
    verbose = true

    // Game channel (Root): C# asis (backend endpoint) + C# reversed (engine endpoint).
    generator {
        language = "csharp"
        transform = "asis"
        packages = modelPackages
        root = "model.game.SparkitectDebugModel"
        directory = backendCsOutDir.absolutePath
    }
    generator {
        language = "csharp"
        transform = "reversed"
        packages = modelPackages
        root = "model.game.SparkitectDebugModel"
        directory = engineCsOutDir.absolutePath
    }

    // Solution republish (Ext on SolutionModel.Solution). Rooted on the platform IdeRoot so the Ext
    // attaches: C# reversed = backend perspective, Kotlin asis = frontend perspective.
    generator {
        language = "csharp"
        transform = "reversed"
        packages = modelPackages
        root = "com.jetbrains.rider.model.nova.ide.IdeRoot"
        directory = backendCsOutDir.absolutePath
    }
    generator {
        language = "kotlin"
        transform = "asis"
        packages = modelPackages
        root = "com.jetbrains.rider.model.nova.ide.IdeRoot"
        directory = frontendExtKtOutDir.absolutePath
    }

    // Shared library. Emitted symmetric for C# (usable both asis on the backend and reversed on the
    // engine) into the backend dir, reversed into the engine dir, and asis Kotlin for the frontend.
    generator {
        language = "csharp"
        transform = "symmetric"
        packages = modelPackages
        root = "model.lib.DebugLibrary"
        directory = backendCsOutDir.absolutePath
    }
    generator {
        language = "csharp"
        transform = "reversed"
        packages = modelPackages
        root = "model.lib.DebugLibrary"
        directory = engineCsOutDir.absolutePath
    }
    generator {
        language = "kotlin"
        transform = "asis"
        packages = modelPackages
        root = "model.lib.DebugLibrary"
        directory = frontendKtOutDir.absolutePath
    }
}

tasks.withType<RdGenTask> {
    group = "rdgen"
    description = "Regenerates the C# (backend + engine) and Kotlin (frontend) debug-channel stubs."
    val classPath = sourceSets["main"].runtimeClasspath
    dependsOn(classPath)
    classpath(classPath)
}

// The frontend Kotlin stubs compiled in the test source set must be generated first.
tasks.named("compileTestKotlin") {
    dependsOn("rdgen")
}

dependencies {
    implementation("com.jetbrains.rd:rd-gen:${libs.versions.rdGen.get()}")
    implementation(libs.kotlinStdLib)
    // rider-model (SolutionModel / IdeRoot) from the Rider platform's lib/rd, exposed by the root
    // project's riderModel configuration. Required to compile the Ext(SolutionModel.Solution) model.
    implementation(project(mapOf("path" to ":", "configuration" to "riderModel")))

    // Compiles the generated frontend Kotlin stubs (game channel + library) against rd-framework.
    testImplementation(libs.rdFramework)
    testImplementation(kotlin("test"))
    testImplementation("org.junit.jupiter:junit-jupiter:5.11.3")
    testRuntimeOnly("org.junit.platform:junit-platform-launcher")
}

tasks.named<Test>("test") {
    useJUnitPlatform()
    testLogging { showStandardStreams = true }
}
