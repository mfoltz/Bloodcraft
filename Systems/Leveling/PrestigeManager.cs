using Bloodcraft.Interfaces;
using Bloodcraft.Patches;
using Bloodcraft.Resources;
using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using ProjectM.Gameplay.Scripting;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Utilities.Misc.PlayerBools;

namespace Bloodcraft.Systems.Leveling;
internal static class PrestigeManager
{
    static EntityManager EntityManager => Core.EntityManager;

    const int EXO_PRESTIGES = 100;

    static readonly SequenceGUID _prestigeEffect = SequenceGUIDs.SEQ_Vampire_LevelUp;

    public static readonly Dictionary<PrestigeType, Func<ulong, (bool Success, KeyValuePair<int, float> Data)>> TryGetExtensionMap = new()
    {
        { PrestigeType.Experience, steamID =>
            {
                if (steamID.TryGetPlayerExperience(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.Exo, steamID =>
            {
                if (steamID.TryGetPlayerExperience(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.SwordExpertise, steamID =>
            {
                if (steamID.TryGetPlayerSwordExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.AxeExpertise, steamID =>
            {
                if (steamID.TryGetPlayerAxeExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.MaceExpertise, steamID =>
            {
                if (steamID.TryGetPlayerMaceExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.SpearExpertise, steamID =>
            {
                if (steamID.TryGetPlayerSpearExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.CrossbowExpertise, steamID =>
            {
                if (steamID.TryGetPlayerCrossbowExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.GreatSwordExpertise, steamID =>
            {
                if (steamID.TryGetPlayerGreatSwordExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.SlashersExpertise, steamID =>
            {
                if (steamID.TryGetPlayerSlashersExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.PistolsExpertise, steamID =>
            {
                if (steamID.TryGetPlayerPistolsExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.ReaperExpertise, steamID =>
            {
                if (steamID.TryGetPlayerReaperExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.LongbowExpertise, steamID =>
            {
                if (steamID.TryGetPlayerLongbowExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.WhipExpertise, steamID =>
            {
                if (steamID.TryGetPlayerWhipExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.UnarmedExpertise, steamID =>
            {
                if (steamID.TryGetPlayerUnarmedExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.FishingPoleExpertise, steamID =>
            {
                if (steamID.TryGetPlayerFishingPoleExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.TwinBladesExpertise, steamID =>
            {
                if (steamID.TryGetPlayerTwinBladesExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.DaggersExpertise, steamID =>
            {
                if (steamID.TryGetPlayerDaggersExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.ClawsExpertise, steamID =>
            {
                if (steamID.TryGetPlayerClawsExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.WorkerLegacy, steamID =>
            {
                if (steamID.TryGetPlayerWorkerLegacy(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.WarriorLegacy, steamID =>
            {
                if (steamID.TryGetPlayerWarriorLegacy(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.ScholarLegacy, steamID =>
            {
                if (steamID.TryGetPlayerScholarLegacy(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.RogueLegacy, steamID =>
            {
                if (steamID.TryGetPlayerRogueLegacy(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.MutantLegacy, steamID =>
            {
                if (steamID.TryGetPlayerMutantLegacy(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.DraculinLegacy, steamID =>
            {
                if (steamID.TryGetPlayerDraculinLegacy(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.ImmortalLegacy, steamID =>
            {
                if (steamID.TryGetPlayerImmortalLegacy(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.CreatureLegacy, steamID =>
            {
                if (steamID.TryGetPlayerCreatureLegacy(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.BruteLegacy, steamID =>
            {
                if (steamID.TryGetPlayerBruteLegacy(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { PrestigeType.CorruptionLegacy, steamID =>
            {
                if (steamID.TryGetPlayerCorruptionLegacy(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        }
    };

    public static readonly Dictionary<PrestigeType, Action<ulong, KeyValuePair<int, float>>> SetExtensionMap = new()
    {
        { PrestigeType.Experience, (steamID, data) => steamID.SetPlayerExperience(data) },
        { PrestigeType.Exo, (steamID, data) => steamID.SetPlayerExperience(data)},
        { PrestigeType.SwordExpertise, (steamID, data) => steamID.SetPlayerSwordExpertise(data) },
        { PrestigeType.AxeExpertise, (steamID, data) => steamID.SetPlayerAxeExpertise(data) },
        { PrestigeType.MaceExpertise, (steamID, data) => steamID.SetPlayerMaceExpertise(data) },
        { PrestigeType.SpearExpertise, (steamID, data) => steamID.SetPlayerSpearExpertise(data) },
        { PrestigeType.CrossbowExpertise, (steamID, data) => steamID.SetPlayerCrossbowExpertise(data) },
        { PrestigeType.GreatSwordExpertise, (steamID, data) => steamID.SetPlayerGreatSwordExpertise(data) },
        { PrestigeType.SlashersExpertise, (steamID, data) => steamID.SetPlayerSlashersExpertise(data) },
        { PrestigeType.PistolsExpertise, (steamID, data) => steamID.SetPlayerPistolsExpertise(data) },
        { PrestigeType.ReaperExpertise, (steamID, data) => steamID.SetPlayerReaperExpertise(data) },
        { PrestigeType.LongbowExpertise, (steamID, data) => steamID.SetPlayerLongbowExpertise(data) },
        { PrestigeType.WhipExpertise, (steamID, data) => steamID.SetPlayerWhipExpertise(data) },
        { PrestigeType.UnarmedExpertise, (steamID, data) => steamID.SetPlayerUnarmedExpertise(data) },
        { PrestigeType.FishingPoleExpertise, (steamID, data) => steamID.SetPlayerFishingPoleExpertise(data) },
        { PrestigeType.TwinBladesExpertise, (steamID, data) => steamID.SetPlayerTwinBladesExpertise(data) },
        { PrestigeType.DaggersExpertise, (steamID, data) => steamID.SetPlayerDaggersExpertise(data) },
        { PrestigeType.ClawsExpertise, (steamID, data) => steamID.SetPlayerClawsExpertise(data) },
        { PrestigeType.WorkerLegacy, (steamID, data) => steamID.SetPlayerWorkerLegacy(data) },
        { PrestigeType.WarriorLegacy, (steamID, data) => steamID.SetPlayerWarriorLegacy(data) },
        { PrestigeType.ScholarLegacy, (steamID, data) => steamID.SetPlayerScholarLegacy(data) },
        { PrestigeType.RogueLegacy, (steamID, data) => steamID.SetPlayerRogueLegacy(data) },
        { PrestigeType.MutantLegacy, (steamID, data) => steamID.SetPlayerRogueLegacy(data) },
        { PrestigeType.DraculinLegacy, (steamID, data) => steamID.SetPlayerDraculinLegacy(data) },
        { PrestigeType.ImmortalLegacy, (steamID, data) => steamID.SetPlayerImmortalLegacy(data) },
        { PrestigeType.CreatureLegacy, (steamID, data) => steamID.SetPlayerCreatureLegacy(data) },
        { PrestigeType.BruteLegacy, (steamID, data) => steamID.SetPlayerBruteLegacy(data) },
        { PrestigeType.CorruptionLegacy, (steamID, data) => steamID.SetPlayerCorruptionLegacy(data) }
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

            LocalizationService.Reply(ctx, $"<color=#90EE90>{parsedPrestigeType}</color> Prestige Info:");
            LocalizationService.Reply(ctx, $"Current Prestige Level: <color=yellow>{prestigeLevel}</color>/{maxPrestigeLevel}");
            LocalizationService.Reply(ctx, $"Growth rate reduction from <color=#90EE90>{parsedPrestigeType}</color> prestige level: <color=yellow>-{percentageReductionString}</color>");
            LocalizationService.Reply(ctx, $"Stat bonuses improvement: <color=green>{statGainString}</color>");
            LocalizationService.Reply(ctx, $"Total change in growth rate including leveling prestige bonus: <color=yellow>{totalEffectString}</color>");
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

        ctx.Event.SenderCharacterEntity.PlaySequence(_prestigeEffect);
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

        LocalizationService.Reply(ctx, $"You have prestiged in <color=#90EE90>Experience</color>[<color=white>{prestigeLevel}</color>]! Growth rates for all expertise/legacies increased by <color=green>{gainPercentage}</color>, experience from unit kills reduced by <color=red>{reductionPercentage}</color>.");
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

        LocalizationService.Reply(ctx, $"<color=#90EE90>{parsedPrestigeType}</color>[<color=white>{prestigeLevel}</color>] prestiged successfully! Growth rate reduced by <color=red>{percentageReductionString}</color> and stat bonuses improved by <color=green>{statGainString}</color>. The total change in growth rate with leveling prestige bonus is <color=yellow>{totalEffectString}</color>.");
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
                                 .FirstOrDefault(pt => pt.ToString().Contains(prestigeType, StringComparison.OrdinalIgnoreCase));

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
            .Where(p => p.Value.ContainsKey(prestigeType))
            .Where(p => !DataService.PlayerDictionaries._ignorePrestigeLeaderboard.Contains(p.Key))
            .Select(p => new
            {
                SteamId = p.Key,
                Prestige = p.Value[prestigeType]
            })
            .Select(p => new
            {
                PlayerName = SteamIdPlayerInfoCache.FirstOrDefault(pc => pc.Key == p.SteamId).Value.User.CharacterName.Value,
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
