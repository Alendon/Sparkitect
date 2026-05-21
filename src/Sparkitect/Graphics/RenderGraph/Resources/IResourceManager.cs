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
