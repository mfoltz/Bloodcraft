using Bloodstone.API;
using Cobalt.Core;
using ProjectM;
using Unity.Entities;
using static Cobalt.Systems.ProfessionUtilities;
using User = ProjectM.Network.User;

namespace Cobalt.Systems
{
    public class ProfessionSystem
    {
        private static readonly float ProfessionMultiplier = 10; // multiplier for profession experience per harvest
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

            float ProfessionValue = Victim.Read<EntityCategory>().ResourceLevel;
            if (Victim.Read<UnitLevel>().Level > ProfessionValue)
            {
                ProfessionValue = Victim.Read<UnitLevel>().Level;
            }
            Plugin.Log.LogInfo($"{Victim.Read<EntityCategory>().ResourceLevel}|{Victim.Read<UnitLevel>().Level} || {prefabGUID.LookupName()}");
            if (ProfessionValue.Equals(0))
            {
                ProfessionValue = 10;
            }

            ProfessionValue = (int)(ProfessionValue * ProfessionMultiplier);

            IProfessionHandler handler = ProfessionHandlerFactory.GetProfessionHandler(prefabGUID);

            if (handler != null)
            {
                if (handler.GetProfessionName().ToLower().Contains("woodcutting"))
                {
                    ProfessionValue *= GetWoodcuttingModifier(prefabGUID);
                }

                SetProfession(user, SteamID, ProfessionValue, handler);
            }
            else
            {
                //Plugin.Log.LogError($"No handler found for profession...");
            }
        }

        public static void SetProfession(User user, ulong steamID, float value, IProfessionHandler handler)
        {
            EntityManager entityManager = VWorld.Server.EntityManager;

            handler.AddExperience(steamID, value);
            handler.SaveChanges();

            var xpData = handler.GetExperienceData(steamID);
            UpdatePlayerExperience(entityManager, user, steamID, xpData, value, handler);
        }

        private static void UpdatePlayerExperience(EntityManager entityManager, User user, ulong steamID, KeyValuePair<int, float> xpData, float gainedXP, IProfessionHandler handler)
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

        private static void NotifyPlayer(EntityManager entityManager, User user, ulong steamID, float gainedXP, bool leveledUp, IProfessionHandler handler)
        {
            gainedXP = (int)gainedXP;
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
        private static readonly Dictionary<string, int> FishingMultipliers = new()
        {
            { "farbane", 1 },
            { "dunley", 2 },
            { "gloomrot", 3 },
            { "cursed", 4 },
            { "silverlight", 4 }
        };

        private static readonly Dictionary<string, int> WoodcuttingMultipliers = new()
        {
            { "hallow", 2 },
            { "gloom", 3 },
            { "cursed", 4 }
        };

        private static readonly Dictionary<string, int> TierMultiplier = new()
        {
            { "t01", 1 },
            { "t02", 2 },
            { "t03", 3 },
            { "t04", 4 },
            { "t05", 5 },
            { "t06", 6 },
            { "t07", 7 },
            { "t08", 8 },
            { "t09", 9 },
        };

        public static int GetFishingModifier(PrefabGUID prefab)
        {
            foreach (KeyValuePair<string, int> location in FishingMultipliers)
            {
                if (prefab.LookupName().ToLower().Contains(location.Key))
                {
                    return location.Value;
                }
            }
            return 1;
        }

        public static int GetWoodcuttingModifier(PrefabGUID prefab)
        {
            foreach (KeyValuePair<string, int> location in WoodcuttingMultipliers)
            {
                if (prefab.LookupName().ToLower().Contains(location.Key))
                {
                    return location.Value;
                }
            }
            return 1;
        }

        public static int GetTierMultiplier(PrefabGUID prefab)
        {
            foreach (KeyValuePair<string, int> tier in TierMultiplier)
            {
                if (prefab.LookupName().ToLower().Contains(tier.Key))
                {
                    return tier.Value;
                }
            }
            return 1;
        }

        public interface IProfessionHandler
        {
            void AddExperience(ulong steamID, float experience);

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
                    default:
                        // Fall back to type checks for other professions
                        if (itemTypeName.Contains("wood"))
                            return new WoodcuttingHandler();
                        if (itemTypeName.Contains("mineral") || itemTypeName.Contains("stone") && !itemTypeName.Contains("magicsource"))
                            return new MiningHandler();
                        if (itemTypeName.Contains("weapon"))
                            return new BlacksmithingHandler();
                        if (itemTypeName.Contains("armor") || itemTypeName.Contains("cloak") || itemTypeName.Contains("bag"))
                            return new TailoringHandler();
                        if (itemTypeName.Contains("fishing"))
                            return new FishingHandler();
                        if (itemTypeName.Contains("canteen") || itemTypeName.Contains("potion") || itemTypeName.Contains("bottle") || itemTypeName.Contains("flask"))
                            return new AlchemyHandler();
                        if (itemTypeName.Contains("plant"))
                            return new HarvestingHandler();
                        if (itemTypeName.Contains("gem") || itemTypeName.Contains("jewel") || itemTypeName.Contains("magicsource"))
                            return new JewelcraftingHandler();
                        else
                            return null;
                }
            }
        }

        public abstract class BaseProfessionHandler : IProfessionHandler
        {
            // Abstract property to get the specific data structure for each profession.
            protected abstract IDictionary<ulong, KeyValuePair<int, float>> DataStructure { get; }

            public virtual void AddExperience(ulong steamID, float experience)
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

        public class JewelcraftingHandler : BaseProfessionHandler
        {
            // Override the DataStructure to provide the specific dictionary for woodcutting.
            protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => DataStructures.PlayerJewelcrafting;

            public override void SaveChanges()
            {
                DataStructures.SavePlayerJewelcrafting();
            }

            public override string GetProfessionName()
            {
                return "<color=#7E22CE>Jewelcrafting</color>";
            }
        }

        public class AlchemyHandler : BaseProfessionHandler
        {
            // Override the DataStructure to provide the specific dictionary for woodcutting.
            protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => DataStructures.PlayerAlchemy;

            public override void SaveChanges()
            {
                DataStructures.SavePlayerAlchemy();
            }

            public override string GetProfessionName()
            {
                return "<color=#12D4A2>Alchemy</color>";
            }
        }

        public class HarvestingHandler : BaseProfessionHandler
        {
            // Override the DataStructure to provide the specific dictionary for woodcutting.
            protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => DataStructures.PlayerHarvesting;

            public override void SaveChanges()
            {
                DataStructures.SavePlayerHarvesting();
            }

            public override string GetProfessionName()
            {
                return "<color=#008000>Harvesting</color>";
            }
        }

        public class BlacksmithingHandler : BaseProfessionHandler
        {
            // Override the DataStructure to provide the specific dictionary for woodcutting.
            protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => DataStructures.PlayerBlacksmithing;

            public override void SaveChanges()
            {
                DataStructures.SavePlayerBlacksmithing();
            }

            public override string GetProfessionName()
            {
                return "<color=#353641>Blacksmithing</color>";
            }
        }

        public class TailoringHandler : BaseProfessionHandler
        {
            // Override the DataStructure to provide the specific dictionary for woodcutting.
            protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => DataStructures.PlayerTailoring;

            public override void SaveChanges()
            {
                DataStructures.SavePlayerTailoring();
            }

            public override string GetProfessionName()
            {
                return "<color=#F9DEBD>Tailoring</color>";
            }
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