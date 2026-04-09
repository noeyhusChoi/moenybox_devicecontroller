namespace IdScannerTool.Services;

public sealed class StartupStateMachine
{
    private readonly List<StartupStateTransition> _transitions = new();

    public StartupState CurrentState { get; private set; } = StartupState.Booting;
    public IReadOnlyList<StartupStateTransition> Transitions => _transitions;

    public void MoveToCheckingLocalSerial(string reason)
        => TransitionTo(StartupState.CheckingLocalSerial, reason);

    public void MoveToVerifyingSerial(string reason)
        => TransitionTo(StartupState.VerifyingSerial, reason);

    public void MoveToNeedsRegistration(string reason)
        => TransitionTo(StartupState.NeedsRegistration, reason);

    public void MoveToReady(string reason)
        => TransitionTo(StartupState.Ready, reason);

    public void MoveToFailed(string reason)
        => TransitionTo(StartupState.Failed, reason);

    private void TransitionTo(StartupState nextState, string reason)
    {
        if (!CanTransition(CurrentState, nextState))
        {
            throw new InvalidOperationException(
                $"Invalid startup state transition: {CurrentState} -> {nextState}");
        }

        var transition = new StartupStateTransition(
            From: CurrentState,
            To: nextState,
            Reason: reason,
            TimestampUtc: DateTimeOffset.UtcNow);

        _transitions.Add(transition);
        CurrentState = nextState;
    }

    private static bool CanTransition(StartupState from, StartupState to)
        => (from, to) switch
        {
            (StartupState.Booting, StartupState.CheckingLocalSerial) => true,
            (StartupState.Booting, StartupState.VerifyingSerial) => true,

            (StartupState.CheckingLocalSerial, StartupState.VerifyingSerial) => true,
            (StartupState.CheckingLocalSerial, StartupState.NeedsRegistration) => true,
            (StartupState.CheckingLocalSerial, StartupState.Failed) => true,
            (StartupState.CheckingLocalSerial, StartupState.Ready) => true,

            (StartupState.VerifyingSerial, StartupState.CheckingLocalSerial) => true,
            (StartupState.VerifyingSerial, StartupState.Ready) => true,
            (StartupState.VerifyingSerial, StartupState.NeedsRegistration) => true,
            (StartupState.VerifyingSerial, StartupState.Failed) => true,

            (StartupState.NeedsRegistration, StartupState.VerifyingSerial) => true,
            (StartupState.NeedsRegistration, StartupState.Failed) => true,

            (StartupState.Ready, StartupState.VerifyingSerial) => true,

            _ => false
        };
}
