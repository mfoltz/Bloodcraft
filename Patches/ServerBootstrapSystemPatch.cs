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
                Core.DataStructures.PlayerSanguimancySpells.Add(steamId, (0, 0));
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

            if (!Core.DataStructures.PlayerWeaponStats.ContainsKey(steamId))
            {
                Core.DataStructures.PlayerWeaponStats.Add(steamId, []);
                Core.DataStructures.SavePlayerWeaponStats();
            }
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
            Entity character = user.LocalCharacter._Entity;
            if (!Plugin.LevelingSystem.Value)
            {
                if (InventoryUtilities.TryGetInventoryEntity(Core.EntityManager, character, out Entity playerInventory) && Core.ServerGameManager.TryGetBuffer<InventoryBuffer>(playerInventory, out var playerBuffer))
                {
                    foreach (var item in playerBuffer)
                    {
                        if (item.ItemEntity._Entity.Has<ArmorLevelSource>())
                        {
                            // restore armor levels
                            PrefabCollectionSystem prefabCollectionSystem = Core.PrefabCollectionSystem;
                            ArmorLevelSource armorLevelSource = prefabCollectionSystem._PrefabGuidToEntityMap[item.ItemType].Read<ArmorLevelSource>();
                            item.ItemEntity._Entity.Write(armorLevelSource);
                        }
                    }
                }
            }
            else
            {
                if (InventoryUtilities.TryGetInventoryEntity(Core.EntityManager, character, out Entity inventory) && Core.ServerGameManager.TryGetBuffer<InventoryBuffer>(inventory, out var buffer))
                {
                    foreach (var item in buffer)
                    {
                        if (item.ItemEntity._Entity.Has<ArmorLevelSource>() && !item.ItemEntity._Entity.Read<ArmorLevelSource>().Level.Equals(0))
                        {
                            item.ItemEntity._Entity.Write(new ArmorLevelSource { Level = 0 });
                        }
                    }
                }
            }
            if (!Plugin.ExpertiseSystem.Value)
            {
                if (InventoryUtilities.TryGetInventoryEntity(Core.EntityManager, character, out Entity playerInventory) && Core.ServerGameManager.TryGetBuffer<InventoryBuffer>(playerInventory, out var playerBuffer))
                {
                    foreach (var item in playerBuffer)
                    {
                        if (item.ItemEntity._Entity.Has<WeaponLevelSource>())
                        {
                            // restore weapon levels
                            PrefabCollectionSystem prefabCollectionSystem = Core.PrefabCollectionSystem;
                            WeaponLevelSource weaponLevelSource = prefabCollectionSystem._PrefabGuidToEntityMap[item.ItemType].Read<WeaponLevelSource>();
                            item.ItemEntity._Entity.Write(weaponLevelSource);
                        }
                    }
                }
            }
        }
    }
}