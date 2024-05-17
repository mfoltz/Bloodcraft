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
        [HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUserConnected))]
        [HarmonyPrefix]
        private static unsafe void OnUserConnectedPrefix(ServerBootstrapSystem __instance, NetConnectionId netConnectionId)
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
                { "ExperienceLogging", true },
                { "ExperienceShare", false },
                { "ProfessionLogging", true },
                { "BloodLogging", true },
                { "CombatLogging", true }
            });
                Core.DataStructures.SavePlayerBools();
            }

            if (!Core.DataStructures.PlayerExperience.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerExperience.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerExperience();
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
            if (!Core.DataStructures.PlayerSwordMastery.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerSwordMastery.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerSwordMastery();
            }
            if (!Core.DataStructures.PlayerAxeMastery.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerAxeMastery.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerAxeMastery();
            }
            if (!Core.DataStructures.PlayerMaceMastery.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerMaceMastery.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerMaceMastery();
            }
            if (!Core.DataStructures.PlayerSpearMastery.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerSpearMastery.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerSpearMastery();
            }
            if (!Core.DataStructures.PlayerCrossbowMastery.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerCrossbowMastery.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerCrossbowMastery();
            }
            if (!Core.DataStructures.PlayerGreatSwordMastery.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerGreatSwordMastery.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerGreatSwordMastery();
            }
            if (!Core.DataStructures.PlayerSlashersMastery.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerSlashersMastery.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerSlashersMastery();
            }
            if (!Core.DataStructures.PlayerPistolsMastery.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerPistolsMastery.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerPistolsMastery();
            }
            if (!Core.DataStructures.PlayerReaperMastery.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerReaperMastery.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerReaperMastery();
            }
            if (!Core.DataStructures.PlayerLongbowMastery.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerLongbowMastery.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerLongbowMastery();
            }
            if (!Core.DataStructures.PlayerWhipMastery.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerWhipMastery.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerWhipMastery();
            }
            if (!Core.DataStructures.PlayerUnarmedMastery.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerUnarmedMastery.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                Core.DataStructures.SavePlayerUnarmedMastery();
            }

            if (!Core.DataStructures.PlayerEquippedWeapon.ContainsKey(steamId))
            {
                var weapons = new Dictionary<string, bool>();
                foreach (ExpertiseSystem.WeaponType weaponType in Enum.GetValues(typeof(ExpertiseSystem.WeaponType)))
                {
                    weapons.Add(weaponType.ToString(), false);
                }
                Core.DataStructures.PlayerEquippedWeapon.Add(steamId, weapons);
                Core.DataStructures.SavePlayerEquippedWeapon();
            }

            if (!Core.DataStructures.PlayerWeaponChoices.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerWeaponChoices.Add(steamId, []);
                Core.DataStructures.SavePlayerWeaponChoices();
            }

            if (!Core.DataStructures.PlayerBloodChoices.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerBloodChoices.Add(steamId, []);
                Core.DataStructures.SavePlayerBloodChoices();
            }
            if (!Core.DataStructures.PlayerCraftingJobs.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerCraftingJobs.Add(steamId, []);
            }
        }
    }
}