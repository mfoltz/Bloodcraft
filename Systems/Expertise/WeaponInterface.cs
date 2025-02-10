using Bloodcraft.Services;

namespace Bloodcraft.Systems.Expertise;
public enum WeaponType
{
    Sword,
    Axe,
    Mace,
    Spear,
    Crossbow,
    GreatSword,
    Slashers,
    Pistols,
    Reaper,
    Longbow,
    Whip,
    Unarmed,
    FishingPole
}
internal interface IWeaponHandler
{
    KeyValuePair<int, float> GetExpertiseData(ulong steamID);
    void SetExpertiseData(ulong steamID, KeyValuePair<int, float> xpData);
    WeaponType GetWeaponType();
}
internal static class ExpertiseHandlerFactory
{
    public static IWeaponHandler GetExpertiseHandler(WeaponType weaponType)
    {
        return weaponType switch
        {
            WeaponType.Sword => new SwordHandler(),
            WeaponType.Axe => new AxeHandler(),
            WeaponType.Mace => new MaceHandler(),
            WeaponType.Spear => new SpearHandler(),
            WeaponType.Crossbow => new CrossbowHandler(),
            WeaponType.GreatSword => new GreatSwordHandler(),
            WeaponType.Slashers => new SlashersHandler(),
            WeaponType.Pistols => new PistolsHandler(),
            WeaponType.Reaper => new ReaperHandler(),
            WeaponType.Longbow => new LongbowHandler(),
            WeaponType.Whip => new WhipHandler(),
            WeaponType.FishingPole => new FishingPoleHandler(),
            WeaponType.Unarmed => new UnarmedHandler(),
            _ => null,
        };
    }
}
public abstract class BaseExpertiseHandler : IWeaponHandler
{
    public abstract KeyValuePair<int, float> GetExpertiseData(ulong steamID);
    public abstract void SetExpertiseData(ulong steamID, KeyValuePair<int, float> data);
    public abstract WeaponType GetWeaponType();
}
public class SwordHandler : BaseExpertiseHandler
{
    public override KeyValuePair<int, float> GetExpertiseData(ulong steamID)
    {
        return steamID.TryGetPlayerSwordExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetExpertiseData(ulong steamID, KeyValuePair<int, float> data)
    {
        steamID.SetPlayerSwordExpertise(data);
    }
    public override WeaponType GetWeaponType()
    {
        return WeaponType.Sword;
    }
}
public class AxeHandler : BaseExpertiseHandler
{
    public override KeyValuePair<int, float> GetExpertiseData(ulong steamID)
    {
        return steamID.TryGetPlayerAxeExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetExpertiseData(ulong steamID, KeyValuePair<int, float> data)
    {
        steamID.SetPlayerAxeExpertise(data);
    }
    public override WeaponType GetWeaponType()
    {
        return WeaponType.Axe;
    }
}
public class MaceHandler : BaseExpertiseHandler
{
    public override KeyValuePair<int, float> GetExpertiseData(ulong steamID)
    {
        return steamID.TryGetPlayerMaceExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetExpertiseData(ulong steamID, KeyValuePair<int, float> data)
    {
        steamID.SetPlayerMaceExpertise(data);
    }
    public override WeaponType GetWeaponType()
    {
        return WeaponType.Mace;
    }
}
public class SpearHandler : BaseExpertiseHandler
{
    public override KeyValuePair<int, float> GetExpertiseData(ulong steamID)
    {
        return steamID.TryGetPlayerSpearExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetExpertiseData(ulong steamID, KeyValuePair<int, float> data)
    {
        steamID.SetPlayerSpearExpertise(data);
    }
    public override WeaponType GetWeaponType()
    {
        return WeaponType.Spear;
    }
}
public class CrossbowHandler : BaseExpertiseHandler
{
    public override KeyValuePair<int, float> GetExpertiseData(ulong steamID)
    {
        return steamID.TryGetPlayerCrossbowExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetExpertiseData(ulong steamID, KeyValuePair<int, float> data)
    {
        steamID.SetPlayerCrossbowExpertise(data);
    }
    public override WeaponType GetWeaponType()
    {
        return WeaponType.Crossbow;
    }
}
public class GreatSwordHandler : BaseExpertiseHandler
{
    public override KeyValuePair<int, float> GetExpertiseData(ulong steamID)
    {
        return steamID.TryGetPlayerGreatSwordExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetExpertiseData(ulong steamID, KeyValuePair<int, float> data)
    {
        steamID.SetPlayerGreatSwordExpertise(data);
    }
    public override WeaponType GetWeaponType()
    {
        return WeaponType.GreatSword;
    }
}
public class SlashersHandler : BaseExpertiseHandler
{
    public override KeyValuePair<int, float> GetExpertiseData(ulong steamID)
    {
        return steamID.TryGetPlayerSlashersExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetExpertiseData(ulong steamID, KeyValuePair<int, float> data)
    {
        steamID.SetPlayerSlashersExpertise(data);
    }
    public override WeaponType GetWeaponType()
    {
        return WeaponType.Slashers;
    }
}
public class PistolsHandler : BaseExpertiseHandler
{
    public override KeyValuePair<int, float> GetExpertiseData(ulong steamID)
    {
        return steamID.TryGetPlayerPistolsExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetExpertiseData(ulong steamID, KeyValuePair<int, float> data)
    {
        steamID.SetPlayerPistolsExpertise(data);
    }
    public override WeaponType GetWeaponType()
    {
        return WeaponType.Pistols;
    }
}
public class ReaperHandler : BaseExpertiseHandler
{
    public override KeyValuePair<int, float> GetExpertiseData(ulong steamID)
    {
        return steamID.TryGetPlayerReaperExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetExpertiseData(ulong steamID, KeyValuePair<int, float> data)
    {
        steamID.SetPlayerReaperExpertise(data);
    }
    public override WeaponType GetWeaponType()
    {
        return WeaponType.Reaper;
    }
}
public class LongbowHandler : BaseExpertiseHandler
{
    public override KeyValuePair<int, float> GetExpertiseData(ulong steamID)
    {
        return steamID.TryGetPlayerLongbowExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetExpertiseData(ulong steamID, KeyValuePair<int, float> data)
    {
        steamID.SetPlayerLongbowExpertise(data);
    }
    public override WeaponType GetWeaponType()
    {
        return WeaponType.Longbow;
    }
}
public class WhipHandler : BaseExpertiseHandler
{
    public override KeyValuePair<int, float> GetExpertiseData(ulong steamID)
    {
        return steamID.TryGetPlayerWhipExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetExpertiseData(ulong steamID, KeyValuePair<int, float> data)
    {
        steamID.SetPlayerWhipExpertise(data);
    }
    public override WeaponType GetWeaponType()
    {
        return WeaponType.Whip;
    }
}
public class FishingPoleHandler : BaseExpertiseHandler
{
    public override KeyValuePair<int, float> GetExpertiseData(ulong steamID)
    {
        return steamID.TryGetPlayerFishingPoleExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetExpertiseData(ulong steamID, KeyValuePair<int, float> data)
    {
        steamID.SetPlayerFishingPoleExpertise(data);
    }
    public override WeaponType GetWeaponType()
    {
        return WeaponType.FishingPole;
    }
}
public class UnarmedHandler : BaseExpertiseHandler
{
    public override KeyValuePair<int, float> GetExpertiseData(ulong steamID)
    {
        return steamID.TryGetPlayerUnarmedExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetExpertiseData(ulong steamID, KeyValuePair<int, float> data)
    {
        steamID.SetPlayerUnarmedExpertise(data);
    }
    public override WeaponType GetWeaponType()
    {
        return WeaponType.Unarmed;
    }
}