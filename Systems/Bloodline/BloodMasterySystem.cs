using Bloodstone.API;
using Cobalt.Core;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using static Cobalt.Systems.Bloodline.BloodlineStatsSystem;

namespace Cobalt.Systems.Bloodline
{
    public class BloodMasterySystem
    {
        private static readonly float BloodlineMultiplier = 1f; // mastery points multiplier from normal units
        private static readonly int MaxBloodline = 10000; // maximum stored mastery points
        private static readonly float VBloodMultiplier = 10; // mastery points multiplier from VBlood units

        private static PrefabGUID vBloodType = new(1557174542);

        public static void UpdateBloodline(Entity Killer, Entity Victim)
        {
            EntityManager entityManager = VWorld.Server.EntityManager;
            if (Killer == Victim) return;
            if (entityManager.HasComponent<Minion>(Victim)) return;

            Entity userEntity = entityManager.GetComponentData<PlayerCharacter>(Killer).UserEntity;
            User User = entityManager.GetComponentData<User>(userEntity);
            ulong SteamID = User.PlatformId;

            var VictimStats = entityManager.GetComponentData<UnitStats>(Victim);

            bool isVBlood;
            if (entityManager.HasComponent<BloodConsumeSource>(Victim))
            {
                BloodConsumeSource BloodSource = entityManager.GetComponentData<BloodConsumeSource>(Victim);
                isVBlood = BloodSource.UnitBloodType.Equals(vBloodType);
            }
            else
            {
                isVBlood = false;
            }
            int BloodValue = (int)((VictimStats.SpellPower + VictimStats.PhysicalPower) / 4);
            if (isVBlood) BloodValue *= (int)VBloodMultiplier;

            BloodValue = (int)(BloodValue * BloodlineMultiplier);
            SetBlood(SteamID, BloodValue);

            if (DataStructures.PlayerBools.TryGetValue(SteamID, out var bools) && bools["MasteryLogging"])
            {
                ServerChatUtils.SendSystemMessageToClient(entityManager, User, $"You've gained <color=pink>{BloodValue}</color> bloodline experience.");
            }
            HandleUpdate(Killer, entityManager);
        }

        public static void HandleUpdate(Entity player, EntityManager entityManager)
        {
            if (!entityManager.HasComponent<PlayerCharacter>(player)) return;

            var userEntity = player.Read<PlayerCharacter>().UserEntity;
            var steamId = userEntity.Read<User>().PlatformId;

            UnitStats stats = entityManager.GetComponentData<UnitStats>(player);
            UpdateStats(player, stats, steamId, entityManager);
        }

        public static void UpdateStats(Entity player, UnitStats stats, ulong steamId, EntityManager entityManager)
        {
            if (!DataStructures.PlayerBloodlineStats.TryGetValue(steamId, out PlayerBloodlineStats bloodStats))
            {
                return; // No bloodline stats to check
            }

            UpdateStatIfIncreased(player, entityManager, ref stats.ResourceYieldModifier, bloodStats.ResourceYield, stats.ResourceYieldModifier._Value);
            UpdateStatIfIncreased(player, entityManager, ref stats.ReducedResourceDurabilityLoss, bloodStats.DurabilityLoss, stats.ReducedResourceDurabilityLoss._Value);
            UpdateStatIfIncreased(player, entityManager, ref stats.PhysicalResistance, bloodStats.PhysicalResistance, stats.PhysicalResistance._Value);
            UpdateStatIfIncreased(player, entityManager, ref stats.SpellResistance, bloodStats.SpellResistance, stats.SpellResistance._Value);
            UpdateStatIfIncreasedInt(player, entityManager, ref stats.SunResistance, bloodStats.SunResistance, stats.SunResistance._Value);
            UpdateStatIfIncreasedInt(player, entityManager, ref stats.FireResistance, bloodStats.FireResistance, stats.FireResistance._Value);
            UpdateStatIfIncreasedInt(player, entityManager, ref stats.HolyResistance, bloodStats.HolyResistance, stats.HolyResistance._Value);
            UpdateStatIfIncreasedInt(player, entityManager, ref stats.SilverResistance, bloodStats.SilverResistance, stats.SilverResistance._Value);
            UpdateStatIfIncreased(player, entityManager, ref stats.PassiveHealthRegen, bloodStats.PassiveHealthRegene, stats.PassiveHealthRegen._Value);

            player.Write(stats); // Assuming there's at least one stat update
        }

        public static void UpdateStatIfIncreased(Entity player, EntityManager entityManager, ref ModifiableFloat currentStat, float bloodIncrease, float currentStatValue)
        {
            float newStatValue = currentStatValue + bloodIncrease;
            if (newStatValue > currentStat._Value)
            {
                currentStat._Value = newStatValue;
            }
        }
        public static void UpdateStatIfIncreasedInt(Entity player, EntityManager entityManager, ref ModifiableInt currentStat, float bloodIncrease, int currentStatValue)
        {
            int newStatValue = (int)(currentStatValue + bloodIncrease);
            if (newStatValue > currentStat._Value)
            {
                currentStat._Value = newStatValue;
            }
        }

        public static void SetBlood(ulong SteamID, int Value)
        {
            bool isPlayerFound = DataStructures.PlayerMastery.TryGetValue(SteamID, out var Mastery);
            if (isPlayerFound)
            {
                if (Value + Mastery.Key > MaxBloodline)
                {
                    KeyValuePair<int, float> BloodMastery = new(MaxBloodline, 0f);
                    DataStructures.PlayerMastery[SteamID] = BloodMastery;
                }
                else
                {
                    KeyValuePair<int, float> BloodMastery = new(Value + Mastery.Key, 0f);
                    DataStructures.PlayerMastery[SteamID] = BloodMastery;
                }
            }
            else
            {
                KeyValuePair<int, float> BloodMastery = new(Value, 0f);
                DataStructures.PlayerMastery.Add(SteamID, BloodMastery);
            }

            DataStructures.SavePlayerBloodLine();
        }
    }
}