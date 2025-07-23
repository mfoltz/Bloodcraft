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
    void DoPrestige(ulong steamId);
    int GetPrestigeLevel(ulong steamId);
    KeyValuePair<int, float> GetPrestigeData(ulong steamId);
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
    public abstract void DoPrestige(ulong steamId);
    public virtual int GetPrestigeLevel(ulong steamId)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            return prestigeLevel;
        }
        return 0;
    }
    public abstract KeyValuePair<int, float> GetPrestigeData(ulong steamId);
    public abstract PrestigeType GetPrestigeType();
}
internal class LevelingPrestige : Prestige
{
    public override void DoPrestige(ulong steamId)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamId.TryGetPlayerExperience(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamId.SetPlayerExperience(data);
                steamId.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamId)
    {
        return base.GetPrestigeLevel(steamId);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamId)
    {
        return steamId.TryGetPlayerExperience(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.Experience;
    }
}
internal class SwordPrestige : Prestige
{
    public override void DoPrestige(ulong steamId)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamId.TryGetPlayerSwordExpertise(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamId.SetPlayerSwordExpertise(data);
                steamId.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamId)
    {
        return base.GetPrestigeLevel(steamId);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamId)
    {
        return steamId.TryGetPlayerSwordExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.SwordExpertise;
    }
}
internal class AxePrestige : Prestige
{
    public override void DoPrestige(ulong steamId)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamId.TryGetPlayerAxeExpertise(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamId.SetPlayerAxeExpertise(data);
                steamId.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamId)
    {
        return base.GetPrestigeLevel(steamId);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamId)
    {
        return steamId.TryGetPlayerAxeExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.AxeExpertise;
    }
}
internal class MacePrestige : Prestige
{
    public override void DoPrestige(ulong steamId)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamId.TryGetPlayerMaceExpertise(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamId.SetPlayerMaceExpertise(data);
                steamId.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamId)
    {
        return base.GetPrestigeLevel(steamId);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamId)
    {
        return steamId.TryGetPlayerMaceExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.MaceExpertise;
    }
}
internal class SpearPrestige : Prestige
{
    public override void DoPrestige(ulong steamId)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamId.TryGetPlayerSpearExpertise(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamId.SetPlayerSpearExpertise(data);
                steamId.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamId)
    {
        return base.GetPrestigeLevel(steamId);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamId)
    {
        return steamId.TryGetPlayerSpearExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.SpearExpertise;
    }
}
internal class CrossbowPrestige : Prestige
{
    public override void DoPrestige(ulong steamId)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamId.TryGetPlayerCrossbowExpertise(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamId.SetPlayerCrossbowExpertise(data);
                steamId.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamId)
    {
        return base.GetPrestigeLevel(steamId);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamId)
    {
        return steamId.TryGetPlayerCrossbowExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.CrossbowExpertise;
    }
}
internal class GreatSwordPrestige : Prestige
{
    public override void DoPrestige(ulong steamId)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamId.TryGetPlayerGreatSwordExpertise(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamId.SetPlayerGreatSwordExpertise(data);
                steamId.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamId)
    {
        return base.GetPrestigeLevel(steamId);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamId)
    {
        return steamId.TryGetPlayerGreatSwordExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.GreatSwordExpertise;
    }
}
internal class SlashersPrestige : Prestige
{
    public override void DoPrestige(ulong steamId)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamId.TryGetPlayerSlashersExpertise(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamId.SetPlayerSlashersExpertise(data);
                steamId.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamId)
    {
        return base.GetPrestigeLevel(steamId);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamId)
    {
        return steamId.TryGetPlayerSlashersExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.SlashersExpertise;
    }
}
internal class PistolsPrestige : Prestige
{
    public override void DoPrestige(ulong steamId)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamId.TryGetPlayerPistolsExpertise(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamId.SetPlayerPistolsExpertise(data);
                steamId.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamId)
    {
        return base.GetPrestigeLevel(steamId);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamId)
    {
        return steamId.TryGetPlayerPistolsExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.PistolsExpertise;
    }
}
internal class ReaperPrestige : Prestige
{
    public override void DoPrestige(ulong steamId)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamId.TryGetPlayerReaperExpertise(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamId.SetPlayerReaperExpertise(data);
                steamId.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamId)
    {
        return base.GetPrestigeLevel(steamId);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamId)
    {
        return steamId.TryGetPlayerReaperExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.ReaperExpertise;
    }
}
internal class LongbowPrestige : Prestige
{
    public override void DoPrestige(ulong steamId)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamId.TryGetPlayerLongbowExpertise(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamId.SetPlayerLongbowExpertise(data);
                steamId.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamId)
    {
        return base.GetPrestigeLevel(steamId);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamId)
    {
        return steamId.TryGetPlayerLongbowExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.LongbowExpertise;
    }
}
internal class WhipPrestige : Prestige
{
    public override void DoPrestige(ulong steamId)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamId.TryGetPlayerWhipExpertise(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamId.SetPlayerWhipExpertise(data);
                steamId.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamId)
    {
        return base.GetPrestigeLevel(steamId);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamId)
    {
        return steamId.TryGetPlayerWhipExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.WhipExpertise;
    }
}
internal class UnarmedPrestige : Prestige
{
    public override void DoPrestige(ulong steamId)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamId.TryGetPlayerUnarmedExpertise(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamId.SetPlayerUnarmedExpertise(data);
                steamId.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamId)
    {
        return base.GetPrestigeLevel(steamId);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamId)
    {
        return steamId.TryGetPlayerUnarmedExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.UnarmedExpertise;
    }
}
internal class FishingPolePrestige : Prestige
{
    public override void DoPrestige(ulong steamId)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamId.TryGetPlayerFishingPoleExpertise(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamId.SetPlayerFishingPoleExpertise(data);
                steamId.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamId)
    {
        return base.GetPrestigeLevel(steamId);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamId)
    {
        return steamId.TryGetPlayerFishingPoleExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.FishingPoleExpertise;
    }
}
internal class TwinBladesPrestige : Prestige
{
    public override void DoPrestige(ulong steamId)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamId.TryGetPlayerTwinBladesExpertise(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamId.SetPlayerTwinBladesExpertise(data);
                steamId.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamId)
    {
        return base.GetPrestigeLevel(steamId);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamId)
    {
        return steamId.TryGetPlayerTwinBladesExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.TwinBladesExpertise;
    }
}
internal class DaggersPrestige : Prestige
{
    public override void DoPrestige(ulong steamId)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamId.TryGetPlayerDaggersExpertise(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamId.SetPlayerDaggersExpertise(data);
                steamId.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamId)
    {
        return base.GetPrestigeLevel(steamId);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamId)
    {
        return steamId.TryGetPlayerDaggersExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.DaggersExpertise;
    }
}
internal class ClawsPrestige : Prestige
{
    public override void DoPrestige(ulong steamId)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamId.TryGetPlayerClawsExpertise(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamId.SetPlayerClawsExpertise(data);
                steamId.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamId)
    {
        return base.GetPrestigeLevel(steamId);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamId)
    {
        return steamId.TryGetPlayerClawsExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.ClawsExpertise;
    }
}
internal class WorkerPrestige : Prestige
{
    public override void DoPrestige(ulong steamId)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamId.TryGetPlayerWorkerLegacy(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamId.SetPlayerWorkerLegacy(data);
                steamId.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamId)
    {
        return base.GetPrestigeLevel(steamId);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamId)
    {
        return steamId.TryGetPlayerWorkerLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.WorkerLegacy;
    }
}
internal class WarriorPrestige : Prestige
{
    public override void DoPrestige(ulong steamId)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamId.TryGetPlayerWarriorLegacy(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamId.SetPlayerWarriorLegacy(data);
                steamId.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamId)
    {
        return base.GetPrestigeLevel(steamId);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamId)
    {
        return steamId.TryGetPlayerWarriorLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.WarriorLegacy;
    }
}
internal class ScholarPrestige : Prestige
{
    public override void DoPrestige(ulong steamId)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamId.TryGetPlayerScholarLegacy(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamId.SetPlayerScholarLegacy(data);
                steamId.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamId)
    {
        return base.GetPrestigeLevel(steamId);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamId)
    {
        return steamId.TryGetPlayerScholarLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.ScholarLegacy;
    }
}
internal class RoguePrestige : Prestige
{
    public override void DoPrestige(ulong steamId)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamId.TryGetPlayerRogueLegacy(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamId.SetPlayerRogueLegacy(data);
                steamId.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamId)
    {
        return base.GetPrestigeLevel(steamId);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamId)
    {
        return steamId.TryGetPlayerRogueLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.RogueLegacy;
    }
}
internal class MutantPrestige : Prestige
{
    public override void DoPrestige(ulong steamId)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamId.TryGetPlayerMutantLegacy(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamId.SetPlayerMutantLegacy(data);
                steamId.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamId)
    {
        return base.GetPrestigeLevel(steamId);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamId)
    {
        return steamId.TryGetPlayerMutantLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.MutantLegacy;
    }
}
internal class DraculinPrestige : Prestige
{
    public override void DoPrestige(ulong steamId)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamId.TryGetPlayerDraculinLegacy(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamId.SetPlayerDraculinLegacy(data);
                steamId.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamId)
    {
        return base.GetPrestigeLevel(steamId);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamId)
    {
        return steamId.TryGetPlayerDraculinLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.DraculinLegacy;
    }
}
internal class ImmortalPrestige : Prestige
{
    public override void DoPrestige(ulong steamId)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamId.TryGetPlayerImmortalLegacy(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamId.SetPlayerImmortalLegacy(data);
                steamId.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamId)
    {
        return base.GetPrestigeLevel(steamId);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamId)
    {
        return steamId.TryGetPlayerImmortalLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.ImmortalLegacy;
    }
}
internal class CreaturePrestige : Prestige
{
    public override void DoPrestige(ulong steamId)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamId.TryGetPlayerCreatureLegacy(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamId.SetPlayerCreatureLegacy(data);
                steamId.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamId)
    {
        return base.GetPrestigeLevel(steamId);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamId)
    {
        return steamId.TryGetPlayerCreatureLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.CreatureLegacy;
    }
}
internal class BrutePrestige : Prestige
{
    public override void DoPrestige(ulong steamId)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamId.TryGetPlayerBruteLegacy(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamId.SetPlayerBruteLegacy(data);
                steamId.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamId)
    {
        return base.GetPrestigeLevel(steamId);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamId)
    {
        return steamId.TryGetPlayerBruteLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.BruteLegacy;
    }
}
internal class CorruptionPrestige : Prestige
{
    public override void DoPrestige(ulong steamId)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            if (steamId.TryGetPlayerCorruptionLegacy(out var data))
            {
                data = new KeyValuePair<int, float>(0, 0);
                prestigeLevel++;
                prestigeData[prestigeType] = prestigeLevel;
                steamId.SetPlayerCorruptionLegacy(data);
                steamId.SetPlayerPrestiges(prestigeData);
            }
        }
    }
    public override int GetPrestigeLevel(ulong steamId)
    {
        return base.GetPrestigeLevel(steamId);
    }
    public override KeyValuePair<int, float> GetPrestigeData(ulong steamId)
    {
        return steamId.TryGetPlayerCorruptionLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.CorruptionLegacy;
    }
}