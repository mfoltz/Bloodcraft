namespace Bloodcraft.SystemUtilities.Legacies;
public interface IBloodHandler
{
    void AddLegacy(ulong steamID, float experience);

    void SaveChanges();

    KeyValuePair<int, float> GetLegacyData(ulong steamID);

    void UpdateLegacyData(ulong steamID, KeyValuePair<int, float> xpData);

    LegacyUtilities.BloodType GetBloodType();
}
public static class BloodHandlerFactory
{
    public static IBloodHandler GetBloodHandler(LegacyUtilities.BloodType bloodType)
    {
        return bloodType switch
        {
            LegacyUtilities.BloodType.Worker => new WorkerHandler(),
            LegacyUtilities.BloodType.Warrior => new WarriorHandler(),
            LegacyUtilities.BloodType.Scholar => new ScholarHandler(),
            LegacyUtilities.BloodType.Rogue => new RogueHandler(),
            LegacyUtilities.BloodType.Mutant => new MutantHandler(),
            LegacyUtilities.BloodType.VBlood => new VBloodHandler(),
            LegacyUtilities.BloodType.Draculin => new DraculinHandler(),
            LegacyUtilities.BloodType.Immortal => new ImmortalHandler(),
            LegacyUtilities.BloodType.Creature => new CreatureHandler(),
            LegacyUtilities.BloodType.Brute => new BruteHandler(),
            _ => null,
        };
    }
}
public abstract class BaseBloodHandler : IBloodHandler
{
    protected abstract IDictionary<ulong, KeyValuePair<int, float>> DataStructure { get; }

    public void AddLegacy(ulong steamID, float experience)
    {
        if (DataStructure.TryGetValue(steamID, out var currentData))
        {
            DataStructure[steamID] = new KeyValuePair<int, float>(currentData.Key, currentData.Value + experience);
        }
        else
        {
            DataStructure.Add(steamID, new KeyValuePair<int, float>(0, experience));
        }
    }
    public KeyValuePair<int, float> GetLegacyData(ulong steamID)
    {
        if (DataStructure.TryGetValue(steamID, out var xpData))
            return xpData;
        return new KeyValuePair<int, float>(0, 0);
    }
    public void UpdateLegacyData(ulong steamID, KeyValuePair<int, float> xpData)
    {
        DataStructure[steamID] = xpData;
    }
    public abstract void SaveChanges();
    public abstract LegacyUtilities.BloodType GetBloodType();
}
public class WorkerHandler : BaseBloodHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerWorkerLegacy;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerWorkerLegacy();
    }
    public override LegacyUtilities.BloodType GetBloodType()
    {
        return LegacyUtilities.BloodType.Worker;
    }
}
public class WarriorHandler : BaseBloodHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerWarriorLegacy;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerWarriorLegacy();
    }
    public override LegacyUtilities.BloodType GetBloodType()
    {
        return LegacyUtilities.BloodType.Warrior;
    }
}
public class ScholarHandler : BaseBloodHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerScholarLegacy;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerScholarLegacy();
    }
    public override LegacyUtilities.BloodType GetBloodType()
    {
        return LegacyUtilities.BloodType.Scholar;
    }
}
public class RogueHandler : BaseBloodHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerRogueLegacy;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerRogueLegacy();
    }
    public override LegacyUtilities.BloodType GetBloodType()
    {
        return LegacyUtilities.BloodType.Rogue;
    }
}
public class MutantHandler : BaseBloodHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerMutantLegacy;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerMutantLegacy();
    }
    public override LegacyUtilities.BloodType GetBloodType()
    {
        return LegacyUtilities.BloodType.Mutant;
    }
}
public class VBloodHandler : BaseBloodHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerVBloodLegacy;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerVBloodLegacy();
    }
    public override LegacyUtilities.BloodType GetBloodType()
    {
        return LegacyUtilities.BloodType.VBlood;
    }
}
public class DraculinHandler : BaseBloodHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerDraculinLegacy;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerDraculinLegacy();
    }
    public override LegacyUtilities.BloodType GetBloodType()
    {
        return LegacyUtilities.BloodType.Draculin;
    }
}
public class ImmortalHandler : BaseBloodHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerImmortalLegacy;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerImmortalLegacy();
    }
    public override LegacyUtilities.BloodType GetBloodType()
    {
        return LegacyUtilities.BloodType.Immortal;
    }
}
public class CreatureHandler : BaseBloodHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerCreatureLegacy;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerCreatureLegacy();
    }
    public override LegacyUtilities.BloodType GetBloodType()
    {
        return LegacyUtilities.BloodType.Creature;
    }
}
public class BruteHandler : BaseBloodHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerBruteLegacy;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerBruteLegacy();
    }
    public override LegacyUtilities.BloodType GetBloodType()
    {
        return LegacyUtilities.BloodType.Brute;
    }
}