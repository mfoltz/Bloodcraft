using Bloodcraft.Interfaces;
using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using System.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static Bloodcraft.Patches.DeathEventListenerSystemPatch;
using static Bloodcraft.Utilities.Misc.PlayerBoolsManager;
using static Bloodcraft.Utilities.Progression;
using WeaponType = Bloodcraft.Interfaces.WeaponType;

namespace Bloodcraft.Systems.Expertise;
internal static class WeaponSystem
{
    static EntityManager EntityManager => Core.EntityManager;
    static SystemService SystemService => Core.SystemService;
    static EndSimulationEntityCommandBufferSystem EndSimulationEntityCommandBufferSystem => SystemService.EndSimulationEntityCommandBufferSystem;

    static readonly int _maxExpertiseLevel = ConfigService.MaxExpertiseLevel;
    static readonly int _expertiseStatChoices = ConfigService.ExpertiseStatChoices;

    static readonly float _unitExpertiseMultiplier = ConfigService.UnitExpertiseMultiplier;
    static readonly float _vBloodExpertiseMultiplier = ConfigService.VBloodExpertiseMultiplier;
    static readonly float _prestigeRatesReducer = ConfigService.PrestigeRatesReducer;
    static readonly float _prestigeRateMultiplier = ConfigService.PrestigeRateMultiplier;
    static readonly float _unitSpawnerExpertiseFactor = ConfigService.UnitSpawnerExpertiseFactor;

    static readonly WaitForSeconds _delay = new(1f);
    const float DELAY_ADD = 1.25f;

    static readonly float3 _grey = new(0.75f, 0.75f, 0.75f); // Soft Silver Grey
    static readonly AssetGuid _experienceAssetGuid = AssetGuid.FromString("4210316d-23d4-4274-96f5-d6f0944bd0bb");
    static readonly PrefabGUID _sctGeneric = new(-1687715009);

    public static readonly Dictionary<WeaponType, Func<ulong, (bool Success, KeyValuePair<int, float> Data)>> TryGetExtensionMap = new()
    {
        { WeaponType.Sword, steamID =>
            {
                if (steamID.TryGetPlayerSwordExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { WeaponType.Axe, steamID =>
            {
                if (steamID.TryGetPlayerAxeExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { WeaponType.Mace, steamID =>
            {
                if (steamID.TryGetPlayerMaceExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { WeaponType.Spear, steamID =>
            {
                if (steamID.TryGetPlayerSpearExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { WeaponType.Crossbow, steamID =>
            {
                if (steamID.TryGetPlayerCrossbowExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { WeaponType.GreatSword, steamID =>
            {
                if (steamID.TryGetPlayerGreatSwordExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { WeaponType.Slashers, steamID =>
            {
                if (steamID.TryGetPlayerSlashersExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { WeaponType.Pistols, steamID =>
            {
                if (steamID.TryGetPlayerPistolsExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { WeaponType.Reaper, steamID =>
            {
                if (steamID.TryGetPlayerReaperExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { WeaponType.Longbow, steamID =>
            {
                if (steamID.TryGetPlayerLongbowExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { WeaponType.Whip, steamID =>
            {
                if (steamID.TryGetPlayerWhipExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { WeaponType.Unarmed, steamID =>
            {
                if (steamID.TryGetPlayerUnarmedExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { WeaponType.FishingPole, steamID =>
            {
                if (steamID.TryGetPlayerFishingPoleExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { WeaponType.TwinBlades, steamID =>
            {
                if (steamID.TryGetPlayerTwinBladesExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { WeaponType.Daggers, steamID =>
            {
                if (steamID.TryGetPlayerDaggersExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { WeaponType.Claws, steamID =>
            {
                if (steamID.TryGetPlayerClawsExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        }
    };
    public static readonly Dictionary<WeaponType, Action<ulong, KeyValuePair<int, float>>> SetExtensionMap = new()
    {
        { WeaponType.Sword, (steamID, data) => steamID.SetPlayerSwordExpertise(data) },
        { WeaponType.Axe, (steamID, data) => steamID.SetPlayerAxeExpertise(data) },
        { WeaponType.Mace, (steamID, data) => steamID.SetPlayerMaceExpertise(data) },
        { WeaponType.Spear, (steamID, data) => steamID.SetPlayerSpearExpertise(data) },
        { WeaponType.Crossbow, (steamID, data) => steamID.SetPlayerCrossbowExpertise(data) },
        { WeaponType.GreatSword, (steamID, data) => steamID.SetPlayerGreatSwordExpertise(data) },
        { WeaponType.Slashers, (steamID, data) => steamID.SetPlayerSlashersExpertise(data) },
        { WeaponType.Pistols, (steamID, data) => steamID.SetPlayerPistolsExpertise(data) },
        { WeaponType.Reaper, (steamID, data) => steamID.SetPlayerReaperExpertise(data) },
        { WeaponType.Longbow, (steamID, data) => steamID.SetPlayerLongbowExpertise(data) },
        { WeaponType.Whip, (steamID, data) => steamID.SetPlayerWhipExpertise(data) },
        { WeaponType.Unarmed, (steamID, data) => steamID.SetPlayerUnarmedExpertise(data) },
        { WeaponType.FishingPole, (steamID, data) => steamID.SetPlayerFishingPoleExpertise(data) },
        { WeaponType.TwinBlades, (steamID, data) => steamID.SetPlayerTwinBladesExpertise(data) },
        { WeaponType.Daggers, (steamID, data) => steamID.SetPlayerDaggersExpertise(data) },
        { WeaponType.Claws, (steamID, data) => steamID.SetPlayerClawsExpertise(data) }
    };
    public static IReadOnlyDictionary<WeaponType, PrestigeType> WeaponPrestigeTypes => _weaponPrestigeTypes;
    static readonly Dictionary<WeaponType, PrestigeType> _weaponPrestigeTypes = new()
    {
        { WeaponType.Sword, PrestigeType.SwordExpertise },
        { WeaponType.Axe, PrestigeType.AxeExpertise },
        { WeaponType.Mace, PrestigeType.MaceExpertise },
        { WeaponType.Spear, PrestigeType.SpearExpertise },
        { WeaponType.Crossbow, PrestigeType.CrossbowExpertise },
        { WeaponType.GreatSword, PrestigeType.GreatSwordExpertise },
        { WeaponType.Slashers, PrestigeType.SlashersExpertise },
        { WeaponType.Pistols, PrestigeType.PistolsExpertise },
        { WeaponType.Reaper, PrestigeType.ReaperExpertise },
        { WeaponType.Longbow, PrestigeType.LongbowExpertise },
        { WeaponType.Whip, PrestigeType.WhipExpertise },
        { WeaponType.Unarmed, PrestigeType.UnarmedExpertise },
        { WeaponType.FishingPole, PrestigeType.FishingPoleExpertise },
        { WeaponType.TwinBlades, PrestigeType.TwinBladesExpertise },
        { WeaponType.Daggers, PrestigeType.DaggersExpertise },
        { WeaponType.Claws, PrestigeType.ClawsExpertise }
    };
    public static void OnUpdate(object sender, DeathEventArgs deathEvent)
    {
        ProcessExpertise(deathEvent, 1f);
    }
    public static void ProcessExpertise(DeathEventArgs deathEvent, float groupMultiplier = 1f)
    {
        Entity playerCharacter = deathEvent.Source;
        Entity target = deathEvent.Target;

        if (target.Has<Minion>()) return;

        Entity userEntity = playerCharacter.GetUserEntity();
        User user = userEntity.GetUser();
        ulong steamId = user.PlatformId;

        WeaponType weaponType = WeaponManager.GetCurrentWeaponType(playerCharacter);

        if (target.TryGetComponent(out UnitStats unitStats))
        {
            float expertiseValue = CalculateExpertiseValue(unitStats, target.Has<VBloodConsumeSource>());
            float changeFactor = 1f;

            if (_unitSpawnerExpertiseFactor < 1 && target.TryGetComponent(out IsMinion isMinion) && isMinion.Value)
            {
                expertiseValue *= _unitSpawnerExpertiseFactor;
                if (expertiseValue == 0) return;
            }

            if (steamId.TryGetPlayerPrestiges(out var prestiges))
            {
                if (prestiges.TryGetValue(WeaponPrestigeTypes[weaponType], out var expertisePrestige))
                {
                    changeFactor -= (_prestigeRatesReducer * expertisePrestige);
                }

                if (prestiges.TryGetValue(PrestigeType.Experience, out var xpPrestige))
                {
                    changeFactor += (_prestigeRateMultiplier * xpPrestige);
                }
            }

            expertiseValue *= changeFactor * groupMultiplier;

            IWeaponExpertise handler = WeaponExpertiseFactory.GetExpertise(weaponType);
            if (handler != null)
            {
                SaveExpertiseExperience(steamId, handler, expertiseValue, out bool leveledUp, out int newLevel);
                NotifyPlayer(playerCharacter, userEntity, user, steamId, weaponType, expertiseValue, leveledUp, newLevel, handler, deathEvent.ScrollingTextDelay);
                deathEvent.ScrollingTextDelay += DELAY_ADD;
            }
        }
    }
    static float CalculateExpertiseValue(UnitStats unitStats, bool isVBlood)
    {
        float ExpertiseValue = unitStats.SpellPower + unitStats.PhysicalPower;

        if (isVBlood) return ExpertiseValue * _vBloodExpertiseMultiplier;
        else return ExpertiseValue * _unitExpertiseMultiplier;
    }
    public static void SaveExpertiseExperience(ulong steamId, IWeaponExpertise handler, float gainedXP, out bool leveledUp, out int newLevel)
    {
        var xpData = handler.GetExpertiseData(steamId);
        int currentLevel = xpData.Key;
        float currentXP = xpData.Value;

        if (currentLevel >= _maxExpertiseLevel)
        {
            leveledUp = false;
            newLevel = currentLevel;
            return;
        }

        float newExperience = currentXP + gainedXP;
        newLevel = ConvertXpToLevel(newExperience);
        leveledUp = false;

        if (newLevel > currentLevel)
        {
            leveledUp = true;
            if (newLevel > _maxExpertiseLevel)
            {
                newLevel = _maxExpertiseLevel;
                newExperience = ConvertLevelToXp(_maxExpertiseLevel);
            }
        }

        handler.SetExpertiseData(steamId, new KeyValuePair<int, float>(newLevel, newExperience));
    }
    public static void NotifyPlayer(Entity playerCharacter, Entity userEntity, User user, ulong steamId, WeaponType weaponType, float gainedXP, bool leveledUp, int newLevel, IWeaponExpertise handler, float delay)
    {
        int gainedIntXP = (int)gainedXP;
        int levelProgress = GetLevelProgress(steamId, handler);

        if (newLevel >= _maxExpertiseLevel) return;
        else if (gainedXP <= 0) return;

        if (leveledUp)
        {
            HandleWeaponLevelUp(user, weaponType, newLevel, steamId);
            Buffs.RefreshStats(playerCharacter);
        }

        if (GetPlayerBool(steamId, WEAPON_LOG_KEY))
        {
            LocalizationService.HandleServerReply(EntityManager, user,
                $"+<color=yellow>{gainedIntXP}</color> <color=#c0c0c0>{weaponType.ToString().ToLower()}</color> <color=#FFC0CB>expertise</color> (<color=white>{levelProgress}%</color>)");
        }

        if (GetPlayerBool(steamId, SCT_PLAYER_WEP_KEY))
        {
            // Core.Log.LogInfo($"Expertise SCT for {user.CharacterName.Value} with gainedXP: {gainedXP} and delay: {delay}");
            PlayerExpertiseSCTDelayRoutine(playerCharacter, userEntity, _grey, gainedXP, delay).Run();
        }
    }
    static void HandleWeaponLevelUp(User user, WeaponType weaponType, int newLevel, ulong steamID)
    {
        if (newLevel <= _maxExpertiseLevel)
        {
            LocalizationService.HandleServerReply(EntityManager, user,
                $"<color=#c0c0c0>{weaponType}</color> improved to [<color=white>{newLevel}</color>]!");
        }

        if (GetPlayerBool(steamID, REMINDERS_KEY))
        {
            if (steamID.TryGetPlayerWeaponStats(out var weaponStats) && weaponStats.TryGetValue(weaponType, out var stats))
            {
                int currentStatCount = stats.Count;
                if (currentStatCount < _expertiseStatChoices)
                {
                    int choicesLeft = _expertiseStatChoices - currentStatCount;
                    string bonusString = choicesLeft > 1 ? "bonuses" : "bonus";

                    LocalizationService.HandleServerReply(EntityManager, user,
                        $"{choicesLeft} <color=white>stat</color> <color=#00FFFF>{bonusString}</color> available for <color=#c0c0c0>{weaponType.ToString().ToLower()}</color>; use '<color=white>.wep cst {weaponType} [Stat]</color>' to choose and '<color=white>.wep lst'</color> to view expertise stat options. (toggle reminders with <color=white>'.misc remindme'</color>)");
                }
            }
        }
    }
    static IEnumerator PlayerExpertiseSCTDelayRoutine(Entity playerCharacter, Entity userEntity, float3 color, float gainedXP, float delay) // maybe just have one of these in progression utilities but later
    {
        yield return new WaitForSeconds(delay);

        float3 position = playerCharacter.GetPosition();
        ScrollingCombatTextMessage.Create(EntityManager, EndSimulationEntityCommandBufferSystem.CreateCommandBuffer(), _experienceAssetGuid, position, color, playerCharacter, gainedXP, _sctGeneric, userEntity);
        // delay += DELAY_ADD;
    }
    public static int GetLevelProgress(ulong steamID, IWeaponExpertise handler)
    {
        int currentLevel = GetLevel(steamID, handler);
        float currentXP = GetXp(steamID, handler);

        int currentLevelXP = ConvertLevelToXp(currentLevel);
        int nextLevelXP = ConvertLevelToXp(++currentLevel);

        double neededXP = nextLevelXP - currentLevelXP;
        double earnedXP = nextLevelXP - currentXP;

        return 100 - (int)Math.Ceiling(earnedXP / neededXP * 100);
    }
    static float GetXp(ulong steamID, IWeaponExpertise handler)
    {
        var xpData = handler.GetExpertiseData(steamID);
        return xpData.Value;
    }
    public static int GetLevel(ulong steamID, IWeaponExpertise handler)
    {
        var xpData = handler.GetExpertiseData(steamID);
        return xpData.Key;
    }
    public static WeaponType GetWeaponTypeFromWeaponEntity(Entity weaponEntity)
    {
        if (!weaponEntity.HasValue()) return WeaponType.Unarmed;
        string weaponCheck = weaponEntity.GetPrefabGuid().GetPrefabName();

        return Enum.GetValues(typeof(WeaponType))
            .Cast<WeaponType>()
            .FirstOrDefault(type =>
            weaponCheck.Contains(type.ToString(), StringComparison.CurrentCultureIgnoreCase) &&
            !(type == WeaponType.Sword && weaponCheck.Contains("GreatSword", StringComparison.CurrentCultureIgnoreCase))
            );
    }
}