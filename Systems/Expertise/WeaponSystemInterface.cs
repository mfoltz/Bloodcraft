using Cobalt.Core;

namespace Cobalt.Systems.WeaponMastery
{
    public interface IWeaponMasteryHandler
    {
        void AddExperience(ulong steamID, float experience);

        void SaveChanges();

        KeyValuePair<int, float> GetExperienceData(ulong steamID);

        void UpdateExperienceData(ulong steamID, KeyValuePair<int, float> xpData);

        string GetWeaponType();
    }

    public static class WeaponMasteryHandlerFactory
    {
        public static IWeaponMasteryHandler GetWeaponMasteryHandler(string weaponType)
        {
            return weaponType.ToLower() switch
            {
                "sword" => new SwordMasteryHandler(),
                "axe" => new AxeMasteryHandler(),
                "mace" => new MaceMasteryHandler(),
                "spear" => new SpearMasteryHandler(),
                "crossbow" => new CrossbowMasteryHandler(),
                "greatsword" => new GreatSwordMasteryHandler(),
                "slashers" => new SlashersMasteryHandler(),
                "pistols" => new PistolsMasteryHandler(),
                "reaper" => new ReaperMasteryHandler(),
                "longbow" => new LongbowMasteryHandler(),
                "whip" => new WhipMasteryHandler(),
                "unarmed" => new UnarmedMasteryHandler(),
                _ => null,
            };
        }
    }

    public abstract class BaseWeaponMasteryHandler : IWeaponMasteryHandler
    {
        protected abstract IDictionary<ulong, KeyValuePair<int, float>> DataStructure { get; }

        public void AddExperience(ulong steamID, float experience)
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
            if (DataStructure.TryGetValue(steamID, out var xpData))
                return xpData;
            return new KeyValuePair<int, float>(0, 0);
        }

        public void UpdateExperienceData(ulong steamID, KeyValuePair<int, float> xpData)
        {
            DataStructure[steamID] = xpData;
        }

        public abstract void SaveChanges();

        public abstract string GetWeaponType();
    }

    // Implementations for each weapon type
    public class SwordMasteryHandler : BaseWeaponMasteryHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => DataStructures.PlayerSwordMastery;

        public override void SaveChanges()
        {
            DataStructures.SavePlayerSwordMastery();
        }

        public override string GetWeaponType()
        {
            return "Sword";
        }
    }

    public class AxeMasteryHandler : BaseWeaponMasteryHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => DataStructures.PlayerAxeMastery;

        public override void SaveChanges()
        {
            DataStructures.SavePlayerAxeMastery();
        }

        public override string GetWeaponType()
        {
            return "Axe";
        }
    }

    public class MaceMasteryHandler : BaseWeaponMasteryHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => DataStructures.PlayerMaceMastery;

        public override void SaveChanges()
        {
            DataStructures.SavePlayerMaceMastery();
        }

        public override string GetWeaponType()
        {
            return "Mace";
        }
    }

    public class SpearMasteryHandler : BaseWeaponMasteryHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => DataStructures.PlayerSpearMastery;

        public override void SaveChanges()
        {
            DataStructures.SavePlayerSpearMastery();
        }

        public override string GetWeaponType()
        {
            return "Spear";
        }
    }

    public class CrossbowMasteryHandler : BaseWeaponMasteryHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => DataStructures.PlayerCrossbowMastery;

        public override void SaveChanges()
        {
            DataStructures.SavePlayerCrossbowMastery();
        }

        public override string GetWeaponType()
        {
            return "Crossbow";
        }
    }

    public class GreatSwordMasteryHandler : BaseWeaponMasteryHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => DataStructures.PlayerGreatSwordMastery;

        public override void SaveChanges()
        {
            DataStructures.SavePlayerGreatSwordMastery();
        }

        public override string GetWeaponType()
        {
            return "GreatSword";
        }
    }

    public class SlashersMasteryHandler : BaseWeaponMasteryHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => DataStructures.PlayerSlashersMastery;

        public override void SaveChanges()
        {
            DataStructures.SavePlayerSlashersMastery();
        }

        public override string GetWeaponType()
        {
            return "Slashers";
        }
    }

    public class PistolsMasteryHandler : BaseWeaponMasteryHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => DataStructures.PlayerPistolsMastery;

        public override void SaveChanges()
        {
            DataStructures.SavePlayerPistolsMastery();
        }

        public override string GetWeaponType()
        {
            return "Pistols";
        }
    }

    public class ReaperMasteryHandler : BaseWeaponMasteryHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => DataStructures.PlayerReaperMastery;

        public override void SaveChanges()
        {
            DataStructures.SavePlayerReaperMastery();
        }

        public override string GetWeaponType()
        {
            return "Reaper";
        }
    }

    public class LongbowMasteryHandler : BaseWeaponMasteryHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => DataStructures.PlayerLongbowMastery;

        public override void SaveChanges()
        {
            DataStructures.SavePlayerLongbowMastery();
        }

        public override string GetWeaponType()
        {
            return "Longbow";
        }
    }

    public class WhipMasteryHandler : BaseWeaponMasteryHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => DataStructures.PlayerWhipMastery;

        public override void SaveChanges()
        {
            DataStructures.SavePlayerWhipMastery();
        }

        public override string GetWeaponType()
        {
            return "Whip";
        }
    }

    public class UnarmedMasteryHandler : BaseWeaponMasteryHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => DataStructures.PlayerUnarmedMastery;

        public override void SaveChanges()
        {
            DataStructures.SavePlayerUnarmedMastery();
        }

        public override string GetWeaponType()
        {
            return "Unarmed";
        }
    }
}