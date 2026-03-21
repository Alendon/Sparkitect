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