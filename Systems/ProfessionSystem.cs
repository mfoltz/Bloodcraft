using Bloodstone.API;
using Cobalt.Core;
using ProjectM;
using ProjectM.Behaviours;
using ProjectM.Network;
using ProjectM.Sequencer;
using Steamworks;
using Unity.Entities;
using static Cobalt.Systems.ProfessionUtilities;
using static VCF.Core.Basics.RoleCommands;
using User = ProjectM.Network.User;

namespace Cobalt.Systems
{
    public class ProfessionSystem
    {
        private static readonly float ProfessionMultiplier = 1; // multiplier for profession experience per harvest
        private static readonly float ProfessionConstant = 0.1f; // constant for calculating level from xp
        private static readonly int ProfessionXPPower = 2; // power for calculating level from xp
        private static readonly int MaxProfessionLevel = 99; // maximum level

        public static void UpdateProfessions(Entity Killer, Entity Victim)
        {
            EntityManager entityManager = VWorld.Server.EntityManager;
            if (Killer == Victim) return;

            Entity userEntity = entityManager.GetComponentData<PlayerCharacter>(Killer).UserEntity;
            User user = entityManager.GetComponentData<User>(userEntity);
            ulong SteamID = user.PlatformId;
            if (!Victim.Has<UnitLevel>()) return;
            //var VictimLevel = entityManager.GetComponentData<UnitLevel>(Victim);

            PrefabGUID prefabGUID = new(0);
            if (entityManager.HasComponent<YieldResourcesOnDamageTaken>(Victim) && entityManager.HasComponent<EntityCategory>(Victim))
            {
                //Victim.LogComponentTypes();
                var yield = Victim.ReadBuffer<YieldResourcesOnDamageTaken>();
                if (yield.IsCreated && !yield.IsEmpty)
                {
                    prefabGUID = yield[0].ItemType;
                }
            }
            else
            {
                return;
            }

            int ProfessionValue = Victim.Read<EntityCategory>().ResourceLevel;
     
            Plugin.Log.LogInfo(ProfessionValue);

            ProfessionValue = (int)(ProfessionValue * ProfessionMultiplier);

            IProfessionHandler handler = ProfessionHandlerFactory.GetProfessionHandler(prefabGUID);

            if (handler != null)
            {
                SetProfession(user, SteamID, ProfessionValue, prefabGUID, handler);
            }
            else
            {
                //Plugin.Log.LogError($"No handler found for profession...");
            }
        }

        public static void SetProfession(User user, ulong steamID, int value, PrefabGUID prefabGUID, IProfessionHandler handler)
        {
            if (prefabGUID.GuidHash.Equals(0)) return;

            EntityManager entityManager = VWorld.Server.EntityManager;

            handler.AddExperience(steamID, value);
            handler.SaveChanges();

            var xpData = handler.GetExperienceData(steamID);
            UpdatePlayerExperience(entityManager, user, steamID, xpData, value, handler);
        }

        private static void UpdatePlayerExperience(EntityManager entityManager, User user, ulong steamID, KeyValuePair<int, float> xpData, int gainedXP, IProfessionHandler handler)
        {
            float newExperience = xpData.Value + gainedXP;
            int newLevel = ConvertXpToLevel(newExperience);
            bool leveledUp = false;

            if (newLevel > xpData.Key)
            {
                leveledUp = true;
                if (newLevel > MaxProfessionLevel)
                {
                    newLevel = MaxProfessionLevel;
                    newExperience = ConvertLevelToXp(MaxProfessionLevel);
                }
            }

            // Update the experience data with the new values
            var updatedXPData = new KeyValuePair<int, float>(newLevel, newExperience);
            handler.UpdateExperienceData(steamID, updatedXPData);

            // Notify player about the changes
            NotifyPlayer(entityManager, user, steamID, gainedXP, leveledUp, handler);
        }

        private static void NotifyPlayer(EntityManager entityManager, User user, ulong steamID, int gainedXP, bool leveledUp, IProfessionHandler handler)
        {
            string professionName = handler.GetProfessionName();
            if (leveledUp)
            {
                int newLevel = ConvertXpToLevel(handler.GetExperienceData(steamID).Value);
                ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"{professionName} improved to [<color=white>{newLevel}</color>]");
            }
            else
            {
                if (DataStructures.PlayerBools.TryGetValue(steamID, out var bools) && bools["ProfessionLogging"])
                {
                    int levelProgress = GetLevelProgress(steamID, handler);
                    ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"+<color=yellow>{gainedXP}</color> {professionName.ToLower()} (<color=white>{levelProgress}%</color>)");
                }
            }

            
        }

        private static int ConvertXpToLevel(float xp)
        {
            // Assuming a basic square root scaling for experience to level conversion
            return (int)(ProfessionConstant * Math.Sqrt(xp));
        }

        private static int ConvertLevelToXp(int level)
        {
            // Reversing the formula used in ConvertXpToLevel for consistency
            return (int)Math.Pow(level / ProfessionConstant, ProfessionXPPower);
        }

        private static float GetXp(ulong steamID, IProfessionHandler handler)
        {
            var xpData = handler.GetExperienceData(steamID);
            return xpData.Value;
        }

        private static int GetLevel(ulong steamID, IProfessionHandler handler)
        {
            return ConvertXpToLevel(GetXp(steamID, handler));
        }

        private static int GetLevelProgress(ulong steamID, IProfessionHandler handler)
        {
            float currentXP = GetXp(steamID, handler);
            int currentLevel = GetLevel(steamID, handler);
            int nextLevelXP = ConvertLevelToXp(currentLevel + 1);
            //Plugin.Log.LogInfo($"Lv: {currentLevel} | xp: {currentXP} | toNext: {nextLevelXP}");
            int percent = (int)(currentXP / nextLevelXP * 100);
            return percent;
        }
    }

    public class ProfessionUtilities
    {
        private static readonly Dictionary<string, int> FishingLocations = new()
        {
            { "farbane", 1 },
            { "dunley", 2 },
            { "gloomrot", 3 },
            { "cursed", 4 },
            { "silverlight", 4 }
        };

        public static int GetFishingModifier(PrefabGUID prefab)
        {
            foreach (KeyValuePair<string, int> location in FishingLocations)
            {
                if (prefab.LookupName().ToLower().Contains(location.Key))
                {
                    return location.Value;
                }
            }
            return 1;
        }

        public interface IProfessionHandler
        {
            void AddExperience(ulong steamID, int experience);

            void SaveChanges();

            KeyValuePair<int, float> GetExperienceData(ulong steamID);

            void UpdateExperienceData(ulong steamID, KeyValuePair<int, float> xpData);

            string GetProfessionName();
        }

        public static class ProfessionHandlerFactory
        {
            public static IProfessionHandler GetProfessionHandler(PrefabGUID prefabGUID, string context = "")
            {
                string itemTypeName = prefabGUID.LookupName().ToLower();

                // Check context to decide on a handler
                switch (context)
                {
                    case "fishing":
                        return new FishingHandler();

                    default:
                        // Fall back to type checks for other professions
                        if (itemTypeName.Contains("wood"))
                            return new WoodcuttingHandler();
                        if (itemTypeName.Contains("mineral") || itemTypeName.Contains("stone"))
                            return new MiningHandler();
                        else
                            return null;
                }
            }
        }

        public abstract class BaseProfessionHandler : IProfessionHandler
        {
            // Abstract property to get the specific data structure for each profession.
            protected abstract IDictionary<ulong, KeyValuePair<int, float>> DataStructure { get; }

            public virtual void AddExperience(ulong steamID, int experience)
            {
                if (DataStructure.TryGetValue(steamID, out var currentData))
                {
                    DataStructure[steamID] = new KeyValuePair<int, float>(currentData.Key, currentData.Value + experience);
                }
                else
                {
                    DataStructure.Add(steamID, new KeyValuePair<int, float>(0, experience));
                }
            }

            public KeyValuePair<int, float> GetExperienceData(ulong steamID)
            {
                return DataStructure[steamID];
            }

            public void UpdateExperienceData(ulong steamID, KeyValuePair<int, float> xpData)
            {
                DataStructure[steamID] = xpData;
            }

            // Abstract methods to be implemented by derived classes.
            public abstract void SaveChanges();

            public abstract string GetProfessionName();
        }

        public class WoodcuttingHandler : BaseProfessionHandler
        {
            // Override the DataStructure to provide the specific dictionary for woodcutting.
            protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => DataStructures.PlayerWoodcutting;

            public override void SaveChanges()
            {
                DataStructures.SavePlayerWoodcutting();
            }

            public override string GetProfessionName()
            {
                return "<color=#A52A2A>Woodcutting</color>";
            }
        }

        public class MiningHandler : BaseProfessionHandler
        {
            // Override the DataStructure to provide the specific dictionary for woodcutting.
            protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => DataStructures.PlayerMining;

            public override void SaveChanges()
            {
                DataStructures.SavePlayerMining();
            }

            public override string GetProfessionName()
            {
                return "<color=#808080>Mining</color>";
            }
        }

        public class FishingHandler : BaseProfessionHandler
        {
            // Override the DataStructure to provide the specific dictionary for woodcutting.
            protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => DataStructures.PlayerFishing;

            public override void SaveChanges()
            {
                DataStructures.SavePlayerFishing();
            }

            public override string GetProfessionName()
            {
                return "<color=#00FFFF>Fishing</color>";
            }
        }
    }
}