namespace Bloodcraft.Systems.Expertise;

public interface IExpertiseHandler
{
    void AddExpertise(ulong steamID, float experience);

    void SaveChanges();

    KeyValuePair<int, float> GetExpertiseData(ulong steamID);

    void UpdateExpertiseData(ulong steamID, KeyValuePair<int, float> xpData);

    ExpertiseUtilities.WeaponType GetWeaponType();
}

public static class ExpertiseHandlerFactory
{
    public static IExpertiseHandler GetExpertiseHandler(ExpertiseUtilities.WeaponType weaponType)
    {
        return weaponType switch
        {
            ExpertiseUtilities.WeaponType.Sword => new SwordHandler(),
            ExpertiseUtilities.WeaponType.Axe => new AxeHandler(),
            ExpertiseUtilities.WeaponType.Mace => new MaceHandler(),
            ExpertiseUtilities.WeaponType.Spear => new SpearHandler(),
            ExpertiseUtilities.WeaponType.Crossbow => new CrossbowHandler(),
            ExpertiseUtilities.WeaponType.GreatSword => new GreatSwordHandler(),
            ExpertiseUtilities.WeaponType.Slashers => new SlashersHandler(),
            ExpertiseUtilities.WeaponType.Pistols => new PistolsHandler(),
            ExpertiseUtilities.WeaponType.Reaper => new ReaperHandler(),
            ExpertiseUtilities.WeaponType.Longbow => new LongbowHandler(),
            ExpertiseUtilities.WeaponType.Whip => new WhipHandler(),
            ExpertiseUtilities.WeaponType.Unarmed => new SanguimancyHandler(),
            _ => null,
        };
    }
}

public abstract class BaseExpertiseHandler : IExpertiseHandler
{
    protected abstract IDictionary<ulong, KeyValuePair<int, float>> DataStructure { get; }

    public void AddExpertise(ulong steamID, float experience)
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

    public KeyValuePair<int, float> GetExpertiseData(ulong steamID)
    {
        if (DataStructure.TryGetValue(steamID, out var xpData))
            return xpData;
        return new KeyValuePair<int, float>(0, 0);
    }

    public void UpdateExpertiseData(ulong steamID, KeyValuePair<int, float> xpData)
    {
        DataStructure[steamID] = xpData;
    }

    public abstract void SaveChanges();

    public abstract ExpertiseUtilities.WeaponType GetWeaponType();
}

// Implementations for each weapon type
public class SwordHandler : BaseExpertiseHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerSwordExpertise;

    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerSwordExpertise();
    }

    public override ExpertiseUtilities.WeaponType GetWeaponType()
    {
        return ExpertiseUtilities.WeaponType.Sword;
    }
}

public class AxeHandler : BaseExpertiseHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerAxeExpertise;

    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerAxeExpertise();
    }

    public override ExpertiseUtilities.WeaponType GetWeaponType()
    {
        return ExpertiseUtilities.WeaponType.Axe;
    }
}

public class MaceHandler : BaseExpertiseHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerMaceExpertise;

    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerMaceExpertise();
    }

    public override ExpertiseUtilities.WeaponType GetWeaponType()
    {
        return ExpertiseUtilities.WeaponType.Mace;
    }
}

public class SpearHandler : BaseExpertiseHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerSpearExpertise;

    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerSpearExpertise();
    }

    public override ExpertiseUtilities.WeaponType GetWeaponType()
    {
        return ExpertiseUtilities.WeaponType.Spear;
    }
}

public class CrossbowHandler : BaseExpertiseHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerCrossbowExpertise;

    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerCrossbowExpertise();
    }

    public override ExpertiseUtilities.WeaponType GetWeaponType()
    {
        return ExpertiseUtilities.WeaponType.Crossbow;
    }
}

public class GreatSwordHandler : BaseExpertiseHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerGreatSwordExpertise;

    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerGreatSwordExpertise();
    }

    public override ExpertiseUtilities.WeaponType GetWeaponType()
    {
        return ExpertiseUtilities.WeaponType.GreatSword;
    }
}

public class SlashersHandler : BaseExpertiseHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerSlashersExpertise;

    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerSlashersExpertise();
    }

    public override ExpertiseUtilities.WeaponType GetWeaponType()
    {
        return ExpertiseUtilities.WeaponType.Slashers;
    }
}

public class PistolsHandler : BaseExpertiseHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerPistolsExpertise;

    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerPistolsExpertise();
    }

    public override ExpertiseUtilities.WeaponType GetWeaponType()
    {
        return ExpertiseUtilities.WeaponType.Pistols;
    }
}

public class ReaperHandler : BaseExpertiseHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerReaperExpertise;

    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerReaperExpertise();
    }

    public override ExpertiseUtilities.WeaponType GetWeaponType()
    {
        return ExpertiseUtilities.WeaponType.Reaper;
    }
}

public class LongbowHandler : BaseExpertiseHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerLongbowExpertise;

    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerLongbowExpertise();
    }

    public override ExpertiseUtilities.WeaponType GetWeaponType()
    {
        return ExpertiseUtilities.WeaponType.Longbow;
    }
}

public class WhipHandler : BaseExpertiseHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerWhipExpertise;

    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerWhipExpertise();
    }

    public override ExpertiseUtilities.WeaponType GetWeaponType()
    {
        return ExpertiseUtilities.WeaponType.Whip;
    }
}

public class SanguimancyHandler : BaseExpertiseHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerSanguimancy;

    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerSanguimancy();
    }

    public override ExpertiseUtilities.WeaponType GetWeaponType()
    {
        return ExpertiseUtilities.WeaponType.Unarmed;
    }
}