namespace Sparkitect;

/// <summary>
/// Holds the engine's process entry arguments (as passed to <see cref="EngineBootstrapper.Main"/>) so both
/// the pre-container logger read (D-16) and the CLI settings source read one authoritative arg set. Set once
/// at startup; the CLI acquisition flows solely through this after the <c>ICliArgumentHandler</c> retirement.
/// </summary>
internal static class EngineEntryArguments
{
    private static IReadOnlyList<string> _args = [];

    /// <summary>The entry arguments (excluding the executable path). Empty until <see cref="Set"/> runs.</summary>
    public static IReadOnlyList<string> Args => _args;

    /// <summary>Records the engine entry arguments. Called once from <see cref="EngineBootstrapper.Main"/>.</summary>
    /// <param name="args">The arguments passed to the engine entry point.</param>
    public static void Set(string[] args) => _args = args;
}
