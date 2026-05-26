---
uid: sparkitect.core.utility-infrastructure
title: Utility Infrastructure
description: Cross-cutting utilities used across engine modules and exposed to mod authors — discriminated unions, caller-context injection, metadata system, CLI handling, log enrichment.
---

Sparkitect's modules (DI, ECS, Modding, Graphics, Registry, GameState) share a small set of building-block types and source-generator patterns that none of them owns outright. This article documents those cross-cutting utilities: the discriminated-union types every fallible operation returns, the caller-context injection that threads call-site information into diagnostics, the metadata declaration system that lets mods extend the engine without subclassing, and the CLI argument surface available to mod authors.

Some of these are direct mod-authoring surface — `Result<,>` shows up the moment you call a Vulkan API, and `MetadataAttribute<T>` is how mods declare custom annotations. Others are infrastructure mods benefit from transparently: caller context attribution flows into allocation tracking so resource leaks surface with their call origin, and object tracking instruments resource lifetimes for leak detection. The sections that follow document each in turn, with mod-author relevance called out per section.

## Discriminated Unions

Sparkitect uses [Sundew.DiscriminatedUnions](https://github.com/sundews/Sundew.DiscriminatedUnions) as a bridge to native C# `union` types: a small source generator emits the case constructors and discriminator methods. Two utility types in `Sparkitect.Utils.DU` turn up across the engine — `Result<,>` for fallible operations, `Variant<>` for polymorphic inputs and returns.

### Result

`Result<TOk, TError>` carries either an `Ok(value)` or an `Error(value)` arm. The void-success sibling `Result<TError>` carries `Ok()` or `Error(value)`. Implicit conversions from each arm type keep producers terse — return either side without naming the constructor:

```csharp
public Result<Buffer, VkError> CreateBuffer(BufferDesc desc)
{
    if (Vk.CreateBuffer(desc, out var handle) is VkError err)
        return err;
    return new Buffer(handle);
}
```

Consumers match on the arm. For a single-arm guard, use `is`:

```csharp
if (context.CreateBuffer(desc) is not Result<Buffer, VkError>.Ok(var buffer))
    return;

// use buffer
```

When both arms are interesting, use `switch`:

```csharp
switch (context.CreateBuffer(desc))
{
    case Result<Buffer, VkError>.Ok(var buffer):
        Use(buffer);
        break;
    case Result<Buffer, VkError>.Error(var error):
        error.Throw();
        break;
}
```

`error.Throw()` is `ThrowHelperExtension` — an `Exception` extension marked `[DoesNotReturn]`. It lets error arms read as one-line statements while the compiler still understands control flow cannot continue past them.

### Variant

`Variant<T1, T2>` through `Variant<T1, T2, T3, T4, T5>` are polymorphic carriers — used when an API accepts or returns one of several unrelated types. Each arm is named `Of1`, `Of2`, … and implicit conversions work in both directions:

```csharp
Variant<Identification, string> input = id;      // takes the Of1 arm
Variant<Identification, string> input2 = "core"; // takes the Of2 arm

return input switch
{
    Variant<Identification, string>.Of1(var i) => i,
    Variant<Identification, string>.Of2(var name) => Resolve(name),
};
```

Variant is engine-internal more often than not — most code reaches for `Result<,>` or a hand-rolled Sundew partial record. Use it when the polymorphism is genuinely about input or return shape rather than error handling.

## Caller Context Injection

When a method needs concrete "who called me" diagnostic data, the conventional answer is to inspect a stack frame at runtime. Sparkitect answers the same question at compile time: a source generator rewrites call sites to pass an explicit location value. The primary current consumer is allocation tracking, where the tracker wants to attribute every resource back to its origin.

The performance gap is the point. `CallerContextGenerator` inserts a const value at each call site; the runtime cost is reading a string and an int. Reflective stack walking is dramatically more expensive and depends on debug symbols. The mechanism trades a tiny build-time cost for near-zero runtime cost.

Three pieces compose it: the `CallerContext` record carries the data (shape kept deliberately undocumented here — it may evolve for better diagnostic value), `[InjectCallerContext]` is the parameter attribute that opts a method in, and `CallerContextGenerator` is the rewriter.

### Authoring an opt-in method

Mark a `CallerContext` parameter with `[InjectCallerContext]` and give it a default. The generator handles the rest:

```csharp
public Result<Buffer, VkError> CreateBuffer(
    BufferDesc desc,
    [InjectCallerContext] CallerContext callerContext = default);
```

Callers see a single-argument method:

```csharp
var result = context.CreateBuffer(desc);
```

The generator intercepts that call site and rewrites it to pass a `CallerContext` derived from the source location. Inside the method body the value is opaque — hand it to a tracker, log enricher, or diagnostic sink without inspecting its shape:

```csharp
public Result<Buffer, VkError> CreateBuffer(
    BufferDesc desc,
    [InjectCallerContext] CallerContext callerContext = default)
{
    _tracker.Track(this, callerContext);
    // ...
}
```

## Object Lifetime Tracking

`IObjectTracker<T>` tracks live instances of a type with low overhead, so the engine can report counts and identify leaked allocations. The primary current consumer is Vulkan resource tracking; combined with `[InjectCallerContext]` from the previous section, every allocation can be attributed back to its call origin.

This is not normally something mods build against. The interface lives in `Sparkitect.Utils` for engine-internal use; mods see the effects — leak reports, resource counts in diagnostics — without authoring against `IObjectTracker<T>` themselves.

The thread-safe `ObjectTracker<T>` is the production implementation, with `NullObjectTracker<T>` as a no-op fallback when tracking is disabled. Both implement `IObjectTracker<T>` and are available via DI.

## Metadata Declaration System

The metadata system has two layers. The bottom layer — the **metadata entrypoint system** — is a foundational mechanism any engine component uses to attach typed data to specific `Identification` values. The top layer — a **source generator** — turns a declarative "attribute as metadata" pattern into entrypoint code automatically. New and existing engine components reach for the entrypoint system whenever they need per-`Identification` data mapping; the SG layer is a convenience when a class-with-attributes is the natural source of that data.

### The metadata entrypoint system

The contract is `ApplyMetadataEntrypoint<TMetadata>` — subclass it, override `CollectMetadata(Dictionary<Identification, TMetadata>)`, and mark the subclass with `[ApplyMetadataEntrypointAttribute<TMetadata>]` for discovery:

```csharp
[ApplyMetadataEntrypointAttribute<MyMetadata>]
public sealed class MyMetadataEntrypoint : ApplyMetadataEntrypoint<MyMetadata>
{
    public override void CollectMetadata(Dictionary<Identification, MyMetadata> metadata)
    {
        // populate metadata per Identification
    }
}
```

Multiple metadata categories run in parallel; each has its own entrypoint and its own dictionary. Entrypoints can be hand-written or SG-generated. Engine consumers obtain the populated dictionary through DI's entrypoint system — see the [Entrypoint System](xref:sparkitect.core.dependency-injection) section in the DI article for consumer-side mechanics.

### Attribute-driven metadata

A source generator builds entrypoints from a declarative pattern. Define a **metadata type** whose constructor parameters are the attribute types (or arrays of them) to pull from each decorated site, and a **marker attribute** deriving from `MetadataAttribute<TMetadata>` and itself bearing `[MetadataCategoryMarker]`:

```csharp
public class MyMetadata(
    MyConfigAttribute config,
    MyTagAttribute[] tags);

[MetadataCategoryMarker]
public sealed class MyMetadataAttribute : MetadataAttribute<MyMetadata>;
```

Apply the marker plus the data-bearing attributes at a use site. The type must implement `IHasIdentification` so the SG has a stable dictionary key:

```csharp
[MyMetadata]
[MyConfig("primary")]
[MyTag("renderable")]
[MyTag("dynamic")]
public partial class SomeEntity : IHasIdentification
{
    public static Identification StaticId => /* ... */;
}
```

The SG walks the marker's base to find `TMetadata`, inspects its constructor, matches each parameter against attributes on the decorated type, and emits an `ApplyMetadataEntrypoint<TMetadata>` that calls `new TMetadata(...)` with the matched values. Constructor matching follows two conventions: `T` pulls a single `T` attribute (nullable for optional); `T[]` pulls all `T` instances. Only one instance of a given marker kind can appear per type (standard attribute uniqueness), but different marker kinds can coexist on the same type — useful when an engine-generated entrypoint and a user-facing config marker both target it.

## CLI Argument Handling

Sparkitect exposes process-level CLI arguments through a single DI-registered service so mods don't have to re-parse `args[]` themselves. Engine bootstrap initializes it once at startup with the combined engine + mod argument list.

Two pieces compose the API: `ICliArgumentHandler` is the query surface (`HasArgument`, `TryGetArgumentValue`, `TryGetArgumentValues`), and `CliArgValue` in `Sparkitect.Utils.DU` is the underlying value shape — a discriminated union with `Flag`, `Single(string)`, and `Multi(IReadOnlyList<string>)` arms. Direct use of the union is uncommon; most consumers go through the `TryGet*` helpers which unwrap it for you.

Argument syntax: `-flag` parses to `Flag`; `-key=value` parses to `Single(value)`; `-key=value1;value2` parses to `Multi([value1, value2])`. Keys are case-insensitive.

Inject `ICliArgumentHandler` like any other service:

```csharp
public class MyManager(ICliArgumentHandler cliArgs) : IMyManager
{
    public void Initialize()
    {
        if (cliArgs.HasArgument("my-mod-debug"))
            // enable debug behavior

        if (cliArgs.TryGetArgumentValue("my-mod-config", out var config))
            // use the config value
    }
}
```
