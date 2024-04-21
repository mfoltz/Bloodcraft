using Bloodstone.API;
using Cobalt.Core;
using KindredCommands;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using static Cobalt.Systems.WeaponStatsSystem;

namespace Cobalt.Systems
{
    public class ArmsSystem
    {
        private static readonly float MasteryMultiplier = 5; // mastery points multiplier from normal units
        private static readonly int MaxMastery = 1000; // maximum stored mastery points
        private static readonly float VBloodMultiplier = 10; // mastery points multiplier from VBlood units

        private static PrefabGUID vBloodType = new(1557174542);

        public static void UpdateMastery(Entity Killer, Entity Victim)
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
            int MasteryValue = (int)((VictimStats.SpellPower + VictimStats.PhysicalPower) / 2);
            if (isVBlood) MasteryValue *= (int)VBloodMultiplier;

            MasteryValue = (int)(MasteryValue * MasteryMultiplier);
            SetMastery(SteamID, MasteryValue);

            if (DataStructures.PlayerBools.TryGetValue(SteamID, out var bools) && bools["MasteryLogging"])
            {
                ServerChatUtils.SendSystemMessageToClient(entityManager, User, $"You've gained <color=white>{MasteryValue}</color> points!");
            }
            HandleUpdate(Killer, entityManager);
        }

        public static void HandleUpdate(Entity player, EntityManager entityManager)
        {
            var Owner = entityManager.GetComponentData<EntityOwner>(player).Owner;
            if (!entityManager.HasComponent<PlayerCharacter>(Owner)) return;

            var userEntity = entityManager.GetComponentData<PlayerCharacter>(Owner).UserEntity;
            var steamId = entityManager.GetComponentData<User>(userEntity).PlatformId;

            UnitStats stats = entityManager.GetComponentData<UnitStats>(player);
            Health health = entityManager.GetComponentData<Health>(player);
            UpdateStats(player, stats, health, steamId, entityManager);
        }

        public static void UpdateStats(Entity player, UnitStats stats, Health health, ulong steamId, EntityManager entityManager)
        {
            if (!DataStructures.PlayerStats.TryGetValue(steamId, out PlayerStats masteryStats))
            {
                return; // No mastery stats to check
            }

            UpdateStatIfIncreased(player, entityManager, ref health.MaxHealth, masteryStats.MaxHealth, health.MaxHealth._Value);
            UpdateStatIfIncreased(player, entityManager, ref stats.AttackSpeed, masteryStats.CastSpeed, stats.AttackSpeed._Value);
            UpdateStatIfIncreased(player, entityManager, ref stats.PrimaryAttackSpeed, masteryStats.AttackSpeed, stats.PrimaryAttackSpeed._Value);
            UpdateStatIfIncreased(player, entityManager, ref stats.PhysicalPower, masteryStats.PhysicalPower, stats.PhysicalPower._Value);
            UpdateStatIfIncreased(player, entityManager, ref stats.SpellPower, masteryStats.SpellPower, stats.SpellPower._Value);
            UpdateStatIfIncreased(player, entityManager, ref stats.PhysicalCriticalStrikeChance, masteryStats.PhysicalCritChance, stats.PhysicalCriticalStrikeChance._Value);
            UpdateStatIfIncreased(player, entityManager, ref stats.PhysicalCriticalStrikeDamage, masteryStats.PhysicalCritDamage, stats.PhysicalCriticalStrikeDamage._Value);
            UpdateStatIfIncreased(player, entityManager, ref stats.SpellCriticalStrikeChance, masteryStats.SpellCritChance, stats.SpellCriticalStrikeChance._Value);
            UpdateStatIfIncreased(player, entityManager, ref stats.SpellCriticalStrikeDamage, masteryStats.SpellCritDamage, stats.SpellCriticalStrikeDamage._Value);

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

        public static void SetMastery(ulong SteamID, int Value)
        {
            bool isPlayerFound = DataStructures.PlayerMastery.TryGetValue(SteamID, out var Mastery);
            if (isPlayerFound)
            {
                if (Value + Mastery.Key > MaxMastery)
                {
                    KeyValuePair<int, DateTime> WeaponMastery = new(MaxMastery, DateTime.Now);
                    DataStructures.PlayerMastery[SteamID] = WeaponMastery;
                }
                else
                {
                    KeyValuePair<int, DateTime> WeaponMastery = new(Value + Mastery.Key, DateTime.Now);
                    DataStructures.PlayerMastery[SteamID] = WeaponMastery;
                }
            }
            else
            {
                KeyValuePair<int, DateTime> WeaponMastery = new(Value, DateTime.Now);
                DataStructures.PlayerMastery.Add(SteamID, WeaponMastery);
            }

            DataStructures.SavePlayerMastery();
        }
    }
}