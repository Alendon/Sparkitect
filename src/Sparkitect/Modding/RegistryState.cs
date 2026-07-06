using Sparkitect.Utils;

namespace Sparkitect.Modding;

/// <summary>
/// Lifecycle phase of the registry system.
/// </summary>
internal enum RegistryPhase
{
    Idle,
    Populating,
    TearingDown
}

/// <summary>
/// Tracks the current lifecycle phase and evaluates <see cref="RegistryOperation"/> legality.
/// Replaces the raw <c>_isMutationExpected</c> boolean with explicit phase tracking.
/// </summary>
internal class RegistryState
{
    private RegistryPhase _phase = RegistryPhase.Idle;

    /// <summary>Current lifecycle phase.</summary>
    public RegistryPhase CurrentPhase => _phase;

    /// <summary>
    /// Transition to Populating. Idempotent if already Populating.
    /// Throws if currently in TearingDown.
    /// </summary>
    public void EnterPopulating()
    {
        if (_phase == RegistryPhase.Populating) return;
        if (_phase == RegistryPhase.TearingDown)
            new InvalidOperationException(
                $"Cannot enter Populating phase from {_phase}.").Throw();
        _phase = RegistryPhase.Populating;
    }

    /// <summary>
    /// Transition to TearingDown. Idempotent if already TearingDown.
    /// Throws if currently in Populating.
    /// </summary>
    public void EnterTearingDown()
    {
        if (_phase == RegistryPhase.TearingDown) return;
        if (_phase == RegistryPhase.Populating)
            new InvalidOperationException(
                $"Cannot enter TearingDown phase from {_phase}.").Throw();
        _phase = RegistryPhase.TearingDown;
    }

    /// <summary>
    /// Unconditionally return to Idle.
    /// </summary>
    public void ReturnToIdle()
    {
        _phase = RegistryPhase.Idle;
    }

    /// <summary>
    /// Single mutation API — all registration/unregistration paths go through here.
    /// Allocate/Mutate allowed only in Populating; Destroy allowed only in TearingDown.
    /// </summary>
    public void MutationRequest(RegistryOperation operation, Identification target)
    {
        bool allowed = operation switch
        {
            RegistryOperation.Allocate => _phase == RegistryPhase.Populating,
            RegistryOperation.Mutate   => _phase == RegistryPhase.Populating,
            RegistryOperation.Destroy  => _phase == RegistryPhase.TearingDown,
        };

        if (!allowed)
            new InvalidOperationException(
                $"Operation {operation} on {target} is not allowed in phase {_phase}.").Throw();
    }
}
