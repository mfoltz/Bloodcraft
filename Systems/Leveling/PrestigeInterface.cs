namespace Bloodcraft.SystemUtilities.Leveling;
public interface IPrestigeHandler
{
    void Prestige(ulong steamID);
    KeyValuePair<int, float> GetExperienceData(ulong steamID);
    int GetPrestigeLevel(ulong steamID);
    void SaveChanges();
    PrestigeUtilities.PrestigeType GetPrestigeType();
}
public static class PrestigeHandlerFactory
{
    public static IPrestigeHandler GetPrestigeHandler(PrestigeUtilities.PrestigeType prestigeType)
    {
        return prestigeType switch
        {
            PrestigeUtilities.PrestigeType.Experience => new LevelingPrestigeHandler(),
            PrestigeUtilities.PrestigeType.SwordExpertise => new SwordPrestigeHandler(),
            PrestigeUtilities.PrestigeType.AxeExpertise => new AxePrestigeHandler(),
            PrestigeUtilities.PrestigeType.MaceExpertise => new MacePrestigeHandler(),
            PrestigeUtilities.PrestigeType.SpearExpertise => new SpearPrestigeHandler(),
            PrestigeUtilities.PrestigeType.CrossbowExpertise => new CrossbowPrestigeHandler(),
            PrestigeUtilities.PrestigeType.GreatSwordExpertise => new GreatSwordPrestigeHandler(),
            PrestigeUtilities.PrestigeType.SlashersExpertise => new SlashersPrestigeHandler(),
            PrestigeUtilities.PrestigeType.PistolsExpertise => new PistolsPrestigeHandler(),
            PrestigeUtilities.PrestigeType.ReaperExpertise => new ReaperPrestigeHandler(),
            PrestigeUtilities.PrestigeType.LongbowExpertise => new LongbowPrestigeHandler(),
            PrestigeUtilities.PrestigeType.WhipExpertise => new WhipPrestigeHandler(),
            PrestigeUtilities.PrestigeType.UnarmedExpertise => new UnarmedPrestigeHandler(),
            PrestigeUtilities.PrestigeType.FishingPoleExpertise => new FishingPolePrestigeHandler(),
            PrestigeUtilities.PrestigeType.WorkerLegacy => new WorkerLegacyPrestigeHandler(),
            PrestigeUtilities.PrestigeType.WarriorLegacy => new WarriorLegacyPrestigeHandler(),
            PrestigeUtilities.PrestigeType.ScholarLegacy => new ScholarLegacyPrestigeHandler(),
            PrestigeUtilities.PrestigeType.RogueLegacy => new RogueLegacyPrestigeHandler(),
            PrestigeUtilities.PrestigeType.MutantLegacy => new MutantLegacyPrestigeHandler(),
            PrestigeUtilities.PrestigeType.DraculinLegacy => new DraculinLegacyPrestigeHandler(),
            PrestigeUtilities.PrestigeType.ImmortalLegacy => new ImmortalLegacyPrestigeHandler(),
            PrestigeUtilities.PrestigeType.CreatureLegacy => new CreatureLegacyPrestigeHandler(),
            PrestigeUtilities.PrestigeType.BruteLegacy => new BrutePrestigeHandler(),
            _ => throw new ArgumentOutOfRangeException(nameof(prestigeType), prestigeType, null)
        };
    }
}
public abstract class BasePrestigeHandler : IPrestigeHandler
{
    protected abstract IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure { get; }
    protected abstract IDictionary<ulong, Dictionary<PrestigeUtilities.PrestigeType, int>> SecondaryStructure { get; }
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
    public abstract PrestigeUtilities.PrestigeType GetPrestigeType();
}
public class LevelingPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerExperience;
    protected override IDictionary<ulong, Dictionary<PrestigeUtilities.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerExperience();
    }
    public override PrestigeUtilities.PrestigeType GetPrestigeType()
    {
        return PrestigeUtilities.PrestigeType.Experience;
    }
}
public class SwordPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerSwordExpertise;
    protected override IDictionary<ulong, Dictionary<PrestigeUtilities.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerSwordExpertise();
    }
    public override PrestigeUtilities.PrestigeType GetPrestigeType()
    {
        return PrestigeUtilities.PrestigeType.SwordExpertise;
    }
}
public class AxePrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerAxeExpertise;
    protected override IDictionary<ulong, Dictionary<PrestigeUtilities.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerAxeExpertise();
    }
    public override PrestigeUtilities.PrestigeType GetPrestigeType()
    {
        return PrestigeUtilities.PrestigeType.AxeExpertise;
    }
}
public class MacePrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerMaceExpertise;
    protected override IDictionary<ulong, Dictionary<PrestigeUtilities.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerMaceExpertise();
    }
    public override PrestigeUtilities.PrestigeType GetPrestigeType()
    {
        return PrestigeUtilities.PrestigeType.MaceExpertise;
    }
}
public class SpearPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerSpearExpertise;
    protected override IDictionary<ulong, Dictionary<PrestigeUtilities.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerSpearExpertise();
    }
    public override PrestigeUtilities.PrestigeType GetPrestigeType()
    {
        return PrestigeUtilities.PrestigeType.SpearExpertise;
    }
}
public class CrossbowPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerCrossbowExpertise;
    protected override IDictionary<ulong, Dictionary<PrestigeUtilities.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerCrossbowExpertise();
    }
    public override PrestigeUtilities.PrestigeType GetPrestigeType()
    {
        return PrestigeUtilities.PrestigeType.CrossbowExpertise;
    }
}
public class GreatSwordPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerGreatSwordExpertise;
    protected override IDictionary<ulong, Dictionary<PrestigeUtilities.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerGreatSwordExpertise();
    }
    public override PrestigeUtilities.PrestigeType GetPrestigeType()
    {
        return PrestigeUtilities.PrestigeType.GreatSwordExpertise;
    }
}
public class SlashersPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerSlashersExpertise;
    protected override IDictionary<ulong, Dictionary<PrestigeUtilities.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerSlashersExpertise();
    }
    public override PrestigeUtilities.PrestigeType GetPrestigeType()
    {
        return PrestigeUtilities.PrestigeType.SlashersExpertise;
    }
}
public class PistolsPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerPistolsExpertise;
    protected override IDictionary<ulong, Dictionary<PrestigeUtilities.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerPistolsExpertise();
    }
    public override PrestigeUtilities.PrestigeType GetPrestigeType()
    {
        return PrestigeUtilities.PrestigeType.PistolsExpertise;
    }
}
public class ReaperPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerReaperExpertise;
    protected override IDictionary<ulong, Dictionary<PrestigeUtilities.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerReaperExpertise();
    }
    public override PrestigeUtilities.PrestigeType GetPrestigeType()
    {
        return PrestigeUtilities.PrestigeType.ReaperExpertise;
    }
}
public class LongbowPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerLongbowExpertise;
    protected override IDictionary<ulong, Dictionary<PrestigeUtilities.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerLongbowExpertise();
    }
    public override PrestigeUtilities.PrestigeType GetPrestigeType()
    {
        return PrestigeUtilities.PrestigeType.LongbowExpertise;
    }
}
public class WhipPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerWhipExpertise;
    protected override IDictionary<ulong, Dictionary<PrestigeUtilities.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerWhipExpertise();
    }
    public override PrestigeUtilities.PrestigeType GetPrestigeType()
    {
        return PrestigeUtilities.PrestigeType.WhipExpertise;
    }
}
public class UnarmedPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerUnarmedExpertise;
    protected override IDictionary<ulong, Dictionary<PrestigeUtilities.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerUnarmedExpertise();
    }
    public override PrestigeUtilities.PrestigeType GetPrestigeType()
    {
        return PrestigeUtilities.PrestigeType.UnarmedExpertise;
    }
}
public class FishingPolePrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerFishingPoleExpertise;
    protected override IDictionary<ulong, Dictionary<PrestigeUtilities.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerFishingPoleExpertise();
    }
    public override PrestigeUtilities.PrestigeType GetPrestigeType()
    {
        return PrestigeUtilities.PrestigeType.FishingPoleExpertise;
    }
}
public class WorkerLegacyPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerWorkerLegacy;
    protected override IDictionary<ulong, Dictionary<PrestigeUtilities.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerWorkerLegacy();
    }
    public override PrestigeUtilities.PrestigeType GetPrestigeType()
    {
        return PrestigeUtilities.PrestigeType.WorkerLegacy;
    }
}
public class WarriorLegacyPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerWarriorLegacy;
    protected override IDictionary<ulong, Dictionary<PrestigeUtilities.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerWarriorLegacy();
    }
    public override PrestigeUtilities.PrestigeType GetPrestigeType()
    {
        return PrestigeUtilities.PrestigeType.WarriorLegacy;
    }
}
public class ScholarLegacyPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerScholarLegacy;
    protected override IDictionary<ulong, Dictionary<PrestigeUtilities.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerScholarLegacy();
    }
    public override PrestigeUtilities.PrestigeType GetPrestigeType()
    {
        return PrestigeUtilities.PrestigeType.ScholarLegacy;
    }
}
public class RogueLegacyPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerRogueLegacy;
    protected override IDictionary<ulong, Dictionary<PrestigeUtilities.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerRogueLegacy();
    }
    public override PrestigeUtilities.PrestigeType GetPrestigeType()
    {
        return PrestigeUtilities.PrestigeType.RogueLegacy;
    }
}
public class MutantLegacyPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerMutantLegacy;
    protected override IDictionary<ulong, Dictionary<PrestigeUtilities.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerMutantLegacy();
    }
    public override PrestigeUtilities.PrestigeType GetPrestigeType()
    {
        return PrestigeUtilities.PrestigeType.MutantLegacy;
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

    public override PrestigeSystem.PrestigeType GetPrestigeType()
    {
        return PrestigeSystem.PrestigeType.VBloodLegacy;
    }
}
*/
public class DraculinLegacyPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerDraculinLegacy;
    protected override IDictionary<ulong, Dictionary<PrestigeUtilities.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerDraculinLegacy();
    }
    public override PrestigeUtilities.PrestigeType GetPrestigeType()
    {
        return PrestigeUtilities.PrestigeType.DraculinLegacy;
    }
}
public class ImmortalLegacyPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerImmortalLegacy;
    protected override IDictionary<ulong, Dictionary<PrestigeUtilities.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerImmortalLegacy();
    }
    public override PrestigeUtilities.PrestigeType GetPrestigeType()
    {
        return PrestigeUtilities.PrestigeType.ImmortalLegacy;
    }
}
public class CreatureLegacyPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerCreatureLegacy;
    protected override IDictionary<ulong, Dictionary<PrestigeUtilities.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerCreatureLegacy();
    }
    public override PrestigeUtilities.PrestigeType GetPrestigeType()
    {
        return PrestigeUtilities.PrestigeType.CreatureLegacy;
    }
}
public class BrutePrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerBruteLegacy;
    protected override IDictionary<ulong, Dictionary<PrestigeUtilities.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerBruteLegacy();
    }
    public override PrestigeUtilities.PrestigeType GetPrestigeType()
    {
        return PrestigeUtilities.PrestigeType.BruteLegacy;
    }
}