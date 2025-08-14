using Bloodcraft.Interfaces;
using Bloodcraft.Patches;
using Bloodcraft.Resources;
using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Utilities.Misc.PlayerBoolsManager;

namespace Bloodcraft.Systems.Leveling;
internal static class PrestigeManager
{
    static EntityManager EntityManager => Core.EntityManager;

    const int EXO_PRESTIGES = 100;

    static readonly SequenceGUID _prestigeSequence = SequenceGUIDs.SEQ_Vampire_LevelUp;

    public static readonly Dictionary<PrestigeType, Func<ulong, (bool Success, KeyValuePair<int, float> Data)>> TryGetExtensionMap = new()
    {
        { PrestigeType.Experience, steamId =>
            {
                if (steamId.TryGetPlayerExperience(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.Exo, steamId =>
            {
                if (steamId.TryGetPlayerExperience(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.SwordExpertise, steamId =>
            {
                if (steamId.TryGetPlayerSwordExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.AxeExpertise, steamId =>
            {
                if (steamId.TryGetPlayerAxeExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.MaceExpertise, steamId =>
            {
                if (steamId.TryGetPlayerMaceExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.SpearExpertise, steamId =>
            {
                if (steamId.TryGetPlayerSpearExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.CrossbowExpertise, steamId =>
            {
                if (steamId.TryGetPlayerCrossbowExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.GreatSwordExpertise, steamId =>
            {
                if (steamId.TryGetPlayerGreatSwordExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.SlashersExpertise, steamId =>
            {
                if (steamId.TryGetPlayerSlashersExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.PistolsExpertise, steamId =>
            {
                if (steamId.TryGetPlayerPistolsExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.ReaperExpertise, steamId =>
            {
                if (steamId.TryGetPlayerReaperExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.LongbowExpertise, steamId =>
            {
                if (steamId.TryGetPlayerLongbowExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.WhipExpertise, steamId =>
            {
                if (steamId.TryGetPlayerWhipExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.UnarmedExpertise, steamId =>
            {
                if (steamId.TryGetPlayerUnarmedExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.FishingPoleExpertise, steamId =>
            {
                if (steamId.TryGetPlayerFishingPoleExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.TwinBladesExpertise, steamId =>
            {
                if (steamId.TryGetPlayerTwinBladesExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.DaggersExpertise, steamId =>
            {
                if (steamId.TryGetPlayerDaggersExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.ClawsExpertise, steamId =>
            {
                if (steamId.TryGetPlayerClawsExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.WorkerLegacy, steamId =>
            {
                if (steamId.TryGetPlayerWorkerLegacy(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.WarriorLegacy, steamId =>
            {
                if (steamId.TryGetPlayerWarriorLegacy(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.ScholarLegacy, steamId =>
            {
                if (steamId.TryGetPlayerScholarLegacy(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.RogueLegacy, steamId =>
            {
                if (steamId.TryGetPlayerRogueLegacy(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.MutantLegacy, steamId =>
            {
                if (steamId.TryGetPlayerMutantLegacy(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.DraculinLegacy, steamId =>
            {
                if (steamId.TryGetPlayerDraculinLegacy(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.ImmortalLegacy, steamId =>
            {
                if (steamId.TryGetPlayerImmortalLegacy(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.CreatureLegacy, steamId =>
            {
                if (steamId.TryGetPlayerCreatureLegacy(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.BruteLegacy, steamId =>
            {
                if (steamId.TryGetPlayerBruteLegacy(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.CorruptionLegacy, steamId =>
            {
                if (steamId.TryGetPlayerCorruptionLegacy(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        }
    };

    public static readonly Dictionary<PrestigeType, Action<ulong, KeyValuePair<int, float>>> SetExtensionMap = new()
    {
        { PrestigeType.Experience, (steamId, data) => steamId.SetPlayerExperience(data) },
        { PrestigeType.Exo, (steamId, data) => steamId.SetPlayerExperience(data)},
        { PrestigeType.SwordExpertise, (steamId, data) => steamId.SetPlayerSwordExpertise(data) },
        { PrestigeType.AxeExpertise, (steamId, data) => steamId.SetPlayerAxeExpertise(data) },
        { PrestigeType.MaceExpertise, (steamId, data) => steamId.SetPlayerMaceExpertise(data) },
        { PrestigeType.SpearExpertise, (steamId, data) => steamId.SetPlayerSpearExpertise(data) },
        { PrestigeType.CrossbowExpertise, (steamId, data) => steamId.SetPlayerCrossbowExpertise(data) },
        { PrestigeType.GreatSwordExpertise, (steamId, data) => steamId.SetPlayerGreatSwordExpertise(data) },
        { PrestigeType.SlashersExpertise, (steamId, data) => steamId.SetPlayerSlashersExpertise(data) },
        { PrestigeType.PistolsExpertise, (steamId, data) => steamId.SetPlayerPistolsExpertise(data) },
        { PrestigeType.ReaperExpertise, (steamId, data) => steamId.SetPlayerReaperExpertise(data) },
        { PrestigeType.LongbowExpertise, (steamId, data) => steamId.SetPlayerLongbowExpertise(data) },
        { PrestigeType.WhipExpertise, (steamId, data) => steamId.SetPlayerWhipExpertise(data) },
        { PrestigeType.UnarmedExpertise, (steamId, data) => steamId.SetPlayerUnarmedExpertise(data) },
        { PrestigeType.FishingPoleExpertise, (steamId, data) => steamId.SetPlayerFishingPoleExpertise(data) },
        { PrestigeType.TwinBladesExpertise, (steamId, data) => steamId.SetPlayerTwinBladesExpertise(data) },
        { PrestigeType.DaggersExpertise, (steamId, data) => steamId.SetPlayerDaggersExpertise(data) },
        { PrestigeType.ClawsExpertise, (steamId, data) => steamId.SetPlayerClawsExpertise(data) },
        { PrestigeType.WorkerLegacy, (steamId, data) => steamId.SetPlayerWorkerLegacy(data) },
        { PrestigeType.WarriorLegacy, (steamId, data) => steamId.SetPlayerWarriorLegacy(data) },
        { PrestigeType.ScholarLegacy, (steamId, data) => steamId.SetPlayerScholarLegacy(data) },
        { PrestigeType.RogueLegacy, (steamId, data) => steamId.SetPlayerRogueLegacy(data) },
        { PrestigeType.MutantLegacy, (steamId, data) => steamId.SetPlayerRogueLegacy(data) },
        { PrestigeType.DraculinLegacy, (steamId, data) => steamId.SetPlayerDraculinLegacy(data) },
        { PrestigeType.ImmortalLegacy, (steamId, data) => steamId.SetPlayerImmortalLegacy(data) },
        { PrestigeType.CreatureLegacy, (steamId, data) => steamId.SetPlayerCreatureLegacy(data) },
        { PrestigeType.BruteLegacy, (steamId, data) => steamId.SetPlayerBruteLegacy(data) },
        { PrestigeType.CorruptionLegacy, (steamId, data) => steamId.SetPlayerCorruptionLegacy(data) }
    };

    public static readonly Dictionary<PrestigeType, int> PrestigeTypeToMaxLevel = new()
    {
        { PrestigeType.Experience, ConfigService.MaxLevel },
        { PrestigeType.Exo, ConfigService.MaxLevelingPrestiges },
        { PrestigeType.SwordExpertise, ConfigService.MaxExpertiseLevel },
        { PrestigeType.AxeExpertise, ConfigService.MaxExpertiseLevel },
        { PrestigeType.MaceExpertise, ConfigService.MaxExpertiseLevel },
        { PrestigeType.SpearExpertise, ConfigService.MaxExpertiseLevel },
        { PrestigeType.CrossbowExpertise, ConfigService.MaxExpertiseLevel },
        { PrestigeType.GreatSwordExpertise, ConfigService.MaxExpertiseLevel },
        { PrestigeType.SlashersExpertise, ConfigService.MaxExpertiseLevel },
        { PrestigeType.PistolsExpertise, ConfigService.MaxExpertiseLevel },
        { PrestigeType.ReaperExpertise, ConfigService.MaxExpertiseLevel },
        { PrestigeType.LongbowExpertise, ConfigService.MaxExpertiseLevel },
        { PrestigeType.WhipExpertise, ConfigService.MaxExpertiseLevel },
        { PrestigeType.UnarmedExpertise, ConfigService.MaxExpertiseLevel },
        { PrestigeType.FishingPoleExpertise, ConfigService.MaxExpertiseLevel },
        { PrestigeType.TwinBladesExpertise, ConfigService.MaxExpertiseLevel },
        { PrestigeType.DaggersExpertise, ConfigService.MaxExpertiseLevel },
        { PrestigeType.ClawsExpertise, ConfigService.MaxExpertiseLevel },
        { PrestigeType.WorkerLegacy, ConfigService.MaxBloodLevel },
        { PrestigeType.WarriorLegacy, ConfigService.MaxBloodLevel },
        { PrestigeType.ScholarLegacy, ConfigService.MaxBloodLevel },
        { PrestigeType.RogueLegacy, ConfigService.MaxBloodLevel },
        { PrestigeType.MutantLegacy, ConfigService.MaxBloodLevel },
        { PrestigeType.DraculinLegacy, ConfigService.MaxBloodLevel },
        { PrestigeType.ImmortalLegacy, ConfigService.MaxBloodLevel },
        { PrestigeType.CreatureLegacy, ConfigService.MaxBloodLevel },
        { PrestigeType.BruteLegacy, ConfigService.MaxBloodLevel },
        { PrestigeType.CorruptionLegacy, ConfigService.MaxBloodLevel }
    };

    public static readonly Dictionary<PrestigeType, int> PrestigeTypeToMaxPrestiges = new()
    {
        { PrestigeType.Experience, ConfigService.MaxLevelingPrestiges },
        { PrestigeType.Exo, EXO_PRESTIGES },
        { PrestigeType.SwordExpertise, ConfigService.MaxExpertisePrestiges },
        { PrestigeType.AxeExpertise, ConfigService.MaxExpertisePrestiges },
        { PrestigeType.MaceExpertise, ConfigService.MaxExpertisePrestiges },
        { PrestigeType.SpearExpertise, ConfigService.MaxExpertisePrestiges },
        { PrestigeType.CrossbowExpertise, ConfigService.MaxExpertisePrestiges },
        { PrestigeType.GreatSwordExpertise, ConfigService.MaxExpertisePrestiges },
        { PrestigeType.SlashersExpertise, ConfigService.MaxExpertisePrestiges },
        { PrestigeType.PistolsExpertise, ConfigService.MaxExpertisePrestiges },
        { PrestigeType.ReaperExpertise, ConfigService.MaxExpertisePrestiges },
        { PrestigeType.LongbowExpertise, ConfigService.MaxExpertisePrestiges },
        { PrestigeType.WhipExpertise, ConfigService.MaxExpertisePrestiges },
        { PrestigeType.UnarmedExpertise, ConfigService.MaxExpertisePrestiges },
        { PrestigeType.FishingPoleExpertise, ConfigService.MaxExpertisePrestiges },
        { PrestigeType.TwinBladesExpertise, ConfigService.MaxExpertisePrestiges },
        { PrestigeType.DaggersExpertise, ConfigService.MaxExpertisePrestiges },
        { PrestigeType.ClawsExpertise, ConfigService.MaxExpertisePrestiges },
        { PrestigeType.WorkerLegacy, ConfigService.MaxLegacyPrestiges },
        { PrestigeType.WarriorLegacy, ConfigService.MaxLegacyPrestiges },
        { PrestigeType.ScholarLegacy, ConfigService.MaxLegacyPrestiges },
        { PrestigeType.RogueLegacy, ConfigService.MaxLegacyPrestiges },
        { PrestigeType.MutantLegacy, ConfigService.MaxLegacyPrestiges },
        { PrestigeType.DraculinLegacy, ConfigService.MaxLegacyPrestiges },
        { PrestigeType.ImmortalLegacy, ConfigService.MaxLegacyPrestiges },
        { PrestigeType.CreatureLegacy, ConfigService.MaxLegacyPrestiges },
        { PrestigeType.BruteLegacy, ConfigService.MaxLegacyPrestiges },
        { PrestigeType.CorruptionLegacy, ConfigService.MaxLegacyPrestiges }
    };
    public static void DisplayPrestigeInfo(ChatCommandContext ctx, ulong steamId, PrestigeType parsedPrestigeType, int prestigeLevel, int maxPrestigeLevel)
    {
        float reductionFactor = 1.0f;
        float gainFactor = 1.0f;

        if (parsedPrestigeType == PrestigeType.Experience)
        {
            if (steamId.TryGetPlayerPrestiges(out var prestigeData) &&
                prestigeData.TryGetValue(PrestigeType.Experience, out var expPrestigeLevel) && expPrestigeLevel > 0)
            {
                // Apply flat rate reduction for leveling experience
                reductionFactor = ConfigService.LevelingPrestigeReducer * expPrestigeLevel;

                // Apply rate gain with linear increase for expertise/legacy
                gainFactor = ConfigService.PrestigeRateMultiplier * expPrestigeLevel;
            }

            string reductionPercentage = (reductionFactor * 100).ToString("F2") + "%";
            string gainPercentage = (gainFactor * 100).ToString("F2") + "%";

            ctx.Reply($"<color=#90EE90>{parsedPrestigeType}</color> Prestige Info:");
            ctx.Reply($"Current Prestige Level: <color=yellow>{prestigeLevel}</color>/{maxPrestigeLevel}");
            ctx.Reply($"Growth rate increase for expertise and legacies: <color=green>{gainPercentage}</color>");
            ctx.Reply($"Growth rate reduction for experience: <color=yellow>{reductionPercentage}</color>");

            if (prestigeData.TryGetValue(PrestigeType.Exo, out var exoData) && exoData > 0)
            {
                ctx.Reply($"Experience rate reduction for leveling no longer applies for exo prestiging.");
            }
        }
        else
        {
            if (steamId.TryGetPlayerPrestiges(out var prestigeData) &&
                prestigeData.TryGetValue(parsedPrestigeType, out var parsedPrestigeLevel) && parsedPrestigeLevel > 0
                && prestigeData.TryGetValue(PrestigeType.Experience, out var expPrestigeLevel) && expPrestigeLevel > 0)
            {
                // Apply flat rate reduction for leveling experience
                reductionFactor = ConfigService.PrestigeRatesReducer * parsedPrestigeLevel;

                // Apply rate gain with linear increase for expertise/legacy
                gainFactor = ConfigService.PrestigeRateMultiplier * expPrestigeLevel;
            }

            float combinedFactor = gainFactor - reductionFactor;
            string percentageReductionString = (reductionFactor * 100).ToString("F2") + "%";

            // Fixed additive stat gain increase based on base value
            float statGainIncrease = ConfigService.PrestigeStatMultiplier * prestigeLevel;
            string statGainString = (statGainIncrease * 100).ToString("F2") + "%";

            string totalEffectString = (combinedFactor >= 0 ? "+" : "-") + (combinedFactor * 100).ToString("F2") + "%";

            LocalizationService.HandleReply(ctx, $"<color=#90EE90>{parsedPrestigeType}</color> Prestige Info:");
            LocalizationService.HandleReply(ctx, $"Current Prestige Level: <color=yellow>{prestigeLevel}</color>/{maxPrestigeLevel}");
            LocalizationService.HandleReply(ctx, $"Growth rate reduction from <color=#90EE90>{parsedPrestigeType}</color> prestige level: <color=yellow>-{percentageReductionString}</color>");
            LocalizationService.HandleReply(ctx, $"Stat bonuses improvement: <color=green>{statGainString}</color>");
            LocalizationService.HandleReply(ctx, $"Total change in growth rate including leveling prestige bonus: <color=yellow>{totalEffectString}</color>");
        }
    }
    public static bool CanPrestige(ulong steamId, PrestigeType parsedPrestigeType, int xpKey)
    {
        return xpKey >= PrestigeTypeToMaxLevel[parsedPrestigeType] &&
               steamId.TryGetPlayerPrestiges(out var prestigeData) &&
               prestigeData.TryGetValue(parsedPrestigeType, out var prestigeLevel) &&
               prestigeLevel < PrestigeTypeToMaxPrestiges[parsedPrestigeType];
    }
    public static void PerformPrestige(ChatCommandContext ctx, ulong steamId, PrestigeType parsedPrestigeType, IPrestige handler, KeyValuePair<int, float> xpData)
    {
        if (!steamId.TryGetPlayerPrestiges(out var prestigeData)
            || !prestigeData.TryGetValue(parsedPrestigeType, out int updatedPrestigeLevel)) return;

        handler.DoPrestige(steamId);

        if (parsedPrestigeType == PrestigeType.Experience)
        {
            HandleExperiencePrestige(ctx, updatedPrestigeLevel, xpData);
            Progression.PlayerProgressionCacheManager.UpdatePlayerProgressionPrestige(steamId, true);
        }
        else
        {
            HandleOtherPrestige(ctx, steamId, parsedPrestigeType, updatedPrestigeLevel);
        }

        ctx.Event.SenderCharacterEntity.PlaySequence(_prestigeSequence);
    }
    static void HandleExperiencePrestige(ChatCommandContext ctx, int prestigeLevel, KeyValuePair<int, float> xpData)
    {
        Entity playerCharacter = ctx.Event.SenderCharacterEntity;
        ulong steamId = ctx.Event.User.PlatformId;

        LevelingSystem.SetLevel(playerCharacter);

        List<int> buffs = Configuration.ParseIntegersFromString(ConfigService.PrestigeBuffs);
        PrefabGUID buffPrefab = new(buffs[prestigeLevel - 1]);
        if (!buffPrefab.GuidHash.Equals(0)) Buffs.TryApplyPermanentBuff(playerCharacter, buffPrefab);

        if (ConfigService.RestedXPSystem) LevelingSystem.UpdateMaxRestedXP(steamId, xpData);

        float levelingReducer = ConfigService.LevelingPrestigeReducer * prestigeLevel;
        string reductionPercentage = (levelingReducer * 100).ToString("F2") + "%";

        float gainMultiplier = ConfigService.PrestigeRateMultiplier * prestigeLevel;
        string gainPercentage = (gainMultiplier * 100).ToString("F2") + "%";

        /*
        if (playerCharacter.TryApplyAndGetBuff(_experienceBuff, out Entity buffEntity))
        {
            buffEntity.With((ref LifeTime lifeTime) =>
            {
                lifeTime.Duration = 2f;
            });

            // if (buffEntity.Has<ModifyTargetHUDBuff>()) buffEntity.Remove<ModifyTargetHUDBuff>();
            if (buffEntity.Has<AbilityProjectileFanOnGameplayEvent_DataServer>()) buffEntity.Remove<AbilityProjectileFanOnGameplayEvent_DataServer>();
            // if (buffEntity.Has<BlockFeedBuff>()) buffEntity.Remove<BlockFeedBuff>();
        }
        */

        LocalizationService.HandleReply(ctx, $"You have prestiged in <color=#90EE90>Experience</color>[<color=white>{prestigeLevel}</color>]! Growth rates for all expertise/legacies increased by <color=green>{gainPercentage}</color>, experience from unit kills reduced by <color=red>{reductionPercentage}</color>.");
    }
    static void HandleOtherPrestige(ChatCommandContext ctx, ulong steamId, PrestigeType parsedPrestigeType, int prestigeLevel)
    {
        Entity playerCharacter = ctx.Event.SenderCharacterEntity;

        int expPrestige = steamId.TryGetPlayerPrestiges(out var prestiges) && prestiges.TryGetValue(PrestigeType.Experience, out var xpLevel) ? xpLevel : 0;

        float ratesReduction = prestigeLevel * ConfigService.PrestigeRatesReducer;
        float ratesMultiplier = expPrestige * ConfigService.PrestigeRateMultiplier;

        float combinedFactor = ratesMultiplier - ratesReduction;

        string percentageReductionString = (ratesReduction * 100).ToString("F2") + "%";

        // Fixed additive stat gain increase based on base value
        float statGainIncrease = ConfigService.PrestigeStatMultiplier * prestigeLevel;
        string statGainString = (statGainIncrease * 100).ToString("F2") + "%";

        string totalEffectString = (combinedFactor * 100).ToString("F2") + "%";

        /*
        if (parsedPrestigeType.ToString().Contains("Legacy"))
        {
            Buffs.TryApplyBuff(playerCharacter, _legacyBuff);
        }
        else if (parsedPrestigeType.ToString().Contains("Expertise"))
        {
            if (playerCharacter.TryApplyAndGetBuff(_expertiseBuff, out Entity buffEntity))
            {
                buffEntity.With((ref LifeTime lifeTime) =>
                {
                    lifeTime.Duration = 2f;
                });
            }
        }
        */

        LocalizationService.HandleReply(ctx, $"<color=#90EE90>{parsedPrestigeType}</color>[<color=white>{prestigeLevel}</color>] prestiged successfully! Growth rate reduced by <color=red>{percentageReductionString}</color> and stat bonuses improved by <color=green>{statGainString}</color>. The total change in growth rate with leveling prestige bonus is <color=yellow>{totalEffectString}</color>.");
    }
    public static void RemovePrestigeBuffs(Entity character)
    {
        List<PrefabGUID> prestigeBuffs = UpdateBuffsBufferDestroyPatch.PrestigeBuffs;

        foreach (PrefabGUID buffPrefab in prestigeBuffs)
        {
            character.TryRemoveBuff(buffPrefabGuid: buffPrefab);
        }
    }
    public static void ApplyPrestigeBuffs(Entity character, int prestigeLevel)
    {
        List<PrefabGUID> prestigeBuffs = UpdateBuffsBufferDestroyPatch.PrestigeBuffs;

        for (int i = 0; i < prestigeLevel; i++)
        {
            Buffs.TryApplyPermanentBuff(character, prestigeBuffs[i]);
        }
    }
    public static void ReplyExperiencePrestigeEffects(User user, int level)
    {
        float levelingReducer = ConfigService.LevelingPrestigeReducer * level;

        string reductionPercentage = (levelingReducer * 100).ToString("F2") + "%";
        float gainMultiplier = ConfigService.PrestigeRateMultiplier * level;

        string gainPercentage = (gainMultiplier * 100).ToString("F2") + "%";
        LocalizationService.HandleServerReply(EntityManager, user, $"Player <color=green>{user.CharacterName.Value}</color> has prestiged in <color=#90EE90>Experience</color>[<color=white>{level}</color>]! Growth rates for expertise/legacies increased by <color=green>{gainPercentage}</color>, experience from unit kills reduced by <color=red>{reductionPercentage}</color>.");
    }
    public static void ReplyOtherPrestigeEffects(User user, ulong playerId, PrestigeType parsedPrestigeType, int level)
    {
        int expPrestige = playerId.TryGetPlayerPrestiges(out var prestiges) && prestiges.TryGetValue(PrestigeType.Experience, out var xpLevel) ? xpLevel : 0;

        float ratesReduction = level * ConfigService.PrestigeRatesReducer;
        float ratesMultiplier = expPrestige * ConfigService.PrestigeRateMultiplier;

        float combinedFactor = ratesMultiplier - ratesReduction;

        string percentageReductionString = (ratesReduction * 100).ToString("F0") + "%";

        float statGainIncrease = ConfigService.PrestigeStatMultiplier * level;
        string statGainString = (statGainIncrease * 100).ToString("F2") + "%";

        string totalEffectString = (combinedFactor * 100).ToString("F2") + "%";
        LocalizationService.HandleServerReply(EntityManager, user, $"Player <color=green>{user.CharacterName.Value}</color> has prestiged in <color=#90EE90>{parsedPrestigeType}</color>[<color=white>{level}</color>]! Growth rate reduced by <color=yellow>{percentageReductionString}</color> and stat bonuses improved by <color=green>{statGainString}</color>. The total change in growth rate with leveling prestige bonus is <color=yellow>{totalEffectString}</color>.");
    }
    public static bool TryParsePrestigeType(string prestigeType, out PrestigeType parsedPrestigeType)
    {
        if (Enum.TryParse(prestigeType, true, out parsedPrestigeType))
        {
            return true; // Successfully parsed
        }

        parsedPrestigeType = Enum.GetValues(typeof(PrestigeType))
                                 .Cast<PrestigeType>()
                                 .FirstOrDefault(pt => pt.ToString().Contains(prestigeType, StringComparison.InvariantCultureIgnoreCase));

        if (!parsedPrestigeType.Equals(default(PrestigeType)))
        {
            return true; // Found a matching enum value
        }

        parsedPrestigeType = default;
        return false;
    }
    public static Dictionary<string, int> GetPrestigeForType(PrestigeType prestigeType)
    {
        return DataService.PlayerDictionaries._playerPrestiges
            .Where(p => p.Value.ContainsKey(prestigeType) && !DataService.PlayerDictionaries._ignorePrestigeLeaderboard.Contains(p.Key))
            .Select(p => new
            {
                steamId = p.Key,
                Prestige = p.Value[prestigeType]
            })
            .Select(p => new
            {
                PlayerName = SteamIdPlayerInfoCache.FirstOrDefault(pc => pc.Key == p.steamId).Value.User.CharacterName.Value,
                p.Prestige
            })
            .Where(p => !string.IsNullOrEmpty(p.PlayerName))
            .ToDictionary(p => p.PlayerName, p => p.Prestige);
    }
    public static void GlobalPurgePrestigeBuffs(ChatCommandContext ctx)
    {
        List<PlayerInfo> playerCache = [..SteamIdPlayerInfoCache.Values];

        foreach (PlayerInfo playerInfo in playerCache)
        {
            ulong steamId = playerInfo.User.PlatformId;
            SetPlayerBool(steamId, PRESTIGE_BUFFS_KEY, false);

            RemovePrestigeBuffs(playerInfo.CharEntity);
        }

        ctx.Reply("Removed prestige buffs from all players.");
    }
    public static bool HasPrestiged(ulong steamId)
    {
        return steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(PrestigeType.Experience, out int prestiges) && prestiges > 0;
    }
    public static bool HasExoPrestiged(ulong steamId)
    {
        return steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(PrestigeType.Exo, out int prestiges) && prestiges > 0;
    }
}
