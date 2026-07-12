namespace Sparkitect.DI.Container;

/// <summary>
/// Attempts to dispose every owned disposable exactly once, aggregating failures instead of
/// swallowing them. A failure on one sibling never prevents attempting the rest.
/// </summary>
internal static class DisposalAggregator
{
    /// <summary>
    /// Disposes every disposable in <paramref name="instances"/>, attempting each exactly once.
    /// Throws a flattened <see cref="AggregateException"/> naming the owner and failed types if any
    /// disposal failed; non-disposable instances are ignored.
    /// </summary>
    public static void DisposeAll(string ownerName, IEnumerable<object> instances)
    {
        List<Exception>? failures = null;
        List<string>? failedTypeNames = null;
        var attempted = 0;

        foreach (var instance in instances)
        {
            if (instance is not IDisposable disposable)
                continue;

            attempted++;

            try
            {
                disposable.Dispose();
            }
            catch (Exception ex)
            {
                (failures ??= []).Add(ex);
                (failedTypeNames ??= []).Add(instance.GetType().Name);
            }
        }

        if (failures is not { Count: > 0 })
            return;

        throw new AggregateException(
                $"{ownerName} failed to dispose {failures.Count} of {attempted} owned disposable(s): {string.Join(", ", failedTypeNames!)}.",
                failures)
            .Flatten();
    }
}
