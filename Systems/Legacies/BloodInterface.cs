using Bloodcraft.Services;

namespace Bloodcraft.Systems.Legacies;
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
    Brute
}
public interface IBloodHandler
{
    KeyValuePair<int, float> GetLegacyData(ulong steamID);
    void SetLegacyData(ulong steamID, KeyValuePair<int, float> xpData);
    BloodType GetBloodType();
}
public static class BloodHandlerFactory
{
    public static IBloodHandler GetBloodHandler(BloodType bloodType)
    {
        return bloodType switch
        {
            BloodType.Worker => new WorkerHandler(),
            BloodType.Warrior => new WarriorHandler(),
            BloodType.Scholar => new ScholarHandler(),
            BloodType.Rogue => new RogueHandler(),
            BloodType.Mutant => new MutantHandler(),
            BloodType.VBlood => new VBloodHandler(),
            BloodType.Draculin => new DraculinHandler(),
            BloodType.Immortal => new ImmortalHandler(),
            BloodType.Creature => new CreatureHandler(),
            BloodType.Brute => new BruteHandler(),
            _ => null,
        };
    }
}
public abstract class BaseBloodHandler : IBloodHandler
{
    public abstract KeyValuePair<int, float> GetLegacyData(ulong steamID);
    public abstract void SetLegacyData(ulong steamID, KeyValuePair<int, float> data);
    public abstract BloodType GetBloodType();
}
public class WorkerHandler : BaseBloodHandler
{
    public override KeyValuePair<int, float> GetLegacyData(ulong steamID)
    {
        return steamID.TryGetPlayerWorkerLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetLegacyData(ulong steamID, KeyValuePair<int, float> data)
    {
        steamID.SetPlayerWorkerLegacy(data);
    }
    public override BloodType GetBloodType()
    {
        return BloodType.Worker;
    }
}
public class WarriorHandler : BaseBloodHandler
{
    public override KeyValuePair<int, float> GetLegacyData(ulong steamID)
    {
        return steamID.TryGetPlayerWarriorLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetLegacyData(ulong steamID, KeyValuePair<int, float> data)
    {
        steamID.SetPlayerWarriorLegacy(data);
    }
    public override BloodType GetBloodType()
    {
        return BloodType.Warrior;
    }
}
public class ScholarHandler : BaseBloodHandler
{
    public override KeyValuePair<int, float> GetLegacyData(ulong steamID)
    {
        return steamID.TryGetPlayerScholarLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetLegacyData(ulong steamID, KeyValuePair<int, float> data)
    {
        steamID.SetPlayerScholarLegacy(data);
    }
    public override BloodType GetBloodType()
    {
        return BloodType.Scholar;
    }
}
public class RogueHandler : BaseBloodHandler
{
    public override KeyValuePair<int, float> GetLegacyData(ulong steamID)
    {
        return steamID.TryGetPlayerRogueLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetLegacyData(ulong steamID, KeyValuePair<int, float> data)
    {
        steamID.SetPlayerRogueLegacy(data);
    }
    public override BloodType GetBloodType()
    {
        return BloodType.Rogue;
    }
}
public class MutantHandler : BaseBloodHandler
{
    public override KeyValuePair<int, float> GetLegacyData(ulong steamID)
    {
        return steamID.TryGetPlayerMutantLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetLegacyData(ulong steamID, KeyValuePair<int, float> data)
    {
        steamID.SetPlayerMutantLegacy(data);
    }
    public override BloodType GetBloodType()
    {
        return BloodType.Mutant;
    }
}
public class VBloodHandler : BaseBloodHandler
{
    public override KeyValuePair<int, float> GetLegacyData(ulong steamID)
    {
        return steamID.TryGetPlayerVBloodLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetLegacyData(ulong steamID, KeyValuePair<int, float> data)
    {
        steamID.SetPlayerVBloodLegacy(data);
    }
    public override BloodType GetBloodType()
    {
        return BloodType.VBlood;
    }
}
public class DraculinHandler : BaseBloodHandler
{
    public override KeyValuePair<int, float> GetLegacyData(ulong steamID)
    {
        return steamID.TryGetPlayerDraculinLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetLegacyData(ulong steamID, KeyValuePair<int, float> data)
    {
        steamID.SetPlayerDraculinLegacy(data);
    }
    public override BloodType GetBloodType()
    {
        return BloodType.Draculin;
    }
}
public class ImmortalHandler : BaseBloodHandler
{
    public override KeyValuePair<int, float> GetLegacyData(ulong steamID)
    {
        return steamID.TryGetPlayerImmortalLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetLegacyData(ulong steamID, KeyValuePair<int, float> data)
    {
        steamID.SetPlayerImmortalLegacy(data);
    }
    public override BloodType GetBloodType()
    {
        return BloodType.Immortal;
    }
}
public class CreatureHandler : BaseBloodHandler
{
    public override KeyValuePair<int, float> GetLegacyData(ulong steamID)
    {
        return steamID.TryGetPlayerCreatureLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetLegacyData(ulong steamID, KeyValuePair<int, float> data)
    {
        steamID.SetPlayerCreatureLegacy(data);
    }
    public override BloodType GetBloodType()
    {
        return BloodType.Creature;
    }
}
public class BruteHandler : BaseBloodHandler
{
    public override KeyValuePair<int, float> GetLegacyData(ulong steamID)
    {
        return steamID.TryGetPlayerBruteLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetLegacyData(ulong steamID, KeyValuePair<int, float> data)
    {
        steamID.SetPlayerBruteLegacy(data);
    }
    public override BloodType GetBloodType()
    {
        return BloodType.Brute;
    }
}