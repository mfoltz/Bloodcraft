using Bloodcraft.Patches;
using Bloodcraft.Systems.Experience;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
using static Bloodcraft.Systems.Experience.LevelingSystem;

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
        float reductionFactor = 1.0f;
        float gainMultiplier = 1.0f;

        if (parsedPrestigeType == PrestigeSystem.PrestigeType.Experience)
        {
            if (Core.DataStructures.PlayerPrestiges.TryGetValue(steamId, out var prestigeData) &&
                prestigeData.TryGetValue(PrestigeSystem.PrestigeType.Experience, out var expPrestigeLevel) && expPrestigeLevel > 0)
            {
                // Apply flat rate reduction for leveling experience
                reductionFactor = Plugin.LevelingPrestigeReducer.Value * expPrestigeLevel;

                // Apply rate gain with linear increase for expertise/legacy
                gainMultiplier = Plugin.PrestigeRatesMultiplier.Value * expPrestigeLevel;
            }

            string reductionPercentage = (reductionFactor * 100).ToString("F2") + "%";
            string gainPercentage = (gainMultiplier * 100).ToString("F2") + "%";

            ctx.Reply($"<color=#90EE90>{parsedPrestigeType}</color> Prestige Info:");
            ctx.Reply($"Current Prestige Level: <color=yellow>{prestigeLevel}</color>/{maxPrestigeLevel}");
            ctx.Reply($"Growth rate improvement for expertise/legacies: <color=green>{gainPercentage}</color>");
            ctx.Reply($"Growth rate reduction for experience: <color=yellow>{reductionPercentage}</color>");
        }
        else
        {
            if (Core.DataStructures.PlayerPrestiges.TryGetValue(steamId, out var prestigeData) &&
                prestigeData.TryGetValue(PrestigeSystem.PrestigeType.Experience, out var expPrestigeLevel) && expPrestigeLevel > 0)
            {
                // Apply flat rate reduction for leveling experience
                reductionFactor = Plugin.LevelingPrestigeReducer.Value * expPrestigeLevel;

                // Apply rate gain with linear increase for expertise/legacy
                gainMultiplier = Plugin.PrestigeRatesMultiplier.Value * expPrestigeLevel;
            }

            float combinedFactor = gainMultiplier - reductionFactor;
            string percentageReductionString = (reductionFactor * 100).ToString("F2") + "%";

            // Fixed additive stat gain increase based on base value
            float statGainIncrease = Plugin.PrestigeStatMultiplier.Value * prestigeLevel;
            string statGainString = (statGainIncrease * 100).ToString("F2") + "%";

            string totalEffectString = (combinedFactor * 100).ToString("F2") + "%";

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
    public static void HandlePrestigeBuff(Entity player, PrefabGUID buffPrefab)
    {
        ServerGameManager serverGameManager = Core.ServerGameManager;
        DebugEventsSystem debugEventsSystem = Core.DebugEventsSystem;
        ApplyBuffDebugEvent applyBuffDebugEvent = new()
        {
            BuffPrefabGUID = buffPrefab,
        };
        FromCharacter fromCharacter = new()
        {
            Character = player,
            User = player.Read<PlayerCharacter>().UserEntity,
        };
        // apply level up buff here
        debugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
        if (serverGameManager.TryGetBuff(player, buffPrefab.ToIdentifier(), out Entity buff))
        {
            Core.Log.LogInfo(buff.Read<PrefabGUID>().LookupName());
            //buff.LogComponentTypes();
            if (buff.Has<RemoveBuffOnGameplayEvent>())
            {
                buff.Remove<RemoveBuffOnGameplayEvent>();
            }
            if (buff.Has<RemoveBuffOnGameplayEventEntry>())
            {
                buff.Remove<RemoveBuffOnGameplayEventEntry>();
            }
            if (buff.Has<CreateGameplayEventsOnSpawn>())
            {
                buff.Remove<CreateGameplayEventsOnSpawn>();
            }
            if (buff.Has<GameplayEventListeners>())
            {
                buff.Remove<GameplayEventListeners>();
            }
            if (!buff.Has<Buff_Persists_Through_Death>())
            {
                buff.Write(new Buff_Persists_Through_Death());
            }
            if (buff.Has<LifeTime>())
            {
                LifeTime lifeTime = buff.Read<LifeTime>();
                lifeTime.Duration = -1;
                lifeTime.EndAction = LifeTimeEndAction.None;
                buff.Write(lifeTime);
            }
        }
    }
    
    static void HandleExperiencePrestige(ChatCommandContext ctx, int prestigeLevel)
    {
        GearOverride.SetLevel(ctx.Event.SenderCharacterEntity);

        List<int> buffs = Core.ParseConfigString(Plugin.PrestigeBuffs.Value);
        PrefabGUID buffPrefab = new(buffs[prestigeLevel-1]);
        if (!buffPrefab.GuidHash.Equals(0)) HandlePrestigeBuff(ctx.Event.SenderCharacterEntity, buffPrefab);

        if (Plugin.SoftSynergies.Value || Plugin.HardSynergies.Value)
        {
            if (Core.DataStructures.PlayerClasses.TryGetValue(ctx.Event.User.PlatformId, out var classes) && classes.Keys.Count > 0)
            {
                LevelingSystem.PlayerClasses playerClass = classes.FirstOrDefault().Key;
                buffs = Core.ParseConfigString(LevelingSystem.ClassPrestigeBuffsMap[playerClass]);
                buffPrefab = new(buffs[prestigeLevel - 1]);
                if (!buffPrefab.GuidHash.Equals(0)) HandlePrestigeBuff(ctx.Event.SenderCharacterEntity, buffPrefab);
            }
        }

        float levelingReducer = Plugin.LevelingPrestigeReducer.Value * prestigeLevel;

        string reductionPercentage = (levelingReducer * 100).ToString("F2") + "%";

        float gainMultiplier = Plugin.PrestigeRatesMultiplier.Value * prestigeLevel;

        string gainPercentage = (gainMultiplier * 100).ToString("F2") + "%";
        ctx.Reply($"You have prestiged in <color=#90EE90>Experience</color>[<color=white>{prestigeLevel}</color>]! Growth rates for all expertise/legacies increased by <color=green>{gainPercentage}</color>, growth rates for experience reduced by <color=yellow>{reductionPercentage}</color>");
    }

    static void HandleOtherPrestige(ChatCommandContext ctx, ulong steamId, PrestigeSystem.PrestigeType parsedPrestigeType, int prestigeLevel)
    {
        int expPrestige = Core.DataStructures.PlayerPrestiges.TryGetValue(steamId, out var prestiges) && prestiges.TryGetValue(PrestigeSystem.PrestigeType.Experience, out var xpLevel) ? xpLevel : 0;

        float ratesReduction = prestigeLevel * Plugin.PrestigeRatesReducer.Value; // Example: 0.1 (10%)
        float ratesMultiplier = expPrestige * Plugin.PrestigeRatesMultiplier.Value;

        float combinedFactor = ratesMultiplier - ratesReduction;

        string percentageReductionString = (ratesReduction * 100).ToString("F0") + "%";

        // Fixed additive stat gain increase based on base value
        float statGainIncrease = Plugin.PrestigeStatMultiplier.Value * prestigeLevel;
        string statGainString = (statGainIncrease * 100).ToString("F2") + "%";

        string totalEffectString = (combinedFactor * 100).ToString("F2") + "%";

        ctx.Reply($"You have prestiged in <color=#90EE90>{parsedPrestigeType}</color>[<color=white>{prestigeLevel}</color>]! Growth rate reduced by <color=yellow>{percentageReductionString}</color> and stat bonuses improved by <color=green>{statGainString}</color>. The total change in growth rate with leveling prestige bonus is <color=yellow>{totalEffectString}</color>.");
    }

    public static void RemoveCurrentBuffs(ChatCommandContext ctx, PlayerClasses playerClass, int level)
    {
        var buffs = Core.ParseConfigString(LevelingSystem.ClassPrestigeBuffsMap[playerClass]);
        var buffSpawner = BuffUtility.BuffSpawner.Create(Core.ServerGameManager);
        var entityCommandBuffer = Core.EntityCommandBufferSystem.CreateCommandBuffer();

        for (int i = 0; i < level; i++)
        {
            var buffPrefab = new PrefabGUID(buffs[i]);
            if (Core.ServerGameManager.TryGetBuff(ctx.Event.SenderCharacterEntity, buffPrefab.ToIdentifier(), out var buff))
            {
                BuffUtility.TryRemoveBuff(ref buffSpawner, entityCommandBuffer, buffPrefab.ToIdentifier(), ctx.Event.SenderCharacterEntity);
            }
        }
    }
    public static void RemoveAllBuffs(ChatCommandContext ctx, ulong steamId, int prestigeLevel)
    {
        var buffs = Core.ParseConfigString(Plugin.PrestigeBuffs.Value);
        var classBuffs = GetClassBuffs(steamId);
        var buffSpawner = BuffUtility.BuffSpawner.Create(Core.ServerGameManager);
        var entityCommandBuffer = Core.EntityCommandBufferSystem.CreateCommandBuffer();

        for (int i = 0; i < prestigeLevel; i++)
        {
            RemoveBuff(ctx, buffs[i], buffSpawner, entityCommandBuffer);
            if (classBuffs.Count == 0 || classBuffs[i] == 0) continue;
            RemoveBuff(ctx, classBuffs[i], buffSpawner, entityCommandBuffer);
        }
    }
    private static void RemoveBuff(ChatCommandContext ctx, int buffId, BuffUtility.BuffSpawner buffSpawner, EntityCommandBuffer entityCommandBuffer)
    {
        var buffPrefab = new PrefabGUID(buffId);
        if (Core.ServerGameManager.TryGetBuff(ctx.Event.SenderCharacterEntity, buffPrefab.ToIdentifier(), out var buff))
        {
            BuffUtility.TryRemoveBuff(ref buffSpawner, entityCommandBuffer, buffPrefab.ToIdentifier(), ctx.Event.SenderCharacterEntity);
        }
    }
    private static List<int> GetClassBuffs(ulong steamId)
    {
        if (Core.DataStructures.PlayerClasses.TryGetValue(steamId, out var classes))
        {
            var playerClass = classes.Keys.FirstOrDefault();
            if (playerClass != default)
            {
                return Core.ParseConfigString(LevelingSystem.ClassPrestigeBuffsMap[playerClass]);
            }
        }

        return new List<int>();
    }

    public static int GetExperiencePrestigeLevel(ulong steamId)
    {
        return Core.DataStructures.PlayerPrestiges.TryGetValue(steamId, out var prestigeData) &&
               prestigeData.TryGetValue(PrestigeSystem.PrestigeType.Experience, out var prestigeLevel) &&
               prestigeLevel > 0 ? prestigeLevel : 0;
    }
    /*
    public static bool TryParsePrestigeType(string prestigeType, out PrestigeSystem.PrestigeType parsedPrestigeType)
    {
        if (!Enum.TryParse(prestigeType, true, out parsedPrestigeType))
        {
            parsedPrestigeType = Enum.GetValues(typeof(PrestigeSystem.PrestigeType))
                                     .Cast<PrestigeSystem.PrestigeType>()
                                     .FirstOrDefault(pt => pt.ToString().Contains(prestigeType, StringComparison.OrdinalIgnoreCase));

        }

        return true;
    }
    */
    public static bool TryParsePrestigeType(string prestigeType, out PrestigeSystem.PrestigeType parsedPrestigeType)
    {
        // Attempt to parse the prestigeType string to the PrestigeType enum.
        if (Enum.TryParse(prestigeType, true, out parsedPrestigeType))
        {
            return true; // Successfully parsed
        }

        // If the initial parse failed, try to find a matching PrestigeType enum value containing the input string.
        parsedPrestigeType = Enum.GetValues(typeof(PrestigeSystem.PrestigeType))
                                 .Cast<PrestigeSystem.PrestigeType>()
                                 .FirstOrDefault(pt => pt.ToString().Contains(prestigeType, StringComparison.OrdinalIgnoreCase));

        // Check if a valid enum value was found that contains the input string.
        if (!parsedPrestigeType.Equals(default(PrestigeSystem.PrestigeType)))
        {
            return true; // Found a matching enum value
        }

        // If no match is found, return false and set the out parameter to default value.
        parsedPrestigeType = default;
        return false; // Parsing failed
    }

}
