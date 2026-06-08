using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>Base marker for any resource manager registered with the render graph.</summary>
[PublicAPI]
public interface IGraphResourceManager;

/// <summary>Resource-type erasure surface used by <c>SetupContext</c> to dispatch a typed Declare.</summary>
[PublicAPI]
public interface IGraphResourceManagerFor<TResource> : IGraphResourceManager
    where TResource : IHasIdentification
{
    IGraphResource<TResource> DeclareUntyped(
        Identification passId, int slot, IResourceRequest<TResource> request);
}

/// <summary>
/// Push capability: a manager that accepts a published <typeparamref name="TResource"/> value. The push
/// analog of <see cref="IGraphResourceManagerFor{TResource}"/> — one level, no request to erase, the value
/// <em>is</em> the resource. The type-routed <c>Publish&lt;T&gt;</c> door casts the Id-resolved manager to
/// this and calls <see cref="Publish"/>.
/// </summary>
[PublicAPI]
public interface IGraphPushTargetFor<TResource> : IGraphResourceManager
    where TResource : IHasIdentification
{
    void Publish(TResource value);
}

/// <summary>Typed manager: declares <typeparamref name="TResource"/> from a <typeparamref name="TRequest"/> shape.</summary>
[PublicAPI]
public interface IGraphResourceManager<TResource, TRequest> : IGraphResourceManagerFor<TResource>
    where TRequest : IResourceRequest<TResource>
    where TResource : IHasIdentification
{
    IGraphResource<TResource> Declare(Identification passId, int slot, TRequest request);

    IGraphResource<TResource> IGraphResourceManagerFor<TResource>.DeclareUntyped(
        Identification passId, int slot, IResourceRequest<TResource> request)
        => Declare(passId, slot, (TRequest)request);
}
