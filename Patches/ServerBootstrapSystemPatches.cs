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
using System.Collections.Concurrent;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using static Bloodcraft.Services.DataService.FamiliarPersistence;
using static Bloodcraft.Services.PlayerService;
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

    static PrefabLookupMap PrefabLookupMap = PrefabCollectionSystem._PrefabLookupMap;

    static readonly WaitForSeconds Delay = new(2.5f);

    static readonly PrefabGUID InsideWoodenCoffin = new(381160212);
    static readonly PrefabGUID InsideStoneCoffin = new(569692162);

    static readonly bool Classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;
    static readonly bool BloodSystem = ConfigService.BloodSystem;
    static readonly bool Leveling = ConfigService.LevelingSystem;
    static readonly bool Prestige = ConfigService.PrestigeSystem;
    static readonly bool FamiliarSystem = ConfigService.FamiliarSystem;
    static readonly bool ExpertiseSystem = ConfigService.ExpertiseSystem;
    static readonly bool QuestSystem = ConfigService.QuestSystem;
    static readonly bool ClientCompanion = ConfigService.ClientCompanion;
    static readonly bool ExoPrestiging = ConfigService.ExoPrestiging;
    static readonly bool RestedXPSystem = ConfigService.RestedXPSystem;
    static readonly bool Professions = ConfigService.ProfessionSystem;

    static readonly float RestedXPTickRate = ConfigService.RestedXPTickRate;
    static readonly float RestedXPRate = ConfigService.RestedXPRate;
    static readonly int RestedXPMax = ConfigService.RestedXPMax;
    static readonly int StartingLevel = ConfigService.StartingLevel;
    static readonly int MaxLevel = ConfigService.MaxLevel;

    static readonly ConcurrentDictionary<string, bool> DefaultBools = new()
    {
        ["ExperienceLogging"] = false,
        ["QuestLogging"] = false,
        ["ProfessionLogging"] = false,
        ["ExpertiseLogging"] = false,
        ["BloodLogging"] = false,
        ["FamiliarLogging"] = false,
        ["SpellLock"] = false,
        ["ShiftLock"] = false,
        ["Grouping"] = false,
        ["Emotes"] = false,
        ["Binding"] = false,
        ["Kit"] = false,
        ["VBloodEmotes"] = true,
        ["FamiliarVisual"] = true,
        ["ShinyChoice"] = false,
        ["Reminders"] = true,
        ["ScrollingText"] = true,
        ["ExoForm"] = false,
        ["Shroud"] = true
    };

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
        yield return Delay;

        if (!steamId.TryGetPlayerBools(out var bools))
        {
            steamId.SetPlayerBools(DefaultBools);
        }
        else
        {
            foreach (string key in DefaultBools.Keys)
            {
                if (!bools.ContainsKey(key))
                {
                    bools[key] = DefaultBools[key];
                }
            }

            steamId.SetPlayerBools(bools);
        }

        if (Professions)
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

        if (ExpertiseSystem)
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

        if (BloodSystem)
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

        if (Leveling)
        {
            if (!steamId.TryGetPlayerExperience(out var experience))
            {
                steamId.SetPlayerExperience(new KeyValuePair<int, float>(StartingLevel, ConvertLevelToXp(StartingLevel)));
            }

            if (RestedXPSystem)
            {
                if (!steamId.TryGetPlayerRestedXP(out var restedData))
                {
                    steamId.SetPlayerRestedXP(new KeyValuePair<DateTime, float>(DateTime.UtcNow, 0));
                }
                else if (exists)
                {
                    float restedMultiplier = 0f;

                    if (ServerGameManager.HasBuff(playerCharacter, InsideWoodenCoffin)) restedMultiplier = 0.5f;
                    else if (ServerGameManager.HasBuff(playerCharacter, InsideStoneCoffin)) restedMultiplier = 1f;

                    DateTime lastLogout = restedData.Key;
                    TimeSpan timeOffline = DateTime.UtcNow - lastLogout;

                    if (timeOffline.TotalMinutes >= RestedXPTickRate && restedMultiplier != 0f && experience.Key < MaxLevel)
                    {
                        float currentRestedXP = restedData.Value;

                        int currentLevel = experience.Key;
                        int maxRestedLevel = Math.Min(RestedXPMax + currentLevel, MaxLevel);
                        float restedCap = ConvertLevelToXp(maxRestedLevel) - ConvertLevelToXp(currentLevel);

                        float earnedPerTick = RestedXPRate * restedCap;
                        float earnedRestedXP = (float)timeOffline.TotalMinutes / RestedXPTickRate * earnedPerTick * restedMultiplier;

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

        if (Prestige)
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

                if (ExoPrestiging && exists && prestiges.TryGetValue(PrestigeType.Experience, out int exoPrestiges) && exoPrestiges > 0)
                {
                    PrestigeSystem.ResetDamageResistCategoryStats(playerCharacter); // undo old exo stuff

                    if (!steamId.TryGetPlayerExoFormData(out var _))
                    {
                        KeyValuePair<DateTime, float> timeEnergyPair = new(DateTime.UtcNow, ExoForm.CalculateFormDuration(exoPrestiges));
                        steamId.SetPlayerExoFormData(timeEnergyPair);
                    }
                }

                if (ExoPrestiging && !steamId.TryGetPlayerExoFormData(out var _))
                {
                    KeyValuePair<DateTime, float> timeEnergyPair = new(DateTime.MaxValue, 0f);
                    steamId.SetPlayerExoFormData(timeEnergyPair);
                }
            }
        }

        if (FamiliarSystem)
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
                steamId.SetFamiliarBattleGroup([0,0,0]);
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

        if (Classes)
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

        if (ClientCompanion && exists)
        {
            PlayerInfo playerInfo = new()
            {
                CharEntity = playerCharacter,
                UserEntity = userEntity,
                User = user
            };

            if (!OnlineCache.ContainsKey(steamId.ToString())) OnlineCache.TryAdd(steamId.ToString(), playerInfo);
            if (!PlayerCache.ContainsKey(steamId.ToString())) PlayerCache.TryAdd(steamId.ToString(), playerInfo);
        }
    }
    static IEnumerator UpdatePlayerFamiliar(Entity playerCharacter, Entity familiar)
    {
        yield return Delay;

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

        if (Leveling)
        {
            if (RestedXPSystem && steamId.TryGetPlayerRestedXP(out var restedData))
            {
                restedData = new KeyValuePair<DateTime, float>(DateTime.UtcNow, restedData.Value);
                steamId.SetPlayerRestedXP(restedData);
            }
        }
        
        if (ClientCompanion)
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
                        if (Leveling)
                        {
                            if (RestedXPSystem && steamId.TryGetPlayerRestedXP(out var restedData))
                            {
                                restedData = new KeyValuePair<DateTime, float>(DateTime.UtcNow, restedData.Value);
                                steamId.SetPlayerRestedXP(restedData);
                            }
                        }

                        if (ClientCompanion)
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