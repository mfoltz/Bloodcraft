namespace Cobalt.Systems.Expertise
{
    public interface IWeaponExpertiseHandler
    {
        void AddExperience(ulong steamID, float experience);

        void SaveChanges();

        KeyValuePair<int, float> GetExperienceData(ulong steamID);

        void UpdateExperienceData(ulong steamID, KeyValuePair<int, float> xpData);

        ExpertiseSystem.WeaponType GetWeaponType();
    }

    public static class WeaponExpertiseHandlerFactory
    {
        public static IWeaponExpertiseHandler GetWeaponExpertiseHandler(ExpertiseSystem.WeaponType weaponType)
        {
            return weaponType switch
            {
                ExpertiseSystem.WeaponType.Sword => new SwordExpertiseHandler(),
                ExpertiseSystem.WeaponType.Axe => new AxeExpertiseHandler(),
                ExpertiseSystem.WeaponType.Mace => new MaceExpertiseHandler(),
                ExpertiseSystem.WeaponType.Spear => new SpearExpertiseHandler(),
                ExpertiseSystem.WeaponType.Crossbow => new CrossbowExpertiseHandler(),
                ExpertiseSystem.WeaponType.GreatSword => new GreatSwordExpertiseHandler(),
                ExpertiseSystem.WeaponType.Slashers => new SlashersExpertiseHandler(),
                ExpertiseSystem.WeaponType.Pistols => new PistolsExpertiseHandler(),
                ExpertiseSystem.WeaponType.Reaper => new ReaperExpertiseHandler(),
                ExpertiseSystem.WeaponType.Longbow => new LongbowExpertiseHandler(),
                ExpertiseSystem.WeaponType.Whip => new WhipExpertiseHandler(),
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

        public abstract ExpertiseSystem.WeaponType GetWeaponType();
    }

    // Implementations for each weapon type
    public class SwordExpertiseHandler : BaseWeaponExpertiseHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerSwordExpertise;

        public override void SaveChanges()
        {
            Core.DataStructures.SavePlayerSwordExpertise();
        }

        public override ExpertiseSystem.WeaponType GetWeaponType()
        {
            return ExpertiseSystem.WeaponType.Sword;
        }
    }

    public class AxeExpertiseHandler : BaseWeaponExpertiseHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerAxeExpertise;

        public override void SaveChanges()
        {
            Core.DataStructures.SavePlayerAxeExpertise();
        }

        public override ExpertiseSystem.WeaponType GetWeaponType()
        {
            return ExpertiseSystem.WeaponType.Axe;
        }
    }

    public class MaceExpertiseHandler : BaseWeaponExpertiseHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerMaceExpertise;

        public override void SaveChanges()
        {
            Core.DataStructures.SavePlayerMaceExpertise();
        }

        public override ExpertiseSystem.WeaponType GetWeaponType()
        {
            return ExpertiseSystem.WeaponType.Mace;
        }
    }

    public class SpearExpertiseHandler : BaseWeaponExpertiseHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerSpearExpertise;

        public override void SaveChanges()
        {
            Core.DataStructures.SavePlayerSpearExpertise();
        }

        public override ExpertiseSystem.WeaponType GetWeaponType()
        {
            return ExpertiseSystem.WeaponType.Spear;
        }
    }

    public class CrossbowExpertiseHandler : BaseWeaponExpertiseHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerCrossbowExpertise;

        public override void SaveChanges()
        {
            Core.DataStructures.SavePlayerCrossbowExpertise();
        }

        public override ExpertiseSystem.WeaponType GetWeaponType()
        {
            return ExpertiseSystem.WeaponType.Crossbow;
        }
    }

    public class GreatSwordExpertiseHandler : BaseWeaponExpertiseHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerGreatSwordExpertise;

        public override void SaveChanges()
        {
            Core.DataStructures.SavePlayerGreatSwordExpertise();
        }

        public override ExpertiseSystem.WeaponType GetWeaponType()
        {
            return ExpertiseSystem.WeaponType.GreatSword;
        }
    }

    public class SlashersExpertiseHandler : BaseWeaponExpertiseHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerSlashersExpertise;

        public override void SaveChanges()
        {
            Core.DataStructures.SavePlayerSlashersExpertise();
        }

        public override ExpertiseSystem.WeaponType GetWeaponType()
        {
            return ExpertiseSystem.WeaponType.Slashers;
        }
    }

    public class PistolsExpertiseHandler : BaseWeaponExpertiseHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerPistolsExpertise;

        public override void SaveChanges()
        {
            Core.DataStructures.SavePlayerPistolsExpertise();
        }

        public override ExpertiseSystem.WeaponType GetWeaponType()
        {
            return ExpertiseSystem.WeaponType.Pistols;
        }
    }

    public class ReaperExpertiseHandler : BaseWeaponExpertiseHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerReaperExpertise;

        public override void SaveChanges()
        {
            Core.DataStructures.SavePlayerReaperExpertise();
        }

        public override ExpertiseSystem.WeaponType GetWeaponType()
        {
            return ExpertiseSystem.WeaponType.Reaper;
        }
    }

    public class LongbowExpertiseHandler : BaseWeaponExpertiseHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerLongbowExpertise;

        public override void SaveChanges()
        {
            Core.DataStructures.SavePlayerLongbowExpertise();
        }

        public override ExpertiseSystem.WeaponType GetWeaponType()
        {
            return ExpertiseSystem.WeaponType.Longbow;
        }
    }

    public class WhipExpertiseHandler : BaseWeaponExpertiseHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerWhipExpertise;

        public override void SaveChanges()
        {
            Core.DataStructures.SavePlayerWhipExpertise();
        }

        public override ExpertiseSystem.WeaponType GetWeaponType()
        {
            return ExpertiseSystem.WeaponType.Whip;
        }
    }
}