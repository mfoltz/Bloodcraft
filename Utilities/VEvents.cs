using System.Collections.Concurrent;

namespace Bloodcraft.Utilities;

internal static class VEvents
{
    public enum GameplayEventType : ushort
    {
        None = 0,
        ServantUpgrade = 1,
    }

    public interface IGameplayEvent;

    public readonly struct GameplayEventId(GameplayEventType eventType, ushort key) : IEquatable<GameplayEventId>
    {
        public readonly GameplayEventType EventType = eventType;
        public readonly ushort EventKey = key;

        static ushort _key = 1;

        internal static ushort GenerateId()
        {
            unchecked
            {
                if (_key == 0)
                    _key = 1;

                return _key++;
            }
        }

        public bool IsValid()
            => EventType != GameplayEventType.None && EventKey != 0;

        public bool Equals(GameplayEventId other)
            => EventType == other.EventType && EventKey == other.EventKey;

        public override bool Equals(object obj)
            => obj is GameplayEventId other && Equals(other);

        public override int GetHashCode()
            => ((ushort)EventType << 16) ^ EventKey;

        public override string ToString()
            => $"{EventType}:{EventKey}";
    }

    public readonly struct ServantUpgradeEvent(string player, string servant) : IGameplayEvent
    {
        public readonly GameplayEventId Id = new(GameplayEventType.ServantUpgrade, GameplayEventId.GenerateId());
        public readonly string Player = player;
        public readonly string Servant = servant;
    }

    static readonly Queue<ServantUpgradeEvent> _servantUpgradeQueue = [];
    static readonly ConcurrentDictionary<(string, string), bool> _servantUpgradeReceipts = [];

    public static void Dispatch(ServantUpgradeEvent servantUpgradeEvent)
        => _servantUpgradeQueue.Enqueue(servantUpgradeEvent);

    public static bool TryReceive(out ServantUpgradeEvent servantUpgradeEvent)
        => _servantUpgradeQueue.TryDequeue(out servantUpgradeEvent);

    public static void KeepReceipt(ServantUpgradeEvent servantUpgradeEvent, bool wasUpgraded = false)
        => _servantUpgradeReceipts.TryAdd(new(servantUpgradeEvent.Player, servantUpgradeEvent.Servant), wasUpgraded);

    public static bool HasRefund((string Player, string Servant) tupleKey)
    {
        return _servantUpgradeReceipts.TryRemove(tupleKey, out bool wasUpgraded) && !wasUpgraded;
    }
}
