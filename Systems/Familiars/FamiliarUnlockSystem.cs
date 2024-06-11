using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using static Bloodcraft.Core;

namespace Bloodcraft.Systems.Familiars
{
    public class FamiliarUnlockSystem
    {
        private static readonly float UnitChance = Plugin.UnitUnlockChance.Value;
        private static readonly float VBloodChance = Plugin.VBloodUnlockChance.Value;
        static readonly bool allowVBloods = Plugin.AllowVBloods.Value;
        static readonly Random Random = new();

        // List of banned PrefabGUIDs
        public static List<int> ExemptPrefabs = new()
        {
            // Add banned PrefabGUID for this list in config
        };

        public static void HandleUnitUnlock(Entity killer, Entity died)
        {
            EntityCategory diedCategory = died.Read<EntityCategory>();
            PrefabGUID diedPrefab = died.Read<PrefabGUID>();
            string lowerName = diedPrefab.LookupName().ToLower();
            //Core.Log.LogInfo(lowerName);
            if (died.Has<Minion>()) return; // component checks
            if (lowerName.Contains("trader") || lowerName.Contains("carriage") || lowerName.Contains("horse") || lowerName.Contains("crystal") || lowerName.Contains("werewolf")) return; // prefab name checks
            if (IsBanned(diedPrefab)) return; // banned prefab checks, no using currently
            if ((int)diedCategory.UnitCategory < 5)
            {
                HandleRoll(UnitChance, died, killer);
            }
            else if (lowerName.Contains("vblood"))
            {
                if (allowVBloods) HandleRoll(VBloodChance, died, killer);
            }
        }
        static bool IsBanned(PrefabGUID prefab)
        {
            return ExemptPrefabs.Contains(prefab.GuidHash);
        }
        static void HandleRoll(float dropChance, Entity died, Entity killer)
        {
            if (RollForChance(dropChance)) HandleUnlock(died, killer);
        }
        static void HandleUnlock(Entity died, Entity player)
        {
            int familiarKey = died.Read<PrefabGUID>().GuidHash;
            User user = player.Read<PlayerCharacter>().UserEntity.Read<User>();
            ulong playerId = user.PlatformId;

            DataStructures.UnlockedFamiliarData data = FamiliarUnlocksManager.LoadUnlockedFamiliars(playerId);
            string lastListName = data.UnlockedFamiliars.Keys.LastOrDefault();

            if (string.IsNullOrEmpty(lastListName) || data.UnlockedFamiliars[lastListName].Count >= 10)
            {
                lastListName = $"fl{data.UnlockedFamiliars.Count + 1}";
                data.UnlockedFamiliars[lastListName] = [];
                if (Core.DataStructures.FamiliarSet[playerId] == "")
                {
                    Core.DataStructures.FamiliarSet[playerId] = lastListName;
                    Core.DataStructures.SavePlayerFamiliarSets();
                }
                
            }

            bool isAlreadyUnlocked = false;

            foreach (var list in data.UnlockedFamiliars.Values)
            {
                if (list.Contains(familiarKey))
                {
                    isAlreadyUnlocked = true;
                    break;
                }
            }

            if (!isAlreadyUnlocked)
            {
                // Assuming `lastListName` is the name of the list you want to add the familiar to
                List<int> currentList = data.UnlockedFamiliars[lastListName];
                currentList.Add(familiarKey);
                FamiliarUnlocksManager.SaveUnlockedFamiliars(playerId, data);

                var message = $"New unit unlocked: <color=green>{died.Read<PrefabGUID>().GetPrefabName()}</color>";
                ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, user, message);
            }
        }
        static bool RollForChance(float chance)
        {
            float roll = (float)Random.NextDouble();
            return roll < chance;
        }
    }
}