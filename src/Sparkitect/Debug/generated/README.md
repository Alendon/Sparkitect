# Debug channel — engine-side generated stubs (committed snapshot)

These `*.Generated.cs` files are the **C#-reversed** endpoint of the rd debug channel, produced by
`./gradlew :protocol:rdgen` in `tools/RiderPlugin` from the single rdgen model design
(`protocol/src/main/kotlin/model/{lib,game}`). Two roots land here:

- `DebugLibrary.Generated.cs` — the shared snapshot data design (`Sparkitect.Debug.Protocol`):
  `IdName`, `ModuleOrigin`, `StatelessFunctionKind`, `ModuleEntry`, `StatelessFunctionEntry`,
  `StateFrame`, `DebugSnapshot`, plus the generic extensible-entry floor (`DebugEntryField`/`GenericDebugEntry`).
- `SparkitectDebugModel.Generated.cs` — the game channel `RdExtBase` (`Sparkitect.Debug.Protocol.Game`)
  the engine hosts over a socket.

## Committed snapshot, not a build-time regeneration

The engine build (`dotnet build`/`dotnet run`) compiles these directly and must NOT depend on Gradle
or the Rider SDK. They are therefore **committed** and version-controlled, not regenerated per build.
Regenerate them only when the model changes: run `:protocol:rdgen` (or regenerate just these two
reversed roots) and commit the result. The single-model design is guarded by the `SerializationHash`
(`-7967425333677576098L`), identical across the backend-asis and engine-reversed C# by construction.
