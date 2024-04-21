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
        private static readonly float BloodlineMultiplier = 0.5f; // mastery points multiplier from normal units
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
            Health health = entityManager.GetComponentData<Health>(player);
            UpdateStats(player, stats, health, steamId, entityManager);
        }

        public static void UpdateStats(Entity player, UnitStats stats, Health health, ulong steamId, EntityManager entityManager)
        {
            if (!DataStructures.PlayerBloodlineStats.TryGetValue(steamId, out PlayerBloodlineStats bloodStats))
            {
                return; // No bloodline stats to check
            }

            UpdateStatIfIncreased(player, entityManager, ref health.MaxHealth, bloodStats.MaxHealth, health.MaxHealth._Value);
            UpdateStatIfIncreased(player, entityManager, ref stats.AttackSpeed, bloodStats.CastSpeed, stats.AttackSpeed._Value);
            UpdateStatIfIncreased(player, entityManager, ref stats.PrimaryAttackSpeed, bloodStats.AttackSpeed, stats.PrimaryAttackSpeed._Value);
            UpdateStatIfIncreased(player, entityManager, ref stats.PhysicalPower, bloodStats.PhysicalPower, stats.PhysicalPower._Value);
            UpdateStatIfIncreased(player, entityManager, ref stats.SpellPower, bloodStats.SpellPower, stats.SpellPower._Value);
            UpdateStatIfIncreased(player, entityManager, ref stats.PhysicalCriticalStrikeChance, bloodStats.PhysicalCritChance, stats.PhysicalCriticalStrikeChance._Value);
            UpdateStatIfIncreased(player, entityManager, ref stats.PhysicalCriticalStrikeDamage, bloodStats.PhysicalCritDamage, stats.PhysicalCriticalStrikeDamage._Value);
            UpdateStatIfIncreased(player, entityManager, ref stats.SpellCriticalStrikeChance, bloodStats.SpellCritChance, stats.SpellCriticalStrikeChance._Value);
            UpdateStatIfIncreased(player, entityManager, ref stats.SpellCriticalStrikeDamage, bloodStats.SpellCritDamage, stats.SpellCriticalStrikeDamage._Value);

            player.Write(stats); // Assuming there's at least one stat update
        }

        public static void UpdateStatIfIncreased(Entity player, EntityManager entityManager, ref ModifiableFloat currentStat, float masteryIncrease, float currentStatValue)
        {
            float newStatValue = currentStatValue + masteryIncrease;
            if (newStatValue > currentStat._Value)
            {
                currentStat = ModifiableFloat.Create(player, entityManager, newStatValue);
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