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
