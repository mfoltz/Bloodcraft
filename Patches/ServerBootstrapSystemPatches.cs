using Bloodcraft.Services;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Legacies;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Stunlock.Network;
using System.Collections;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using static Bloodcraft.Services.DataService.FamiliarPersistence;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Utilities.Misc.PlayerBoolsManager;
using static Bloodcraft.Utilities.Progression;
using User = ProjectM.Network.User;
using WeaponType = Bloodcraft.Systems.Expertise.WeaponType;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class ServerBootstrapSystemPatches
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;
    static EntityCommandBufferSystem EntityCommandBufferSystem => SystemService.EntityCommandBufferSystem;

    static PrefabLookupMap _prefabLookupMap = PrefabCollectionSystem._PrefabLookupMap;

    static readonly WaitForSeconds _delay = new(2.5f);

    static readonly PrefabGUID _insideWoodenCoffin = new(381160212);
    static readonly PrefabGUID _insideStoneCoffin = new(569692162);

    static readonly bool _classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;
    static readonly bool _bloodSystem = ConfigService.BloodSystem;
    static readonly bool _leveling = ConfigService.LevelingSystem;
    static readonly bool _prestige = ConfigService.PrestigeSystem;
    static readonly bool _familiarSystem = ConfigService.FamiliarSystem;
    static readonly bool _expertiseSystem = ConfigService.ExpertiseSystem;
    static readonly bool _questSystem = ConfigService.QuestSystem;
    static readonly bool _clientCompanion = ConfigService.ClientCompanion;
    static readonly bool _exoPrestiging = ConfigService.ExoPrestiging;
    static readonly bool _restedXPSystem = ConfigService.RestedXPSystem;
    static readonly bool _professions = ConfigService.ProfessionSystem;

    static readonly float _restedXPTickRate = ConfigService.RestedXPTickRate;
    static readonly float _restedXPRate = ConfigService.RestedXPRate;
    static readonly int _restedXPMax = ConfigService.RestedXPMax;
    static readonly int _startingLevel = ConfigService.StartingLevel;
    static readonly int _maxLevel = ConfigService.MaxLevel;

    [HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUserConnected))]
    [HarmonyPostfix]
    static void OnUserConnectedPostfix(ServerBootstrapSystem __instance, NetConnectionId netConnectionId)
    {
        int userIndex = __instance._NetEndPointToApprovedUserIndex[netConnectionId];
        ServerBootstrapSystem.ServerClient serverClient = __instance._ApprovedUsersLookup[userIndex];
        Entity userEntity = serverClient.UserEntity;
        User user = __instance.EntityManager.GetComponentData<User>(userEntity);
        ulong steamId = user.PlatformId;
        Entity playerCharacter = user.LocalCharacter.GetEntityOnServer();

        bool exists = playerCharacter.Exists();

        Core.StartCoroutine(UpdatePlayerData(steamId, playerCharacter, userEntity, user, exists));
    }
    static IEnumerator UpdatePlayerData(ulong steamId, Entity playerCharacter, Entity userEntity, User user, bool exists)
    {
        yield return _delay;

        DataService.PlayerBoolsManager.GetOrInitializePlayerBools(steamId, DefaultBools);

        if (_professions)
        {
            if (!steamId.TryGetPlayerWoodcutting(out var _))
            {
                steamId.SetPlayerWoodcutting(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerMining(out var _))
            {
                steamId.SetPlayerMining(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerFishing(out var _))
            {
                steamId.SetPlayerFishing(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerBlacksmithing(out var _))
            {
                steamId.SetPlayerBlacksmithing(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerTailoring(out var _))
            {
                steamId.SetPlayerTailoring(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerAlchemy(out var _))
            {
                steamId.SetPlayerAlchemy(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerHarvesting(out var _))
            {
                steamId.SetPlayerHarvesting(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerEnchanting(out var _))
            {
                steamId.SetPlayerEnchanting(new KeyValuePair<int, float>(0, 0f));
            }
        }

        if (_expertiseSystem)
        {
            if (!steamId.TryGetPlayerUnarmedExpertise(out var _))
            {
                steamId.SetPlayerUnarmedExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerSpells(out var _))
            {
                steamId.SetPlayerSpells((0, 0, 0));
            }

            if (!steamId.TryGetPlayerSwordExpertise(out var _))
            {
                steamId.SetPlayerSwordExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerAxeExpertise(out var _))
            {
                steamId.SetPlayerAxeExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerMaceExpertise(out var _))
            {
                steamId.SetPlayerMaceExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerSpearExpertise(out var _))
            {
                steamId.SetPlayerSpearExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerCrossbowExpertise(out var _))
            {
                steamId.SetPlayerCrossbowExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerGreatSwordExpertise(out var _))
            {
                steamId.SetPlayerGreatSwordExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerSlashersExpertise(out var _))
            {
                steamId.SetPlayerSlashersExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerPistolsExpertise(out var _))
            {
                steamId.SetPlayerPistolsExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerReaperExpertise(out var _))
            {
                steamId.SetPlayerReaperExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerLongbowExpertise(out var _))
            {
                steamId.SetPlayerLongbowExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerWhipExpertise(out var _))
            {
                steamId.SetPlayerWhipExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerFishingPoleExpertise(out var _))
            {
                steamId.SetPlayerFishingPoleExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerWeaponStats(out var weaponStats))
            {
                weaponStats = [];
                foreach (WeaponType weaponType in Enum.GetValues<WeaponType>())
                {
                    weaponStats.Add(weaponType, []);
                }
                steamId.SetPlayerWeaponStats(weaponStats);  // Assuming the weapon stats are a list or similar collection.
            }
            else
            {
                foreach (WeaponType weaponType in Enum.GetValues<WeaponType>())
                {
                    if (!weaponStats.ContainsKey(weaponType)) weaponStats.Add(weaponType, []);
                }
            }
        }

        if (_bloodSystem)
        {
            if (!steamId.TryGetPlayerWorkerLegacy(out var _))
            {
                steamId.SetPlayerWorkerLegacy(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerWarriorLegacy(out var _))
            {
                steamId.SetPlayerWarriorLegacy(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerScholarLegacy(out var _))
            {
                steamId.SetPlayerScholarLegacy(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerRogueLegacy(out var _))
            {
                steamId.SetPlayerRogueLegacy(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerMutantLegacy(out var _))
            {
                steamId.SetPlayerMutantLegacy(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerVBloodLegacy(out var _))
            {
                steamId.SetPlayerVBloodLegacy(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerDraculinLegacy(out var _))
            {
                steamId.SetPlayerDraculinLegacy(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerImmortalLegacy(out var _))
            {
                steamId.SetPlayerImmortalLegacy(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerCreatureLegacy(out var _))
            {
                steamId.SetPlayerCreatureLegacy(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerBruteLegacy(out var _))
            {
                steamId.SetPlayerBruteLegacy(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerBloodStats(out var bloodStats))
            {
                bloodStats = [];
                foreach (BloodType bloodType in Enum.GetValues<BloodType>())
                {
                    bloodStats.Add(bloodType, []);
                }
                steamId.SetPlayerBloodStats(bloodStats);
            }
            else
            {
                foreach (BloodType bloodType in Enum.GetValues<BloodType>())
                {
                    if (!bloodStats.ContainsKey(bloodType)) bloodStats.Add(bloodType, []);
                }
            }
        }

        if (_leveling)
        {
            if (!steamId.TryGetPlayerExperience(out var experience))
            {
                steamId.SetPlayerExperience(new KeyValuePair<int, float>(_startingLevel, ConvertLevelToXp(_startingLevel)));
            }

            if (_restedXPSystem)
            {
                if (!steamId.TryGetPlayerRestedXP(out var restedData))
                {
                    steamId.SetPlayerRestedXP(new KeyValuePair<DateTime, float>(DateTime.UtcNow, 0));
                }
                else if (exists)
                {
                    float restedMultiplier = 0f;

                    if (ServerGameManager.HasBuff(playerCharacter, _insideWoodenCoffin)) restedMultiplier = 0.5f;
                    else if (ServerGameManager.HasBuff(playerCharacter, _insideStoneCoffin)) restedMultiplier = 1f;

                    DateTime lastLogout = restedData.Key;
                    TimeSpan timeOffline = DateTime.UtcNow - lastLogout;

                    if (timeOffline.TotalMinutes >= _restedXPTickRate && restedMultiplier != 0f && experience.Key < _maxLevel)
                    {
                        float currentRestedXP = restedData.Value;

                        int currentLevel = experience.Key;
                        int maxRestedLevel = Math.Min(_restedXPMax + currentLevel, _maxLevel);
                        float restedCap = ConvertLevelToXp(maxRestedLevel) - ConvertLevelToXp(currentLevel);

                        float earnedPerTick = _restedXPRate * restedCap;
                        float earnedRestedXP = (float)timeOffline.TotalMinutes / _restedXPTickRate * earnedPerTick * restedMultiplier;

                        currentRestedXP = Math.Min(currentRestedXP + earnedRestedXP, restedCap);
                        int roundedXP = (int)(Math.Round(currentRestedXP / 100.0) * 100);

                        steamId.SetPlayerRestedXP(new KeyValuePair<DateTime, float>(DateTime.UtcNow, currentRestedXP));
                        string message = $"+<color=#FFD700>{roundedXP}</color> <color=green>rested</color> <color=#FFC0CB>experience</color> earned from being logged out in your coffin!";
                        LocalizationService.HandleServerReply(EntityManager, user, message);
                    }
                }
            }

            if (exists) LevelingSystem.SetLevel(playerCharacter);
        }

        if (_prestige)
        {
            if (!steamId.TryGetPlayerPrestiges(out var prestiges))
            {
                var prestigeDict = new Dictionary<PrestigeType, int>();

                foreach (var prestigeType in Enum.GetValues<PrestigeType>())
                {
                    prestigeDict.Add(prestigeType, 0);
                }

                steamId.SetPlayerPrestiges(prestigeDict);
            }
            else
            {
                foreach (var prestigeType in Enum.GetValues<PrestigeType>())
                {
                    if (!prestiges.ContainsKey(prestigeType)) prestiges.Add(prestigeType, 0);
                }

                if (_exoPrestiging && exists && prestiges.TryGetValue(PrestigeType.Experience, out int exoPrestiges) && exoPrestiges > 0)
                {
                    PrestigeSystem.ResetDamageResistCategoryStats(playerCharacter); // undo old exo stuff

                    if (!steamId.TryGetPlayerExoFormData(out var _))
                    {
                        KeyValuePair<DateTime, float> timeEnergyPair = new(DateTime.UtcNow, ExoForm.CalculateFormDuration(exoPrestiges));
                        steamId.SetPlayerExoFormData(timeEnergyPair);
                    }
                }

                if (_exoPrestiging && !steamId.TryGetPlayerExoFormData(out var _))
                {
                    KeyValuePair<DateTime, float> timeEnergyPair = new(DateTime.MaxValue, 0f);
                    steamId.SetPlayerExoFormData(timeEnergyPair);
                }
            }
        }

        if (_familiarSystem)
        {
            if (!steamId.TryGetFamiliarActives(out var _))
            {
                steamId.SetFamiliarActives(new(Entity.Null, 0));
            }

            if (!steamId.TryGetFamiliarBox(out var _))
            {
                steamId.SetFamiliarBox("");
            }

            if (!steamId.TryGetFamiliarBattleGroup(out var _))
            {
                steamId.SetFamiliarBattleGroup([0, 0, 0]);
            }

            FamiliarExperienceManager.SaveFamiliarExperience(steamId, FamiliarExperienceManager.LoadFamiliarExperience(steamId));
            FamiliarUnlocksManager.SaveUnlockedFamiliars(steamId, FamiliarUnlocksManager.LoadUnlockedFamiliars(steamId));

            Entity familiar = Familiars.FindPlayerFamiliar(playerCharacter);

            if (!familiar.Exists())
            {
                Familiars.ClearFamiliarActives(steamId);
            }
            else
            {
                Core.StartCoroutine(UpdatePlayerFamiliar(playerCharacter, familiar));
            }
        }

        if (_classes)
        {
            if (!steamId.TryGetPlayerClasses(out var _))
            {
                steamId.SetPlayerClasses([]);
            }

            if (!steamId.TryGetPlayerSpells(out var _))
            {
                steamId.SetPlayerSpells((0, 0, 0));
            }
        }

        if (_clientCompanion && exists)
        {
            PlayerInfo playerInfo = new()
            {
                CharEntity = playerCharacter,
                UserEntity = userEntity,
                User = user
            };

            if (!OnlineCache.ContainsKey(steamId)) OnlineCache.TryAdd(steamId, playerInfo);
            if (!PlayerCache.ContainsKey(steamId)) PlayerCache.TryAdd(steamId, playerInfo);
        }
    }
    static IEnumerator UpdatePlayerFamiliar(Entity playerCharacter, Entity familiar)
    {
        yield return _delay;

        FamiliarSummonSystem.HandleFamiliar(playerCharacter, familiar);
    }

    [HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUserDisconnected))]
    [HarmonyPrefix]
    static void OnUserDisconnectedPrefix(ServerBootstrapSystem __instance, NetConnectionId netConnectionId)
    {
        int userIndex = __instance._NetEndPointToApprovedUserIndex[netConnectionId];
        ServerBootstrapSystem.ServerClient serverClient = __instance._ApprovedUsersLookup[userIndex];

        Entity userEntity = serverClient.UserEntity;
        User user = __instance.EntityManager.GetComponentData<User>(userEntity);
        ulong steamId = user.PlatformId;

        if (_leveling)
        {
            if (_restedXPSystem && steamId.TryGetPlayerRestedXP(out var restedData))
            {
                restedData = new KeyValuePair<DateTime, float>(DateTime.UtcNow, restedData.Value);
                steamId.SetPlayerRestedXP(restedData);
            }
        }

        if (_clientCompanion)
        {
            if (EclipseService.RegisteredUsersAndClientVersions.ContainsKey(steamId)) EclipseService.RegisteredUsersAndClientVersions.Remove(steamId);
        }
    }

    [HarmonyPatch(typeof(KickBanSystem_Server), nameof(KickBanSystem_Server.OnUpdate))] // treat this an OnUserDisconnected to account for player swaps
    [HarmonyPrefix]
    static void OnUserDisconnectedPrefix(KickBanSystem_Server __instance)
    {
        NativeArray<Entity> entities = __instance._KickQuery.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (entity.TryGetComponent(out KickEvent kickEvent))
                {
                    ulong steamId = kickEvent.PlatformId;

                    if (steamId.TryGetPlayerInfo(out PlayerInfo playerInfo) && playerInfo.CharEntity.Exists())
                    {
                        if (_leveling)
                        {
                            if (_restedXPSystem && steamId.TryGetPlayerRestedXP(out var restedData))
                            {
                                restedData = new KeyValuePair<DateTime, float>(DateTime.UtcNow, restedData.Value);
                                steamId.SetPlayerRestedXP(restedData);
                            }
                        }

                        if (_clientCompanion)
                        {
                            if (EclipseService.RegisteredUsersAndClientVersions.ContainsKey(steamId)) EclipseService.RegisteredUsersAndClientVersions.Remove(steamId);
                        }
                    }
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
}