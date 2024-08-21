namespace Bloodcraft.SystemUtilities.Leveling;
public interface IPrestigeHandler
{
    void Prestige(ulong steamID);
    KeyValuePair<int, float> GetExperienceData(ulong steamID);
    int GetPrestigeLevel(ulong steamID);
    void SaveChanges();
    PrestigeSystem.PrestigeType GetPrestigeType();
}
public static class PrestigeHandlerFactory
{
    public static IPrestigeHandler GetPrestigeHandler(PrestigeSystem.PrestigeType prestigeType)
    {
        return prestigeType switch
        {
            PrestigeSystem.PrestigeType.Experience => new LevelingPrestigeHandler(),
            PrestigeSystem.PrestigeType.SwordExpertise => new SwordPrestigeHandler(),
            PrestigeSystem.PrestigeType.AxeExpertise => new AxePrestigeHandler(),
            PrestigeSystem.PrestigeType.MaceExpertise => new MacePrestigeHandler(),
            PrestigeSystem.PrestigeType.SpearExpertise => new SpearPrestigeHandler(),
            PrestigeSystem.PrestigeType.CrossbowExpertise => new CrossbowPrestigeHandler(),
            PrestigeSystem.PrestigeType.GreatSwordExpertise => new GreatSwordPrestigeHandler(),
            PrestigeSystem.PrestigeType.SlashersExpertise => new SlashersPrestigeHandler(),
            PrestigeSystem.PrestigeType.PistolsExpertise => new PistolsPrestigeHandler(),
            PrestigeSystem.PrestigeType.ReaperExpertise => new ReaperPrestigeHandler(),
            PrestigeSystem.PrestigeType.LongbowExpertise => new LongbowPrestigeHandler(),
            PrestigeSystem.PrestigeType.WhipExpertise => new WhipPrestigeHandler(),
            PrestigeSystem.PrestigeType.UnarmedExpertise => new UnarmedPrestigeHandler(),
            PrestigeSystem.PrestigeType.FishingPoleExpertise => new FishingPolePrestigeHandler(),
            PrestigeSystem.PrestigeType.WorkerLegacy => new WorkerLegacyPrestigeHandler(),
            PrestigeSystem.PrestigeType.WarriorLegacy => new WarriorLegacyPrestigeHandler(),
            PrestigeSystem.PrestigeType.ScholarLegacy => new ScholarLegacyPrestigeHandler(),
            PrestigeSystem.PrestigeType.RogueLegacy => new RogueLegacyPrestigeHandler(),
            PrestigeSystem.PrestigeType.MutantLegacy => new MutantLegacyPrestigeHandler(),
            PrestigeSystem.PrestigeType.DraculinLegacy => new DraculinLegacyPrestigeHandler(),
            PrestigeSystem.PrestigeType.ImmortalLegacy => new ImmortalLegacyPrestigeHandler(),
            PrestigeSystem.PrestigeType.CreatureLegacy => new CreatureLegacyPrestigeHandler(),
            PrestigeSystem.PrestigeType.BruteLegacy => new BrutePrestigeHandler(),
            _ => throw new ArgumentOutOfRangeException(nameof(prestigeType), prestigeType, null)
        };
    }
}
public abstract class BasePrestigeHandler : IPrestigeHandler
{
    protected abstract IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure { get; }
    protected abstract IDictionary<ulong, Dictionary<PrestigeSystem.PrestigeType, int>> SecondaryStructure { get; }
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
    public abstract PrestigeSystem.PrestigeType GetPrestigeType();
}
public class LevelingPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerExperience;
    protected override IDictionary<ulong, Dictionary<PrestigeSystem.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerExperience();
    }
    public override PrestigeSystem.PrestigeType GetPrestigeType()
    {
        return PrestigeSystem.PrestigeType.Experience;
    }
}
public class SwordPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerSwordExpertise;
    protected override IDictionary<ulong, Dictionary<PrestigeSystem.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerSwordExpertise();
    }
    public override PrestigeSystem.PrestigeType GetPrestigeType()
    {
        return PrestigeSystem.PrestigeType.SwordExpertise;
    }
}
public class AxePrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerAxeExpertise;
    protected override IDictionary<ulong, Dictionary<PrestigeSystem.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerAxeExpertise();
    }
    public override PrestigeSystem.PrestigeType GetPrestigeType()
    {
        return PrestigeSystem.PrestigeType.AxeExpertise;
    }
}
public class MacePrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerMaceExpertise;
    protected override IDictionary<ulong, Dictionary<PrestigeSystem.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerMaceExpertise();
    }
    public override PrestigeSystem.PrestigeType GetPrestigeType()
    {
        return PrestigeSystem.PrestigeType.MaceExpertise;
    }
}
public class SpearPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerSpearExpertise;
    protected override IDictionary<ulong, Dictionary<PrestigeSystem.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerSpearExpertise();
    }
    public override PrestigeSystem.PrestigeType GetPrestigeType()
    {
        return PrestigeSystem.PrestigeType.SpearExpertise;
    }
}
public class CrossbowPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerCrossbowExpertise;
    protected override IDictionary<ulong, Dictionary<PrestigeSystem.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerCrossbowExpertise();
    }
    public override PrestigeSystem.PrestigeType GetPrestigeType()
    {
        return PrestigeSystem.PrestigeType.CrossbowExpertise;
    }
}
public class GreatSwordPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerGreatSwordExpertise;
    protected override IDictionary<ulong, Dictionary<PrestigeSystem.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerGreatSwordExpertise();
    }
    public override PrestigeSystem.PrestigeType GetPrestigeType()
    {
        return PrestigeSystem.PrestigeType.GreatSwordExpertise;
    }
}
public class SlashersPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerSlashersExpertise;
    protected override IDictionary<ulong, Dictionary<PrestigeSystem.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerSlashersExpertise();
    }
    public override PrestigeSystem.PrestigeType GetPrestigeType()
    {
        return PrestigeSystem.PrestigeType.SlashersExpertise;
    }
}
public class PistolsPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerPistolsExpertise;
    protected override IDictionary<ulong, Dictionary<PrestigeSystem.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerPistolsExpertise();
    }
    public override PrestigeSystem.PrestigeType GetPrestigeType()
    {
        return PrestigeSystem.PrestigeType.PistolsExpertise;
    }
}
public class ReaperPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerReaperExpertise;
    protected override IDictionary<ulong, Dictionary<PrestigeSystem.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerReaperExpertise();
    }
    public override PrestigeSystem.PrestigeType GetPrestigeType()
    {
        return PrestigeSystem.PrestigeType.ReaperExpertise;
    }
}
public class LongbowPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerLongbowExpertise;
    protected override IDictionary<ulong, Dictionary<PrestigeSystem.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerLongbowExpertise();
    }
    public override PrestigeSystem.PrestigeType GetPrestigeType()
    {
        return PrestigeSystem.PrestigeType.LongbowExpertise;
    }
}
public class WhipPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerWhipExpertise;
    protected override IDictionary<ulong, Dictionary<PrestigeSystem.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerWhipExpertise();
    }
    public override PrestigeSystem.PrestigeType GetPrestigeType()
    {
        return PrestigeSystem.PrestigeType.WhipExpertise;
    }
}
public class UnarmedPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerUnarmedExpertise;
    protected override IDictionary<ulong, Dictionary<PrestigeSystem.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerUnarmedExpertise();
    }
    public override PrestigeSystem.PrestigeType GetPrestigeType()
    {
        return PrestigeSystem.PrestigeType.UnarmedExpertise;
    }
}
public class FishingPolePrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerFishingPoleExpertise;
    protected override IDictionary<ulong, Dictionary<PrestigeSystem.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerFishingPoleExpertise();
    }
    public override PrestigeSystem.PrestigeType GetPrestigeType()
    {
        return PrestigeSystem.PrestigeType.FishingPoleExpertise;
    }
}
public class WorkerLegacyPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerWorkerLegacy;
    protected override IDictionary<ulong, Dictionary<PrestigeSystem.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerWorkerLegacy();
    }
    public override PrestigeSystem.PrestigeType GetPrestigeType()
    {
        return PrestigeSystem.PrestigeType.WorkerLegacy;
    }
}
public class WarriorLegacyPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerWarriorLegacy;
    protected override IDictionary<ulong, Dictionary<PrestigeSystem.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerWarriorLegacy();
    }
    public override PrestigeSystem.PrestigeType GetPrestigeType()
    {
        return PrestigeSystem.PrestigeType.WarriorLegacy;
    }
}
public class ScholarLegacyPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerScholarLegacy;
    protected override IDictionary<ulong, Dictionary<PrestigeSystem.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerScholarLegacy();
    }
    public override PrestigeSystem.PrestigeType GetPrestigeType()
    {
        return PrestigeSystem.PrestigeType.ScholarLegacy;
    }
}
public class RogueLegacyPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerRogueLegacy;
    protected override IDictionary<ulong, Dictionary<PrestigeSystem.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerRogueLegacy();
    }
    public override PrestigeSystem.PrestigeType GetPrestigeType()
    {
        return PrestigeSystem.PrestigeType.RogueLegacy;
    }
}
public class MutantLegacyPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerMutantLegacy;
    protected override IDictionary<ulong, Dictionary<PrestigeSystem.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerMutantLegacy();
    }
    public override PrestigeSystem.PrestigeType GetPrestigeType()
    {
        return PrestigeSystem.PrestigeType.MutantLegacy;
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
    protected override IDictionary<ulong, Dictionary<PrestigeSystem.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerDraculinLegacy();
    }
    public override PrestigeSystem.PrestigeType GetPrestigeType()
    {
        return PrestigeSystem.PrestigeType.DraculinLegacy;
    }
}
public class ImmortalLegacyPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerImmortalLegacy;
    protected override IDictionary<ulong, Dictionary<PrestigeSystem.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerImmortalLegacy();
    }
    public override PrestigeSystem.PrestigeType GetPrestigeType()
    {
        return PrestigeSystem.PrestigeType.ImmortalLegacy;
    }
}
public class CreatureLegacyPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerCreatureLegacy;
    protected override IDictionary<ulong, Dictionary<PrestigeSystem.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerCreatureLegacy();
    }
    public override PrestigeSystem.PrestigeType GetPrestigeType()
    {
        return PrestigeSystem.PrestigeType.CreatureLegacy;
    }
}
public class BrutePrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> PrimaryStructure => Core.DataStructures.PlayerBruteLegacy;
    protected override IDictionary<ulong, Dictionary<PrestigeSystem.PrestigeType, int>> SecondaryStructure => Core.DataStructures.PlayerPrestiges;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerBruteLegacy();
    }
    public override PrestigeSystem.PrestigeType GetPrestigeType()
    {
        return PrestigeSystem.PrestigeType.BruteLegacy;
    }
}