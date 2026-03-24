// ReSharper disable once CheckNamespace
namespace Sparkitect.DI.GeneratorAttributes;

/// <summary>
/// Base class for facade marker attributes. Facades provide subsystem-exclusive APIs that are not normally
/// resolvable through the main core container. Allows specific subsystems to access specialized interfaces
/// (e.g., registries calling manager methods) while keeping those APIs hidden from general DI resolution.
/// Triggers source generation of facade configurators.
/// </summary>
/// <typeparam name="TFacade">The exclusive facade interface type accessible only to the specific subsystem.</typeparam>
public abstract class FacadeMarkerAttribute<TFacade> : Attribute where TFacade : class;

/// <summary>
/// Navigation hint attribute placed on facade interfaces to enable source generator
/// back-tracking from facade type to service type. Not a category marker -- purely
/// for SG extraction.
/// </summary>
/// <typeparam name="TService">The service interface type this facade belongs to.</typeparam>
[AttributeUsage(AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
public sealed class FacadeForAttribute<TService> : Attribute where TService : class;

/// <summary>
/// Marker attribute applied to SF category attribute class definitions to indicate
/// which facade category should be used for facade extraction on methods decorated
/// with that SF attribute. SF categories without this marker produce no facade mappings.
/// </summary>
/// <typeparam name="TFacadeCategory">The facade category attribute type
/// (e.g., StateFacadeAttribute).</typeparam>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class FacadeCategoryMappingAttribute<TFacadeCategory> : Attribute
    where TFacadeCategory : Attribute;
