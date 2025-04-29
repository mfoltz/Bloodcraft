using Bloodcraft.Services;

namespace Bloodcraft.Interfaces;
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
    FishingPole,
    TwinBlades,
    Daggers,
    Claws
}
internal interface IWeaponExpertise
{
    KeyValuePair<int, float> GetExpertiseData(ulong steamId);
    void SetExpertiseData(ulong steamId, KeyValuePair<int, float> xpData);
    WeaponType GetWeaponType();
}
internal static class WeaponExpertiseFactory
{
    public static IWeaponExpertise GetExpertise(WeaponType weaponType)
    {
        return weaponType switch
        {
            WeaponType.Sword => new SwordExpertise(),
            WeaponType.Axe => new AxeExpertise(),
            WeaponType.Mace => new MaceExpertise(),
            WeaponType.Spear => new SpearExpertise(),
            WeaponType.Crossbow => new CrossbowExpertise(),
            WeaponType.GreatSword => new GreatSwordExpertise(),
            WeaponType.Slashers => new SlashersExpertise(),
            WeaponType.Pistols => new PistolsExpertise(),
            WeaponType.Reaper => new ReaperExpertise(),
            WeaponType.Longbow => new LongbowExpertise(),
            WeaponType.Whip => new WhipExpertise(),
            WeaponType.FishingPole => new FishingPoleExpertise(),
            WeaponType.Unarmed => new UnarmedExpertise(),
            WeaponType.TwinBlades => new TwinBladesExpertise(),
            WeaponType.Daggers => new DaggersExpertise(),
            WeaponType.Claws => new ClawsExpertise(),
            _ => null,
        };
    }
}
internal abstract class WeaponExpertise : IWeaponExpertise
{
    public abstract KeyValuePair<int, float> GetExpertiseData(ulong steamId);
    public abstract void SetExpertiseData(ulong steamId, KeyValuePair<int, float> data);
    public abstract WeaponType GetWeaponType();
}
internal class SwordExpertise : WeaponExpertise
{
    public override KeyValuePair<int, float> GetExpertiseData(ulong steamId)
    {
        return steamId.TryGetPlayerSwordExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetExpertiseData(ulong steamId, KeyValuePair<int, float> data)
    {
        steamId.SetPlayerSwordExpertise(data);
    }
    public override WeaponType GetWeaponType()
    {
        return WeaponType.Sword;
    }
}
internal class AxeExpertise : WeaponExpertise
{
    public override KeyValuePair<int, float> GetExpertiseData(ulong steamId)
    {
        return steamId.TryGetPlayerAxeExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetExpertiseData(ulong steamId, KeyValuePair<int, float> data)
    {
        steamId.SetPlayerAxeExpertise(data);
    }
    public override WeaponType GetWeaponType()
    {
        return WeaponType.Axe;
    }
}
internal class MaceExpertise : WeaponExpertise
{
    public override KeyValuePair<int, float> GetExpertiseData(ulong steamId)
    {
        return steamId.TryGetPlayerMaceExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetExpertiseData(ulong steamId, KeyValuePair<int, float> data)
    {
        steamId.SetPlayerMaceExpertise(data);
    }
    public override WeaponType GetWeaponType()
    {
        return WeaponType.Mace;
    }
}
internal class SpearExpertise : WeaponExpertise
{
    public override KeyValuePair<int, float> GetExpertiseData(ulong steamId)
    {
        return steamId.TryGetPlayerSpearExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetExpertiseData(ulong steamId, KeyValuePair<int, float> data)
    {
        steamId.SetPlayerSpearExpertise(data);
    }
    public override WeaponType GetWeaponType()
    {
        return WeaponType.Spear;
    }
}
internal class CrossbowExpertise : WeaponExpertise
{
    public override KeyValuePair<int, float> GetExpertiseData(ulong steamId)
    {
        return steamId.TryGetPlayerCrossbowExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetExpertiseData(ulong steamId, KeyValuePair<int, float> data)
    {
        steamId.SetPlayerCrossbowExpertise(data);
    }
    public override WeaponType GetWeaponType()
    {
        return WeaponType.Crossbow;
    }
}
internal class GreatSwordExpertise : WeaponExpertise
{
    public override KeyValuePair<int, float> GetExpertiseData(ulong steamId)
    {
        return steamId.TryGetPlayerGreatSwordExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetExpertiseData(ulong steamId, KeyValuePair<int, float> data)
    {
        steamId.SetPlayerGreatSwordExpertise(data);
    }
    public override WeaponType GetWeaponType()
    {
        return WeaponType.GreatSword;
    }
}
internal class SlashersExpertise : WeaponExpertise
{
    public override KeyValuePair<int, float> GetExpertiseData(ulong steamId)
    {
        return steamId.TryGetPlayerSlashersExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetExpertiseData(ulong steamId, KeyValuePair<int, float> data)
    {
        steamId.SetPlayerSlashersExpertise(data);
    }
    public override WeaponType GetWeaponType()
    {
        return WeaponType.Slashers;
    }
}
internal class PistolsExpertise : WeaponExpertise
{
    public override KeyValuePair<int, float> GetExpertiseData(ulong steamId)
    {
        return steamId.TryGetPlayerPistolsExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetExpertiseData(ulong steamId, KeyValuePair<int, float> data)
    {
        steamId.SetPlayerPistolsExpertise(data);
    }
    public override WeaponType GetWeaponType()
    {
        return WeaponType.Pistols;
    }
}
internal class ReaperExpertise : WeaponExpertise
{
    public override KeyValuePair<int, float> GetExpertiseData(ulong steamId)
    {
        return steamId.TryGetPlayerReaperExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetExpertiseData(ulong steamId, KeyValuePair<int, float> data)
    {
        steamId.SetPlayerReaperExpertise(data);
    }
    public override WeaponType GetWeaponType()
    {
        return WeaponType.Reaper;
    }
}
internal class LongbowExpertise : WeaponExpertise
{
    public override KeyValuePair<int, float> GetExpertiseData(ulong steamId)
    {
        return steamId.TryGetPlayerLongbowExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetExpertiseData(ulong steamId, KeyValuePair<int, float> data)
    {
        steamId.SetPlayerLongbowExpertise(data);
    }
    public override WeaponType GetWeaponType()
    {
        return WeaponType.Longbow;
    }
}
internal class WhipExpertise : WeaponExpertise
{
    public override KeyValuePair<int, float> GetExpertiseData(ulong steamId)
    {
        return steamId.TryGetPlayerWhipExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetExpertiseData(ulong steamId, KeyValuePair<int, float> data)
    {
        steamId.SetPlayerWhipExpertise(data);
    }
    public override WeaponType GetWeaponType()
    {
        return WeaponType.Whip;
    }
}
internal class FishingPoleExpertise : WeaponExpertise
{
    public override KeyValuePair<int, float> GetExpertiseData(ulong steamId)
    {
        return steamId.TryGetPlayerFishingPoleExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetExpertiseData(ulong steamId, KeyValuePair<int, float> data)
    {
        steamId.SetPlayerFishingPoleExpertise(data);
    }
    public override WeaponType GetWeaponType()
    {
        return WeaponType.FishingPole;
    }
}
internal class UnarmedExpertise : WeaponExpertise
{
    public override KeyValuePair<int, float> GetExpertiseData(ulong steamId)
    {
        return steamId.TryGetPlayerUnarmedExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetExpertiseData(ulong steamId, KeyValuePair<int, float> data)
    {
        steamId.SetPlayerUnarmedExpertise(data);
    }
    public override WeaponType GetWeaponType()
    {
        return WeaponType.Unarmed;
    }
}
internal class TwinBladesExpertise : WeaponExpertise
{
    public override KeyValuePair<int, float> GetExpertiseData(ulong steamId)
    {
        return steamId.TryGetPlayerTwinBladesExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetExpertiseData(ulong steamId, KeyValuePair<int, float> data)
    {
        steamId.SetPlayerTwinBladesExpertise(data);
    }
    public override WeaponType GetWeaponType()
    {
        return WeaponType.TwinBlades;
    }
}
internal class DaggersExpertise : WeaponExpertise
{
    public override KeyValuePair<int, float> GetExpertiseData(ulong steamId)
    {
        return steamId.TryGetPlayerDaggersExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetExpertiseData(ulong steamId, KeyValuePair<int, float> data)
    {
        steamId.SetPlayerDaggersExpertise(data);
    }
    public override WeaponType GetWeaponType()
    {
        return WeaponType.Daggers;
    }
}
internal class ClawsExpertise : WeaponExpertise
{
    public override KeyValuePair<int, float> GetExpertiseData(ulong steamId)
    {
        return steamId.TryGetPlayerClawsExpertise(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetExpertiseData(ulong steamId, KeyValuePair<int, float> data)
    {
        steamId.SetPlayerClawsExpertise(data);
    }
    public override WeaponType GetWeaponType()
    {
        return WeaponType.Claws;
    }
}