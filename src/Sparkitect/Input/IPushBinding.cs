using JetBrains.Annotations;

namespace Sparkitect.Input;

/// <summary>
/// A live push binding: the callback holder and the consumer's lifetime unit for one resolved
/// action. Disposing stops the callback immediately and is idempotent; the providing
/// implementation auto-cleans any undisposed residual at its own teardown, warning with
/// provenance.
/// </summary>
[PublicAPI]
public interface IPushBinding : IDisposable;
