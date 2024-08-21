using Bloodcraft.Patches;
using Bloodcraft.Services;
using Bloodcraft.SystemUtilities.Experience;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
using static Bloodcraft.Utilities;

namespace Bloodcraft.SystemUtilities.Leveling;
public static class PrestigeSystem
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static PlayerService PlayerService => Core.PlayerService;
    static ConfigService ConfigService => Core.ConfigService;
    static LocalizationService LocalizationService => Core.LocalizationService;
    static DebugEventsSystem DebugEventsSystem => SystemService.DebugEventsSystem;
    static EntityCommandBufferSystem EntityCommandBufferSystem => SystemService.EntityCommandBufferSystem;
    public enum PrestigeType
    {
        Experience,
        Exo,
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
        UnarmedExpertise,
        FishingPoleExpertise,
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

    public static readonly Dictionary<PrestigeSystem.PrestigeType, int> PrestigeTypeToMaxLevel = new()
    {
        { PrestigeSystem.PrestigeType.Experience, ConfigService.MaxPlayerLevel },
        { PrestigeSystem.PrestigeType.Exo, ConfigService.MaxLevelingPrestiges },
        { PrestigeSystem.PrestigeType.SwordExpertise, ConfigService.MaxExpertiseLevel },
        { PrestigeSystem.PrestigeType.AxeExpertise, ConfigService.MaxExpertiseLevel },
        { PrestigeSystem.PrestigeType.MaceExpertise, ConfigService.MaxExpertiseLevel },
        { PrestigeSystem.PrestigeType.SpearExpertise, ConfigService.MaxExpertiseLevel },
        { PrestigeSystem.PrestigeType.CrossbowExpertise, ConfigService.MaxExpertiseLevel },
        { PrestigeSystem.PrestigeType.GreatSwordExpertise, ConfigService.MaxExpertiseLevel },
        { PrestigeSystem.PrestigeType.SlashersExpertise, ConfigService.MaxExpertiseLevel },
        { PrestigeSystem.PrestigeType.PistolsExpertise, ConfigService.MaxExpertiseLevel },
        { PrestigeSystem.PrestigeType.ReaperExpertise, ConfigService.MaxExpertiseLevel },
        { PrestigeSystem.PrestigeType.LongbowExpertise, ConfigService.MaxExpertiseLevel },
        { PrestigeSystem.PrestigeType.WhipExpertise, ConfigService.MaxExpertiseLevel },
        { PrestigeSystem.PrestigeType.UnarmedExpertise, ConfigService.MaxExpertiseLevel },
        { PrestigeSystem.PrestigeType.FishingPoleExpertise, ConfigService.MaxExpertiseLevel },
        { PrestigeSystem.PrestigeType.WorkerLegacy, ConfigService.MaxBloodLevel },
        { PrestigeSystem.PrestigeType.WarriorLegacy, ConfigService.MaxBloodLevel },
        { PrestigeSystem.PrestigeType.ScholarLegacy, ConfigService.MaxBloodLevel },
        { PrestigeSystem.PrestigeType.RogueLegacy, ConfigService.MaxBloodLevel },
        { PrestigeSystem.PrestigeType.MutantLegacy, ConfigService.MaxBloodLevel },
        { PrestigeSystem.PrestigeType.DraculinLegacy, ConfigService.MaxBloodLevel },
        { PrestigeSystem.PrestigeType.ImmortalLegacy, ConfigService.MaxBloodLevel },
        { PrestigeSystem.PrestigeType.CreatureLegacy, ConfigService.MaxBloodLevel },
        { PrestigeSystem.PrestigeType.BruteLegacy, ConfigService.MaxBloodLevel }
    };

    public static readonly Dictionary<PrestigeSystem.PrestigeType, int> PrestigeTypeToMaxPrestiges = new()
    {
        { PrestigeSystem.PrestigeType.Experience, ConfigService.MaxLevelingPrestiges },
        { PrestigeSystem.PrestigeType.Exo, ConfigService.ExoPrestiges },
        { PrestigeSystem.PrestigeType.SwordExpertise, ConfigService.MaxExpertisePrestiges },
        { PrestigeSystem.PrestigeType.AxeExpertise, ConfigService.MaxExpertisePrestiges },
        { PrestigeSystem.PrestigeType.MaceExpertise, ConfigService.MaxExpertisePrestiges },
        { PrestigeSystem.PrestigeType.SpearExpertise, ConfigService.MaxExpertisePrestiges },
        { PrestigeSystem.PrestigeType.CrossbowExpertise, ConfigService.MaxExpertisePrestiges },
        { PrestigeSystem.PrestigeType.GreatSwordExpertise, ConfigService.MaxExpertisePrestiges },
        { PrestigeSystem.PrestigeType.SlashersExpertise, ConfigService.MaxExpertisePrestiges },
        { PrestigeSystem.PrestigeType.PistolsExpertise, ConfigService.MaxExpertisePrestiges },
        { PrestigeSystem.PrestigeType.ReaperExpertise, ConfigService.MaxExpertisePrestiges },
        { PrestigeSystem.PrestigeType.LongbowExpertise, ConfigService.MaxExpertisePrestiges },
        { PrestigeSystem.PrestigeType.WhipExpertise, ConfigService.MaxExpertisePrestiges },
        { PrestigeSystem.PrestigeType.UnarmedExpertise, ConfigService.MaxExpertisePrestiges },
        { PrestigeSystem.PrestigeType.FishingPoleExpertise, ConfigService.MaxExpertisePrestiges },
        { PrestigeSystem.PrestigeType.WorkerLegacy, ConfigService.MaxLegacyPrestiges },
        { PrestigeSystem.PrestigeType.WarriorLegacy, ConfigService.MaxLegacyPrestiges },
        { PrestigeSystem.PrestigeType.ScholarLegacy, ConfigService.MaxLegacyPrestiges },
        { PrestigeSystem.PrestigeType.RogueLegacy, ConfigService.MaxLegacyPrestiges },
        { PrestigeSystem.PrestigeType.MutantLegacy, ConfigService.MaxLegacyPrestiges },
        { PrestigeSystem.PrestigeType.DraculinLegacy, ConfigService.MaxLegacyPrestiges },
        { PrestigeSystem.PrestigeType.ImmortalLegacy, ConfigService.MaxLegacyPrestiges },
        { PrestigeSystem.PrestigeType.CreatureLegacy, ConfigService.MaxLegacyPrestiges },
        { PrestigeSystem.PrestigeType.BruteLegacy, ConfigService.MaxLegacyPrestiges }
    };
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
                reductionFactor = ConfigService.LevelingPrestigeReducer * expPrestigeLevel;

                // Apply rate gain with linear increase for expertise/legacy
                gainMultiplier = ConfigService.PrestigeRatesMultiplier * expPrestigeLevel;
            }

            string reductionPercentage = (reductionFactor * 100).ToString("F2") + "%";
            string gainPercentage = (gainMultiplier * 100).ToString("F2") + "%";

            ctx.Reply($"<color=#90EE90>{parsedPrestigeType}</color> Prestige Info:");
            ctx.Reply($"Current Prestige Level: <color=yellow>{prestigeLevel}</color>/{maxPrestigeLevel}");
            ctx.Reply($"Growth rate improvement for expertise and legacies: <color=green>{gainPercentage}</color>");
            ctx.Reply($"Growth rate reduction for experience: <color=yellow>{reductionPercentage}</color>");
        }
        else
        {
            if (Core.DataStructures.PlayerPrestiges.TryGetValue(steamId, out var prestigeData) &&
                prestigeData.TryGetValue(PrestigeSystem.PrestigeType.Experience, out var expPrestigeLevel) && expPrestigeLevel > 0)
            {
                // Apply flat rate reduction for leveling experience
                reductionFactor = ConfigService.LevelingPrestigeReducer * expPrestigeLevel;

                // Apply rate gain with linear increase for expertise/legacy
                gainMultiplier = ConfigService.PrestigeRatesMultiplier * expPrestigeLevel;
            }

            float combinedFactor = gainMultiplier - reductionFactor;
            string percentageReductionString = (reductionFactor * 100).ToString("F2") + "%";

            // Fixed additive stat gain increase based on base value
            float statGainIncrease = ConfigService.PrestigeStatMultiplier * prestigeLevel;
            string statGainString = (statGainIncrease * 100).ToString("F2") + "%";

            string totalEffectString = (combinedFactor * 100).ToString("F2") + "%";

            LocalizationService.HandleReply(ctx, $"<color=#90EE90>{parsedPrestigeType}</color> Prestige Info:");
            LocalizationService.HandleReply(ctx, $"Current Prestige Level: <color=yellow>{prestigeLevel}</color>/{maxPrestigeLevel}");
            LocalizationService.HandleReply(ctx, $"Growth rate reduction from <color=#90EE90>{parsedPrestigeType}</color> prestige level: <color=yellow>{percentageReductionString}</color>");
            LocalizationService.HandleReply(ctx, $"Stat bonuses improvement: <color=green>{statGainString}</color>");
            LocalizationService.HandleReply(ctx, $"Total change in growth rate including leveling prestige bonus: <color=yellow>{totalEffectString}</color>");
        }
    }
    public static bool CanPrestige(ulong steamId, PrestigeSystem.PrestigeType parsedPrestigeType, int xpKey)
    {
        return xpKey >= PrestigeSystem.PrestigeTypeToMaxLevel[parsedPrestigeType] &&
               Core.DataStructures.PlayerPrestiges.TryGetValue(steamId, out var prestigeData) &&
               prestigeData.TryGetValue(parsedPrestigeType, out var prestigeLevel) &&
               prestigeLevel < PrestigeSystem.PrestigeTypeToMaxPrestiges[parsedPrestigeType];
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
        ApplyBuffDebugEvent applyBuffDebugEvent = new()
        {
            BuffPrefabGUID = buffPrefab,
        };

        FromCharacter fromCharacter = new()
        {
            Character = player,
            User = player.Read<PlayerCharacter>().UserEntity,
        };
        
        if (!ServerGameManager.HasBuff(player, buffPrefab.ToIdentifier()))
        {
            DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
            if (ServerGameManager.TryGetBuff(player, buffPrefab.ToIdentifier(), out Entity buffEntity))
            {
                LevelingSystem.HandleBloodBuff(buffEntity);
                if (buffEntity.Has<RemoveBuffOnGameplayEvent>())
                {
                    buffEntity.Remove<RemoveBuffOnGameplayEvent>();
                }
                if (buffEntity.Has<RemoveBuffOnGameplayEventEntry>())
                {
                    buffEntity.Remove<RemoveBuffOnGameplayEventEntry>();
                }
                if (buffEntity.Has<CreateGameplayEventsOnSpawn>())
                {
                    buffEntity.Remove<CreateGameplayEventsOnSpawn>();
                }
                if (buffEntity.Has<GameplayEventListeners>())
                {
                    buffEntity.Remove<GameplayEventListeners>();
                }
                if (!buffEntity.Has<Buff_Persists_Through_Death>())
                {
                    buffEntity.Add<Buff_Persists_Through_Death>();
                }
                if (buffEntity.Has<DestroyOnGameplayEvent>())
                {
                    buffEntity.Remove<DestroyOnGameplayEvent>();
                }
                if (buffEntity.Has<LifeTime>())
                {
                    LifeTime lifeTime = buffEntity.Read<LifeTime>();
                    lifeTime.Duration = -1;
                    lifeTime.EndAction = LifeTimeEndAction.None;
                    buffEntity.Write(lifeTime);
                }
            }
        }
    }
    static void HandleExperiencePrestige(ChatCommandContext ctx, int prestigeLevel)
    {
        LevelingSystem.SetLevel(ctx.Event.SenderCharacterEntity);
        ulong steamId = ctx.Event.User.PlatformId;

        List<int> buffs = ParseConfigString(ConfigService.PrestigeBuffs);
        PrefabGUID buffPrefab = new(buffs[prestigeLevel-1]);
        if (!buffPrefab.GuidHash.Equals(0)) HandlePrestigeBuff(ctx.Event.SenderCharacterEntity, buffPrefab);

        if (ConfigService.RestedXP) LevelingSystem.ResetRestedXP(steamId);

        float levelingReducer = ConfigService.LevelingPrestigeReducer * prestigeLevel;
        string reductionPercentage = (levelingReducer * 100).ToString("F2") + "%";

        float gainMultiplier = ConfigService.PrestigeRatesMultiplier * prestigeLevel;
        string gainPercentage = (gainMultiplier * 100).ToString("F2") + "%";

        LocalizationService.HandleReply(ctx, $"You have prestiged in <color=#90EE90>Experience</color>[<color=white>{prestigeLevel}</color>]! Growth rates for all expertise/legacies increased by <color=green>{gainPercentage}</color>, growth rates for experience reduced by <color=yellow>{reductionPercentage}</color>");
    }
    static void HandleOtherPrestige(ChatCommandContext ctx, ulong steamId, PrestigeSystem.PrestigeType parsedPrestigeType, int prestigeLevel)
    {
        int expPrestige = Core.DataStructures.PlayerPrestiges.TryGetValue(steamId, out var prestiges) && prestiges.TryGetValue(PrestigeSystem.PrestigeType.Experience, out var xpLevel) ? xpLevel : 0;

        float ratesReduction = prestigeLevel * ConfigService.PrestigeRatesReducer; // Example: 0.1 (10%)
        float ratesMultiplier = expPrestige * ConfigService.PrestigeRatesMultiplier;

        float combinedFactor = ratesMultiplier - ratesReduction;

        string percentageReductionString = (ratesReduction * 100).ToString("F0") + "%";

        // Fixed additive stat gain increase based on base value
        float statGainIncrease = ConfigService.PrestigeStatMultiplier * prestigeLevel;
        string statGainString = (statGainIncrease * 100).ToString("F2") + "%";

        string totalEffectString = (combinedFactor * 100).ToString("F2") + "%";

        LocalizationService.HandleReply(ctx, $"You have prestiged in <color=#90EE90>{parsedPrestigeType}</color>[<color=white>{prestigeLevel}</color>]! Growth rate reduced by <color=yellow>{percentageReductionString}</color> and stat bonuses improved by <color=green>{statGainString}</color>. The total change in growth rate with leveling prestige bonus is <color=yellow>{totalEffectString}</color>.");
    }
    public static void RemovePrestigeBuffs(Entity character, int prestigeLevel)
    {
        var buffs = ParseConfigString(ConfigService.PrestigeBuffs);
        var buffSpawner = BuffUtility.BuffSpawner.Create(ServerGameManager);
        var entityCommandBuffer = EntityCommandBufferSystem.CreateCommandBuffer();

        for (int i = 0; i < prestigeLevel; i++)
        {
            RemoveBuff(character, buffs[i], buffSpawner, entityCommandBuffer);
        }
    }
    public static void ApplyPrestigeBuffs(Entity character, int prestigeLevel)
    {
        List<int> buffs = ParseConfigString(ConfigService.PrestigeBuffs);
        if (buffs.Count == 0) return;
        for (int i = 0; i < prestigeLevel; i++)
        {
            PrefabGUID buffPrefab = new(buffs[i]);
            if (buffPrefab.GuidHash == 0) continue;
            HandlePrestigeBuff(character, buffPrefab);
        }
    }
    public static void ApplyExperiencePrestigeEffects(User user, int level)
    {
        float levelingReducer = ConfigService.LevelingPrestigeReducer * level;

        string reductionPercentage = (levelingReducer * 100).ToString("F2") + "%";

        float gainMultiplier = ConfigService.PrestigeRatesMultiplier * level;

        string gainPercentage = (gainMultiplier * 100).ToString("F2") + "%";
        LocalizationService.HandleServerReply(EntityManager, user, $"Player <color=green>{user.CharacterName.Value}</color> has prestiged in <color=#90EE90>Experience</color>[<color=white>{level}</color>]! Growth rates for all expertise/legacies increased by <color=green>{gainPercentage}</color>, growth rates for experience reduced by <color=yellow>{reductionPercentage}</color>");
    }
    public static void ApplyOtherPrestigeEffects(User user, ulong playerId, PrestigeSystem.PrestigeType parsedPrestigeType, int level)
    {
        int expPrestige = Core.DataStructures.PlayerPrestiges.TryGetValue(playerId, out var prestiges) && prestiges.TryGetValue(PrestigeSystem.PrestigeType.Experience, out var xpLevel) ? xpLevel : 0;

        float ratesReduction = level * ConfigService.PrestigeRatesReducer;
        float ratesMultiplier = expPrestige * ConfigService.PrestigeRatesMultiplier;

        float combinedFactor = ratesMultiplier - ratesReduction;

        string percentageReductionString = (ratesReduction * 100).ToString("F0") + "%";

        float statGainIncrease = ConfigService.PrestigeStatMultiplier * level;
        string statGainString = (statGainIncrease * 100).ToString("F2") + "%";

        string totalEffectString = (combinedFactor * 100).ToString("F2") + "%";
        LocalizationService.HandleServerReply(EntityManager, user, $"Player <color=green>{user.CharacterName.Value}</color> has prestiged in <color=#90EE90>{parsedPrestigeType}</color>[<color=white>{level}</color>]! Growth rate reduced by <color=yellow>{percentageReductionString}</color> and stat bonuses improved by <color=green>{statGainString}</color>. The total change in growth rate with leveling prestige bonus is <color=yellow>{totalEffectString}</color>.");
    }
    static void RemoveBuff(Entity character, int buffId, BuffUtility.BuffSpawner buffSpawner, EntityCommandBuffer entityCommandBuffer)
    {
        var buffPrefab = new PrefabGUID(buffId);
        if (ServerGameManager.HasBuff(character, buffPrefab.ToIdentifier()))
        {
            BuffUtility.TryRemoveBuff(ref buffSpawner, entityCommandBuffer, buffPrefab.ToIdentifier(), character);
        }
    }
    public static int GetExperiencePrestigeLevel(ulong steamId)
    {
        return Core.DataStructures.PlayerPrestiges.TryGetValue(steamId, out var prestigeData) &&
               prestigeData.TryGetValue(PrestigeSystem.PrestigeType.Experience, out var prestigeLevel) &&
               prestigeLevel > 0 ? prestigeLevel : 0;
    }
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
    public static Dictionary<string, int> GetPrestigeForType(PrestigeType prestigeType)
    {
        return Core.DataStructures.PlayerPrestiges
            .Where(p => p.Value.ContainsKey(prestigeType))
            .Select(p => new
            {
                SteamId = p.Key,
                Prestige = p.Value[prestigeType]
            })
            .Select(p => new
            {
                PlayerName = PlayerService.UserCache.FirstOrDefault(pc => pc.Value.Read<User>().PlatformId == p.SteamId).Key,
                p.Prestige
            })
            .Where(p => !string.IsNullOrEmpty(p.PlayerName))
            .ToDictionary(p => p.PlayerName, p => p.Prestige);
    }
    public static void AdjustCharacterStats(Entity character, ulong platformId)
    {
        var prestigeData = Core.DataStructures.PlayerPrestiges[platformId];
        float damageTakenMultiplier = ConfigService.ExoPrestigeDamageTakenMultiplier * prestigeData[PrestigeSystem.PrestigeType.Exo];
        float damageDealtMultiplier = ConfigService.ExoPrestigeDamageDealtMultiplier * prestigeData[PrestigeSystem.PrestigeType.Exo];

        AdjustResistStats(character, -damageTakenMultiplier);
        AdjustDamageStats(character, damageDealtMultiplier);
    }
    static void AdjustResistStats(Entity character, float multiplier)
    {
        ResistCategoryStats resistCategoryStats = character.Read<ResistCategoryStats>();
        resistCategoryStats.ResistVsBeasts._Value = multiplier;
        resistCategoryStats.ResistVsHumans._Value = multiplier;
        resistCategoryStats.ResistVsUndeads._Value = multiplier;
        resistCategoryStats.ResistVsDemons._Value = multiplier;
        resistCategoryStats.ResistVsMechanical._Value = multiplier;
        resistCategoryStats.ResistVsVampires._Value = multiplier;
        character.Write(resistCategoryStats);
    }
    static void AdjustDamageStats(Entity character, float multiplier)
    {
        DamageCategoryStats damageCategoryStats = character.Read<DamageCategoryStats>();
        damageCategoryStats.DamageVsBeasts._Value = 1 + multiplier;
        damageCategoryStats.DamageVsHumans._Value = 1 + multiplier;
        damageCategoryStats.DamageVsUndeads._Value = 1 + multiplier;
        damageCategoryStats.DamageVsDemons._Value = 1 + multiplier;
        damageCategoryStats.DamageVsMechanical._Value = 1 + multiplier;
        damageCategoryStats.DamageVsVampires._Value = 1 + multiplier;
        character.Write(damageCategoryStats);
    }
}
