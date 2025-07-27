using Bloodcraft.Interfaces;
using Bloodcraft.Resources;
using Bloodcraft.Services;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Stunlock.Network;
using System.Collections;
using Unity.Entities;
using UnityEngine;
using static Bloodcraft.Services.DataService.FamiliarPersistence;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Utilities.Familiars;
using static Bloodcraft.Utilities.Misc.PlayerBoolsManager;
using static Bloodcraft.Utilities.Progression;
using User = ProjectM.Network.User;
using WeaponType = Bloodcraft.Interfaces.WeaponType;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class ServerBootstrapSystemPatches
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly WaitForSeconds _delay = new(1f);
    static readonly WaitForSeconds _newCharacterDelay = new(2.5f);

    static readonly PrefabGUID _insideWoodenCoffin = new(381160212);
    static readonly PrefabGUID _insideStoneCoffin = new(569692162);

    static readonly bool _classes = ConfigService.ClassSystem;
    static readonly bool _legacies = ConfigService.LegacySystem;
    static readonly bool _leveling = ConfigService.LevelingSystem;
    static readonly bool _prestige = ConfigService.PrestigeSystem;
    static readonly bool _familiars = ConfigService.FamiliarSystem;
    static readonly bool _expertise = ConfigService.ExpertiseSystem;
    static readonly bool _quests = ConfigService.QuestSystem;
    static readonly bool _exoForm = ConfigService.ExoPrestiging;
    static readonly bool _restedXP = ConfigService.RestedXPSystem;
    static readonly bool _professions = ConfigService.ProfessionSystem;

    static readonly float _restedXPTickRate = ConfigService.RestedXPTickRate;
    static readonly float _restedXPRate = ConfigService.RestedXPRate;
    static readonly int _restedXPMax = ConfigService.RestedXPMax;
    static readonly int _startingLevel = ConfigService.StartingLevel;
    static readonly int _maxLevel = ConfigService.MaxLevel;

    static readonly PrefabGUID _shroudBuff = Buffs.ShroudBuff;
    static readonly PrefabGUID _shroudCloak = PrefabGUIDs.Item_Cloak_Main_ShroudOfTheForest;
    static readonly PrefabGUID _bonusStatsBuff = Buffs.BonusPlayerStatsBuff;

    static readonly PrefabGUID _relicDebuff = PrefabGUIDs.Buff_General_RelicCarryDebuff;

    [HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUserConnected))]
    [HarmonyPostfix]
    static void OnUserConnectedPostfix(ServerBootstrapSystem __instance, NetConnectionId netConnectionId)
    {
        if (!__instance._NetEndPointToApprovedUserIndex.TryGetValue(netConnectionId, out int userIndex)) return;
        ServerBootstrapSystem.ServerClient serverClient = __instance._ApprovedUsersLookup[userIndex];

        Entity userEntity = serverClient.UserEntity;
        User user = __instance.EntityManager.GetComponentData<User>(userEntity);
        ulong steamId = user.PlatformId;

        Entity playerCharacter = user.LocalCharacter.GetEntityOnServer();
        bool exists = playerCharacter.Exists();

        UpdatePlayerData(steamId, playerCharacter, userEntity, user, exists).Start();
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

        if (_expertise)
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

            if (!steamId.TryGetPlayerTwinBladesExpertise(out var _))
            {
                steamId.SetPlayerTwinBladesExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerDaggersExpertise(out var _))
            {
                steamId.SetPlayerDaggersExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerClawsExpertise(out var _))
            {
                steamId.SetPlayerClawsExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerWeaponStats(out var weaponStats))
            {
                weaponStats = [];
                foreach (WeaponType weaponType in Enum.GetValues<WeaponType>())
                {
                    weaponStats.Add(weaponType, []);
                }
                steamId.SetPlayerWeaponStats(weaponStats);
            }
            else
            {
                foreach (WeaponType weaponType in Enum.GetValues<WeaponType>())
                {
                    if (!weaponStats.ContainsKey(weaponType)) weaponStats.Add(weaponType, []);
                }
            }
        }

        if (_legacies)
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

            if (!steamId.TryGetPlayerCorruptionLegacy(out var _))
            {
                steamId.SetPlayerCorruptionLegacy(new KeyValuePair<int, float>(0, 0f));
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

            if (_restedXP)
            {
                if (!steamId.TryGetPlayerRestedXP(out var restedData))
                {
                    steamId.SetPlayerRestedXP(new KeyValuePair<DateTime, float>(DateTime.UtcNow, 0));
                }
                else if (exists) // why isn't this a method, shaaaame but later
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

                if (_exoForm && exists && prestiges.TryGetValue(PrestigeType.Experience, out int exoPrestiges) && exoPrestiges > 0)
                {
                    if (!steamId.TryGetPlayerExoFormData(out var _))
                    {
                        KeyValuePair<DateTime, float> timeEnergyPair = new(DateTime.UtcNow, Shapeshifts.CalculateFormDuration(exoPrestiges));
                        steamId.SetPlayerExoFormData(timeEnergyPair);
                    }
                }

                if (_exoForm && !steamId.TryGetPlayerExoFormData(out var _))
                {
                    KeyValuePair<DateTime, float> timeEnergyPair = new(DateTime.MaxValue, 0f);
                    steamId.SetPlayerExoFormData(timeEnergyPair);
                }
            }

            SetPlayerBool(steamId, SHROUD_KEY, true);

            if (UpdateBuffsBufferDestroyPatch.PrestigeBuffs.Contains(_shroudBuff) && !playerCharacter.HasBuff(_shroudBuff)
                && steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(PrestigeType.Experience, out var experiencePrestiges) && experiencePrestiges > UpdateBuffsBufferDestroyPatch.PrestigeBuffs.IndexOf(_shroudBuff))
            {
                Buffs.TryApplyPermanentBuff(playerCharacter, _shroudBuff);
            }
            else
            {
                SetPlayerBool(steamId, SHROUD_KEY, false);
            }
        }

        if (_familiars)
        {
            if (!steamId.HasActiveFamiliar())
            {
                ActiveFamiliarManager.ResetActiveFamiliarData(steamId);
            }

            if (!steamId.TryGetFamiliarBox(out var _))
            {
                steamId.SetFamiliarBox();
            }

            FamiliarExperienceManager.SaveFamiliarExperienceData(steamId, FamiliarExperienceManager.LoadFamiliarExperienceData(steamId));
            FamiliarUnlocksManager.SaveFamiliarUnlocksData(steamId, FamiliarUnlocksManager.LoadFamiliarUnlocksData(steamId));

            if (playerCharacter.TryGetBuffer<BuffByItemCategoryCount>(out var buffer) && buffer.IsIndexWithinRange(1))
            {
                BuffByItemCategoryCount buffByItemCategoryCount = buffer[1];

                if (buffByItemCategoryCount.ItemCategory.Equals(ItemCategory.Relic))
                {
                    buffer.RemoveAt(1);
                    // Core.Log.LogWarning($"[UpdatePlayerData] - BuffByItemCategoryCount Relic entry removed!");
                }
            }

            if (playerCharacter.HasBuff(_relicDebuff))
            {
                playerCharacter.TryRemoveBuff(buffPrefabGuid: _relicDebuff);
            }
        }

        if (_classes)
        {
            if (!steamId.TryGetPlayerSpells(out var _))
            {
                steamId.SetPlayerSpells((0, 0, 0));
            }
        }

        if (exists)
        {
            PlayerInfo playerInfo = new()
            {
                CharEntity = playerCharacter,
                UserEntity = userEntity,
                User = user
            };

            HandleConnection(steamId, playerInfo);

            if (!playerCharacter.HasBuff(_bonusStatsBuff))
            {
                Buffs.RefreshStats(playerInfo.CharEntity);
            }
        }
        else if (Core.Eclipsed)
        {
            EclipseService.HandlePreRegistration(steamId);
        }
    }
    public static void UnbindFamiliarOnUserDisconnected(User user, Entity playerCharacter)
    {
        ulong steamId = user.PlatformId;

        bool hasActive = steamId.HasActiveFamiliar();
        bool hasDismissed = steamId.HasDismissedFamiliar();

        if (hasActive && hasDismissed)
        {
            Entity familiar = GetActiveFamiliar(playerCharacter);

            CallFamiliar(playerCharacter, familiar, user, user.PlatformId);
            UnbindFamiliar(user, playerCharacter); // maybe also statChangeUtility?
        }
        else if (hasActive && !hasDismissed)
        {
            UnbindFamiliar(user, playerCharacter);
        }
    }

    [HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUserDisconnected))]
    [HarmonyPrefix]
    static void OnUserDisconnectedPrefix(ServerBootstrapSystem __instance, NetConnectionId netConnectionId)
    {
        if (!__instance._NetEndPointToApprovedUserIndex.ContainsKey(netConnectionId)) return;
        int userIndex = __instance._NetEndPointToApprovedUserIndex[netConnectionId];

        ServerBootstrapSystem.ServerClient serverClient = __instance._ApprovedUsersLookup[userIndex];
        Entity userEntity = serverClient.UserEntity;

        User user = __instance.EntityManager.GetComponentData<User>(userEntity);
        Entity playerCharacter = user.LocalCharacter.GetEntityOnServer();
        ulong steamId = user.PlatformId;

        if (_leveling)
        {
            if (_restedXP && steamId.TryGetPlayerRestedXP(out var restedData))
            {
                restedData = new KeyValuePair<DateTime, float>(DateTime.UtcNow, restedData.Value);
                steamId.SetPlayerRestedXP(restedData);
            }
        }

        /*
        if (_eclipse)
        {
            if (EclipseService.RegisteredUsersAndClientVersions.ContainsKey(steamId)) EclipseService.TryUnregisterUser(steamId);
        }
        */

        if (_prestige && playerCharacter.Exists())
        {
            SetPlayerBool(steamId, SHROUD_KEY, false);

            if (playerCharacter.HasBuff(_shroudBuff) && playerCharacter.TryGetComponent(out Equipment equipment))
            {
                if (!equipment.IsEquipped(_shroudCloak, out var _)) playerCharacter.TryRemoveBuff(buffPrefabGuid: _shroudBuff);
            }
        }

        if (_familiars && playerCharacter.Exists())
        {
            UnbindFamiliarOnUserDisconnected(user, playerCharacter); // need to yeet immediately to account for server restarts where no time after everyone 'logs out'
        }

        // if (SteamIdOnlinePlayerInfoCache.ContainsKey(steamId)) HandleDisconnection(steamId, userIndex);
        HandleDisconnection(steamId);
    }

    [HarmonyPatch(typeof(KickBanSystem_Server), nameof(KickBanSystem_Server.OnUpdate))] // treat this an OnUserDisconnected to account for player swaps
    [HarmonyPrefix]
    static void OnUpdatePrefix(KickBanSystem_Server __instance)
    {
        using NativeAccessor<KickEvent> kickEvents = __instance._KickQuery.ToComponentDataArrayAccessor<KickEvent>();

        try
        {
            for (int i = 0; i < kickEvents.Length; i++)
            {
                KickEvent kickEvent = kickEvents[i];
                ulong steamId = kickEvent.PlatformId;

                if (steamId.TryGetPlayerInfo(out PlayerInfo playerInfo))
                {
                    if (_leveling)
                    {
                        if (_restedXP && steamId.TryGetPlayerRestedXP(out var restedData))
                        {
                            restedData = new KeyValuePair<DateTime, float>(DateTime.UtcNow, restedData.Value);
                            steamId.SetPlayerRestedXP(restedData);
                        }
                    }

                    /*
                    if (_eclipse)
                    {
                        if (EclipseService.RegisteredUsersAndClientVersions.ContainsKey(steamId)) EclipseService.TryUnregisterUser(steamId);
                    }
                    */

                    if (_prestige)
                    {
                        SetPlayerBool(steamId, SHROUD_KEY, false);

                        if (playerInfo.CharEntity.HasBuff(_shroudBuff) && playerInfo.CharEntity.TryGetComponent(out Equipment equipment))
                        {
                            if (!equipment.IsEquipped(_shroudCloak, out var _)) playerInfo.CharEntity.TryRemoveBuff(buffPrefabGuid: _shroudBuff);
                        }
                    }

                    if (_familiars && playerInfo.CharEntity.Exists())
                    {
                        UnbindFamiliarOnUserDisconnected(playerInfo.User, playerInfo.CharEntity);
                    }

                    // if (SteamIdOnlinePlayerInfoCache.ContainsKey(steamId)) HandleDisconnection(steamId); 
                    HandleDisconnection(steamId);
                }

            }
        }
        catch (Exception ex)
        {
            Core.Log.LogError($"Error in KickBanSystem_Server: {ex}");
        }
    }

    [HarmonyPatch(typeof(HandleCreateCharacterEventSystem), nameof(HandleCreateCharacterEventSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnCharacterCreated(HandleCreateCharacterEventSystem __instance)
    {
        using NativeAccessor<FromCharacter> fromCharacterEvents = __instance._CreateCharacterEventQuery.ToComponentDataArrayAccessor<FromCharacter>();

        try
        {
            for (int i = 0; i < fromCharacterEvents.Length; i++)
            {
                FromCharacter fromCharacter = fromCharacterEvents[i];
                Entity userEntity = fromCharacter.User;

                // Core.Log.LogWarning($"[HandleCreateCharacterEventSystem] PreRegistration for new character...");
                HandleCharacterCreatedRoutine(userEntity).Start();
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogError($"Error in HandleCreateCharacterEventSystem: {ex}");
        }
    }
    static IEnumerator HandleCharacterCreatedRoutine(Entity userEntity)
    {
        yield return _newCharacterDelay;

        User user = userEntity.GetUser();
        // Core.Log.LogWarning($"New character created - {user.PlatformId} | {user.LocalCharacter.GetEntityOnServer().Exists()}");

        PlayerInfo playerInfo = new()
        {
            CharEntity = user.LocalCharacter.GetEntityOnServer(),
            UserEntity = userEntity,
            User = user
        };

        HandleConnection(user.PlatformId, playerInfo);

        if (!playerInfo.CharEntity.HasBuff(_bonusStatsBuff))
        {
            // playerInfo.CharEntity.TryApplyBuff(_bonusStatsBuff);
            Buffs.RefreshStats(playerInfo.CharEntity);
        }
    }
}
