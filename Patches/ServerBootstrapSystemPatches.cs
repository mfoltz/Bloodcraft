using Bloodcraft.Services;
using Bloodcraft.Systems.Legacies;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Stunlock.Network;
using Unity.Entities;
using static Bloodcraft.Services.DataService.FamiliarPersistence;
using static Bloodcraft.Services.PlayerService;
using User = ProjectM.Network.User;
using WeaponType = Bloodcraft.Systems.Expertise.WeaponType;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class ServerBootstrapSystemPatches
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly PrefabGUID woodenCoffin = new(381160212);
    static readonly PrefabGUID stoneCoffin = new(569692162);
    static bool Classes => ConfigService.SoftSynergies || ConfigService.HardSynergies;

    static readonly Dictionary<string, bool> DefaultBools = new()
    {
        { "ExperienceLogging", false },
        { "QuestLogging", false },
        { "ProfessionLogging", false },
        { "ExpertiseLogging", false },
        { "BloodLogging", false },
        { "FamiliarLogging", false },
        { "SpellLock", false },
        { "ShiftLock", false },
        { "Grouping", false },
        { "Emotes", false },
        { "Binding", false },
        { "Kit", true },
        { "VBloodEmotes", true },
        { "FamiliarVisual", true},
        { "ShinyChoice", false },
        { "Reminders", true }
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

        bool exists = false;
        Entity character = Entity.Null;

        if (user.LocalCharacter._Entity.Exists())
        {
            exists = true;
            character = user.LocalCharacter._Entity;
        }

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

        if (ConfigService.ProfessionSystem)
        {
            if (!steamId.TryGetPlayerWoodcutting(out var woodcutting))
            {
                steamId.SetPlayerWoodcutting(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerMining(out var rmining))
            {
                steamId.SetPlayerMining(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerFishing(out var fishing))
            {
                steamId.SetPlayerFishing(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerBlacksmithing(out var blacksmithing))
            {
                steamId.SetPlayerBlacksmithing(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerTailoring(out var tailoring))
            {
                steamId.SetPlayerTailoring(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerAlchemy(out var alchemy))
            {
                steamId.SetPlayerAlchemy(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerHarvesting(out var harvesting))
            {
                steamId.SetPlayerHarvesting(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerEnchanting(out var enchanting))
            {
                steamId.SetPlayerEnchanting(new KeyValuePair<int, float>(0, 0f));
            }
        }

        if (ConfigService.ExpertiseSystem)
        {
            if (!steamId.TryGetPlayerUnarmedExpertise(out var unarmed))
            {
                steamId.SetPlayerUnarmedExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerSpells(out var spells))
            {
                steamId.SetPlayerSpells((0, 0, 0));
            }

            if (!steamId.TryGetPlayerSwordExpertise(out var sword))
            {
                steamId.SetPlayerSwordExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerAxeExpertise(out var axe))
            {
                steamId.SetPlayerAxeExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerMaceExpertise(out var mace))
            {
                steamId.SetPlayerMaceExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerSpearExpertise(out var spear))
            {
                steamId.SetPlayerSpearExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerCrossbowExpertise(out var crossbow))
            {
                steamId.SetPlayerCrossbowExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerGreatSwordExpertise(out var greatSword))
            {
                steamId.SetPlayerGreatSwordExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerSlashersExpertise(out var slashers))
            {
                steamId.SetPlayerSlashersExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerPistolsExpertise(out var pistols))
            {
                steamId.SetPlayerPistolsExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerReaperExpertise(out var reaper))
            {
                steamId.SetPlayerReaperExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerLongbowExpertise(out var longbow))
            {
                steamId.SetPlayerLongbowExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerWhipExpertise(out var whip))
            {
                steamId.SetPlayerWhipExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerFishingPoleExpertise(out var fishingPole))
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

        if (ConfigService.BloodSystem)
        {
            if (!steamId.TryGetPlayerWorkerLegacy(out var worker))
            {
                steamId.SetPlayerWorkerLegacy(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerWarriorLegacy(out var warrior))
            {
                steamId.SetPlayerWarriorLegacy(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerScholarLegacy(out var scholar))
            {
                steamId.SetPlayerScholarLegacy(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerRogueLegacy(out var rogue))
            {
                steamId.SetPlayerRogueLegacy(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerMutantLegacy(out var mutant))
            {
                steamId.SetPlayerMutantLegacy(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerVBloodLegacy(out var vBlood))
            {
                steamId.SetPlayerVBloodLegacy(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerDraculinLegacy(out var draculin))
            {
                steamId.SetPlayerDraculinLegacy(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerImmortalLegacy(out var immortal))
            {
                steamId.SetPlayerImmortalLegacy(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerCreatureLegacy(out var creature))
            {
                steamId.SetPlayerCreatureLegacy(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerBruteLegacy(out var brute))
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

        if (ConfigService.LevelingSystem)
        {
            if (!steamId.TryGetPlayerExperience(out var experience))
            {
                steamId.SetPlayerExperience(new KeyValuePair<int, float>(ConfigService.StartingLevel, LevelingSystem.ConvertLevelToXp(ConfigService.StartingLevel)));
            }

            if (ConfigService.RestedXPSystem)
            {
                if (!steamId.TryGetPlayerRestedXP(out var restedData))
                {
                    steamId.SetPlayerRestedXP(new KeyValuePair<DateTime, float>(DateTime.UtcNow, 0));
                }
                else if (character.Exists())
                {
                    float restedMultiplier = 0;

                    if (ServerGameManager.HasBuff(character, woodenCoffin)) restedMultiplier = 0.5f;
                    else if (ServerGameManager.HasBuff(character, stoneCoffin)) restedMultiplier = 1f;

                    DateTime lastLogout = restedData.Key;
                    TimeSpan timeOffline = DateTime.UtcNow - lastLogout;

                    if (timeOffline.TotalMinutes >= ConfigService.RestedXPTickRate && restedMultiplier != 0 && experience.Key < ConfigService.MaxLevel)
                    {
                        float currentRestedXP = restedData.Value;

                        int currentLevel = experience.Key;
                        int maxRestedLevel = Math.Min(ConfigService.RestedXPMax + currentLevel, ConfigService.MaxLevel);
                        float restedCap = LevelingSystem.ConvertLevelToXp(maxRestedLevel) - LevelingSystem.ConvertLevelToXp(currentLevel);

                        float earnedPerTick = ConfigService.RestedXPRate * restedCap;
                        float earnedRestedXP = (float)timeOffline.TotalMinutes / ConfigService.RestedXPTickRate * earnedPerTick * restedMultiplier;

                        currentRestedXP = Math.Min(currentRestedXP + earnedRestedXP, restedCap);
                        int roundedXP = (int)(Math.Round(currentRestedXP / 100.0) * 100);

                        //Core.Log.LogInfo($"Rested XP: {currentRestedXP} | Earned: {earnedRestedXP} | Rounded: {roundedXP} | Rested Cap: {restedCap} | Max Rested Level: {maxRestedLevel}");
                        steamId.SetPlayerRestedXP(new KeyValuePair<DateTime, float>(DateTime.UtcNow, currentRestedXP));
                        string message = $"+<color=#FFD700>{roundedXP}</color> <color=green>rested</color> <color=#FFC0CB>experience</color> earned from being logged out in your coffin!";
                        LocalizationService.HandleServerReply(EntityManager, user, message);
                    }
                }
            }

            if (exists) LevelingSystem.SetLevel(character);
        }

        if (ConfigService.PrestigeSystem)
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
            }
        }

        if (ConfigService.FamiliarSystem)
        {
            if (!steamId.TryGetFamiliarActives(out var actives))
            {
                steamId.SetFamiliarActives(new(Entity.Null, 0));
            }

            if (!steamId.TryGetFamiliarBox(out var box))
            {
                steamId.SetFamiliarBox("");
            }

            FamiliarExperienceManager.SaveFamiliarExperience(steamId, FamiliarExperienceManager.LoadFamiliarExperience(steamId));
            FamiliarUnlocksManager.SaveUnlockedFamiliars(steamId, FamiliarUnlocksManager.LoadUnlockedFamiliars(steamId));

            if (character.Has<FollowerBuffer>())
            {
                var buffer = character.ReadBuffer<FollowerBuffer>();

                foreach (var follower in buffer)
                {
                    if (follower.Entity._Entity.Exists())
                    {
                        DestroyUtility.Destroy(EntityManager, follower.Entity._Entity);
                    }
                }

                FamiliarUtilities.
                                ClearFamiliarActives(steamId);
            }
        }

        if (Classes)
        {
            if (!steamId.TryGetPlayerClasses(out var classes))
            {
                steamId.SetPlayerClasses([]);
            }

            if (!steamId.TryGetPlayerSpells(out var spells))
            {
                steamId.SetPlayerSpells((0, 0, 0));
            }
        }

        if (ConfigService.ClientCompanion && exists)
        {
            PlayerInfo playerInfo = new()
            {
                CharEntity = character,
                UserEntity = userEntity,
                User = user
            };

            if (!OnlineCache.ContainsKey(steamId.ToString())) OnlineCache.TryAdd(steamId.ToString(), playerInfo);
        }
    }

    [HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUserDisconnected))]
    [HarmonyPrefix]
    static void OnUserDisconnectedPrefix(ServerBootstrapSystem __instance, NetConnectionId netConnectionId)
    {
        int userIndex = __instance._NetEndPointToApprovedUserIndex[netConnectionId];
        ServerBootstrapSystem.ServerClient serverClient = __instance._ApprovedUsersLookup[userIndex];
        Entity userEntity = serverClient.UserEntity;
        User user = __instance.EntityManager.GetComponentData<User>(userEntity);
        Entity character = user.LocalCharacter._Entity;
        ulong steamId = user.PlatformId;

        if (ConfigService.FamiliarSystem && character.Has<FollowerBuffer>())
        {
            var buffer = character.ReadBuffer<FollowerBuffer>();

            foreach (var follower in buffer)
            {
                if (follower.Entity._Entity.Exists())
                {
                    DestroyUtility.Destroy(EntityManager, follower.Entity._Entity);
                }
            }

            FamiliarUtilities.
                        ClearFamiliarActives(steamId);
        }

        if (ConfigService.LevelingSystem)
        {
            if (ConfigService.RestedXPSystem && steamId.TryGetPlayerRestedXP(out var restedData))
            {
                restedData = new KeyValuePair<DateTime, float>(DateTime.UtcNow, restedData.Value);
                steamId.SetPlayerRestedXP(restedData);
            }
        }

        if (ConfigService.ClientCompanion)
        {
            if (EclipseService.RegisteredUsers.Contains(steamId)) EclipseService.RegisteredUsers.Remove(steamId);
        }
    }
}