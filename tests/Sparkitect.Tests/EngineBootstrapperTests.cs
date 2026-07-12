namespace Sparkitect.Tests;

/// <summary>
/// Covers the process-boundary shutdown collector in isolation: ordering, dependency-aware
/// skipping, non-short-circuiting failure aggregation, and primary-cause preservation. Exercised
/// through the pure static algorithm rather than real containers/game-state/Serilog so the tests
/// stay fast and only exercise this boundary's own logic (see the boundary-aware shutdown
/// learning).
/// </summary>
public class EngineBootstrapperTests
{
    // RunBoundarySequence - ordering, dependency-skip, non-short-circuit

    [Test]
    public async Task RunBoundarySequence_AllStepsAvailableAndSucceed_AttemptsAllAndReturnsNoFailures()
    {
        // Arrange
        var unwindCalled = false;
        var cleanupCalled = false;
        var flushCalled = false;

        // Act
        var failures = EngineBootstrapper.RunBoundarySequence(
            hasGameStateManager: true,
            attemptTerminalUnwind: () => unwindCalled = true,
            hasCoreContainer: true,
            attemptRootCleanup: () => cleanupCalled = true,
            attemptLoggerFlush: () => flushCalled = true);

        // Assert
        await Assert.That(unwindCalled).IsTrue();
        await Assert.That(cleanupCalled).IsTrue();
        await Assert.That(flushCalled).IsTrue();
        await Assert.That(failures.Count).IsEqualTo(0);
    }

    [Test]
    public async Task RunBoundarySequence_NoGameStateManager_SkipsUnwindButRunsUnrelatedBranches()
    {
        // Arrange
        var unwindCalled = false;
        var cleanupCalled = false;
        var flushCalled = false;

        // Act - the terminal-unwind prerequisite (a live game state manager) never existed
        var failures = EngineBootstrapper.RunBoundarySequence(
            hasGameStateManager: false,
            attemptTerminalUnwind: () => unwindCalled = true,
            hasCoreContainer: true,
            attemptRootCleanup: () => cleanupCalled = true,
            attemptLoggerFlush: () => flushCalled = true);

        // Assert - unsafe dependant skipped, unrelated branches still ran
        await Assert.That(unwindCalled).IsFalse();
        await Assert.That(cleanupCalled).IsTrue();
        await Assert.That(flushCalled).IsTrue();
        await Assert.That(failures.Count).IsEqualTo(0);
    }

    [Test]
    public async Task RunBoundarySequence_NoCoreContainer_SkipsCleanupButRunsUnrelatedBranches()
    {
        // Arrange
        var unwindCalled = false;
        var cleanupCalled = false;
        var flushCalled = false;

        // Act - the root-cleanup prerequisite (a built container) never existed
        var failures = EngineBootstrapper.RunBoundarySequence(
            hasGameStateManager: true,
            attemptTerminalUnwind: () => unwindCalled = true,
            hasCoreContainer: false,
            attemptRootCleanup: () => cleanupCalled = true,
            attemptLoggerFlush: () => flushCalled = true);

        // Assert
        await Assert.That(unwindCalled).IsTrue();
        await Assert.That(cleanupCalled).IsFalse();
        await Assert.That(flushCalled).IsTrue();
        await Assert.That(failures.Count).IsEqualTo(0);
    }

    [Test]
    public async Task RunBoundarySequence_TerminalUnwindThrows_StillAttemptsCleanupAndFlush()
    {
        // Arrange
        var cleanupCalled = false;
        var flushCalled = false;

        // Act
        var failures = EngineBootstrapper.RunBoundarySequence(
            hasGameStateManager: true,
            attemptTerminalUnwind: () => throw new InvalidOperationException("unwind boom"),
            hasCoreContainer: true,
            attemptRootCleanup: () => cleanupCalled = true,
            attemptLoggerFlush: () => flushCalled = true);

        // Assert - unrelated branches are not abandoned after a failure
        await Assert.That(cleanupCalled).IsTrue();
        await Assert.That(flushCalled).IsTrue();
        await Assert.That(failures.Count).IsEqualTo(1);
    }

    [Test]
    public async Task RunBoundarySequence_RootCleanupThrows_StillAttemptsFlush()
    {
        // Arrange
        var flushCalled = false;

        // Act
        var failures = EngineBootstrapper.RunBoundarySequence(
            hasGameStateManager: true,
            attemptTerminalUnwind: () => { },
            hasCoreContainer: true,
            attemptRootCleanup: () => throw new InvalidOperationException("cleanup boom"),
            attemptLoggerFlush: () => flushCalled = true);

        // Assert
        await Assert.That(flushCalled).IsTrue();
        await Assert.That(failures.Count).IsEqualTo(1);
    }

    [Test]
    public async Task RunBoundarySequence_AllThreeStepsThrow_ObservesAllThreeFailuresOnce()
    {
        // Act
        var failures = EngineBootstrapper.RunBoundarySequence(
            hasGameStateManager: true,
            attemptTerminalUnwind: () => throw new InvalidOperationException("unwind boom"),
            hasCoreContainer: true,
            attemptRootCleanup: () => throw new InvalidOperationException("cleanup boom"),
            attemptLoggerFlush: () => throw new InvalidOperationException("flush boom"));

        // Assert - every failure is observable exactly once
        await Assert.That(failures.Count).IsEqualTo(3);
    }

    [Test]
    public async Task RunBoundarySequence_LoggerFlushThrows_WritesFailureToStderr()
    {
        // Arrange - inject a capture writer rather than touching the global Console.Error
        var capturedError = new StringWriter();

        // Act
        var failures = EngineBootstrapper.RunBoundarySequence(
            hasGameStateManager: false,
            attemptTerminalUnwind: () => { },
            hasCoreContainer: false,
            attemptRootCleanup: () => { },
            attemptLoggerFlush: () => throw new InvalidOperationException("flush boom"),
            stderr: capturedError);

        // Assert - the logger cannot be trusted to report its own flush failure
        await Assert.That(failures.Count).IsEqualTo(1);
        await Assert.That(capturedError.ToString()).Contains("Logger flush failed");
    }

    [Test]
    public async Task RunBoundarySequence_UnwindAndCleanupFailures_DoNotWriteToStderr()
    {
        // Arrange - only the logger-flush failure is stderr-reported; the rest is failure-list only
        var capturedError = new StringWriter();

        // Act
        var failures = EngineBootstrapper.RunBoundarySequence(
            hasGameStateManager: true,
            attemptTerminalUnwind: () => throw new InvalidOperationException("unwind boom"),
            hasCoreContainer: true,
            attemptRootCleanup: () => throw new InvalidOperationException("cleanup boom"),
            attemptLoggerFlush: () => { },
            stderr: capturedError);

        // Assert
        await Assert.That(failures.Count).IsEqualTo(2);
        await Assert.That(capturedError.ToString()).IsEqualTo(string.Empty);
    }

    // BuildBoundaryException - primary-cause preservation, aggregation, flattening

    [Test]
    public async Task BuildBoundaryException_NothingFailed_ReturnsNull()
    {
        // Act
        var result = EngineBootstrapper.BuildBoundaryException(null, []);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task BuildBoundaryException_OnlyPrimaryFailure_AggregatesWithPrimaryPreserved()
    {
        // Arrange
        var primary = new InvalidOperationException("runtime boom");

        // Act
        var result = EngineBootstrapper.BuildBoundaryException(primary, []);

        // Assert
        await Assert.That(result).IsTypeOf<AggregateException>();
        var aggregate = (AggregateException)result!;
        await Assert.That(aggregate.InnerExceptions.Count).IsEqualTo(1);
        await Assert.That(aggregate.InnerExceptions[0]).IsEqualTo(primary);
    }

    [Test]
    public async Task BuildBoundaryException_OnlyShutdownFailures_AggregatesWithoutPrimary()
    {
        // Arrange
        var shutdownFailure = new InvalidOperationException("cleanup boom");

        // Act
        var result = EngineBootstrapper.BuildBoundaryException(null, [shutdownFailure]);

        // Assert
        await Assert.That(result).IsTypeOf<AggregateException>();
        var aggregate = (AggregateException)result!;
        await Assert.That(aggregate.InnerExceptions.Count).IsEqualTo(1);
        await Assert.That(aggregate.InnerExceptions[0]).IsEqualTo(shutdownFailure);
    }

    [Test]
    public async Task BuildBoundaryException_PrimaryAndShutdownFailures_PrimaryStaysFirst()
    {
        // Arrange
        var primary = new InvalidOperationException("runtime boom");
        var cleanupFailure = new InvalidOperationException("cleanup boom");
        var flushFailure = new InvalidOperationException("flush boom");

        // Act - the primary runtime cause must never be shadowed or replaced by cleanup failures
        var result = EngineBootstrapper.BuildBoundaryException(primary, [cleanupFailure, flushFailure]);

        // Assert
        await Assert.That(result).IsTypeOf<AggregateException>();
        var aggregate = (AggregateException)result!;
        await Assert.That(aggregate.InnerExceptions.Count).IsEqualTo(3);
        await Assert.That(aggregate.InnerExceptions[0]).IsEqualTo(primary);
        await Assert.That(aggregate.InnerExceptions[1]).IsEqualTo(cleanupFailure);
        await Assert.That(aggregate.InnerExceptions[2]).IsEqualTo(flushFailure);
    }

    [Test]
    public async Task BuildBoundaryException_NestedAggregateFailure_IsFlattened()
    {
        // Arrange - a shutdown step (e.g. container disposal) can itself throw an AggregateException
        var primary = new InvalidOperationException("runtime boom");
        var nested = new AggregateException("nested", new InvalidOperationException("a"), new InvalidOperationException("b"));

        // Act
        var result = EngineBootstrapper.BuildBoundaryException(primary, [nested]);

        // Assert - Flatten() lifts the nested aggregate's inner exceptions to the top level
        var aggregate = (AggregateException)result!;
        await Assert.That(aggregate.InnerExceptions.Count).IsEqualTo(3);
        await Assert.That(aggregate.InnerExceptions[0]).IsEqualTo(primary);
    }
}
