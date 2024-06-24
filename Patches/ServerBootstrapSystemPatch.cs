using Bloodcraft.Systems.Experience;
using Bloodcraft.Systems.Leveling;
using HarmonyLib;
using ProjectM;
using Stunlock.Network;
using Unity.Entities;
using User = ProjectM.Network.User;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class ServerBootstrapSystemPatch
{
    [HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUserConnected))]
    [HarmonyPostfix]
    static void OnUserConnectedPostfix(ServerBootstrapSystem __instance, NetConnectionId netConnectionId)
    {
        int userIndex = __instance._NetEndPointToApprovedUserIndex[netConnectionId];
        ServerBootstrapSystem.ServerClient serverClient = __instance._ApprovedUsersLookup[userIndex];
        Entity userEntity = serverClient.UserEntity;
        User user = __instance.EntityManager.GetComponentData<User>(userEntity);
        ulong steamId = user.PlatformId;

        if (!Core.DataStructures.PlayerBools.ContainsKey(steamId))
        {
            Core.DataStructures.PlayerBools.Add(steamId, new Dictionary<string, bool>
            {
                { "ExperienceLogging", false },
                { "ProfessionLogging", false },
                { "ExpertiseLogging", false },
                { "BloodLogging", false },
                { "FamiliarLogging", false },
                { "ShiftLock", false },
                { "SpellLock", false },
                { "Grouping", false },
                { "Emotes", false },
                { "Binding", false }
            });
            Core.DataStructures.SavePlayerBools();
        }
        else
        {
            var existingDict = Core.DataStructures.PlayerBools[steamId];

            // Define the default values
            var defaultValues = new Dictionary<string, bool>
            {
                { "ExperienceLogging", false },
                { "ProfessionLogging", false },
                { "ExpertiseLogging", false },
                { "BloodLogging", false },
                { "FamiliarLogging", false },
                { "SpellLock", false },
                { "ShiftLock", false },
                { "Grouping", false },
                { "Emotes", false },
                { "Binding", false }
            };

            // Add missing default values to the existing dictionary
            foreach (var key in defaultValues.Keys)
            {
                if (!existingDict.ContainsKey(key))
                {
                    existingDict[key] = defaultValues[key];
                }
            }

            Core.DataStructures.PlayerBools[steamId] = existingDict;
            Core.DataStructures.SavePlayerBools();
        }

        if (Plugin.ProfessionSystem.Value)
        {
            if (!Core.DataStructures.PlayerWoodcutting.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerWoodcutting.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerWoodcutting();
            }

            if (!Core.DataStructures.PlayerMining.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerMining.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerMining();
            }

            if (!Core.DataStructures.PlayerFishing.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerFishing.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerFishing();
            }

            if (!Core.DataStructures.PlayerBlacksmithing.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerBlacksmithing.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerBlacksmithing();
            }

            if (!Core.DataStructures.PlayerTailoring.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerTailoring.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerTailoring();
            }

            if (!Core.DataStructures.PlayerAlchemy.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerAlchemy.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerAlchemy();
            }

            if (!Core.DataStructures.PlayerHarvesting.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerHarvesting.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerHarvesting();
            }

            if (!Core.DataStructures.PlayerEnchanting.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerEnchanting.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerEnchanting();
            }
        }

        if (Plugin.ExpertiseSystem.Value)
        {

            if (!Core.DataStructures.PlayerSanguimancy.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerSanguimancy.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerSanguimancy();
            }

            if (!Core.DataStructures.PlayerSpells.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerSpells.Add(steamId, (0, 0, 0));
                Core.DataStructures.SavePlayerSpells();
            }


            if (!Core.DataStructures.PlayerSwordExpertise.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerSwordExpertise.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerSwordExpertise();
            }

            if (!Core.DataStructures.PlayerAxeExpertise.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerAxeExpertise.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerAxeExpertise();
            }

            if (!Core.DataStructures.PlayerMaceExpertise.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerMaceExpertise.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerMaceExpertise();
            }

            if (!Core.DataStructures.PlayerSpearExpertise.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerSpearExpertise.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerSpearExpertise();
            }

            if (!Core.DataStructures.PlayerCrossbowExpertise.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerCrossbowExpertise.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerCrossbowExpertise();
            }

            if (!Core.DataStructures.PlayerGreatSwordExpertise.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerGreatSwordExpertise.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerGreatSwordExpertise();
            }

            if (!Core.DataStructures.PlayerSlashersExpertise.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerSlashersExpertise.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerSlashersExpertise();
            }

            if (!Core.DataStructures.PlayerPistolsExpertise.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerPistolsExpertise.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerPistolsExpertise();
            }

            if (!Core.DataStructures.PlayerReaperExpertise.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerReaperExpertise.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerReaperExpertise();
            }

            if (!Core.DataStructures.PlayerLongbowExpertise.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerLongbowExpertise.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerLongbowExpertise();
            }

            if (!Core.DataStructures.PlayerWhipExpertise.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerWhipExpertise.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerWhipExpertise();
            }

            if (!Core.DataStructures.PlayerWeaponStats.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerWeaponStats.Add(steamId, []);
                Core.DataStructures.SavePlayerWeaponStats();
            }
        }

        if (Plugin.BloodSystem.Value)
        {
            if (!Core.DataStructures.PlayerWorkerLegacy.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerWorkerLegacy.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerWorkerLegacy();
            }

            if (!Core.DataStructures.PlayerWarriorLegacy.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerWarriorLegacy.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerWarriorLegacy();
            }

            if (!Core.DataStructures.PlayerScholarLegacy.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerScholarLegacy.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerScholarLegacy();
            }

            if (!Core.DataStructures.PlayerRogueLegacy.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerRogueLegacy.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerRogueLegacy();
            }

            if (!Core.DataStructures.PlayerMutantLegacy.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerMutantLegacy.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerMutantLegacy();
            }

            if (!Core.DataStructures.PlayerVBloodLegacy.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerVBloodLegacy.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerVBloodLegacy();
            }

            if (!Core.DataStructures.PlayerDraculinLegacy.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerDraculinLegacy.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerDraculinLegacy();
            }

            if (!Core.DataStructures.PlayerImmortalLegacy.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerImmortalLegacy.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerImmortalLegacy();
            }

            if (!Core.DataStructures.PlayerCreatureLegacy.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerCreatureLegacy.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerCreatureLegacy();
            }

            if (!Core.DataStructures.PlayerBruteLegacy.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerBruteLegacy.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerBruteLegacy();
            }
        }

        if (Plugin.LevelingSystem.Value)
        {
            if (!Core.DataStructures.PlayerExperience.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerExperience.Add(steamId, new KeyValuePair<int, float>(Plugin.StartingLevel.Value, LevelingSystem.ConvertLevelToXp(Plugin.StartingLevel.Value)));
                Core.DataStructures.SavePlayerExperience();
            }
            if (Plugin.PrestigeSystem.Value && !Core.DataStructures.PlayerPrestiges.ContainsKey(steamId))
            {
                var prestigeDict = new Dictionary<PrestigeSystem.PrestigeType, int>();
                foreach (var prestigeType in Enum.GetValues<PrestigeSystem.PrestigeType>())
                {
                    prestigeDict.Add(prestigeType, 0);
                }
                Core.DataStructures.PlayerPrestiges.Add(steamId, prestigeDict);
                Core.DataStructures.SavePlayerPrestiges();
            }
            if (Core.EntityManager.Exists(user.LocalCharacter._Entity)) GearOverride.SetLevel(user.LocalCharacter._Entity);
        }

        if (Plugin.FamiliarSystem.Value)
        {
            if (!Core.DataStructures.FamiliarActives.ContainsKey(steamId))
            {
                Core.DataStructures.FamiliarActives.Add(steamId, (Entity.Null, 0));
                Core.DataStructures.SavePlayerFamiliarActives();
            }
            if (!Core.DataStructures.FamiliarSet.ContainsKey(steamId))
            {
                Core.DataStructures.FamiliarSet.Add(steamId, "");
                Core.DataStructures.SavePlayerFamiliarSets();
            }
            Core.FamiliarExperienceManager.SaveFamiliarExperience(steamId, Core.FamiliarExperienceManager.LoadFamiliarExperience(steamId));
            Core.FamiliarUnlocksManager.SaveUnlockedFamiliars(steamId, Core.FamiliarUnlocksManager.LoadUnlockedFamiliars(steamId));
        }

        if (Plugin.SoftSynergies.Value || Plugin.HardSynergies.Value)
        {
            if (!Core.DataStructures.PlayerClasses.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerClasses.Add(steamId, []);
                Core.DataStructures.SavePlayerClasses();
            }
            if (!Core.DataStructures.PlayerSpells.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerSpells.Add(steamId, (0, 0, 0));
                Core.DataStructures.SavePlayerSpells();
            }
        }
    }
}