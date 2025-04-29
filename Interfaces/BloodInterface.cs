using Bloodcraft.Services;

namespace Bloodcraft.Interfaces;
public enum BloodType
{
    Worker,
    Warrior,
    Scholar,
    Rogue,
    Mutant,
    VBlood,
    None,
    GateBoss,
    Draculin,
    Immortal,
    Creature,
    Brute,
    Corruption
}
internal interface IBloodLegacy
{
    KeyValuePair<int, float> GetLegacyData(ulong steamId);
    void SetLegacyData(ulong steamId, KeyValuePair<int, float> xpData);
    BloodType GetBloodType();
}
internal static class BloodLegacyFactory
{
    public static IBloodLegacy GetBloodHandler(BloodType bloodType)
    {
        return bloodType switch
        {
            BloodType.Worker => new WorkerLegacy(),
            BloodType.Warrior => new WarriorLegacy(),
            BloodType.Scholar => new ScholarLegacy(),
            BloodType.Rogue => new RogueLegacy(),
            BloodType.Mutant => new MutantLegacy(),
            BloodType.Draculin => new DraculinLegacy(),
            BloodType.Immortal => new ImmortalLegacy(),
            BloodType.Creature => new CreatureLegacy(),
            BloodType.Brute => new BruteLegacy(),
            BloodType.Corruption => new CorruptionLegacy(),
            _ => null,
        };
    }
}
internal abstract class BloodLegacy : IBloodLegacy
{
    public abstract KeyValuePair<int, float> GetLegacyData(ulong steamId);
    public abstract void SetLegacyData(ulong steamId, KeyValuePair<int, float> data);
    public abstract BloodType GetBloodType();
}
internal class WorkerLegacy : BloodLegacy
{
    public override KeyValuePair<int, float> GetLegacyData(ulong steamId)
    {
        return steamId.TryGetPlayerWorkerLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetLegacyData(ulong steamId, KeyValuePair<int, float> data)
    {
        steamId.SetPlayerWorkerLegacy(data);
    }
    public override BloodType GetBloodType()
    {
        return BloodType.Worker;
    }
}
internal class WarriorLegacy : BloodLegacy
{
    public override KeyValuePair<int, float> GetLegacyData(ulong steamId)
    {
        return steamId.TryGetPlayerWarriorLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetLegacyData(ulong steamId, KeyValuePair<int, float> data)
    {
        steamId.SetPlayerWarriorLegacy(data);
    }
    public override BloodType GetBloodType()
    {
        return BloodType.Warrior;
    }
}
internal class ScholarLegacy : BloodLegacy
{
    public override KeyValuePair<int, float> GetLegacyData(ulong steamId)
    {
        return steamId.TryGetPlayerScholarLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetLegacyData(ulong steamId, KeyValuePair<int, float> data)
    {
        steamId.SetPlayerScholarLegacy(data);
    }
    public override BloodType GetBloodType()
    {
        return BloodType.Scholar;
    }
}
internal class RogueLegacy : BloodLegacy
{
    public override KeyValuePair<int, float> GetLegacyData(ulong steamId)
    {
        return steamId.TryGetPlayerRogueLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetLegacyData(ulong steamId, KeyValuePair<int, float> data)
    {
        steamId.SetPlayerRogueLegacy(data);
    }
    public override BloodType GetBloodType()
    {
        return BloodType.Rogue;
    }
}
internal class MutantLegacy : BloodLegacy
{
    public override KeyValuePair<int, float> GetLegacyData(ulong steamId)
    {
        return steamId.TryGetPlayerMutantLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetLegacyData(ulong steamId, KeyValuePair<int, float> data)
    {
        steamId.SetPlayerMutantLegacy(data);
    }
    public override BloodType GetBloodType()
    {
        return BloodType.Mutant;
    }
}
internal class DraculinLegacy : BloodLegacy
{
    public override KeyValuePair<int, float> GetLegacyData(ulong steamId)
    {
        return steamId.TryGetPlayerDraculinLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetLegacyData(ulong steamId, KeyValuePair<int, float> data)
    {
        steamId.SetPlayerDraculinLegacy(data);
    }
    public override BloodType GetBloodType()
    {
        return BloodType.Draculin;
    }
}
internal class ImmortalLegacy : BloodLegacy
{
    public override KeyValuePair<int, float> GetLegacyData(ulong steamId)
    {
        return steamId.TryGetPlayerImmortalLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetLegacyData(ulong steamId, KeyValuePair<int, float> data)
    {
        steamId.SetPlayerImmortalLegacy(data);
    }
    public override BloodType GetBloodType()
    {
        return BloodType.Immortal;
    }
}
internal class CreatureLegacy : BloodLegacy
{
    public override KeyValuePair<int, float> GetLegacyData(ulong steamId)
    {
        return steamId.TryGetPlayerCreatureLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetLegacyData(ulong steamId, KeyValuePair<int, float> data)
    {
        steamId.SetPlayerCreatureLegacy(data);
    }
    public override BloodType GetBloodType()
    {
        return BloodType.Creature;
    }
}
internal class BruteLegacy : BloodLegacy
{
    public override KeyValuePair<int, float> GetLegacyData(ulong steamId)
    {
        return steamId.TryGetPlayerBruteLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetLegacyData(ulong steamId, KeyValuePair<int, float> data)
    {
        steamId.SetPlayerBruteLegacy(data);
    }
    public override BloodType GetBloodType()
    {
        return BloodType.Brute;
    }
}
internal class CorruptionLegacy : BloodLegacy
{
    public override KeyValuePair<int, float> GetLegacyData(ulong steamId)
    {
        return steamId.TryGetPlayerCorruptionLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetLegacyData(ulong steamId, KeyValuePair<int, float> data)
    {
        steamId.SetPlayerCorruptionLegacy(data);
    }
    public override BloodType GetBloodType()
    {
        return BloodType.Corruption;
    }
}