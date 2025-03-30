using Bloodcraft.Services;
using Stunlock.Core;
using Unity.Mathematics;

namespace Bloodcraft.Systems.Professions; // need the const string treament but later
internal interface IProfessionHandler
{
    KeyValuePair<int, float> GetProfessionData(ulong steamID);
    void SetProfessionData(ulong steamID, KeyValuePair<int, float> xpData);
    string GetProfessionName();
    float3 GetProfessionColor();
}
internal static class ProfessionHandlerFactory
{
    static readonly List<BaseProfessionHandler> _professionHandlers =
    [
        new WoodcuttingHandler(),
        new MiningHandler(),
        new BlacksmithingHandler(),
        new TailoringHandler(),
        new FishingHandler(),
        new AlchemyHandler(),
        new HarvestingHandler(),
        new EnchantingHandler()
    ];
    public static IProfessionHandler GetProfessionHandler(PrefabGUID prefabGuid = default, string context = "")
    {
        string itemTypeName = string.Empty;

        if (prefabGuid.HasValue()) itemTypeName = prefabGuid.GetPrefabName().ToLower();

        switch (context)
        {
            case "woodcutting":
                return new WoodcuttingHandler();

            case "mining":
                return new MiningHandler();

            case "blacksmithing":
                return new BlacksmithingHandler();

            case "tailoring":
                return new TailoringHandler();

            case "fishing":
                return new FishingHandler();

            case "alchemy":
                return new AlchemyHandler();

            case "harvesting":
                return new HarvestingHandler();

            case "enchanting":
                return new EnchantingHandler();

            default:
                if (itemTypeName.Contains("wood"))
                    return new WoodcuttingHandler();
                if (itemTypeName.Contains("gem") || itemTypeName.Contains("jewel") || itemTypeName.Contains("magicsource"))
                    return new EnchantingHandler();
                if (itemTypeName.Contains("mineral") || itemTypeName.Contains("stone") || itemTypeName.Contains("bloodcrystal") || itemTypeName.Contains("techscrap"))
                    return new MiningHandler();
                if (itemTypeName.Contains("weapon"))
                    return new BlacksmithingHandler();
                if (itemTypeName.Contains("armor") || itemTypeName.Contains("cloak") || itemTypeName.Contains("bag") || itemTypeName.Contains("cloth") || itemTypeName.Contains("chest") || itemTypeName.Contains("boots") || itemTypeName.Contains("gloves") || itemTypeName.Contains("legs"))
                    return new TailoringHandler();
                if (itemTypeName.Contains("fish"))
                    return new FishingHandler();
                if (itemTypeName.Contains("plant") || itemTypeName.Contains("trippyshroom"))
                    return new HarvestingHandler();
                if (itemTypeName.Contains("canteen") || itemTypeName.Contains("potion") || itemTypeName.Contains("bottle") || itemTypeName.Contains("flask") || itemTypeName.Contains("consumable") || itemTypeName.Contains("duskcaller") || itemTypeName.Contains("onyxtear"))
                    return new AlchemyHandler();
                else
                    return null;
        }
    }
    public static string GetAllProfessions()
    {
        return string.Join(", ", _professionHandlers.Select(profession => profession.GetProfessionName()));
    }
}

public abstract class BaseProfessionHandler : IProfessionHandler
{
    // Abstract methods that return the appropriate extension methods for getting and setting data.
    public abstract KeyValuePair<int, float> GetProfessionData(ulong steamID);
    public abstract void SetProfessionData(ulong steamID, KeyValuePair<int, float> data);
    public abstract string GetProfessionName();
    public abstract float3 GetProfessionColor();
}
public class EnchantingHandler : BaseProfessionHandler
{
    public override KeyValuePair<int, float> GetProfessionData(ulong steamID)
    {
        return steamID.TryGetPlayerEnchanting(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetProfessionData(ulong steamID, KeyValuePair<int, float> data)
    {
        steamID.SetPlayerEnchanting(data);
    }
    public override string GetProfessionName()
    {
        return "<color=#7E22CE>Enchanting</color>";
    }
    public override float3 GetProfessionColor()
    {
        return new float3(0.494f, 0.133f, 0.808f);
    }
}
public class AlchemyHandler : BaseProfessionHandler
{
    public override KeyValuePair<int, float> GetProfessionData(ulong steamID)
    {
        return steamID.TryGetPlayerAlchemy(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetProfessionData(ulong steamID, KeyValuePair<int, float> data)
    {
        steamID.SetPlayerAlchemy(data);
    }
    public override string GetProfessionName()
    {
        return "<color=#12D4A2>Alchemy</color>";
    }
    public override float3 GetProfessionColor()
    {
        return new float3(0.071f, 0.831f, 0.635f);
    }
}
public class HarvestingHandler : BaseProfessionHandler
{
    public override KeyValuePair<int, float> GetProfessionData(ulong steamID)
    {
        return steamID.TryGetPlayerHarvesting(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetProfessionData(ulong steamID, KeyValuePair<int, float> data)
    {
        steamID.SetPlayerHarvesting(data);
    }
    public override string GetProfessionName()
    {
        return "<color=#008000>Harvesting</color>";
    }
    public override float3 GetProfessionColor()
    {
        return new float3(0.0f, 0.502f, 0.0f);
    }
}
public class BlacksmithingHandler : BaseProfessionHandler
{
    public override KeyValuePair<int, float> GetProfessionData(ulong steamID)
    {
        return steamID.TryGetPlayerBlacksmithing(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetProfessionData(ulong steamID, KeyValuePair<int, float> data)
    {
        steamID.SetPlayerBlacksmithing(data);
    }
    public override string GetProfessionName()
    {
        return "<color=#353641>Blacksmithing</color>";
    }
    public override float3 GetProfessionColor()
    {
        return new float3(0.208f, 0.212f, 0.255f);
    }
}
public class TailoringHandler : BaseProfessionHandler
{
    public override KeyValuePair<int, float> GetProfessionData(ulong steamID)
    {
        return steamID.TryGetPlayerTailoring(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetProfessionData(ulong steamID, KeyValuePair<int, float> data)
    {
        steamID.SetPlayerTailoring(data);
    }
    public override string GetProfessionName()
    {
        return "<color=#D98C80>Tailoring</color>";
    }
    public override float3 GetProfessionColor()
    {
        return new float3(0.85f, 0.55f, 0.50f);
    }
}
public class WoodcuttingHandler : BaseProfessionHandler
{
    public override KeyValuePair<int, float> GetProfessionData(ulong steamID)
    {
        return steamID.TryGetPlayerWoodcutting(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetProfessionData(ulong steamID, KeyValuePair<int, float> data)
    {
        steamID.SetPlayerWoodcutting(data);
    }
    public override string GetProfessionName()
    {
        return "<color=#A52A2A>Woodcutting</color>";
    }
    public override float3 GetProfessionColor()
    {
        return new float3(0.647f, 0.165f, 0.165f);
    }
}
public class MiningHandler : BaseProfessionHandler
{
    public override KeyValuePair<int, float> GetProfessionData(ulong steamID)
    {
        return steamID.TryGetPlayerMining(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetProfessionData(ulong steamID, KeyValuePair<int, float> data)
    {
        steamID.SetPlayerMining(data);
    }
    public override string GetProfessionName()
    {
        return "<color=#808080>Mining</color>";
    }
    public override float3 GetProfessionColor()
    {
        return new float3(0.502f, 0.502f, 0.502f);
    }
}
public class FishingHandler : BaseProfessionHandler
{
    public override KeyValuePair<int, float> GetProfessionData(ulong steamID)
    {
        return steamID.TryGetPlayerFishing(out var data) ? data : new KeyValuePair<int, float>(0, 0);
    }
    public override void SetProfessionData(ulong steamID, KeyValuePair<int, float> data)
    {
        steamID.SetPlayerFishing(data);
    }
    public override string GetProfessionName()
    {
        return "<color=#0077AA>Fishing</color>";
    }
    public override float3 GetProfessionColor()
    {
        return new float3(0.0f, 0.46f, 0.66f);
    }
}