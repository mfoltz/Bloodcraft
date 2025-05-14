using Bloodcraft.Services;
using Stunlock.Core;
using Unity.Mathematics;

namespace Bloodcraft.Interfaces;
public enum ProfessionType
{
    None,
    Enchanting,
    Alchemy,
    Harvesting,
    Blacksmithing,
    Tailoring,
    Woodcutting,
    Mining,
    Fishing
}
internal interface IProfession
{
    KeyValuePair<int, float> GetProfessionData(ulong steamId);
    int GetProfessionLevel(ulong steamId);
    void SetProfessionData(ulong steamId, KeyValuePair<int, float> xpData);
    string GetProfessionName();
    float3 GetProfessionColor();
}
internal static class ProfessionFactory
{
    static readonly List<ProfessionBase> _professionHandlers =
    [
        new WoodcuttingProfession(),
        new MiningProfession(),
        new BlacksmithingProfession(),
        new TailoringProfession(),
        new FishingProfession(),
        new AlchemyProfession(),
        new HarvestingProfession(),
        new EnchantingProfession()
    ];
    public static IProfession GetProfession(PrefabGUID prefabGuid)
    {
        string itemTypeName = string.Empty;
        if (prefabGuid.HasValue()) itemTypeName = prefabGuid.GetPrefabName().ToLower();

        if (itemTypeName.Contains("wood"))
            return new WoodcuttingProfession();
        else if (itemTypeName.Contains("gem") || itemTypeName.Contains("jewel") || itemTypeName.Contains("magicsource"))
            return new EnchantingProfession();
        else if(itemTypeName.Contains("mineral") || itemTypeName.Contains("stone") || itemTypeName.Contains("bloodcrystal") || itemTypeName.Contains("techscrap"))
            return new MiningProfession();
        else if(itemTypeName.Contains("weapon") || itemTypeName.Contains("onyxtear"))
            return new BlacksmithingProfession();
        else if(itemTypeName.Contains("armor") || itemTypeName.Contains("cloak") || itemTypeName.Contains("bag") || itemTypeName.Contains("cloth") || itemTypeName.Contains("chest") || itemTypeName.Contains("boots") || itemTypeName.Contains("gloves") || itemTypeName.Contains("legs"))
            return new TailoringProfession();
        else if(itemTypeName.Contains("fish"))
            return new FishingProfession();
        else if(itemTypeName.Contains("plant") || itemTypeName.Contains("trippyshroom"))
            return new HarvestingProfession();
        else if(itemTypeName.Contains("canteen") || itemTypeName.Contains("potion") || itemTypeName.Contains("bottle") || itemTypeName.Contains("flask") || itemTypeName.Contains("consumable") || itemTypeName.Contains("duskcaller") || itemTypeName.Contains("elixir") || itemTypeName.Contains("coating"))
            return new AlchemyProfession();

        return null;
    }
    public static IProfession GetProfession(ProfessionType profession)
    {
        return profession switch
        {
            ProfessionType.Woodcutting => new WoodcuttingProfession(),
            ProfessionType.Mining => new MiningProfession(),
            ProfessionType.Blacksmithing => new BlacksmithingProfession(),
            ProfessionType.Tailoring => new TailoringProfession(),
            ProfessionType.Fishing => new FishingProfession(),
            ProfessionType.Alchemy => new AlchemyProfession(),
            ProfessionType.Harvesting => new HarvestingProfession(),
            ProfessionType.Enchanting => new EnchantingProfession(),
            _ => null,
        };
    }
    public static string GetProfessionNames()
    {
        return string.Join(", ", _professionHandlers.Select(profession => profession.GetProfessionName()));
    }
}
internal abstract class ProfessionBase : IProfession
{
    public abstract KeyValuePair<int, float> GetProfessionData(ulong steamId);
    public abstract int GetProfessionLevel(ulong steamId);
    public abstract void SetProfessionData(ulong steamId, KeyValuePair<int, float> data);
    public abstract string GetProfessionName();
    public abstract float3 GetProfessionColor();
}
internal class EnchantingProfession : ProfessionBase
{
    public override KeyValuePair<int, float> GetProfessionData(ulong steamId)
    {
        return steamId.TryGetPlayerEnchanting(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override int GetProfessionLevel(ulong steamId)
    {
        return GetProfessionData(steamId).Key;
    }
    public override void SetProfessionData(ulong steamId, KeyValuePair<int, float> data)
    {
        steamId.SetPlayerEnchanting(data);
    }
    public override string GetProfessionName()
    {
        return "<color=#7E22CE>Enchanting</color>";
    }
    public override float3 GetProfessionColor()
    {
        return new float3(0.5f, 0.1f, 0.8f);
    }
}
internal class AlchemyProfession : ProfessionBase
{
    public override KeyValuePair<int, float> GetProfessionData(ulong steamId)
    {
        return steamId.TryGetPlayerAlchemy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override int GetProfessionLevel(ulong steamId)
    {
        return GetProfessionData(steamId).Key;
    }
    public override void SetProfessionData(ulong steamId, KeyValuePair<int, float> data)
    {
        steamId.SetPlayerAlchemy(data);
    }
    public override string GetProfessionName()
    {
        return "<color=#12D4A2>Alchemy</color>";
    }
    public override float3 GetProfessionColor()
    {
        return new float3(0.1f, 0.8f, 0.6f);
    }
}
internal class HarvestingProfession : ProfessionBase
{
    public override KeyValuePair<int, float> GetProfessionData(ulong steamId)
    {
        return steamId.TryGetPlayerHarvesting(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override int GetProfessionLevel(ulong steamId)
    {
        return GetProfessionData(steamId).Key;
    }
    public override void SetProfessionData(ulong steamId, KeyValuePair<int, float> data)
    {
        steamId.SetPlayerHarvesting(data);
    }
    public override string GetProfessionName()
    {
        return "<color=#008000>Harvesting</color>";
    }
    public override float3 GetProfessionColor()
    {
        return new float3(0f, 0.5f, 0f);
    }
}
internal class BlacksmithingProfession : ProfessionBase
{
    public override KeyValuePair<int, float> GetProfessionData(ulong steamId)
    {
        return steamId.TryGetPlayerBlacksmithing(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override int GetProfessionLevel(ulong steamId)
    {
        return GetProfessionData(steamId).Key;
    }
    public override void SetProfessionData(ulong steamId, KeyValuePair<int, float> data)
    {
        steamId.SetPlayerBlacksmithing(data);
    }
    public override string GetProfessionName()
    {
        return "<color=#353641>Blacksmithing</color>";
    }
    public override float3 GetProfessionColor()
    {
        return new float3(0.2f, 0.2f, 0.3f);
    }
}
internal class TailoringProfession : ProfessionBase
{
    public override KeyValuePair<int, float> GetProfessionData(ulong steamId)
    {
        return steamId.TryGetPlayerTailoring(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override int GetProfessionLevel(ulong steamId)
    {
        return GetProfessionData(steamId).Key;
    }
    public override void SetProfessionData(ulong steamId, KeyValuePair<int, float> data)
    {
        steamId.SetPlayerTailoring(data);
    }
    public override string GetProfessionName()
    {
        return "<color=#D98C80>Tailoring</color>";
    }
    public override float3 GetProfessionColor()
    {
        return new float3(0.9f, 0.6f, 0.5f);
    }
}
internal class WoodcuttingProfession : ProfessionBase
{
    public override KeyValuePair<int, float> GetProfessionData(ulong steamId)
    {
        return steamId.TryGetPlayerWoodcutting(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override int GetProfessionLevel(ulong steamId)
    {
        return GetProfessionData(steamId).Key;
    }
    public override void SetProfessionData(ulong steamId, KeyValuePair<int, float> data)
    {
        steamId.SetPlayerWoodcutting(data);
    }
    public override string GetProfessionName()
    {
        return "<color=#A52A2A>Woodcutting</color>";
    }
    public override float3 GetProfessionColor()
    {
        return new float3(0.6f, 0.2f, 0.2f);
    }
}
internal class MiningProfession : ProfessionBase
{
    public override KeyValuePair<int, float> GetProfessionData(ulong steamId)
    {
        return steamId.TryGetPlayerMining(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override int GetProfessionLevel(ulong steamId)
    {
        return GetProfessionData(steamId).Key;
    }
    public override void SetProfessionData(ulong steamId, KeyValuePair<int, float> data)
    {
        steamId.SetPlayerMining(data);
    }
    public override string GetProfessionName()
    {
        return "<color=#808080>Mining</color>";
    }
    public override float3 GetProfessionColor()
    {
        return new float3(0.5f, 0.5f, 0.5f);
    }
}
internal class FishingProfession : ProfessionBase
{
    public override KeyValuePair<int, float> GetProfessionData(ulong steamId)
    {
        return steamId.TryGetPlayerFishing(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override int GetProfessionLevel(ulong steamId)
    {
        return GetProfessionData(steamId).Key;
    }
    public override void SetProfessionData(ulong steamId, KeyValuePair<int, float> data)
    {
        steamId.SetPlayerFishing(data);
    }
    public override string GetProfessionName()
    {
        return "<color=#0077AA>Fishing</color>";
    }
    public override float3 GetProfessionColor()
    {
        return new float3(0f, 0.5f, 0.7f);
    }
}