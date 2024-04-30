using Bloodstone.API;
using Cobalt.Core;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using static Cobalt.Systems.Bloodline.BloodMasteryStatsSystem;

namespace Cobalt.Systems.Bloodline
{
    public class BloodMasterySystem
    {
        private static readonly float BloodMasteryMultiplier = 1f; // mastery points multiplier from normal units
        private static readonly float BloodValueModifier = 1f;
        private static readonly float BaseBloodMastery = 5; // base mastery points
        private static readonly int MaxBloodMastery = 10000; // maximum stored mastery points
        private static readonly float VBloodMultiplier = 10; // mastery points multiplier from VBlood units

        public static void UpdateBloodMastery(Entity Killer, Entity Victim)
        {
            EntityManager entityManager = VWorld.Server.EntityManager;
            if (Killer == Victim) return;
            if (entityManager.HasComponent<Minion>(Victim) || !Victim.Has<BloodConsumeSource>()) return;

            BloodConsumeSource bloodConsumeSource = Victim.Read<BloodConsumeSource>();
            Entity userEntity = entityManager.GetComponentData<PlayerCharacter>(Killer).UserEntity;
            User User = entityManager.GetComponentData<User>(userEntity);
            ulong SteamID = User.PlatformId;

            //var VictimStats = entityManager.GetComponentData<UnitStats>(Victim);

            bool isVBlood;
            if (entityManager.HasComponent<VBloodConsumeSource>(Victim))
            {
                isVBlood = true;
            }
            else
            {
                isVBlood = false;
            }
            float BloodMasteryValue = bloodConsumeSource.BloodQuality / BloodValueModifier;
            if (isVBlood) BloodMasteryValue *= VBloodMultiplier;

            BloodMasteryValue *= BloodMasteryMultiplier;
            if (BloodMasteryValue.Equals(0) || BloodMasteryValue < 5)
            {
                BloodMasteryValue += BaseBloodMastery;
            }
            SetBloodMastery(SteamID, BloodMasteryValue);
            BloodMasteryValue = (int)BloodMasteryValue;
            if (DataStructures.PlayerBools.TryGetValue(SteamID, out var bools) && bools["BloodLogging"])
            {
                ServerChatUtils.SendSystemMessageToClient(entityManager, User, $"+<color=yellow>{BloodMasteryValue}</color> <color=red>sanguimancy</color>");
            }
            HandleUpdate(Killer, entityManager);
        }

        public static void HandleUpdate(Entity player, EntityManager entityManager)
        {
            if (!entityManager.HasComponent<PlayerCharacter>(player))
            {
                Plugin.Log.LogInfo("No player character found for stats modifying...");
                return;
            }

            var userEntity = player.Read<PlayerCharacter>().UserEntity;
            var steamId = userEntity.Read<User>().PlatformId;

            UnitStats stats = entityManager.GetComponentData<UnitStats>(player);
            UpdateStats(player, stats, steamId, entityManager);
        }

        public static void UpdateStats(Entity player, UnitStats stats, ulong steamId, EntityManager entityManager)
        {
            if (!DataStructures.PlayerBloodStats.TryGetValue(steamId, out BloodMasteryStats bloodStats))
            {
                return; // No bloodline stats to check
            }

            // Check each stat if it's been chosen and update it accordingly
            if (bloodStats.ChosenStats.Contains(BloodMasteryStatManager.BloodFocusSystem.BloodStatType.ResourceYield))
                UpdateStatIfIncreased(ref stats.ResourceYieldModifier, bloodStats.ResourceYield, stats.ResourceYieldModifier._Value);
            if (bloodStats.ChosenStats.Contains(BloodMasteryStatManager.BloodFocusSystem.BloodStatType.PhysicalResistance))
                UpdateStatIfIncreased(ref stats.PhysicalResistance, bloodStats.PhysicalResistance, stats.PhysicalResistance._Value);
            if (bloodStats.ChosenStats.Contains(BloodMasteryStatManager.BloodFocusSystem.BloodStatType.SpellResistance))
                UpdateStatIfIncreased(ref stats.SpellResistance, bloodStats.SpellResistance, stats.SpellResistance._Value);
            if (bloodStats.ChosenStats.Contains(BloodMasteryStatManager.BloodFocusSystem.BloodStatType.SunResistance))
                UpdateStatIfIncreasedInt(ref stats.SunResistance, bloodStats.SunResistance, stats.SunResistance._Value);
            if (bloodStats.ChosenStats.Contains(BloodMasteryStatManager.BloodFocusSystem.BloodStatType.FireResistance))
                UpdateStatIfIncreasedInt(ref stats.FireResistance, bloodStats.FireResistance, stats.FireResistance._Value);
            if (bloodStats.ChosenStats.Contains(BloodMasteryStatManager.BloodFocusSystem.BloodStatType.HolyResistance))
                UpdateStatIfIncreasedInt(ref stats.HolyResistance, bloodStats.HolyResistance, stats.HolyResistance._Value);
            if (bloodStats.ChosenStats.Contains(BloodMasteryStatManager.BloodFocusSystem.BloodStatType.SilverResistance))
                UpdateStatIfIncreasedInt(ref stats.SilverResistance, bloodStats.SilverResistance, stats.SilverResistance._Value);
            if (bloodStats.ChosenStats.Contains(BloodMasteryStatManager.BloodFocusSystem.BloodStatType.PassiveHealthRegen))
                UpdateStatIfIncreased(ref stats.PassiveHealthRegen, bloodStats.PassiveHealthRegene, stats.PassiveHealthRegen._Value);

            player.Write(stats); // Assuming there's at least one stat update
        }

        public static void UpdateStatIfIncreased(ref ModifiableFloat currentStat, float bloodIncrease, float currentStatValue)
        {
            float newStatValue = currentStatValue + bloodIncrease;
            if (newStatValue > currentStat._Value)
            {
                currentStat._Value = newStatValue;
            }
        }
        public static void UpdateStatIfIncreasedInt(ref ModifiableInt currentStat, float bloodIncrease, int currentStatValue)
        {
            int newStatValue = (int)(currentStatValue + bloodIncrease);
            if (newStatValue > currentStat._Value)
            {
                currentStat._Value = newStatValue;
            }
        }

        public static void SetBloodMastery(ulong SteamID, float Value)
        {
            bool isPlayerFound = DataStructures.PlayerBloodMastery.TryGetValue(SteamID, out var Mastery);
            if (isPlayerFound)
            {
                if (Value + Mastery.Key > MaxBloodMastery)
                {
                    KeyValuePair<int, float> BloodMastery = new(0, MaxBloodMastery);
                    DataStructures.PlayerBloodMastery[SteamID] = BloodMastery;
                }
                else
                {
                    KeyValuePair<int, float> BloodMastery = new(0, Value + Mastery.Value);
                    DataStructures.PlayerBloodMastery[SteamID] = BloodMastery;
                }
            }
            else
            {
                KeyValuePair<int, float> BloodMastery = new(0, Value);
                DataStructures.PlayerBloodMastery.Add(SteamID, BloodMastery);
            }

            DataStructures.SavePlayerBloodMastery();
        }
    }
}