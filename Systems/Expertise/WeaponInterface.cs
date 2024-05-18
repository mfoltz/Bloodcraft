namespace Cobalt.Systems.Expertise
{
    public interface IWeaponExpertiseHandler
    {
        void AddExperience(ulong steamID, float experience);

        void SaveChanges();

        KeyValuePair<int, float> GetExperienceData(ulong steamID);

        void UpdateExperienceData(ulong steamID, KeyValuePair<int, float> xpData);

        string GetWeaponType();
    }

    public static class WeaponExpertiseHandlerFactory
    {
        public static IWeaponExpertiseHandler GetWeaponExpertiseHandler(string weaponType)
        {
            return weaponType.ToLower() switch
            {
                "sword" => new SwordExpertiseHandler(),
                "axe" => new AxeExpertiseHandler(),
                "mace" => new MaceExpertiseHandler(),
                "spear" => new SpearExpertiseHandler(),
                "crossbow" => new CrossbowExpertiseHandler(),
                "greatsword" => new GreatSwordExpertiseHandler(),
                "slashers" => new SlashersExpertiseHandler(),
                "pistols" => new PistolsExpertiseHandler(),
                "reaper" => new ReaperExpertiseHandler(),
                "longbow" => new LongbowExpertiseHandler(),
                "whip" => new WhipExpertiseHandler(),
                "unarmed" => new UnarmedExpertiseHandler(),
                _ => null,
            };
        }
    }

    public abstract class BaseWeaponExpertiseHandler : IWeaponExpertiseHandler
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
    public class SwordExpertiseHandler : BaseWeaponExpertiseHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerSwordExpertise;

        public override void SaveChanges()
        {
            Core.DataStructures.SavePlayerSwordExpertise();
        }

        public override string GetWeaponType()
        {
            return "Sword";
        }
    }

    public class AxeExpertiseHandler : BaseWeaponExpertiseHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerAxeExpertise;

        public override void SaveChanges()
        {
            Core.DataStructures.SavePlayerAxeExpertise();
        }

        public override string GetWeaponType()
        {
            return "Axe";
        }
    }

    public class MaceExpertiseHandler : BaseWeaponExpertiseHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerMaceExpertise;

        public override void SaveChanges()
        {
            Core.DataStructures.SavePlayerMaceExpertise();
        }

        public override string GetWeaponType()
        {
            return "Mace";
        }
    }

    public class SpearExpertiseHandler : BaseWeaponExpertiseHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerSpearExpertise;

        public override void SaveChanges()
        {
            Core.DataStructures.SavePlayerSpearExpertise();
        }

        public override string GetWeaponType()
        {
            return "Spear";
        }
    }

    public class CrossbowExpertiseHandler : BaseWeaponExpertiseHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerCrossbowExpertise;

        public override void SaveChanges()
        {
            Core.DataStructures.SavePlayerCrossbowExpertise();
        }

        public override string GetWeaponType()
        {
            return "Crossbow";
        }
    }

    public class GreatSwordExpertiseHandler : BaseWeaponExpertiseHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerGreatSwordExpertise;

        public override void SaveChanges()
        {
            Core.DataStructures.SavePlayerGreatSwordExpertise();
        }

        public override string GetWeaponType()
        {
            return "GreatSword";
        }
    }

    public class SlashersExpertiseHandler : BaseWeaponExpertiseHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerSlashersExpertise;

        public override void SaveChanges()
        {
            Core.DataStructures.SavePlayerSlashersExpertise();
        }

        public override string GetWeaponType()
        {
            return "Slashers";
        }
    }

    public class PistolsExpertiseHandler : BaseWeaponExpertiseHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerPistolsExpertise;

        public override void SaveChanges()
        {
            Core.DataStructures.SavePlayerPistolsExpertise();
        }

        public override string GetWeaponType()
        {
            return "Pistols";
        }
    }

    public class ReaperExpertiseHandler : BaseWeaponExpertiseHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerReaperExpertise;

        public override void SaveChanges()
        {
            Core.DataStructures.SavePlayerReaperExpertise();
        }

        public override string GetWeaponType()
        {
            return "Reaper";
        }
    }

    public class LongbowExpertiseHandler : BaseWeaponExpertiseHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerLongbowExpertise;

        public override void SaveChanges()
        {
            Core.DataStructures.SavePlayerLongbowExpertise();
        }

        public override string GetWeaponType()
        {
            return "Longbow";
        }
    }

    public class WhipExpertiseHandler : BaseWeaponExpertiseHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerWhipExpertise;

        public override void SaveChanges()
        {
            Core.DataStructures.SavePlayerWhipExpertise();
        }

        public override string GetWeaponType()
        {
            return "Whip";
        }
    }

    public class UnarmedExpertiseHandler : BaseWeaponExpertiseHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerUnarmedExpertise;

        public override void SaveChanges()
        {
            Core.DataStructures.SavePlayerUnarmedExpertise();
        }

        public override string GetWeaponType()
        {
            return "Unarmed";
        }
    }
}