namespace Bloodcraft.SystemUtilities.Expertise;
public interface IExpertiseHandler
{
    void AddExpertise(ulong steamID, float experience);

    void SaveChanges();

    KeyValuePair<int, float> GetExpertiseData(ulong steamID);

    void UpdateExpertiseData(ulong steamID, KeyValuePair<int, float> xpData);

    WeaponSystem.WeaponType GetWeaponType();
}
public static class ExpertiseHandlerFactory
{
    public static IExpertiseHandler GetExpertiseHandler(WeaponSystem.WeaponType weaponType)
    {
        return weaponType switch
        {
            WeaponSystem.WeaponType.Sword => new SwordHandler(),
            WeaponSystem.WeaponType.Axe => new AxeHandler(),
            WeaponSystem.WeaponType.Mace => new MaceHandler(),
            WeaponSystem.WeaponType.Spear => new SpearHandler(),
            WeaponSystem.WeaponType.Crossbow => new CrossbowHandler(),
            WeaponSystem.WeaponType.GreatSword => new GreatSwordHandler(),
            WeaponSystem.WeaponType.Slashers => new SlashersHandler(),
            WeaponSystem.WeaponType.Pistols => new PistolsHandler(),
            WeaponSystem.WeaponType.Reaper => new ReaperHandler(),
            WeaponSystem.WeaponType.Longbow => new LongbowHandler(),
            WeaponSystem.WeaponType.Whip => new WhipHandler(),
            WeaponSystem.WeaponType.FishingPole => new FishingPoleHandler(),
            WeaponSystem.WeaponType.Unarmed => new UnarmedHandler(),
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
    public abstract WeaponSystem.WeaponType GetWeaponType();
}
public class SwordHandler : BaseExpertiseHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerSwordExpertise;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerSwordExpertise();
    }
    public override WeaponSystem.WeaponType GetWeaponType()
    {
        return WeaponSystem.WeaponType.Sword;
    }
}
public class AxeHandler : BaseExpertiseHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerAxeExpertise;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerAxeExpertise();
    }
    public override WeaponSystem.WeaponType GetWeaponType()
    {
        return WeaponSystem.WeaponType.Axe;
    }
}
public class MaceHandler : BaseExpertiseHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerMaceExpertise;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerMaceExpertise();
    }
    public override WeaponSystem.WeaponType GetWeaponType()
    {
        return WeaponSystem.WeaponType.Mace;
    }
}
public class SpearHandler : BaseExpertiseHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerSpearExpertise;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerSpearExpertise();
    }
    public override WeaponSystem.WeaponType GetWeaponType()
    {
        return WeaponSystem.WeaponType.Spear;
    }
}
public class CrossbowHandler : BaseExpertiseHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerCrossbowExpertise;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerCrossbowExpertise();
    }
    public override WeaponSystem.WeaponType GetWeaponType()
    {
        return WeaponSystem.WeaponType.Crossbow;
    }
}
public class GreatSwordHandler : BaseExpertiseHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerGreatSwordExpertise;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerGreatSwordExpertise();
    }
    public override WeaponSystem.WeaponType GetWeaponType()
    {
        return WeaponSystem.WeaponType.GreatSword;
    }
}
public class SlashersHandler : BaseExpertiseHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerSlashersExpertise;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerSlashersExpertise();
    }
    public override WeaponSystem.WeaponType GetWeaponType()
    {
        return WeaponSystem.WeaponType.Slashers;
    }
}
public class PistolsHandler : BaseExpertiseHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerPistolsExpertise;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerPistolsExpertise();
    }
    public override WeaponSystem.WeaponType GetWeaponType()
    {
        return WeaponSystem.WeaponType.Pistols;
    }
}
public class ReaperHandler : BaseExpertiseHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerReaperExpertise;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerReaperExpertise();
    }
    public override WeaponSystem.WeaponType GetWeaponType()
    {
        return WeaponSystem.WeaponType.Reaper;
    }
}
public class LongbowHandler : BaseExpertiseHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerLongbowExpertise;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerLongbowExpertise();
    }
    public override WeaponSystem.WeaponType GetWeaponType()
    {
        return WeaponSystem.WeaponType.Longbow;
    }
}
public class WhipHandler : BaseExpertiseHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerWhipExpertise;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerWhipExpertise();
    }
    public override WeaponSystem.WeaponType GetWeaponType()
    {
        return WeaponSystem.WeaponType.Whip;
    }
}
public class FishingPoleHandler : BaseExpertiseHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerFishingPoleExpertise;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerFishingPoleExpertise();
    }
    public override WeaponSystem.WeaponType GetWeaponType()
    {
        return WeaponSystem.WeaponType.FishingPole;
    }
}
public class UnarmedHandler : BaseExpertiseHandler
{
    protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerUnarmedExpertise;
    public override void SaveChanges()
    {
        Core.DataStructures.SavePlayerUnarmedExpertise();
    }
    public override WeaponSystem.WeaponType GetWeaponType()
    {
        return WeaponSystem.WeaponType.Unarmed;
    }
}