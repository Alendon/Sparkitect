using Sparkitect.DI.GeneratorAttributes;

namespace Sparkitect.Modding;

/// <summary>
/// Marks a class as a registry implementation. Triggers source generation of keyed factory and configurator.
/// The class must implement <see cref="IRegistry"/> and be partial.
/// </summary>
/// <remarks> Marker attributes inherited by this class will be treated as specialized Registries and not be
/// processed by the Registry SG beyond their basic one time setup (eg no registry method parsing)</remarks>
public class RegistryAttribute : Attribute
{
    /// <summary>
    /// Unique registry category identifier.
    /// </summary>
    public required string Identifier { get; set; }

    /// <summary>
    /// When true, disables the default registry pipeline (registration attribute generation, keyed factory, etc.).
    /// Use this for registries whose registration targets are not types — e.g., stateless function registries
    /// that register methods. The external source generator is responsible for handling registration instead.
    /// Standard type-based registries (including those with <see cref="RegistryMethodAttribute"/>) should leave
    /// this as false.
    /// </summary>
    public bool External { get; set; } = false;
}

/// <summary>
/// Assembly-level attribute for associating metadata types with registries.
/// </summary>
/// <typeparam name="TMetadata">The metadata type.</typeparam>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class RegistryMetadataAttribute<TMetadata> : Attribute where TMetadata : class;

/// <summary>
/// Marks a registry method that registers objects. Source generators create registration attributes
/// (e.g., [RegisterItem("key")]) for each marked method, which mods use to register objects.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class RegistryMethodAttribute : Attribute;

/// <summary>
/// Declaratively specifies a resource file slot required by a registry.
/// Resource files are loaded from mod archives and made available through <see cref="IResourceManager"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class UseResourceFileAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the file key used in YAML resource declarations.
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// Gets or sets whether this resource file is required. If true, missing file causes an error.
    /// </summary>
    public bool Required { get; set; } = false;

    /// <summary>
    /// Gets or sets whether this is the primary file slot for simplified single-file YAML syntax.
    /// </summary>
    public bool Primary { get; set; } = false;
}

/// <summary>
/// Declaratively specifies a typed resource file slot required by a registry.
/// Resource files are loaded from mod archives and made available through <see cref="IResourceManager"/>.
/// </summary>
/// <typeparam name="TResource">The resource file type implementing <see cref="IResourceFile"/>.</typeparam>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class UseResourceFileAttribute<TResource> : Attribute where TResource : class, IResourceFile
{
    /// <summary>
    /// Gets or sets the file key used in YAML resource declarations.
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// Gets or sets whether this resource file is required. If true, missing file causes an error.
    /// </summary>
    public bool Required { get; set; } = false;

    /// <summary>
    /// Gets or sets whether this is the primary file slot for simplified single-file YAML syntax.
    /// </summary>
    public bool Primary { get; set; } = false;
}

/// <summary>
/// Marks an interface as having a registry-exclusive facade. The facade interface is only accessible
/// within registry contexts (e.g., registry methods, Registrations implementations) and not through normal DI.
/// </summary>
/// <typeparam name="TFacade">The exclusive facade interface type.</typeparam>
[AttributeUsage(AttributeTargets.Interface)]
public class RegistryFacadeAttribute<TFacade> : FacadeMarkerAttribute<TFacade> where TFacade : class;

/// <summary>
/// Non-generic marker attribute for RegistryFacade entrypoint discovery
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class RegistryFacadeAttribute : Attribute;

/// <summary>
/// Opt-in marker for source-generated keyed-factory emission on a type-registration registry method.
/// Apply alongside [RegistryMethod] on a method of shape
/// <c>void RegisterX&lt;TConcrete&gt;(Identification id) where TConcrete : class, TBase, IHasIdentification</c>.
/// The Registry Generator will emit an IFactoryConfigurator&lt;Identification, TBase, …&gt; that registers
/// each marker-flagged provider's concrete type into the keyed-factory map, keyed by IdentificationHelper.Read&lt;TConcrete&gt;().
/// </summary>
/// <typeparam name="TBase">The base interface or class produced by the keyed factory.</typeparam>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class KeyedFactoryGenerationMarkerAttribute<TBase> : Attribute where TBase : class;

/// <summary>
/// Marks an interface or base class as a contract used in typed registration. Final concretes that derive from
/// the marked type are auto-emitted with an <see cref="IHasIdentification"/> implementation by the Registry Generator
/// (Phase 49.3-01) — the marked type itself never extends or implements <see cref="IHasIdentification"/>.
/// </summary>
/// <remarks>
/// <para>Without this marker, a generator (e.g. <c>StatelessFunctionGenerator</c>) cannot tell at extraction time that
/// a containing type whose <see cref="IHasIdentification"/> arrives only via auto-emit (a sibling generator's output)
/// is actually a typed-registration target. Roslyn does not let one <c>[Generator]</c> observe another's output within
/// the same compilation pass; only attributes on user-source declarations are visible across generators.</para>
/// <para>Applying this attribute to the contract surface (typically a marker interface or abstract base) provides the
/// missing signal so dependent generators can opt the type into the same dispatch as a user-source
/// <c>: IHasIdentification</c> declaration.</para>
/// <para>The accompanying analyzer (SPARK0263) promotes use of this attribute when a type is referenced as a generic
/// type argument in a typed-registration registry method.</para>
/// </remarks>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class TypedRegistrationContractAttribute : Attribute;

/// <summary>
/// Marks a class as containing code that depends on an optional mod's types.
/// Isolate optional mod type references to marked classes to prevent TypeLoadException.
/// </summary>
/// <seealso cref="ModLoadedGuardAttribute"/>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class OptionalModDependentAttribute : Attribute
{
    /// <summary>
    /// Gets the mod ID that this class depends on.
    /// </summary>
    public string ModId { get; }

    /// <param name="modId">The mod identifier this class depends on.</param>
    public OptionalModDependentAttribute(string modId)
    {
        ModId = modId ?? throw new ArgumentNullException(nameof(modId));
    }
}

/// <summary>
/// Marks a method as a guarded entry point for optional mod code.
/// Callers must check IsModLoaded before invoking this method.
/// </summary>
/// <seealso cref="OptionalModDependentAttribute"/>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class ModLoadedGuardAttribute : Attribute
{
    /// <summary>
    /// Gets the mod ID that this method guards access to.
    /// </summary>
    public string ModId { get; }

    /// <param name="modId">The mod identifier this method guards.</param>
    public ModLoadedGuardAttribute(string modId)
    {
        ModId = modId ?? throw new ArgumentNullException(nameof(modId));
    }
}