namespace Bloodcraft.Systems.Legacy
{
    public interface IBloodHandler
    {
        void AddLegacy(ulong steamID, float experience);

        void SaveChanges();

        KeyValuePair<int, float> GetLegacyData(ulong steamID);

        void UpdateLegacyData(ulong steamID, KeyValuePair<int, float> xpData);

        BloodSystem.BloodType GetBloodType();
    }

    public static class BloodHandlerFactory
    {
        public static IBloodHandler GetBloodHandler(BloodSystem.BloodType bloodType)
        {
            return bloodType switch
            {
                BloodSystem.BloodType.Worker => new WorkerHandler(),
                BloodSystem.BloodType.Warrior => new WarriorHandler(),
                BloodSystem.BloodType.Scholar => new ScholarHandler(),
                BloodSystem.BloodType.Rogue => new RogueHandler(),
                BloodSystem.BloodType.Mutant => new MutantHandler(),
                BloodSystem.BloodType.VBlood => new VBloodHandler(),
                BloodSystem.BloodType.Draculin => new DraculinHandler(),
                BloodSystem.BloodType.DraculaTheImmortal => new ImmortalHandler(),
                BloodSystem.BloodType.Creature => new CreatureHandler(),
                BloodSystem.BloodType.Brute => new BruteHandler(),
                _ => null,
            };
        }
    }

    public abstract class BaseBloodHandler : IBloodHandler
    {
        protected abstract IDictionary<ulong, KeyValuePair<int, float>> DataStructure { get; }

        public void AddLegacy(ulong steamID, float experience)
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

        public KeyValuePair<int, float> GetLegacyData(ulong steamID)
        {
            if (DataStructure.TryGetValue(steamID, out var xpData))
                return xpData;
            return new KeyValuePair<int, float>(0, 0);
        }

        public void UpdateLegacyData(ulong steamID, KeyValuePair<int, float> xpData)
        {
            DataStructure[steamID] = xpData;
        }

        public abstract void SaveChanges();

        public abstract BloodSystem.BloodType GetBloodType();
    }

    // Implementations for each weapon type
    public class WorkerHandler : BaseBloodHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerWorkerLegacy;

        public override void SaveChanges()
        {
            Core.DataStructures.SavePlayerWorkerLegacy();
        }

        public override BloodSystem.BloodType GetBloodType()
        {
            return BloodSystem.BloodType.Worker;
        }
    }

    public class WarriorHandler : BaseBloodHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerWarriorLegacy;

        public override void SaveChanges()
        {
            Core.DataStructures.SavePlayerWarriorLegacy();
        }

        public override BloodSystem.BloodType GetBloodType()
        {
            return BloodSystem.BloodType.Warrior;
        }
    }

    public class ScholarHandler : BaseBloodHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerScholarLegacy;

        public override void SaveChanges()
        {
            Core.DataStructures.SavePlayerScholarLegacy();
        }

        public override BloodSystem.BloodType GetBloodType()
        {
            return BloodSystem.BloodType.Scholar;
        }
    }

    public class RogueHandler : BaseBloodHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerRogueLegacy;

        public override void SaveChanges()
        {
            Core.DataStructures.SavePlayerRogueLegacy();
        }

        public override BloodSystem.BloodType GetBloodType()
        {
            return BloodSystem.BloodType.Rogue;
        }
    }

    public class MutantHandler : BaseBloodHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerMutantLegacy;

        public override void SaveChanges()
        {
            Core.DataStructures.SavePlayerMutantLegacy();
        }

        public override BloodSystem.BloodType GetBloodType()
        {
            return BloodSystem.BloodType.Mutant;
        }
    }

    public class VBloodHandler : BaseBloodHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerVBloodLegacy;

        public override void SaveChanges()
        {
            Core.DataStructures.SavePlayerVBloodLegacy();
        }

        public override BloodSystem.BloodType GetBloodType()
        {
            return BloodSystem.BloodType.VBlood;
        }
    }

    public class DraculinHandler : BaseBloodHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerDraculinLegacy;

        public override void SaveChanges()
        {
            Core.DataStructures.SavePlayerDraculinLegacy();
        }

        public override BloodSystem.BloodType GetBloodType()
        {
            return BloodSystem.BloodType.Draculin;
        }
    }

    public class ImmortalHandler : BaseBloodHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerImmortalLegacy;

        public override void SaveChanges()
        {
            Core.DataStructures.SavePlayerImmortalLegacy();
        }

        public override BloodSystem.BloodType GetBloodType()
        {
            return BloodSystem.BloodType.DraculaTheImmortal;
        }
    }

    public class CreatureHandler : BaseBloodHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerCreatureLegacy;

        public override void SaveChanges()
        {
            Core.DataStructures.SavePlayerCreatureLegacy();
        }

        public override BloodSystem.BloodType GetBloodType()
        {
            return BloodSystem.BloodType.Creature;
        }
    }

    public class BruteHandler : BaseBloodHandler
    {
        protected override IDictionary<ulong, KeyValuePair<int, float>> DataStructure => Core.DataStructures.PlayerBruteLegacy;

        public override void SaveChanges()
        {
            Core.DataStructures.SavePlayerBruteLegacy();
        }

        public override BloodSystem.BloodType GetBloodType()
        {
            return BloodSystem.BloodType.Brute;
        }
    }
}