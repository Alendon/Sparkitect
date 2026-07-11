using JetBrains.Annotations;

namespace Sparkitect.Input;

/// <summary>
/// Device-neutral axis shape for a default-binding value: two source values driving a -1..+1
/// axis. Core owns only the shape — an input implementation interprets it through its own
/// registered binding adapter for <typeparamref name="TKey"/>'s channel.
/// </summary>
/// <typeparam name="TKey">The source-channel value vocabulary (e.g. a key enum).</typeparam>
/// <param name="Negative">The source value that drives the axis to -1.</param>
/// <param name="Positive">The source value that drives the axis to +1.</param>
[PublicAPI]
public readonly record struct InputAxis<TKey>(TKey Negative, TKey Positive);

/// <summary>
/// Device-neutral composite 2D-vector shape for a default-binding value: four source values
/// (WASD-shaped) composing into a vector. One atomic value type — rebound as a single write,
/// never assembled from partial field writes.
/// </summary>
/// <typeparam name="TKey">The source-channel value vocabulary (e.g. a key enum).</typeparam>
/// <param name="Up">The source value that drives Y toward +1.</param>
/// <param name="Down">The source value that drives Y toward -1.</param>
/// <param name="Left">The source value that drives X toward -1.</param>
/// <param name="Right">The source value that drives X toward +1.</param>
[PublicAPI]
public readonly record struct InputVector2<TKey>(TKey Up, TKey Down, TKey Left, TKey Right);
