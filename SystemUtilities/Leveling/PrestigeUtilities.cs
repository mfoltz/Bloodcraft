using Bloodcraft.Patches;
using Bloodcraft.Services;
using Bloodcraft.SystemUtilities.Experience;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
using static Bloodcraft.Services.LocalizationService;

namespace Bloodcraft.SystemUtilities.Leveling;
public static class PrestigeUtilities
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static DebugEventsSystem DebugEventsSystem => Core.DebugEventsSystem;
    static EntityCommandBufferSystem EntityCommandBufferSystem => Core.EntityCommandBufferSystem;

    static readonly bool RestedXP = Plugin.RestedXP.Value;

    static readonly float LevelingPrestigeReducer = Plugin.LevelingPrestigeReducer.Value;
    static readonly float PrestigeRatesMultiplier = Plugin.PrestigeRatesMultiplier.Value;
    static readonly float PrestigeRatesReducer = Plugin.PrestigeRatesReducer.Value;
    static readonly float PrestigeStatMultiplier = Plugin.PrestigeStatMultiplier.Value;
    static readonly float ExoPrestigeDamageTakenMultiplier = Plugin.ExoPrestigeDamageTakenMultiplier.Value;
    static readonly float ExoPrestigeDamageDealtMultiplier = Plugin.ExoPrestigeDamageDealtMultiplier.Value;
    static readonly int MaxPlayerLevel = Plugin.MaxPlayerLevel.Value;
    static readonly int MaxLevelingPrestiges = Plugin.MaxLevelingPrestiges.Value;
    static readonly int MaxExpertiseLevel = Plugin.MaxExpertiseLevel.Value;
    static readonly int MaxExpertisePrestiges = Plugin.MaxExpertisePrestiges.Value;
    static readonly int MaxBloodLevel = Plugin.MaxBloodLevel.Value;
    static readonly int MaxLegacyPrestiges = Plugin.MaxLegacyPrestiges.Value;
    static readonly int ExoPrestiges = Plugin.ExoPrestiges.Value;
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

    static readonly Dictionary<PrestigeUtilities.PrestigeType, int> prestigeTypeToMaxLevel = new()
    {
        { PrestigeUtilities.PrestigeType.Experience, MaxPlayerLevel },
        { PrestigeUtilities.PrestigeType.Exo, MaxLevelingPrestiges },
        { PrestigeUtilities.PrestigeType.SwordExpertise, MaxExpertiseLevel },
        { PrestigeUtilities.PrestigeType.AxeExpertise, MaxExpertiseLevel },
        { PrestigeUtilities.PrestigeType.MaceExpertise, MaxExpertiseLevel },
        { PrestigeUtilities.PrestigeType.SpearExpertise, MaxExpertiseLevel },
        { PrestigeUtilities.PrestigeType.CrossbowExpertise, MaxExpertiseLevel },
        { PrestigeUtilities.PrestigeType.GreatSwordExpertise, MaxExpertiseLevel },
        { PrestigeUtilities.PrestigeType.SlashersExpertise, MaxExpertiseLevel },
        { PrestigeUtilities.PrestigeType.PistolsExpertise, MaxExpertiseLevel },
        { PrestigeUtilities.PrestigeType.ReaperExpertise, MaxExpertiseLevel },
        { PrestigeUtilities.PrestigeType.LongbowExpertise, MaxExpertiseLevel },
        { PrestigeUtilities.PrestigeType.WhipExpertise, MaxExpertiseLevel },
        { PrestigeUtilities.PrestigeType.UnarmedExpertise, MaxExpertiseLevel },
        { PrestigeUtilities.PrestigeType.FishingPoleExpertise, MaxExpertiseLevel },
        { PrestigeUtilities.PrestigeType.WorkerLegacy, MaxBloodLevel },
        { PrestigeUtilities.PrestigeType.WarriorLegacy, MaxBloodLevel },
        { PrestigeUtilities.PrestigeType.ScholarLegacy, MaxBloodLevel },
        { PrestigeUtilities.PrestigeType.RogueLegacy, MaxBloodLevel },
        { PrestigeUtilities.PrestigeType.MutantLegacy, MaxBloodLevel },
        { PrestigeUtilities.PrestigeType.DraculinLegacy, MaxBloodLevel },
        { PrestigeUtilities.PrestigeType.ImmortalLegacy, MaxBloodLevel },
        { PrestigeUtilities.PrestigeType.CreatureLegacy, MaxBloodLevel },
        { PrestigeUtilities.PrestigeType.BruteLegacy, MaxBloodLevel }
    };
    public static Dictionary<PrestigeUtilities.PrestigeType, int> PrestigeTypeToMaxLevel
    {
       get => prestigeTypeToMaxLevel;
    }
    static readonly Dictionary<PrestigeUtilities.PrestigeType, int> prestigeTypeToMaxPrestigeLevel = new()
    {
        { PrestigeUtilities.PrestigeType.Experience, MaxLevelingPrestiges },
        { PrestigeUtilities.PrestigeType.Exo, ExoPrestiges },
        { PrestigeUtilities.PrestigeType.SwordExpertise, MaxExpertisePrestiges },
        { PrestigeUtilities.PrestigeType.AxeExpertise, MaxExpertisePrestiges },
        { PrestigeUtilities.PrestigeType.MaceExpertise, MaxExpertisePrestiges },
        { PrestigeUtilities.PrestigeType.SpearExpertise, MaxExpertisePrestiges },
        { PrestigeUtilities.PrestigeType.CrossbowExpertise, MaxExpertisePrestiges },
        { PrestigeUtilities.PrestigeType.GreatSwordExpertise, MaxExpertisePrestiges },
        { PrestigeUtilities.PrestigeType.SlashersExpertise, MaxExpertisePrestiges },
        { PrestigeUtilities.PrestigeType.PistolsExpertise, MaxExpertisePrestiges },
        { PrestigeUtilities.PrestigeType.ReaperExpertise, MaxExpertisePrestiges },
        { PrestigeUtilities.PrestigeType.LongbowExpertise, MaxExpertisePrestiges },
        { PrestigeUtilities.PrestigeType.WhipExpertise, MaxExpertisePrestiges },
        { PrestigeUtilities.PrestigeType.UnarmedExpertise, MaxExpertisePrestiges },
        { PrestigeUtilities.PrestigeType.FishingPoleExpertise, MaxExpertisePrestiges },
        { PrestigeUtilities.PrestigeType.WorkerLegacy, MaxLegacyPrestiges },
        { PrestigeUtilities.PrestigeType.WarriorLegacy, MaxLegacyPrestiges },
        { PrestigeUtilities.PrestigeType.ScholarLegacy, MaxLegacyPrestiges },
        { PrestigeUtilities.PrestigeType.RogueLegacy, MaxLegacyPrestiges },
        { PrestigeUtilities.PrestigeType.MutantLegacy, MaxLegacyPrestiges },
        { PrestigeUtilities.PrestigeType.DraculinLegacy, MaxLegacyPrestiges },
        { PrestigeUtilities.PrestigeType.ImmortalLegacy, MaxLegacyPrestiges },
        { PrestigeUtilities.PrestigeType.CreatureLegacy, MaxLegacyPrestiges },
        { PrestigeUtilities.PrestigeType.BruteLegacy, MaxLegacyPrestiges }
    };
    public static Dictionary<PrestigeUtilities.PrestigeType, int> PrestigeTypeToMaxPrestigeLevel
    {
        get => prestigeTypeToMaxPrestigeLevel;
    }
    public static void DisplayPrestigeInfo(ChatCommandContext ctx, ulong steamId, PrestigeUtilities.PrestigeType parsedPrestigeType, int prestigeLevel, int maxPrestigeLevel)
    {
        float reductionFactor = 1.0f;
        float gainMultiplier = 1.0f;

        if (parsedPrestigeType == PrestigeUtilities.PrestigeType.Experience)
        {
            if (Core.DataStructures.PlayerPrestiges.TryGetValue(steamId, out var prestigeData) &&
                prestigeData.TryGetValue(PrestigeUtilities.PrestigeType.Experience, out var expPrestigeLevel) && expPrestigeLevel > 0)
            {
                // Apply flat rate reduction for leveling experience
                reductionFactor = LevelingPrestigeReducer * expPrestigeLevel;

                // Apply rate gain with linear increase for expertise/legacy
                gainMultiplier = PrestigeRatesMultiplier * expPrestigeLevel;
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
                prestigeData.TryGetValue(PrestigeUtilities.PrestigeType.Experience, out var expPrestigeLevel) && expPrestigeLevel > 0)
            {
                // Apply flat rate reduction for leveling experience
                reductionFactor = LevelingPrestigeReducer * expPrestigeLevel;

                // Apply rate gain with linear increase for expertise/legacy
                gainMultiplier = PrestigeRatesMultiplier * expPrestigeLevel;
            }

            float combinedFactor = gainMultiplier - reductionFactor;
            string percentageReductionString = (reductionFactor * 100).ToString("F2") + "%";

            // Fixed additive stat gain increase based on base value
            float statGainIncrease = PrestigeStatMultiplier * prestigeLevel;
            string statGainString = (statGainIncrease * 100).ToString("F2") + "%";

            string totalEffectString = (combinedFactor * 100).ToString("F2") + "%";

            HandleReply(ctx, $"<color=#90EE90>{parsedPrestigeType}</color> Prestige Info:");
            HandleReply(ctx, $"Current Prestige Level: <color=yellow>{prestigeLevel}</color>/{maxPrestigeLevel}");
            HandleReply(ctx, $"Growth rate reduction from <color=#90EE90>{parsedPrestigeType}</color> prestige level: <color=yellow>{percentageReductionString}</color>");
            HandleReply(ctx, $"Stat bonuses improvement: <color=green>{statGainString}</color>");
            HandleReply(ctx, $"Total change in growth rate including leveling prestige bonus: <color=yellow>{totalEffectString}</color>");
        }
    }
    public static bool CanPrestige(ulong steamId, PrestigeUtilities.PrestigeType parsedPrestigeType, int xpKey)
    {
        return xpKey >= PrestigeUtilities.PrestigeTypeToMaxLevel[parsedPrestigeType] &&
               Core.DataStructures.PlayerPrestiges.TryGetValue(steamId, out var prestigeData) &&
               prestigeData.TryGetValue(parsedPrestigeType, out var prestigeLevel) &&
               prestigeLevel < PrestigeUtilities.PrestigeTypeToMaxPrestigeLevel[parsedPrestigeType];
    }
    public static void PerformPrestige(ChatCommandContext ctx, ulong steamId, PrestigeUtilities.PrestigeType parsedPrestigeType, IPrestigeHandler handler)
    {
        handler.Prestige(steamId);
        handler.SaveChanges();

        var updatedPrestigeLevel = Core.DataStructures.PlayerPrestiges[steamId][parsedPrestigeType];
        if (parsedPrestigeType == PrestigeUtilities.PrestigeType.Experience)
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
                PlayerLevelingUtilities.HandleBloodBuff(buffEntity);
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
        GearOverride.SetLevel(ctx.Event.SenderCharacterEntity);
        ulong steamId = ctx.Event.User.PlatformId;

        List<int> buffs = Core.ParseConfigString(Plugin.PrestigeBuffs.Value);
        PrefabGUID buffPrefab = new(buffs[prestigeLevel-1]);
        if (!buffPrefab.GuidHash.Equals(0)) HandlePrestigeBuff(ctx.Event.SenderCharacterEntity, buffPrefab);

        if (RestedXP) PlayerLevelingUtilities.ResetRestedXP(steamId);

        float levelingReducer = LevelingPrestigeReducer * prestigeLevel;

        string reductionPercentage = (levelingReducer * 100).ToString("F2") + "%";

        float gainMultiplier = PrestigeRatesMultiplier * prestigeLevel;

        string gainPercentage = (gainMultiplier * 100).ToString("F2") + "%";
        HandleReply(ctx, $"You have prestiged in <color=#90EE90>Experience</color>[<color=white>{prestigeLevel}</color>]! Growth rates for all expertise/legacies increased by <color=green>{gainPercentage}</color>, growth rates for experience reduced by <color=yellow>{reductionPercentage}</color>");
    }
    static void HandleOtherPrestige(ChatCommandContext ctx, ulong steamId, PrestigeUtilities.PrestigeType parsedPrestigeType, int prestigeLevel)
    {
        int expPrestige = Core.DataStructures.PlayerPrestiges.TryGetValue(steamId, out var prestiges) && prestiges.TryGetValue(PrestigeUtilities.PrestigeType.Experience, out var xpLevel) ? xpLevel : 0;

        float ratesReduction = prestigeLevel * PrestigeRatesReducer; // Example: 0.1 (10%)
        float ratesMultiplier = expPrestige * PrestigeRatesMultiplier;

        float combinedFactor = ratesMultiplier - ratesReduction;

        string percentageReductionString = (ratesReduction * 100).ToString("F0") + "%";

        // Fixed additive stat gain increase based on base value
        float statGainIncrease = PrestigeStatMultiplier * prestigeLevel;
        string statGainString = (statGainIncrease * 100).ToString("F2") + "%";

        string totalEffectString = (combinedFactor * 100).ToString("F2") + "%";

        HandleReply(ctx, $"You have prestiged in <color=#90EE90>{parsedPrestigeType}</color>[<color=white>{prestigeLevel}</color>]! Growth rate reduced by <color=yellow>{percentageReductionString}</color> and stat bonuses improved by <color=green>{statGainString}</color>. The total change in growth rate with leveling prestige bonus is <color=yellow>{totalEffectString}</color>.");
    }
    public static void RemovePrestigeBuffs(Entity character, int prestigeLevel)
    {
        var buffs = Core.ParseConfigString(Plugin.PrestigeBuffs.Value);
        var buffSpawner = BuffUtility.BuffSpawner.Create(ServerGameManager);
        var entityCommandBuffer = EntityCommandBufferSystem.CreateCommandBuffer();

        for (int i = 0; i < prestigeLevel; i++)
        {
            RemoveBuff(character, buffs[i], buffSpawner, entityCommandBuffer);
        }
    }
    public static void ApplyPrestigeBuffs(Entity character, int prestigeLevel)
    {
        List<int> buffs = Core.ParseConfigString(Plugin.PrestigeBuffs.Value);
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
        float levelingReducer = LevelingPrestigeReducer * level;

        string reductionPercentage = (levelingReducer * 100).ToString("F2") + "%";

        float gainMultiplier = PrestigeRatesMultiplier * level;

        string gainPercentage = (gainMultiplier * 100).ToString("F2") + "%";
        //HandleReply(ctx, $"Player <color=green>{ctx.Name}</color> has prestiged in <color=#90EE90>Experience</color>[<color=white>{level}</color>]! Growth rates for all expertise/legacies increased by <color=green>{gainPercentage}</color>, growth rates for experience reduced by <color=yellow>{reductionPercentage}</color>");
        HandleServerReply(EntityManager, user, $"Player <color=green>{user.CharacterName.Value}</color> has prestiged in <color=#90EE90>Experience</color>[<color=white>{level}</color>]! Growth rates for all expertise/legacies increased by <color=green>{gainPercentage}</color>, growth rates for experience reduced by <color=yellow>{reductionPercentage}</color>");
    }
    public static void ApplyOtherPrestigeEffects(User user, ulong playerId, PrestigeUtilities.PrestigeType parsedPrestigeType, int level)
    {
        int expPrestige = Core.DataStructures.PlayerPrestiges.TryGetValue(playerId, out var prestiges) && prestiges.TryGetValue(PrestigeUtilities.PrestigeType.Experience, out var xpLevel) ? xpLevel : 0;

        float ratesReduction = level * PrestigeRatesReducer; // Example: 0.1 (10%)
        float ratesMultiplier = expPrestige * PrestigeRatesMultiplier;

        float combinedFactor = ratesMultiplier - ratesReduction;

        string percentageReductionString = (ratesReduction * 100).ToString("F0") + "%";

        // Fixed additive stat gain increase based on base value
        float statGainIncrease = PrestigeStatMultiplier * level;
        string statGainString = (statGainIncrease * 100).ToString("F2") + "%";

        string totalEffectString = (combinedFactor * 100).ToString("F2") + "%";

        //HandleReply(ctx, $"Player <color=green>{ctx.Name}</color> has prestiged in <color=#90EE90>{parsedPrestigeType}</color>[<color=white>{level}</color>]! Growth rate reduced by <color=yellow>{percentageReductionString}</color> and stat bonuses improved by <color=green>{statGainString}</color>. The total change in growth rate with leveling prestige bonus is <color=yellow>{totalEffectString}</color>.");
        HandleServerReply(EntityManager, user, $"Player <color=green>{user.CharacterName.Value}</color> has prestiged in <color=#90EE90>{parsedPrestigeType}</color>[<color=white>{level}</color>]! Growth rate reduced by <color=yellow>{percentageReductionString}</color> and stat bonuses improved by <color=green>{statGainString}</color>. The total change in growth rate with leveling prestige bonus is <color=yellow>{totalEffectString}</color>.");
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
               prestigeData.TryGetValue(PrestigeUtilities.PrestigeType.Experience, out var prestigeLevel) &&
               prestigeLevel > 0 ? prestigeLevel : 0;
    }
    public static bool TryParsePrestigeType(string prestigeType, out PrestigeUtilities.PrestigeType parsedPrestigeType)
    {
        // Attempt to parse the prestigeType string to the PrestigeType enum.
        if (Enum.TryParse(prestigeType, true, out parsedPrestigeType))
        {
            return true; // Successfully parsed
        }

        // If the initial parse failed, try to find a matching PrestigeType enum value containing the input string.
        parsedPrestigeType = Enum.GetValues(typeof(PrestigeUtilities.PrestigeType))
                                 .Cast<PrestigeUtilities.PrestigeType>()
                                 .FirstOrDefault(pt => pt.ToString().Contains(prestigeType, StringComparison.OrdinalIgnoreCase));

        // Check if a valid enum value was found that contains the input string.
        if (!parsedPrestigeType.Equals(default(PrestigeUtilities.PrestigeType)))
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
                PlayerName = PlayerService.PlayerCache.FirstOrDefault(pc => pc.Value.Read<User>().PlatformId == p.SteamId).Key,
                p.Prestige
            })
            .Where(p => !string.IsNullOrEmpty(p.PlayerName))
            .ToDictionary(p => p.PlayerName, p => p.Prestige);
    }
    public static void AdjustCharacterStats(Entity character, ulong platformId)
    {
        var prestigeData = Core.DataStructures.PlayerPrestiges[platformId];
        float damageTakenMultiplier = ExoPrestigeDamageTakenMultiplier * prestigeData[PrestigeUtilities.PrestigeType.Exo];
        float damageDealtMultiplier = ExoPrestigeDamageDealtMultiplier * prestigeData[PrestigeUtilities.PrestigeType.Exo];

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
