namespace Bloodcraft.Systems.Leveling;
public interface IPrestigeHandler
{
    void Prestige(ulong steamID);
    KeyValuePair<int, float> GetExperienceData(ulong steamID);
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
            PrestigeSystem.PrestigeType.Sanguimancy => new SanguimancyPrestigeHandler(),
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
    protected abstract IDictionary<ulong, KeyValuePair<int, float>> DataStructure { get; }

    public void Prestige(ulong steamID)
    {
        if (DataStructure.TryGetValue(steamID, out var currentData))
        {
            DataStructure[steamID] = new KeyValuePair<int, float>(0, 0);
            // Handle the prestige increase logic
            Core.DataStructures.PlayerPrestiges[steamID][GetPrestigeType()] += 1;
            Core.DataStructures.SavePlayerPrestiges();
        }
    }
    public KeyValuePair<int, float> GetExperienceData(ulong steamID)
    {
        if (DataStructure.TryGetValue(steamID, out var xpData))
            return xpData;
        return new KeyValuePair<int, float>(0, 0);
    }

    public abstract void SaveChanges();
    public abstract PrestigeSystem.PrestigeType GetPrestigeType();
}

// Implementations for leveling
public class LevelingPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerExperience;

    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerExperience();
    }

    public override PrestigeSystem.PrestigeType GetPrestigeType()
    {
        return PrestigeSystem.PrestigeType.Experience;
    }
}

// Implementations for each weapon and legacy type
public class SwordPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerSwordExpertise;

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
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerAxeExpertise;

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
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerMaceExpertise;

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
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerSpearExpertise;

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
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerCrossbowExpertise;

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
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerGreatSwordExpertise;

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
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerSlashersExpertise;

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
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerPistolsExpertise;

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
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerReaperExpertise;

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
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerLongbowExpertise;

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
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerWhipExpertise;

    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerWhipExpertise();
    }

    public override PrestigeSystem.PrestigeType GetPrestigeType()
    {
        return PrestigeSystem.PrestigeType.WhipExpertise;
    }
}
public class SanguimancyPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerSanguimancy;

    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerSanguimancy();
    }

    public override PrestigeSystem.PrestigeType GetPrestigeType()
    {
        return PrestigeSystem.PrestigeType.Sanguimancy;
    }
}

public class WorkerLegacyPrestigeHandler : BasePrestigeHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerWorkerLegacy;

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
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerWarriorLegacy;

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
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerScholarLegacy;

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
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerRogueLegacy;

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
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerMutantLegacy;

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
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerDraculinLegacy;

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
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerImmortalLegacy;

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
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerCreatureLegacy;

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
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerBruteLegacy;

    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerBruteLegacy();
    }

    public override PrestigeSystem.PrestigeType GetPrestigeType()
    {
        return PrestigeSystem.PrestigeType.BruteLegacy;
    }
}