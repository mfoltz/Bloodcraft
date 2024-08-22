using static Bloodcraft.Systems.Leveling.PrestigeSystem;

namespace Bloodcraft.Systems.Leveling;
public interface IPrestigeHandler
{
    void Prestige(ulong steamID);
    KeyValuePair<int, float> GetExperienceData(ulong steamID);
    int GetPrestigeLevel(ulong steamID);
    void SaveChanges();
    PrestigeType GetPrestigeType();
}
public static class PrestigeHandlerFactory
{
    public static IPrestigeHandler GetPrestigeHandler(PrestigeType prestigeType)
    {
        return prestigeType switch
        {
            PrestigeType.Experience => new LevelingPrestigeHandler(),
            PrestigeType.SwordExpertise => new SwordPrestigeHandler(),
            PrestigeType.AxeExpertise => new AxePrestigeHandler(),
            PrestigeType.MaceExpertise => new MacePrestigeHandler(),
            PrestigeType.SpearExpertise => new SpearPrestigeHandler(),
            PrestigeType.CrossbowExpertise => new CrossbowPrestigeHandler(),
            PrestigeType.GreatSwordExpertise => new GreatSwordPrestigeHandler(),
            PrestigeType.SlashersExpertise => new SlashersPrestigeHandler(),
            PrestigeType.PistolsExpertise => new PistolsPrestigeHandler(),
            PrestigeType.ReaperExpertise => new ReaperPrestigeHandler(),
            PrestigeType.LongbowExpertise => new LongbowPrestigeHandler(),
            PrestigeType.WhipExpertise => new WhipPrestigeHandler(),
            PrestigeType.UnarmedExpertise => new UnarmedPrestigeHandler(),
            PrestigeType.FishingPoleExpertise => new FishingPolePrestigeHandler(),
            PrestigeType.WorkerLegacy => new WorkerLegacyPrestigeHandler(),
            PrestigeType.WarriorLegacy => new WarriorLegacyPrestigeHandler(),
            PrestigeType.ScholarLegacy => new ScholarLegacyPrestigeHandler(),
            PrestigeType.RogueLegacy => new RogueLegacyPrestigeHandler(),
            PrestigeType.MutantLegacy => new MutantLegacyPrestigeHandler(),
            PrestigeType.DraculinLegacy => new DraculinLegacyPrestigeHandler(),
            PrestigeType.ImmortalLegacy => new ImmortalLegacyPrestigeHandler(),
            PrestigeType.CreatureLegacy => new CreatureLegacyPrestigeHandler(),
            PrestigeType.BruteLegacy => new BrutePrestigeHandler(),
            _ => throw new ArgumentOutOfRangeException(nameof(prestigeType), prestigeType, null)
        };
    }
}
public abstract class BasePrestigeHandler : IPrestigeHandler
{
    protected abstract IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure { get; }
    protected abstract IDictionary<ulong, Dictionary<PrestigeType, int>> SecondaryStructure { get; }
    public void Prestige(ulong steamID)
    {
        if (PrimaryStructure.ContainsKey(steamID))
        {
            PrimaryStructure[steamID] = new KeyValuePair<int, float>(0, 0);
            Core.DataStructures.PlayerPrestiges[steamID][GetPrestigeType()] += 1;
            Core.DataStructures.SavePlayerPrestiges();
        }
    }
    public KeyValuePair<int, float> GetExperienceData(ulong steamID)
    {
        if (PrimaryStructure.TryGetValue(steamID, out var xpData)) return xpData;
        return new KeyValuePair<int, float>(0, 0);
    }
    public int GetPrestigeLevel(ulong steamID)
    {
        if (SecondaryStructure.TryGetValue(steamID, out var prestigeData))
        {
            if (prestigeData.TryGetValue(GetPrestigeType(), out var prestigeLevel))
            {
                return prestigeLevel;
            }
        }
        return 0;
    }
    public abstract void SaveChanges();
    public abstract PrestigeType GetPrestigeType();
}
public class LevelingPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerExperience;
    protected override IDictionary<ulong, Dictionary<PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerExperience();
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.Experience;
    }
}
public class SwordPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerSwordExpertise;
    protected override IDictionary<ulong, Dictionary<PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerSwordExpertise();
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.SwordExpertise;
    }
}
public class AxePrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerAxeExpertise;
    protected override IDictionary<ulong, Dictionary<PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerAxeExpertise();
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.AxeExpertise;
    }
}
public class MacePrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerMaceExpertise;
    protected override IDictionary<ulong, Dictionary<PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerMaceExpertise();
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.MaceExpertise;
    }
}
public class SpearPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerSpearExpertise;
    protected override IDictionary<ulong, Dictionary<PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerSpearExpertise();
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.SpearExpertise;
    }
}
public class CrossbowPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerCrossbowExpertise;
    protected override IDictionary<ulong, Dictionary<PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerCrossbowExpertise();
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.CrossbowExpertise;
    }
}
public class GreatSwordPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerGreatSwordExpertise;
    protected override IDictionary<ulong, Dictionary<PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerGreatSwordExpertise();
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.GreatSwordExpertise;
    }
}
public class SlashersPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerSlashersExpertise;
    protected override IDictionary<ulong, Dictionary<PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerSlashersExpertise();
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.SlashersExpertise;
    }
}
public class PistolsPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerPistolsExpertise;
    protected override IDictionary<ulong, Dictionary<PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerPistolsExpertise();
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.PistolsExpertise;
    }
}
public class ReaperPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerReaperExpertise;
    protected override IDictionary<ulong, Dictionary<PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerReaperExpertise();
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.ReaperExpertise;
    }
}
public class LongbowPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerLongbowExpertise;
    protected override IDictionary<ulong, Dictionary<PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerLongbowExpertise();
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.LongbowExpertise;
    }
}
public class WhipPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerWhipExpertise;
    protected override IDictionary<ulong, Dictionary<PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerWhipExpertise();
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.WhipExpertise;
    }
}
public class UnarmedPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerUnarmedExpertise;
    protected override IDictionary<ulong, Dictionary<PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerUnarmedExpertise();
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.UnarmedExpertise;
    }
}
public class FishingPolePrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerFishingPoleExpertise;
    protected override IDictionary<ulong, Dictionary<PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerFishingPoleExpertise();
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.FishingPoleExpertise;
    }
}
public class WorkerLegacyPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerWorkerLegacy;
    protected override IDictionary<ulong, Dictionary<PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerWorkerLegacy();
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.WorkerLegacy;
    }
}
public class WarriorLegacyPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerWarriorLegacy;
    protected override IDictionary<ulong, Dictionary<PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerWarriorLegacy();
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.WarriorLegacy;
    }
}
public class ScholarLegacyPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerScholarLegacy;
    protected override IDictionary<ulong, Dictionary<PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerScholarLegacy();
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.ScholarLegacy;
    }
}
public class RogueLegacyPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerRogueLegacy;
    protected override IDictionary<ulong, Dictionary<PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerRogueLegacy();
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.RogueLegacy;
    }
}
public class MutantLegacyPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerMutantLegacy;
    protected override IDictionary<ulong, Dictionary<PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerMutantLegacy();
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.MutantLegacy;
    }
}
/*
public class VBloodLegacyPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerVBloodLegacy;

    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerVBloodLegacy();
    }

    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.VBloodLegacy;
    }
}
*/
public class DraculinLegacyPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerDraculinLegacy;
    protected override IDictionary<ulong, Dictionary<PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerDraculinLegacy();
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.DraculinLegacy;
    }
}
public class ImmortalLegacyPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerImmortalLegacy;
    protected override IDictionary<ulong, Dictionary<PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerImmortalLegacy();
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.ImmortalLegacy;
    }
}
public class CreatureLegacyPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerCreatureLegacy;
    protected override IDictionary<ulong, Dictionary<PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerCreatureLegacy();
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.CreatureLegacy;
    }
}
public class BrutePrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerBruteLegacy;
    protected override IDictionary<ulong, Dictionary<PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerBruteLegacy();
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.BruteLegacy;
    }
}