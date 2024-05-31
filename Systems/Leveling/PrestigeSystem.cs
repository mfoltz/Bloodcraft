using Bloodcraft.Patches;
using VampireCommandFramework;

namespace Bloodcraft.Systems.Leveling;

public class PrestigeSystem
{
    public enum PrestigeType
    {
        Experience,
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
        Sanguimancy,
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
    static readonly Dictionary<PrestigeSystem.PrestigeType, Dictionary<ulong, KeyValuePair<int, float>>> prestigeTypeToPlayerDataMap = new()
    {
        { PrestigeSystem.PrestigeType.Experience, Core.DataStructures.PlayerExperience },
        { PrestigeSystem.PrestigeType.SwordExpertise, Core.DataStructures.PlayerSwordExpertise },
        { PrestigeSystem.PrestigeType.AxeExpertise, Core.DataStructures.PlayerAxeExpertise },
        { PrestigeSystem.PrestigeType.MaceExpertise, Core.DataStructures.PlayerMaceExpertise },
        { PrestigeSystem.PrestigeType.SpearExpertise, Core.DataStructures.PlayerSpearExpertise },
        { PrestigeSystem.PrestigeType.CrossbowExpertise, Core.DataStructures.PlayerCrossbowExpertise },
        { PrestigeSystem.PrestigeType.GreatSwordExpertise, Core.DataStructures.PlayerGreatSwordExpertise },
        { PrestigeSystem.PrestigeType.SlashersExpertise, Core.DataStructures.PlayerSlashersExpertise },
        { PrestigeSystem.PrestigeType.PistolsExpertise, Core.DataStructures.PlayerPistolsExpertise },
        { PrestigeSystem.PrestigeType.ReaperExpertise, Core.DataStructures.PlayerReaperExpertise },
        { PrestigeSystem.PrestigeType.LongbowExpertise, Core.DataStructures.PlayerLongbowExpertise },
        { PrestigeSystem.PrestigeType.WhipExpertise, Core.DataStructures.PlayerWhipExpertise },
        { PrestigeSystem.PrestigeType.Sanguimancy, Core.DataStructures.PlayerSanguimancy },
        { PrestigeSystem.PrestigeType.WorkerLegacy, Core.DataStructures.PlayerWorkerLegacy },
        { PrestigeSystem.PrestigeType.WarriorLegacy, Core.DataStructures.PlayerWarriorLegacy },
        { PrestigeSystem.PrestigeType.ScholarLegacy, Core.DataStructures.PlayerScholarLegacy },
        { PrestigeSystem.PrestigeType.RogueLegacy, Core.DataStructures.PlayerRogueLegacy },
        { PrestigeSystem.PrestigeType.MutantLegacy, Core.DataStructures.PlayerMutantLegacy },
        { PrestigeSystem.PrestigeType.DraculinLegacy, Core.DataStructures.PlayerDraculinLegacy },
        { PrestigeSystem.PrestigeType.ImmortalLegacy, Core.DataStructures.PlayerImmortalLegacy },
        { PrestigeSystem.PrestigeType.CreatureLegacy, Core.DataStructures.PlayerCreatureLegacy },
        { PrestigeSystem.PrestigeType.BruteLegacy, Core.DataStructures.PlayerBruteLegacy }
    };
    public static Dictionary<PrestigeSystem.PrestigeType, Dictionary<ulong, KeyValuePair<int, float>>> PrestigeTypeToPlayerDataMap
    {
        get => prestigeTypeToPlayerDataMap;
    }

    static readonly Dictionary<PrestigeSystem.PrestigeType, int> prestigeTypeToMaxLevel = new()
    {
        { PrestigeSystem.PrestigeType.Experience, Plugin.MaxPlayerLevel.Value },
        { PrestigeSystem.PrestigeType.SwordExpertise, Plugin.MaxExpertiseLevel.Value },
        { PrestigeSystem.PrestigeType.AxeExpertise, Plugin.MaxExpertiseLevel.Value },
        { PrestigeSystem.PrestigeType.MaceExpertise, Plugin.MaxExpertiseLevel.Value },
        { PrestigeSystem.PrestigeType.SpearExpertise, Plugin.MaxExpertiseLevel.Value },
        { PrestigeSystem.PrestigeType.CrossbowExpertise, Plugin.MaxExpertiseLevel.Value },
        { PrestigeSystem.PrestigeType.GreatSwordExpertise, Plugin.MaxExpertiseLevel.Value },
        { PrestigeSystem.PrestigeType.SlashersExpertise, Plugin.MaxExpertiseLevel.Value },
        { PrestigeSystem.PrestigeType.PistolsExpertise, Plugin.MaxExpertiseLevel.Value },
        { PrestigeSystem.PrestigeType.ReaperExpertise, Plugin.MaxExpertiseLevel.Value },
        { PrestigeSystem.PrestigeType.LongbowExpertise, Plugin.MaxExpertiseLevel.Value },
        { PrestigeSystem.PrestigeType.WhipExpertise, Plugin.MaxExpertiseLevel.Value },
        { PrestigeSystem.PrestigeType.Sanguimancy, Plugin.MaxExpertiseLevel.Value },
        { PrestigeSystem.PrestigeType.WorkerLegacy, Plugin.MaxBloodLevel.Value },
        { PrestigeSystem.PrestigeType.WarriorLegacy, Plugin.MaxBloodLevel.Value },
        { PrestigeSystem.PrestigeType.ScholarLegacy, Plugin.MaxBloodLevel.Value },
        { PrestigeSystem.PrestigeType.RogueLegacy, Plugin.MaxBloodLevel.Value },
        { PrestigeSystem.PrestigeType.MutantLegacy, Plugin.MaxBloodLevel.Value },
        { PrestigeSystem.PrestigeType.DraculinLegacy, Plugin.MaxBloodLevel.Value },
        { PrestigeSystem.PrestigeType.ImmortalLegacy, Plugin.MaxBloodLevel.Value },
        { PrestigeSystem.PrestigeType.CreatureLegacy, Plugin.MaxBloodLevel.Value },
        { PrestigeSystem.PrestigeType.BruteLegacy, Plugin.MaxBloodLevel.Value }
    };
    public static Dictionary<PrestigeSystem.PrestigeType, int> PrestigeTypeToMaxLevel
    {
       get => prestigeTypeToMaxLevel;
    }
    static readonly Dictionary<PrestigeSystem.PrestigeType, int> prestigeTypeToMaxPrestigeLevel = new()
    {
        { PrestigeSystem.PrestigeType.Experience, Plugin.MaxLevelingPrestiges.Value },
        { PrestigeSystem.PrestigeType.SwordExpertise, Plugin.MaxExpertisePrestiges.Value },
        { PrestigeSystem.PrestigeType.AxeExpertise, Plugin.MaxExpertisePrestiges.Value },
        { PrestigeSystem.PrestigeType.MaceExpertise, Plugin.MaxExpertisePrestiges.Value },
        { PrestigeSystem.PrestigeType.SpearExpertise, Plugin.MaxExpertisePrestiges.Value },
        { PrestigeSystem.PrestigeType.CrossbowExpertise, Plugin.MaxExpertisePrestiges.Value },
        { PrestigeSystem.PrestigeType.GreatSwordExpertise, Plugin.MaxExpertisePrestiges.Value },
        { PrestigeSystem.PrestigeType.SlashersExpertise, Plugin.MaxExpertisePrestiges.Value },
        { PrestigeSystem.PrestigeType.PistolsExpertise, Plugin.MaxExpertisePrestiges.Value },
        { PrestigeSystem.PrestigeType.ReaperExpertise, Plugin.MaxExpertisePrestiges.Value },
        { PrestigeSystem.PrestigeType.LongbowExpertise, Plugin.MaxExpertisePrestiges.Value },
        { PrestigeSystem.PrestigeType.WhipExpertise, Plugin.MaxExpertisePrestiges.Value },
        { PrestigeSystem.PrestigeType.Sanguimancy, Plugin.MaxExpertisePrestiges.Value },
        { PrestigeSystem.PrestigeType.WorkerLegacy, Plugin.MaxLegacyPrestiges.Value },
        { PrestigeSystem.PrestigeType.WarriorLegacy, Plugin.MaxLegacyPrestiges.Value },
        { PrestigeSystem.PrestigeType.ScholarLegacy, Plugin.MaxLegacyPrestiges.Value },
        { PrestigeSystem.PrestigeType.RogueLegacy, Plugin.MaxLegacyPrestiges.Value },
        { PrestigeSystem.PrestigeType.MutantLegacy, Plugin.MaxLegacyPrestiges.Value },
        { PrestigeSystem.PrestigeType.DraculinLegacy, Plugin.MaxLegacyPrestiges.Value },
        { PrestigeSystem.PrestigeType.ImmortalLegacy, Plugin.MaxLegacyPrestiges.Value },
        { PrestigeSystem.PrestigeType.CreatureLegacy, Plugin.MaxLegacyPrestiges.Value },
        { PrestigeSystem.PrestigeType.BruteLegacy, Plugin.MaxLegacyPrestiges.Value }
    };
    public static Dictionary<PrestigeSystem.PrestigeType, int> PrestigeTypeToMaxPrestigeLevel
    {
        get => prestigeTypeToMaxPrestigeLevel;
    }

    public static void DisplayPrestigeInfo(ChatCommandContext ctx, ulong steamId, PrestigeSystem.PrestigeType parsedPrestigeType, int prestigeLevel, int maxPrestigeLevel)
    {
        float reductionFactor = MathF.Pow(1 - Plugin.PrestigeRatesReducer.Value, prestigeLevel);
        float gainMultiplier = 1.0f;

        if (Core.DataStructures.PlayerPrestiges.TryGetValue(steamId, out var prestigeData) &&
            prestigeData.TryGetValue(PrestigeSystem.PrestigeType.Experience, out var expPrestigeLevel) && expPrestigeLevel > 0)
        {
            gainMultiplier = 1 + (Plugin.PrestigeRatesMultiplier.Value * expPrestigeLevel);
        }

        if (parsedPrestigeType == PrestigeSystem.PrestigeType.Experience)
        {
            string reductionPercentage = ((1 - reductionFactor) * 100).ToString("F2") + "%";
            string gainPercentage = ((gainMultiplier - 1) * 100).ToString("F2") + "%";

            ctx.Reply($"<color=#90EE90>{parsedPrestigeType}</color> Prestige Info:");
            ctx.Reply($"Current Prestige Level: <color=yellow>{prestigeLevel}</color>/{maxPrestigeLevel}");
            ctx.Reply($"Growth rate improvement for expertise/legacies: <color=green>{gainPercentage}</color>");
            ctx.Reply($"Growth rate reduction for experience: <color=yellow>{reductionPercentage}</color>");
        }
        else
        {
            float combinedFactor = gainMultiplier * reductionFactor;
            string percentageReductionString = ((1 - reductionFactor) * 100).ToString("F0") + "%";

            // Fixed additive stat gain increase based on base value
            float statGainIncrease = Plugin.PrestigeStatMultiplier.Value * prestigeLevel;
            string statGainString = (statGainIncrease * 100).ToString("F2") + "%";

            string totalEffectString = ((combinedFactor - 1) * 100).ToString("F2") + "%";

            ctx.Reply($"<color=#90EE90>{parsedPrestigeType}</color> Prestige Info:");
            ctx.Reply($"Current Prestige Level: <color=yellow>{prestigeLevel}</color>/{maxPrestigeLevel}");
            ctx.Reply($"Growth rate reduction from <color=#90EE90>{parsedPrestigeType}</color> prestige level: <color=yellow>{percentageReductionString}</color>");
            ctx.Reply($"Stat bonuses improvement: <color=green>{statGainString}</color>");
            ctx.Reply($"Total change in growth rate including leveling prestige bonus: <color=yellow>{totalEffectString}</color>");
        }
    }

    public static bool CanPrestige(ulong steamId, PrestigeSystem.PrestigeType parsedPrestigeType, int xpKey)
    {
        return xpKey >= PrestigeSystem.PrestigeTypeToMaxLevel[parsedPrestigeType] &&
               Core.DataStructures.PlayerPrestiges.TryGetValue(steamId, out var prestigeData) &&
               prestigeData.TryGetValue(parsedPrestigeType, out var prestigeLevel) &&
               prestigeLevel < PrestigeSystem.PrestigeTypeToMaxPrestigeLevel[parsedPrestigeType];
    }

    public static void PerformPrestige(ChatCommandContext ctx, ulong steamId, PrestigeSystem.PrestigeType parsedPrestigeType, IPrestigeHandler handler)
    {
        handler.Prestige(steamId);
        handler.SaveChanges();

        var updatedPrestigeLevel = Core.DataStructures.PlayerPrestiges[steamId][parsedPrestigeType];
        if (parsedPrestigeType == PrestigeSystem.PrestigeType.Experience)
        {
            HandleExperiencePrestige(ctx, updatedPrestigeLevel);
        }
        else
        {
            HandleOtherPrestige(ctx, steamId, parsedPrestigeType, updatedPrestigeLevel);
        }
    }

    private static void HandleExperiencePrestige(ChatCommandContext ctx, int prestigeLevel)
    {
        GearOverride.SetLevel(ctx.Event.SenderCharacterEntity);
        float expReductionFactor = MathF.Pow(1 - Plugin.PrestigeRatesReducer.Value, prestigeLevel);
        string reductionPercentage = ((1 - expReductionFactor) * 100).ToString("F2") + "%";

        float gainMultiplier = 1.0f;
        if (Core.DataStructures.PlayerPrestiges.TryGetValue(ctx.Event.User.PlatformId, out var prestigeData) &&
            prestigeData.TryGetValue(PrestigeSystem.PrestigeType.Experience, out var expPrestigeLevel) && expPrestigeLevel > 0)
        {
            gainMultiplier = 1 + (Plugin.PrestigeRatesMultiplier.Value * expPrestigeLevel);
        }

        string gainPercentage = ((gainMultiplier - 1) * 100).ToString("F2") + "%";
        ctx.Reply($"You have prestiged in <color=#90EE90>Experience</color>[<color=white>{prestigeLevel}</color>]! Growth rates for all expertise/legacies increased by <color=green>{gainPercentage}</color>, growth rates for experience reduced by <color=yellow>{reductionPercentage}</color>");
    }

    private static void HandleOtherPrestige(ChatCommandContext ctx, ulong steamId, PrestigeSystem.PrestigeType parsedPrestigeType, int prestigeLevel)
    {
        int expPrestige = Core.DataStructures.PlayerPrestiges.TryGetValue(steamId, out var prestiges) && prestiges.TryGetValue(PrestigeSystem.PrestigeType.Experience, out var xpLevel) ? xpLevel : 0;

        float reductionFactor = MathF.Pow(1 - Plugin.PrestigeRatesReducer.Value, prestigeLevel);
        float gainMultiplier = 1 + (Plugin.PrestigeRatesMultiplier.Value * expPrestige);
        float combinedFactor = gainMultiplier * reductionFactor;

        string percentageReductionString = ((1 - reductionFactor) * 100).ToString("F0") + "%";

        // Fixed additive stat gain increase based on base value
        float statGainIncrease = Plugin.PrestigeStatMultiplier.Value * prestigeLevel;
        string statGainString = (statGainIncrease * 100).ToString("F2") + "%";

        string totalEffectString = ((combinedFactor - 1) * 100).ToString("F2") + "%";

        ctx.Reply($"You have prestiged in <color=#90EE90>{parsedPrestigeType}</color>[<color=white>{prestigeLevel}</color>]! Growth rate reduced by <color=yellow>{percentageReductionString}</color> and stat bonuses improved by <color=green>{statGainString}</color>. The total effect on growth rate with leveling prestige bonus is <color=yellow>{totalEffectString}</color>.");
    }

}
