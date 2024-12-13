using Bloodcraft.Services;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Legacies;
using Bloodcraft.Utilities;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;

namespace Bloodcraft.Systems.Leveling;
internal static class PrestigeSystem
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly PrefabGUID ExperienceVisualBuff = new(104224016);
    static readonly PrefabGUID ExpertiseVisualBuff = new(620130895);
    static readonly PrefabGUID LegacyVisualBuff = new(-1381763893);

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
        { PrestigeType.WorkerLegacy, (steamID, data) => steamID.SetPlayerWorkerLegacy(data) },
        { PrestigeType.WarriorLegacy, (steamID, data) => steamID.SetPlayerWarriorLegacy(data) },
        { PrestigeType.ScholarLegacy, (steamID, data) => steamID.SetPlayerScholarLegacy(data) },
        { PrestigeType.RogueLegacy, (steamID, data) => steamID.SetPlayerRogueLegacy(data) },
        { PrestigeType.MutantLegacy, (steamID, data) => steamID.SetPlayerRogueLegacy(data) },
        { PrestigeType.DraculinLegacy, (steamID, data) => steamID.SetPlayerDraculinLegacy(data) },
        { PrestigeType.ImmortalLegacy, (steamID, data) => steamID.SetPlayerImmortalLegacy(data) },
        { PrestigeType.CreatureLegacy, (steamID, data) => steamID.SetPlayerCreatureLegacy(data) },
        { PrestigeType.BruteLegacy, (steamID, data) => steamID.SetPlayerBruteLegacy(data) }
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
        { PrestigeType.WorkerLegacy, ConfigService.MaxBloodLevel },
        { PrestigeType.WarriorLegacy, ConfigService.MaxBloodLevel },
        { PrestigeType.ScholarLegacy, ConfigService.MaxBloodLevel },
        { PrestigeType.RogueLegacy, ConfigService.MaxBloodLevel },
        { PrestigeType.MutantLegacy, ConfigService.MaxBloodLevel },
        { PrestigeType.DraculinLegacy, ConfigService.MaxBloodLevel },
        { PrestigeType.ImmortalLegacy, ConfigService.MaxBloodLevel },
        { PrestigeType.CreatureLegacy, ConfigService.MaxBloodLevel },
        { PrestigeType.BruteLegacy, ConfigService.MaxBloodLevel }
    };

    public static readonly Dictionary<PrestigeType, int> PrestigeTypeToMaxPrestiges = new()
    {
        { PrestigeType.Experience, ConfigService.MaxLevelingPrestiges },
        { PrestigeType.Exo, ConfigService.ExoPrestiges },
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
        { PrestigeType.WorkerLegacy, ConfigService.MaxLegacyPrestiges },
        { PrestigeType.WarriorLegacy, ConfigService.MaxLegacyPrestiges },
        { PrestigeType.ScholarLegacy, ConfigService.MaxLegacyPrestiges },
        { PrestigeType.RogueLegacy, ConfigService.MaxLegacyPrestiges },
        { PrestigeType.MutantLegacy, ConfigService.MaxLegacyPrestiges },
        { PrestigeType.DraculinLegacy, ConfigService.MaxLegacyPrestiges },
        { PrestigeType.ImmortalLegacy, ConfigService.MaxLegacyPrestiges },
        { PrestigeType.CreatureLegacy, ConfigService.MaxLegacyPrestiges },
        { PrestigeType.BruteLegacy, ConfigService.MaxLegacyPrestiges }
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
            ctx.Reply($"Growth rate improvement for expertise and legacies: <color=green>{gainPercentage}</color>");
            ctx.Reply($"Growth rate reduction for experience: <color=yellow>{reductionPercentage}</color>");
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
            LocalizationService.HandleReply(ctx, $"Growth rate reduction from <color=#90EE90>{parsedPrestigeType}</color> prestige level: <color=yellow>{percentageReductionString}</color>");
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
    public static void PerformPrestige(ChatCommandContext ctx, ulong steamId, PrestigeType parsedPrestigeType, IPrestigeHandler handler, KeyValuePair<int, float> xpData)
    {
        handler.Prestige(steamId);

        if (!steamId.TryGetPlayerPrestiges(out var prestigeData)) return;
        int updatedPrestigeLevel = prestigeData[parsedPrestigeType];
        if (parsedPrestigeType == PrestigeType.Experience)
        {
            HandleExperiencePrestige(ctx, updatedPrestigeLevel, xpData);
        }
        else
        {
            HandleOtherPrestige(ctx, steamId, parsedPrestigeType, updatedPrestigeLevel);
        }
    }
    static void HandleExperiencePrestige(ChatCommandContext ctx, int prestigeLevel, KeyValuePair<int, float> xpData)
    {
        LevelingSystem.SetLevel(ctx.Event.SenderCharacterEntity);
        ulong steamId = ctx.Event.User.PlatformId;

        List<int> buffs = Configuration.ParseConfigIntegerString(ConfigService.PrestigeBuffs);
        PrefabGUID buffPrefab = new(buffs[prestigeLevel - 1]);
        if (!buffPrefab.GuidHash.Equals(0)) Buffs.ApplyPermanentBuff(ctx.Event.SenderCharacterEntity, buffPrefab);

        if (ConfigService.RestedXPSystem) LevelingSystem.UpdateMaxRestedXP(steamId, xpData);

        float levelingReducer = ConfigService.LevelingPrestigeReducer * prestigeLevel;
        string reductionPercentage = (levelingReducer * 100).ToString("F2") + "%";

        float gainMultiplier = ConfigService.PrestigeRateMultiplier * prestigeLevel;
        string gainPercentage = (gainMultiplier * 100).ToString("F2") + "%";

        Buffs.TryApplyBuff(ctx.Event.SenderCharacterEntity, ExperienceVisualBuff);
        if (ctx.Event.SenderCharacterEntity.TryGetBuff(ExperienceVisualBuff, out Entity buffEntity))
        {
            if (!buffEntity.Has<LifeTime>())
            {
                buffEntity.Add<LifeTime>();
                buffEntity.Write(new LifeTime { Duration = 3f, EndAction = LifeTimeEndAction.Destroy });
            }
            if (buffEntity.Has<ServerControlsPositionBuff>()) buffEntity.Remove<ServerControlsPositionBuff>();
            if (buffEntity.Has<BuffModificationFlagData>()) buffEntity.Remove<BuffModificationFlagData>();
            if (buffEntity.Has<BlockFeedBuff>()) buffEntity.Remove<BlockFeedBuff>();
        }

        LocalizationService.HandleReply(ctx, $"You have prestiged in <color=#90EE90>Experience</color>[<color=white>{prestigeLevel}</color>]! Growth rates for all expertise/legacies increased by <color=green>{gainPercentage}</color>, growth rates for experience from unit kills reduced by <color=yellow>{reductionPercentage}</color>");
    }
    static void HandleOtherPrestige(ChatCommandContext ctx, ulong steamId, PrestigeType parsedPrestigeType, int prestigeLevel)
    {
        int expPrestige = steamId.TryGetPlayerPrestiges(out var prestiges) && prestiges.TryGetValue(PrestigeType.Experience, out var xpLevel) ? xpLevel : 0;

        float ratesReduction = prestigeLevel * ConfigService.PrestigeRatesReducer; // Example: 0.1 (10%)
        float ratesMultiplier = expPrestige * ConfigService.PrestigeRateMultiplier;

        float combinedFactor = ratesMultiplier - ratesReduction;

        string percentageReductionString = (ratesReduction * 100).ToString("F0") + "%";

        // Fixed additive stat gain increase based on base value
        float statGainIncrease = ConfigService.PrestigeStatMultiplier * prestigeLevel;
        string statGainString = (statGainIncrease * 100).ToString("F2") + "%";

        string totalEffectString = (combinedFactor * 100).ToString("F2") + "%";

        if (BloodSystem.BloodTypeToPrestigeMap.ContainsValue(parsedPrestigeType))
        {
            Buffs.TryApplyBuff(ctx.Event.SenderCharacterEntity, LegacyVisualBuff);
        }
        else if (WeaponSystem.WeaponPrestigeMap.ContainsValue(parsedPrestigeType))
        {
            Buffs.TryApplyBuff(ctx.Event.SenderCharacterEntity, ExpertiseVisualBuff);
        }

        LocalizationService.HandleReply(ctx, $"You have prestiged in <color=#90EE90>{parsedPrestigeType}</color>[<color=white>{prestigeLevel}</color>]! Growth rate reduced by <color=yellow>{percentageReductionString}</color> and stat bonuses improved by <color=green>{statGainString}</color>. The total change in growth rate with leveling prestige bonus is <color=yellow>{totalEffectString}</color>.");
    }
    public static void RemovePrestigeBuffs(Entity character, int prestigeLevel)
    {
        var buffs = Configuration.ParseConfigIntegerString(ConfigService.PrestigeBuffs);

        for (int i = 0; i < buffs.Count; i++)
        {
            RemoveBuff(character, buffs[i]);
        }
    }
    public static void ApplyPrestigeBuffs(Entity character, int prestigeLevel)
    {
        List<int> buffs = Configuration.ParseConfigIntegerString(ConfigService.PrestigeBuffs);
        if (buffs.Count == 0) return;

        for (int i = 0; i < prestigeLevel; i++)
        {
            PrefabGUID buffPrefab = new(buffs[i]);

            if (buffPrefab.GuidHash == 0) continue;
            else Buffs.ApplyPermanentBuff(character, buffPrefab);
        }
    }
    public static void ReplyExperiencePrestigeEffects(User user, int level)
    {
        float levelingReducer = ConfigService.LevelingPrestigeReducer * level;

        string reductionPercentage = (levelingReducer * 100).ToString("F2") + "%";

        float gainMultiplier = ConfigService.PrestigeRateMultiplier * level;

        string gainPercentage = (gainMultiplier * 100).ToString("F2") + "%";
        LocalizationService.HandleServerReply(EntityManager, user, $"Player <color=green>{user.CharacterName.Value}</color> has prestiged in <color=#90EE90>Experience</color>[<color=white>{level}</color>]! Growth rates for all expertise/legacies increased by <color=green>{gainPercentage}</color>, growth rates for experience reduced by <color=yellow>{reductionPercentage}</color>");
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
    static void RemoveBuff(Entity character, int buffId)
    {
        var buffPrefab = new PrefabGUID(buffId);

        if (ServerGameManager.TryGetBuff(character, buffPrefab.ToIdentifier(), out Entity buffEntity))
        {
            //Core.Log.LogInfo($"Removing buff {buffPrefab.LookupName()}...");
            DestroyUtility.Destroy(EntityManager, buffEntity, DestroyDebugReason.TryRemoveBuff);
        }
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
        return DataService.PlayerDictionaries.playerPrestiges
            .Where(p => p.Value.ContainsKey(prestigeType))
            .Select(p => new
            {
                SteamId = p.Key,
                Prestige = p.Value[prestigeType]
            })
            .Select(p => new
            {
                PlayerName = PlayerService.PlayerCache.FirstOrDefault(pc => pc.Key == p.SteamId).Value.User.CharacterName.Value,
                p.Prestige
            })
            .Where(p => !string.IsNullOrEmpty(p.PlayerName))
            .ToDictionary(p => p.PlayerName, p => p.Prestige);
    }
    public static void ResetDamageResistCategoryStats(Entity character)
    {
        AdjustResistStats(character);
        AdjustDamageStats(character);
    }
    static void AdjustResistStats(Entity character)
    {
        ResistCategoryStats resistCategoryStats = character.Read<ResistCategoryStats>();

        resistCategoryStats.ResistVsBeasts._Value = 0;
        resistCategoryStats.ResistVsHumans._Value = 0;
        resistCategoryStats.ResistVsUndeads._Value = 0;
        resistCategoryStats.ResistVsDemons._Value = 0;
        resistCategoryStats.ResistVsMechanical._Value = 0;
        resistCategoryStats.ResistVsVampires._Value = 0;

        character.Write(resistCategoryStats);
    }
    static void AdjustDamageStats(Entity character)
    {
        DamageCategoryStats damageCategoryStats = character.Read<DamageCategoryStats>();

        damageCategoryStats.DamageVsBeasts._Value = 1;
        damageCategoryStats.DamageVsHumans._Value = 1;
        damageCategoryStats.DamageVsUndeads._Value = 1;
        damageCategoryStats.DamageVsDemons._Value = 1;
        damageCategoryStats.DamageVsMechanical._Value = 1;
        damageCategoryStats.DamageVsVampires._Value = 1;

        character.Write(damageCategoryStats);
    }
}
