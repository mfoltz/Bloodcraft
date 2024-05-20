using Cobalt.Systems.Expertise;
using HarmonyLib;
using ProjectM;
using Stunlock.Network;
using Unity.Entities;
using User = ProjectM.Network.User;

namespace Cobalt.Hooks
{
    [HarmonyPatch]
    public class ServerBootstrapPatches
    {
        public static List<User> users = [];

        [HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUserConnected))]
        [HarmonyPrefix]
        private static unsafe void OnUserConnectedPrefix(ServerBootstrapSystem __instance, NetConnectionId netConnectionId)
        {
            int userIndex = __instance._NetEndPointToApprovedUserIndex[netConnectionId];
            ServerBootstrapSystem.ServerClient serverClient = __instance._ApprovedUsersLookup[userIndex];
            Entity userEntity = serverClient.UserEntity;
            User user = __instance.EntityManager.GetComponentData<User>(userEntity);
            ulong steamId = user.PlatformId;

            if (!users.Contains(user))
            {
                users.Add(user);
            }

            if (!Core.DataStructures.PlayerBools.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerBools.Add(steamId, new Dictionary<string, bool>
            {
                { "ExperienceLogging", true },
                { "ProfessionLogging", true },
                { "SanguimancyLogging", true },
                { "ExpertiseLogging", true },
                { "BloodLogging", true },
                { "SpellLock", false }
            });
                Core.DataStructures.SavePlayerBools();
            }

            if (!Core.DataStructures.PlayerExperience.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerExperience.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerExperience();
                GearOverride.SetLevel(user.LocalCharacter._Entity);
            }
            if (!Core.DataStructures.PlayerPrestige.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerPrestige.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerPrestige();
            }

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
            if (!Core.DataStructures.PlayerJewelcrafting.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerJewelcrafting.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerJewelcrafting();
            }
            if (!Core.DataStructures.PlayerSanguimancy.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerSanguimancy.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerSanguimancy();
            }
            if (!Core.DataStructures.PlayerSanguimancySpells.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerSanguimancySpells.Add(steamId, (new(0), new(0)));
                Core.DataStructures.SavePlayerSanguimancySpells();
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

            if (!Core.DataStructures.PlayerWeaponChoices.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerWeaponChoices.Add(steamId, []);
                Core.DataStructures.SavePlayerWeaponChoices();
            }
        }
    }
}