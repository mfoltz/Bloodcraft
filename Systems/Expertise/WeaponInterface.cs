namespace Bloodcraft.Systems.Expertise
{
    public interface IExpertiseHandler
    {
        void AddExpertise(ulong steamID, float experience);

        void SaveChanges();

        KeyValuePair<int, float> GetExpertiseData(ulong steamID);

        void UpdateExpertiseData(ulong steamID, KeyValuePair<int, float> xpData);

        ExpertiseSystem.WeaponType GetWeaponType();
    }

    public static class ExpertiseHandlerFactory
    {
        public static IExpertiseHandler GetExpertiseHandler(ExpertiseSystem.WeaponType weaponType)
        {
            return weaponType switch
            {
                ExpertiseSystem.WeaponType.Sword => new SwordHandler(),
                ExpertiseSystem.WeaponType.Axe => new AxeHandler(),
                ExpertiseSystem.WeaponType.Mace => new MaceHandler(),
                ExpertiseSystem.WeaponType.Spear => new SpearHandler(),
                ExpertiseSystem.WeaponType.Crossbow => new CrossbowHandler(),
                ExpertiseSystem.WeaponType.GreatSword => new GreatSwordHandler(),
                ExpertiseSystem.WeaponType.Slashers => new SlashersHandler(),
                ExpertiseSystem.WeaponType.Pistols => new PistolsHandler(),
                ExpertiseSystem.WeaponType.Reaper => new ReaperHandler(),
                ExpertiseSystem.WeaponType.Longbow => new LongbowHandler(),
                ExpertiseSystem.WeaponType.Whip => new WhipHandler(),
                ExpertiseSystem.WeaponType.Unarmed => new SanguimancyHandler(),
                _ => null,
            };
        }
    }

    public abstract class BaseExpertiseHandler : IExpertiseHandler
    {
        protected abstract IDictionary<ulong, KeyValuePair<int, float>> DataStructure { get; }

        public void AddExpertise(ulong steamID, float experience)
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

        public KeyValuePair<int, float> GetExpertiseData(ulong steamID)
        {
            if (DataStructure.TryGetValue(steamID, out var xpData))
                return xpData;
            return new KeyValuePair<int, float>(0, 0);
        }

        public void UpdateExpertiseData(ulong steamID, KeyValuePair<int, float> xpData)
        {
            DataStructure[steamID] = xpData;
        }

        public abstract void SaveChanges();

        public abstract ExpertiseSystem.WeaponType GetWeaponType();
    }

    // Implementations for each weapon type
    public class SwordHandler : BaseExpertiseHandler
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

    public class AxeHandler : BaseExpertiseHandler
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

    public class MaceHandler : BaseExpertiseHandler
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

    public class SpearHandler : BaseExpertiseHandler
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

    public class CrossbowHandler : BaseExpertiseHandler
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

    public class GreatSwordHandler : BaseExpertiseHandler
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

    public class SlashersHandler : BaseExpertiseHandler
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

    public class PistolsHandler : BaseExpertiseHandler
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

    public class ReaperHandler : BaseExpertiseHandler
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

    public class LongbowHandler : BaseExpertiseHandler
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

    public class WhipHandler : BaseExpertiseHandler
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

    public class SanguimancyHandler : BaseExpertiseHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerSanguimancy;

        public override void SaveChanges()
        {
            Core.DataStructures.SavePlayerSanguimancy();
        }

        public override ExpertiseSystem.WeaponType GetWeaponType()
        {
            return ExpertiseSystem.WeaponType.Unarmed;
        }
    }
}