using System.Threading;
using System.Text.Json;

namespace Bloodcraft.Services;

[Flags]
internal enum StartupState
{
    None = 0,
    ConfigLoaded = 1 << 0,
    PlayerDataLoaded = 1 << 1,
    BootstrapPatched = 1 << 2,
    BootstrapFired = 1 << 3,
    MainHarmonyPatched = 1 << 4,
    CoreInitialized = 1 << 5,
    CommandsRegistered = 1 << 6,
    RconRegistered = 1 << 7
}

internal static class StartupStateService
{
    static int _current;

    static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false
    };

    internal sealed class StartupStateSnapshot
    {
        public bool Ready { get; init; }
        public string[] Current { get; init; } = [];
        public string[] Missing { get; init; } = [];
        public string[] Required { get; init; } = [];
    }

    internal const StartupState RequiredReadyStates =
        StartupState.ConfigLoaded
        | StartupState.PlayerDataLoaded
        | StartupState.BootstrapPatched
        | StartupState.BootstrapFired
        | StartupState.MainHarmonyPatched
        | StartupState.CoreInitialized
        | StartupState.CommandsRegistered
        | StartupState.RconRegistered;

    internal const StartupState BootstrapReadyStates =
        StartupState.BootstrapFired
        | StartupState.CoreInitialized;

    internal static StartupState Current
        => (StartupState)Volatile.Read(ref _current);

    internal static void Mark(StartupState state)
    {
        int current;
        int next;

        do
        {
            current = Volatile.Read(ref _current);
            next = current | (int)state;
        }
        while (Interlocked.CompareExchange(ref _current, next, current) != current);
    }

    internal static bool IsSet(StartupState state)
        => (Current & state) == state;

    internal static StartupState MissingRequired()
        => RequiredReadyStates & ~Current;

    internal static bool IsReady()
        => MissingRequired() == StartupState.None;

    internal static bool IsWaitingForBootstrap()
    {
        StartupState missing = MissingRequired();
        return missing != StartupState.None && (missing & ~BootstrapReadyStates) == StartupState.None;
    }

    static string[] GetOrderedStateNames(StartupState states)
    {
        StartupState[] allStates =
        [
            StartupState.ConfigLoaded,
            StartupState.PlayerDataLoaded,
            StartupState.BootstrapPatched,
            StartupState.BootstrapFired,
            StartupState.MainHarmonyPatched,
            StartupState.CoreInitialized,
            StartupState.CommandsRegistered,
            StartupState.RconRegistered
        ];

        return [..allStates.Where(state => state != StartupState.None && (states & state) == state).Select(state => state.ToString())];
    }

    internal static StartupStateSnapshot GetSnapshot()
    {
        StartupState current = Current;
        StartupState missing = MissingRequired();

        return new()
        {
            Ready = missing == StartupState.None,
            Current = GetOrderedStateNames(current),
            Missing = GetOrderedStateNames(missing),
            Required = GetOrderedStateNames(RequiredReadyStates)
        };
    }

    internal static string BuildSummary()
    {
        StartupStateSnapshot snapshot = GetSnapshot();
        string currentText = snapshot.Current.Length == 0 ? "None" : string.Join(",", snapshot.Current);
        string missingText = snapshot.Missing.Length == 0 ? "None" : string.Join(",", snapshot.Missing);

        return $"Ready: {snapshot.Ready} | Current: {currentText} | Missing: {missingText}";
    }

    internal static string BuildJsonSummary()
        => JsonSerializer.Serialize(GetSnapshot(), _jsonOptions);

    internal static void Reset()
        => Interlocked.Exchange(ref _current, (int)StartupState.None);
}
