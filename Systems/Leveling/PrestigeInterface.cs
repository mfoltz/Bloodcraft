using Bloodcraft.Services;

namespace Bloodcraft.Systems.Leveling;
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
    WorkerLegacy,
    WarriorLegacy,
    ScholarLegacy,
    RogueLegacy,
    MutantLegacy,
    DraculinLegacy,
    ImmortalLegacy,
    CreatureLegacy,
    BruteLegacy
}
public interface IPrestigeHandler
{
    void Prestige(ulong steamID);
    int GetPrestigeLevel(ulong steamID);
    KeyValuePair<int, float> GetPrestigeTypeData(ulong steamID);
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
    public abstract void Prestige(ulong steamID);
    public virtual int GetPrestigeLevel(ulong steamID)
    {
        PrestigeType prestigeType = GetPrestigeType();
        if (steamID.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(prestigeType, out var prestigeLevel))
        {
            return prestigeLevel;
        }
        return 0;
    }
    public abstract KeyValuePair<int, float> GetPrestigeTypeData(ulong steamID);
    public abstract PrestigeType GetPrestigeType();
}
public class LevelingPrestigeHandler : BasePrestigeHandler
{
    public override void Prestige(ulong steamID)
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
    public override KeyValuePair<int, float> GetPrestigeTypeData(ulong steamID)
    {
        return steamID.TryGetPlayerExperience(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.Experience;
    }
}
public class SwordPrestigeHandler : BasePrestigeHandler
{
    public override void Prestige(ulong steamID)
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
    public override KeyValuePair<int, float> GetPrestigeTypeData(ulong steamID)
    {
        return steamID.TryGetPlayerSwordExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.SwordExpertise;
    }
}
public class AxePrestigeHandler : BasePrestigeHandler
{
    public override void Prestige(ulong steamID)
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
    public override KeyValuePair<int, float> GetPrestigeTypeData(ulong steamID)
    {
        return steamID.TryGetPlayerAxeExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.AxeExpertise;
    }
}
public class MacePrestigeHandler : BasePrestigeHandler
{
    public override void Prestige(ulong steamID)
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
    public override KeyValuePair<int, float> GetPrestigeTypeData(ulong steamID)
    {
        return steamID.TryGetPlayerMaceExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.MaceExpertise;
    }
}
public class SpearPrestigeHandler : BasePrestigeHandler
{
    public override void Prestige(ulong steamID)
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
    public override KeyValuePair<int, float> GetPrestigeTypeData(ulong steamID)
    {
        return steamID.TryGetPlayerSpearExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.SpearExpertise;
    }
}
public class CrossbowPrestigeHandler : BasePrestigeHandler
{
    public override void Prestige(ulong steamID)
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
    public override KeyValuePair<int, float> GetPrestigeTypeData(ulong steamID)
    {
        return steamID.TryGetPlayerCrossbowExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.CrossbowExpertise;
    }
}
public class GreatSwordPrestigeHandler : BasePrestigeHandler
{
    public override void Prestige(ulong steamID)
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
    public override KeyValuePair<int, float> GetPrestigeTypeData(ulong steamID)
    {
        return steamID.TryGetPlayerGreatSwordExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.GreatSwordExpertise;
    }
}
public class SlashersPrestigeHandler : BasePrestigeHandler
{
    public override void Prestige(ulong steamID)
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
    public override KeyValuePair<int, float> GetPrestigeTypeData(ulong steamID)
    {
        return steamID.TryGetPlayerSlashersExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.SlashersExpertise;
    }
}
public class PistolsPrestigeHandler : BasePrestigeHandler
{
    public override void Prestige(ulong steamID)
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
    public override KeyValuePair<int, float> GetPrestigeTypeData(ulong steamID)
    {
        return steamID.TryGetPlayerPistolsExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.PistolsExpertise;
    }
}
public class ReaperPrestigeHandler : BasePrestigeHandler
{
    public override void Prestige(ulong steamID)
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
    public override KeyValuePair<int, float> GetPrestigeTypeData(ulong steamID)
    {
        return steamID.TryGetPlayerReaperExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.ReaperExpertise;
    }
}
public class LongbowPrestigeHandler : BasePrestigeHandler
{
    public override void Prestige(ulong steamID)
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
    public override KeyValuePair<int, float> GetPrestigeTypeData(ulong steamID)
    {
        return steamID.TryGetPlayerLongbowExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.LongbowExpertise;
    }
}
public class WhipPrestigeHandler : BasePrestigeHandler
{
    public override void Prestige(ulong steamID)
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
    public override KeyValuePair<int, float> GetPrestigeTypeData(ulong steamID)
    {
        return steamID.TryGetPlayerWhipExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.WhipExpertise;
    }
}
public class UnarmedPrestigeHandler : BasePrestigeHandler
{
    public override void Prestige(ulong steamID)
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
    public override KeyValuePair<int, float> GetPrestigeTypeData(ulong steamID)
    {
        return steamID.TryGetPlayerUnarmedExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.UnarmedExpertise;
    }
}
public class FishingPolePrestigeHandler : BasePrestigeHandler
{
    public override void Prestige(ulong steamID)
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
    public override KeyValuePair<int, float> GetPrestigeTypeData(ulong steamID)
    {
        return steamID.TryGetPlayerFishingPoleExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.FishingPoleExpertise;
    }
}
public class WorkerLegacyPrestigeHandler : BasePrestigeHandler
{
    public override void Prestige(ulong steamID)
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
    public override KeyValuePair<int, float> GetPrestigeTypeData(ulong steamID)
    {
        return steamID.TryGetPlayerWorkerLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.WorkerLegacy;
    }
}
public class WarriorLegacyPrestigeHandler : BasePrestigeHandler
{
    public override void Prestige(ulong steamID)
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
    public override KeyValuePair<int, float> GetPrestigeTypeData(ulong steamID)
    {
        return steamID.TryGetPlayerWarriorLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.WarriorLegacy;
    }
}
public class ScholarLegacyPrestigeHandler : BasePrestigeHandler
{
    public override void Prestige(ulong steamID)
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
    public override KeyValuePair<int, float> GetPrestigeTypeData(ulong steamID)
    {
        return steamID.TryGetPlayerScholarLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.ScholarLegacy;
    }
}
public class RogueLegacyPrestigeHandler : BasePrestigeHandler
{
    public override void Prestige(ulong steamID)
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
    public override KeyValuePair<int, float> GetPrestigeTypeData(ulong steamID)
    {
        return steamID.TryGetPlayerRogueLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.RogueLegacy;
    }
}
public class MutantLegacyPrestigeHandler : BasePrestigeHandler
{
    public override void Prestige(ulong steamID)
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
    public override KeyValuePair<int, float> GetPrestigeTypeData(ulong steamID)
    {
        return steamID.TryGetPlayerMutantLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.MutantLegacy;
    }
}
public class DraculinLegacyPrestigeHandler : BasePrestigeHandler
{
    public override void Prestige(ulong steamID)
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
    public override KeyValuePair<int, float> GetPrestigeTypeData(ulong steamID)
    {
        return steamID.TryGetPlayerDraculinLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.DraculinLegacy;
    }
}
public class ImmortalLegacyPrestigeHandler : BasePrestigeHandler
{
    public override void Prestige(ulong steamID)
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
    public override KeyValuePair<int, float> GetPrestigeTypeData(ulong steamID)
    {
        return steamID.TryGetPlayerImmortalLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.ImmortalLegacy;
    }
}
public class CreatureLegacyPrestigeHandler : BasePrestigeHandler
{
    public override void Prestige(ulong steamID)
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
    public override KeyValuePair<int, float> GetPrestigeTypeData(ulong steamID)
    {
        return steamID.TryGetPlayerCreatureLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.CreatureLegacy;
    }
}
public class BrutePrestigeHandler : BasePrestigeHandler
{
    public override void Prestige(ulong steamID)
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
    public override KeyValuePair<int, float> GetPrestigeTypeData(ulong steamID)
    {
        return steamID.TryGetPlayerBruteLegacy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override PrestigeType GetPrestigeType()
    {
        return PrestigeType.BruteLegacy;
    }
}