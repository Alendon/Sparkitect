using Sparkitect.DI.GeneratorAttributes;

using JetBrains.Annotations;

namespace Sparkitect.Modding;

/// <summary>
/// Marks a class as a registry implementation. Triggers source generation of keyed factory and configurator.
/// The class must implement <see cref="IRegistry"/> and be partial.
/// </summary>
/// <remarks> Marker attributes inherited by this class will be treated as specialized Registries and not be
/// processed by the Registry SG beyond their basic one time setup (eg no registry method parsing)</remarks>
[PublicAPI]
[MeansImplicitUse]
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

    /// <summary>
    /// Optional suffix appended to every alias this registry's methods emit into a target
    /// registry's id space, for provenance and collision-proofing against silent
    /// extension-member shadowing. Null means no suffix.
    /// </summary>
    public string? AliasSuffix { get; set; }
}

/// <summary>
/// Assembly-level attribute for associating metadata types with registries.
/// </summary>
/// <typeparam name="TMetadata">The metadata type.</typeparam>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
[PublicAPI]
public class RegistryMetadataAttribute<TMetadata> : Attribute where TMetadata : class;

/// <summary>
/// Marks a registration-attribute type with the registration category it belongs to.
/// Applied to the generated and hand-authored attribute types that mods place on their registrations.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
[PublicAPI]
public sealed class RegistrationMarkerAttribute(string category) : Attribute
{
    /// <summary>
    /// The registration category identifier this attribute registers into.
    /// </summary>
    public string Category { get; } = category;
}

/// <summary>
/// Annotates a generated identification property with the registration site it originates from.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
[PublicAPI]
public sealed class RegisteredFromAttribute : Attribute
{
    /// <summary>
    /// Annotates a leaf registered through C# source: the originating <paramref name="registeredType"/>
    /// is the navigation target (optionally with a <see cref="Member"/>).
    /// </summary>
    public RegisteredFromAttribute(Type registeredType) => RegisteredType = registeredType;

    /// <summary>
    /// Annotates a leaf registered through a resource file: there is no C# type, only a plain
    /// <see cref="SourcePath"/> coordinate (<see cref="SourceLine"/>/<see cref="SourceColumn"/>).
    /// </summary>
    public RegisteredFromAttribute()
    {
    }

    /// <summary>
    /// The type registered at the originating site. Null when the registration originates from a
    /// resource file, where the navigation target is the <see cref="SourcePath"/> coordinate instead.
    /// </summary>
    public Type? RegisteredType { get; }

    /// <summary>
    /// The member name at the registration site, when the registration is reached through C# source.
    /// </summary>
    public string? Member { get; set; }

    /// <summary>
    /// The resource file path of the registration site, when the registration originates from a resource file.
    /// </summary>
    public string? SourcePath { get; set; }

    /// <summary>
    /// The line within <see cref="SourcePath"/> of the registration site.
    /// </summary>
    public int SourceLine { get; set; }

    /// <summary>
    /// The column within <see cref="SourcePath"/> of the registration site.
    /// </summary>
    public int SourceColumn { get; set; }
}

/// <summary>
/// Marks a registry method that registers objects. Source generators create registration attributes
/// (e.g., [RegisterItem("key")]) for each marked method, which mods use to register objects.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
[PublicAPI]
[MeansImplicitUse]
public class RegistryMethodAttribute : Attribute;

/// <summary>
/// Declaratively specifies a resource file slot required by a registry.
/// Resource files are loaded from mod archives and made available through <see cref="IResourceManager"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
[PublicAPI]
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
[PublicAPI]
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
[PublicAPI]
[MeansImplicitUse]
public class RegistryFacadeAttribute<TFacade> : FacadeMarkerAttribute<TFacade> where TFacade : class;

/// <summary>
/// Non-generic marker attribute for RegistryFacade entrypoint discovery
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
[PublicAPI]
[MeansImplicitUse]
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
[PublicAPI]
public sealed class KeyedFactoryGenerationMarkerAttribute<TBase> : Attribute where TBase : class;

/// <summary>
/// Marks which type parameter of a registry method feeds the <see cref="Identification{T}"/>
/// transform. Presence-only — the generator reads which type parameter carries it and emits
/// <see cref="Identification{T}"/> instead of bare <see cref="Identification"/> for that entry's
/// id properties.
/// </summary>
[AttributeUsage(AttributeTargets.GenericParameter)]
[PublicAPI]
public sealed class TypedIdentificationAttribute : Attribute;

/// <summary>
/// Marks a register-method type parameter as a cross-registry portion emitting a typed alias
/// into the named target registry's id space.
/// </summary>
/// <typeparam name="TTargetRegistry">The target registry type the marked portion emits an alias into.</typeparam>
[AttributeUsage(AttributeTargets.GenericParameter)]
[PublicAPI]
public sealed class TypedIdentificationAttribute<TTargetRegistry> : Attribute;

/// <summary>
/// Marks a class as containing code that depends on an optional mod's types.
/// Isolate optional mod type references to marked classes to prevent TypeLoadException.
/// </summary>
/// <seealso cref="ModLoadedGuardAttribute"/>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
[PublicAPI]
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
[PublicAPI]
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