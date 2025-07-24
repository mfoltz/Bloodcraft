using Bloodcraft.Services;

namespace Bloodcraft.Interfaces;
public enum PrestigeType
{
    Experience,
    Exo,
    SwordExpertise,
    AxeExpertise,
    MaceExpertise,
    SpearExpertise,
    CrossbowExpertise,
    GreatSwordExpertise,
    SlashersExpertise,
    PistolsExpertise,
    ReaperExpertise,
    LongbowExpertise,
    WhipExpertise,
    UnarmedExpertise,
    FishingPoleExpertise,
    TwinBladesExpertise,
    DaggersExpertise,
    ClawsExpertise,
    WorkerLegacy,
    WarriorLegacy,
    ScholarLegacy,
    RogueLegacy,
    MutantLegacy,
    DraculinLegacy,
    ImmortalLegacy,
    CreatureLegacy,
    BruteLegacy,
    CorruptionLegacy
}
internal interface IPrestige
{
    void DoPrestige(ulong steamID);
    int GetPrestigeLevel(ulong steamID);
    KeyValuePair<int, float> GetPrestigeData(ulong steamID);
    PrestigeType GetPrestigeType();
}
internal static class PrestigeFactory
{
    public static IPrestige GetPrestige(PrestigeType prestigeType)
    {
        return prestigeType switch
        {
            PrestigeType.Experience => new LevelingPrestige(),
            PrestigeType.SwordExpertise => new SwordPrestige(),
            PrestigeType.AxeExpertise => new AxePrestige(),
            PrestigeType.MaceExpertise => new MacePrestige(),
            PrestigeType.SpearExpertise => new SpearPrestige(),
            PrestigeType.CrossbowExpertise => new CrossbowPrestige(),
            PrestigeType.GreatSwordExpertise => new GreatSwordPrestige(),
            PrestigeType.SlashersExpertise => new SlashersPrestige(),
            PrestigeType.PistolsExpertise => new PistolsPrestige(),
            PrestigeType.ReaperExpertise => new ReaperPrestige(),
            PrestigeType.LongbowExpertise => new LongbowPrestige(),
            PrestigeType.WhipExpertise => new WhipPrestige(),
            PrestigeType.UnarmedExpertise => new UnarmedPrestige(),
            PrestigeType.FishingPoleExpertise => new FishingPolePrestige(),
            PrestigeType.TwinBladesExpertise => new TwinBladesPrestige(),
            PrestigeType.DaggersExpertise => new DaggersPrestige(),
            PrestigeType.ClawsExpertise => new ClawsPrestige(),
            PrestigeType.WorkerLegacy => new WorkerPrestige(),
            PrestigeType.WarriorLegacy => new WarriorPrestige(),
            PrestigeType.ScholarLegacy => new ScholarPrestige(),
            PrestigeType.RogueLegacy => new RoguePrestige(),
            PrestigeType.MutantLegacy => new MutantPrestige(),
            PrestigeType.DraculinLegacy => new DraculinPrestige(),
            PrestigeType.ImmortalLegacy => new ImmortalPrestige(),
            PrestigeType.CreatureLegacy => new CreaturePrestige(),
            PrestigeType.BruteLegacy => new BrutePrestige(),
            PrestigeType.CorruptionLegacy => new CorruptionPrestige(),
            _ => throw new ArgumentOutOfRangeException(nameof(prestigeType), prestigeType, null)
        };
    }
}
internal abstract class Prestige : IPrestige
{
    public abstract void DoPrestige(ulong steamID);
    public virtual int GetPrestigeLevel(ulong steamID)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamID.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            return prestigeLevel;
        }
        return 0;
    }
    public abstract KeyValuePair<int, float> GetPrestigeData(ulong steamID);
    public abstract PrestigeType GetPrestigeType();
}
internal class LevelingPrestige : Prestige
{
    public override void DoPrestige(ulong steamID)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamID.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamID.TryGetPlayerExperience(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamID.SetPlayerExperience(data);
                steamID.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamID)
    {
        return base.GetPrestigeLevel(steamID);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamID)
    {
        return steamID.TryGetPlayerExperience(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.Experience;
    }
}
internal class SwordPrestige : Prestige
{
    public override void DoPrestige(ulong steamID)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamID.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamID.TryGetPlayerSwordExpertise(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamID.SetPlayerSwordExpertise(data);
                steamID.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamID)
    {
        return base.GetPrestigeLevel(steamID);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamID)
    {
        return steamID.TryGetPlayerSwordExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.SwordExpertise;
    }
}
internal class AxePrestige : Prestige
{
    public override void DoPrestige(ulong steamID)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamID.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamID.TryGetPlayerAxeExpertise(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamID.SetPlayerAxeExpertise(data);
                steamID.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamID)
    {
        return base.GetPrestigeLevel(steamID);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamID)
    {
        return steamID.TryGetPlayerAxeExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.AxeExpertise;
    }
}
internal class MacePrestige : Prestige
{
    public override void DoPrestige(ulong steamID)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamID.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamID.TryGetPlayerMaceExpertise(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamID.SetPlayerMaceExpertise(data);
                steamID.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamID)
    {
        return base.GetPrestigeLevel(steamID);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamID)
    {
        return steamID.TryGetPlayerMaceExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.MaceExpertise;
    }
}
internal class SpearPrestige : Prestige
{
    public override void DoPrestige(ulong steamID)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamID.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamID.TryGetPlayerSpearExpertise(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamID.SetPlayerSpearExpertise(data);
                steamID.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamID)
    {
        return base.GetPrestigeLevel(steamID);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamID)
    {
        return steamID.TryGetPlayerSpearExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.SpearExpertise;
    }
}
internal class CrossbowPrestige : Prestige
{
    public override void DoPrestige(ulong steamID)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamID.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamID.TryGetPlayerCrossbowExpertise(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamID.SetPlayerCrossbowExpertise(data);
                steamID.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamID)
    {
        return base.GetPrestigeLevel(steamID);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamID)
    {
        return steamID.TryGetPlayerCrossbowExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.CrossbowExpertise;
    }
}
internal class GreatSwordPrestige : Prestige
{
    public override void DoPrestige(ulong steamID)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamID.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamID.TryGetPlayerGreatSwordExpertise(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamID.SetPlayerGreatSwordExpertise(data);
                steamID.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamID)
    {
        return base.GetPrestigeLevel(steamID);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamID)
    {
        return steamID.TryGetPlayerGreatSwordExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.GreatSwordExpertise;
    }
}
internal class SlashersPrestige : Prestige
{
    public override void DoPrestige(ulong steamID)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamID.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamID.TryGetPlayerSlashersExpertise(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamID.SetPlayerSlashersExpertise(data);
                steamID.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamID)
    {
        return base.GetPrestigeLevel(steamID);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamID)
    {
        return steamID.TryGetPlayerSlashersExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.SlashersExpertise;
    }
}
internal class PistolsPrestige : Prestige
{
    public override void DoPrestige(ulong steamID)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamID.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamID.TryGetPlayerPistolsExpertise(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamID.SetPlayerPistolsExpertise(data);
                steamID.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamID)
    {
        return base.GetPrestigeLevel(steamID);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamID)
    {
        return steamID.TryGetPlayerPistolsExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.PistolsExpertise;
    }
}
internal class ReaperPrestige : Prestige
{
    public override void DoPrestige(ulong steamID)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamID.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamID.TryGetPlayerReaperExpertise(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamID.SetPlayerReaperExpertise(data);
                steamID.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamID)
    {
        return base.GetPrestigeLevel(steamID);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamID)
    {
        return steamID.TryGetPlayerReaperExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.ReaperExpertise;
    }
}
internal class LongbowPrestige : Prestige
{
    public override void DoPrestige(ulong steamID)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamID.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamID.TryGetPlayerLongbowExpertise(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamID.SetPlayerLongbowExpertise(data);
                steamID.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamID)
    {
        return base.GetPrestigeLevel(steamID);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamID)
    {
        return steamID.TryGetPlayerLongbowExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.LongbowExpertise;
    }
}
internal class WhipPrestige : Prestige
{
    public override void DoPrestige(ulong steamID)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamID.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamID.TryGetPlayerWhipExpertise(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamID.SetPlayerWhipExpertise(data);
                steamID.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamID)
    {
        return base.GetPrestigeLevel(steamID);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamID)
    {
        return steamID.TryGetPlayerWhipExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.WhipExpertise;
    }
}
internal class UnarmedPrestige : Prestige
{
    public override void DoPrestige(ulong steamID)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamID.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamID.TryGetPlayerUnarmedExpertise(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamID.SetPlayerUnarmedExpertise(data);
                steamID.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamID)
    {
        return base.GetPrestigeLevel(steamID);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamID)
    {
        return steamID.TryGetPlayerUnarmedExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.UnarmedExpertise;
    }
}
internal class FishingPolePrestige : Prestige
{
    public override void DoPrestige(ulong steamID)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamID.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamID.TryGetPlayerFishingPoleExpertise(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamID.SetPlayerFishingPoleExpertise(data);
                steamID.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamID)
    {
        return base.GetPrestigeLevel(steamID);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamID)
    {
        return steamID.TryGetPlayerFishingPoleExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.FishingPoleExpertise;
    }
}
internal class TwinBladesPrestige : Prestige
{
    public override void DoPrestige(ulong steamID)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamID.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamID.TryGetPlayerTwinBladesExpertise(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamID.SetPlayerTwinBladesExpertise(data);
                steamID.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamID)
    {
        return base.GetPrestigeLevel(steamID);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamID)
    {
        return steamID.TryGetPlayerTwinBladesExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.TwinBladesExpertise;
    }
}
internal class DaggersPrestige : Prestige
{
    public override void DoPrestige(ulong steamID)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamID.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamID.TryGetPlayerDaggersExpertise(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamID.SetPlayerDaggersExpertise(data);
                steamID.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamID)
    {
        return base.GetPrestigeLevel(steamID);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamID)
    {
        return steamID.TryGetPlayerDaggersExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.DaggersExpertise;
    }
}
internal class ClawsPrestige : Prestige
{
    public override void DoPrestige(ulong steamID)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamID.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamID.TryGetPlayerClawsExpertise(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamID.SetPlayerClawsExpertise(data);
                steamID.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamID)
    {
        return base.GetPrestigeLevel(steamID);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamID)
    {
        return steamID.TryGetPlayerClawsExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.ClawsExpertise;
    }
}
internal class WorkerPrestige : Prestige
{
    public override void DoPrestige(ulong steamID)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamID.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamID.TryGetPlayerWorkerLegacy(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamID.SetPlayerWorkerLegacy(data);
                steamID.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamID)
    {
        return base.GetPrestigeLevel(steamID);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamID)
    {
        return steamID.TryGetPlayerWorkerLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.WorkerLegacy;
    }
}
internal class WarriorPrestige : Prestige
{
    public override void DoPrestige(ulong steamID)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamID.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamID.TryGetPlayerWarriorLegacy(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamID.SetPlayerWarriorLegacy(data);
                steamID.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamID)
    {
        return base.GetPrestigeLevel(steamID);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamID)
    {
        return steamID.TryGetPlayerWarriorLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.WarriorLegacy;
    }
}
internal class ScholarPrestige : Prestige
{
    public override void DoPrestige(ulong steamID)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamID.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamID.TryGetPlayerScholarLegacy(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamID.SetPlayerScholarLegacy(data);
                steamID.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamID)
    {
        return base.GetPrestigeLevel(steamID);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamID)
    {
        return steamID.TryGetPlayerScholarLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.ScholarLegacy;
    }
}
internal class RoguePrestige : Prestige
{
    public override void DoPrestige(ulong steamID)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamID.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamID.TryGetPlayerRogueLegacy(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamID.SetPlayerRogueLegacy(data);
                steamID.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamID)
    {
        return base.GetPrestigeLevel(steamID);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamID)
    {
        return steamID.TryGetPlayerRogueLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.RogueLegacy;
    }
}
internal class MutantPrestige : Prestige
{
    public override void DoPrestige(ulong steamID)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamID.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamID.TryGetPlayerMutantLegacy(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamID.SetPlayerMutantLegacy(data);
                steamID.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamID)
    {
        return base.GetPrestigeLevel(steamID);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamID)
    {
        return steamID.TryGetPlayerMutantLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.MutantLegacy;
    }
}
internal class DraculinPrestige : Prestige
{
    public override void DoPrestige(ulong steamID)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamID.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamID.TryGetPlayerDraculinLegacy(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamID.SetPlayerDraculinLegacy(data);
                steamID.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamID)
    {
        return base.GetPrestigeLevel(steamID);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamID)
    {
        return steamID.TryGetPlayerDraculinLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.DraculinLegacy;
    }
}
internal class ImmortalPrestige : Prestige
{
    public override void DoPrestige(ulong steamID)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamID.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamID.TryGetPlayerImmortalLegacy(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamID.SetPlayerImmortalLegacy(data);
                steamID.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamID)
    {
        return base.GetPrestigeLevel(steamID);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamID)
    {
        return steamID.TryGetPlayerImmortalLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.ImmortalLegacy;
    }
}
internal class CreaturePrestige : Prestige
{
    public override void DoPrestige(ulong steamID)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamID.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamID.TryGetPlayerCreatureLegacy(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamID.SetPlayerCreatureLegacy(data);
                steamID.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamID)
    {
        return base.GetPrestigeLevel(steamID);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamID)
    {
        return steamID.TryGetPlayerCreatureLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.CreatureLegacy;
    }
}
internal class BrutePrestige : Prestige
{
    public override void DoPrestige(ulong steamID)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamID.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamID.TryGetPlayerBruteLegacy(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamID.SetPlayerBruteLegacy(data);
                steamID.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamID)
    {
        return base.GetPrestigeLevel(steamID);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamID)
    {
        return steamID.TryGetPlayerBruteLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.BruteLegacy;
    }
}
internal class CorruptionPrestige : Prestige
{
    public override void DoPrestige(ulong steamID)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamID.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamID.TryGetPlayerCorruptionLegacy(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamID.SetPlayerCorruptionLegacy(data);
                steamID.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamID)
    {
        return base.GetPrestigeLevel(steamID);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamID)
    {
        return steamID.TryGetPlayerCorruptionLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.CorruptionLegacy;
    }
}