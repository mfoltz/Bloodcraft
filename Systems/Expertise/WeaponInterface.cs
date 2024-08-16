namespace Bloodcraft.SystemUtilities.Expertise;
public interface IExpertiseHandler
{
    void AddExpertise(ulong steamID, float experience);

    void SaveChanges();

    KeyValuePair<int, float> GetExpertiseData(ulong steamID);

    void UpdateExpertiseData(ulong steamID, KeyValuePair<int, float> xpData);

    ExpertiseHandler.WeaponType GetWeaponType();
}

public static class ExpertiseHandlerFactory
{
    public static IExpertiseHandler GetExpertiseHandler(ExpertiseHandler.WeaponType weaponType)
    {
        return weaponType switch
        {
            ExpertiseHandler.WeaponType.Sword => new SwordHandler(),
            ExpertiseHandler.WeaponType.Axe => new AxeHandler(),
            ExpertiseHandler.WeaponType.Mace => new MaceHandler(),
            ExpertiseHandler.WeaponType.Spear => new SpearHandler(),
            ExpertiseHandler.WeaponType.Crossbow => new CrossbowHandler(),
            ExpertiseHandler.WeaponType.GreatSword => new GreatSwordHandler(),
            ExpertiseHandler.WeaponType.Slashers => new SlashersHandler(),
            ExpertiseHandler.WeaponType.Pistols => new PistolsHandler(),
            ExpertiseHandler.WeaponType.Reaper => new ReaperHandler(),
            ExpertiseHandler.WeaponType.Longbow => new LongbowHandler(),
            ExpertiseHandler.WeaponType.Whip => new WhipHandler(),
            ExpertiseHandler.WeaponType.FishingPole => new FishingPoleHandler(),
            ExpertiseHandler.WeaponType.Unarmed => new UnarmedHandler(),
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
    public abstract ExpertiseHandler.WeaponType GetWeaponType();
}
public class SwordHandler : BaseExpertiseHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerSwordExpertise;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerSwordExpertise();
    }
    public override ExpertiseHandler.WeaponType GetWeaponType()
    {
        return ExpertiseHandler.WeaponType.Sword;
    }
}
public class AxeHandler : BaseExpertiseHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerAxeExpertise;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerAxeExpertise();
    }
    public override ExpertiseHandler.WeaponType GetWeaponType()
    {
        return ExpertiseHandler.WeaponType.Axe;
    }
}
public class MaceHandler : BaseExpertiseHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerMaceExpertise;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerMaceExpertise();
    }
    public override ExpertiseHandler.WeaponType GetWeaponType()
    {
        return ExpertiseHandler.WeaponType.Mace;
    }
}
public class SpearHandler : BaseExpertiseHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerSpearExpertise;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerSpearExpertise();
    }
    public override ExpertiseHandler.WeaponType GetWeaponType()
    {
        return ExpertiseHandler.WeaponType.Spear;
    }
}
public class CrossbowHandler : BaseExpertiseHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerCrossbowExpertise;

    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerCrossbowExpertise();
    }

    public override ExpertiseHandler.WeaponType GetWeaponType()
    {
        return ExpertiseHandler.WeaponType.Crossbow;
    }
}
public class GreatSwordHandler : BaseExpertiseHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerGreatSwordExpertise;

    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerGreatSwordExpertise();
    }

    public override ExpertiseHandler.WeaponType GetWeaponType()
    {
        return ExpertiseHandler.WeaponType.GreatSword;
    }
}
public class SlashersHandler : BaseExpertiseHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerSlashersExpertise;

    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerSlashersExpertise();
    }

    public override ExpertiseHandler.WeaponType GetWeaponType()
    {
        return ExpertiseHandler.WeaponType.Slashers;
    }
}
public class PistolsHandler : BaseExpertiseHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerPistolsExpertise;

    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerPistolsExpertise();
    }

    public override ExpertiseHandler.WeaponType GetWeaponType()
    {
        return ExpertiseHandler.WeaponType.Pistols;
    }
}
public class ReaperHandler : BaseExpertiseHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerReaperExpertise;

    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerReaperExpertise();
    }

    public override ExpertiseHandler.WeaponType GetWeaponType()
    {
        return ExpertiseHandler.WeaponType.Reaper;
    }
}
public class LongbowHandler : BaseExpertiseHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerLongbowExpertise;

    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerLongbowExpertise();
    }

    public override ExpertiseHandler.WeaponType GetWeaponType()
    {
        return ExpertiseHandler.WeaponType.Longbow;
    }
}
public class WhipHandler : BaseExpertiseHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerWhipExpertise;

    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerWhipExpertise();
    }

    public override ExpertiseHandler.WeaponType GetWeaponType()
    {
        return ExpertiseHandler.WeaponType.Whip;
    }
}
public class FishingPoleHandler : BaseExpertiseHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerFishingPoleExpertise;

    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerFishingPoleExpertise();
    }

    public override ExpertiseHandler.WeaponType GetWeaponType()
    {
        return ExpertiseHandler.WeaponType.FishingPole;
    }
}
public class UnarmedHandler : BaseExpertiseHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerUnarmedExpertise;

    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerUnarmedExpertise();
    }

    public override ExpertiseHandler.WeaponType GetWeaponType()
    {
        return ExpertiseHandler.WeaponType.Unarmed;
    }
}