using Stunlock.Core;

namespace Cobalt.Systems.Professions
{
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
                case "woodcutting":
                    return new WoodcuttingHandler();

                case "mining":
                    return new MiningHandler();

                case "blacksmithing":
                    return new BlacksmithingHandler();

                case "tailoring":
                    return new TailoringHandler();

                case "fishing":
                    return new FishingHandler();

                case "alchemy":
                    return new AlchemyHandler();

                case "harvesting":
                    return new HarvestingHandler();

                case "jewelcrafting":
                    return new JewelcraftingHandler();

                default:
                    if (itemTypeName.Contains("wood"))
                        return new WoodcuttingHandler();
                    if (itemTypeName.Contains("mineral") || itemTypeName.Contains("stone") && !itemTypeName.Contains("magicsource"))
                        return new MiningHandler();
                    if (itemTypeName.Contains("weapon"))
                        return new BlacksmithingHandler();
                    if (itemTypeName.Contains("armor") || itemTypeName.Contains("cloak") || itemTypeName.Contains("bag"))
                        return new TailoringHandler();
                    if (itemTypeName.Contains("fish"))
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