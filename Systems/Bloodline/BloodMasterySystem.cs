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
        private static readonly int MaxBloodMasteryLevel = 99; // maximum level
        private static readonly float BloodMasteryConstant = 0.1f; // constant for calculating level from xp
        private static readonly int BloodMasteryXPPower = 2; // power for calculating level from xp
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
            SetBloodMastery(User, BloodMasteryValue, entityManager);
            
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

            if (!DataStructures.PlayerBloodMastery.TryGetValue(steamId, out var mastery))
            {
                return; // No mastery data found
            }

            int playerLevel = ConvertXpToLevel(mastery.Value);
            float levelPercentage = playerLevel / MaxBloodMasteryLevel; // Calculate the percentage of the max level (99)

            foreach (var statType in bloodStats.ChosenStats)
            {
                float baseCap = BloodMasteryStatManager.BloodFocusSystem.BaseCaps[statType];
                float scaledCap = baseCap * levelPercentage; // Scale cap based on the player's level
                float currentStatValue = bloodStats.GetStatValue(statType);
                float statIncrease = Math.Min(currentStatValue, scaledCap); // Ensure it doesn't exceed the scaled cap

                ApplyStatIncrease(stats, statType, statIncrease);
            }

            player.Write(stats); // Update player stats
        }
        
        private static void ApplyStatIncrease(UnitStats stats, BloodMasteryStatManager.BloodFocusSystem.BloodStatType statType, float increase)
        {
            switch (statType)
            {
                case BloodMasteryStatManager.BloodFocusSystem.BloodStatType.ResourceYield:
                    stats.ResourceYieldModifier._Value = Math.Max(stats.ResourceYieldModifier._Value, increase);
                    break;
                case BloodMasteryStatManager.BloodFocusSystem.BloodStatType.PhysicalResistance:
                    stats.PhysicalResistance._Value = Math.Max(stats.PhysicalResistance._Value, increase);
                    break;
                case BloodMasteryStatManager.BloodFocusSystem.BloodStatType.SpellResistance:
                    stats.SpellResistance._Value = Math.Max(stats.SpellResistance._Value, increase);
                    break;
                case BloodMasteryStatManager.BloodFocusSystem.BloodStatType.SunResistance:
                    stats.SunResistance._Value = (int)Math.Max(stats.SunResistance._Value, increase);
                    break;
                case BloodMasteryStatManager.BloodFocusSystem.BloodStatType.FireResistance:
                    stats.FireResistance._Value = (int)Math.Max(stats.FireResistance._Value, increase);
                    break;
                case BloodMasteryStatManager.BloodFocusSystem.BloodStatType.HolyResistance:
                    stats.HolyResistance._Value = (int)Math.Max(stats.HolyResistance._Value, increase);
                    break;
                case BloodMasteryStatManager.BloodFocusSystem.BloodStatType.SilverResistance:
                    stats.SilverResistance._Value = (int)Math.Max(stats.SilverResistance._Value, increase);
                    break;
                case BloodMasteryStatManager.BloodFocusSystem.BloodStatType.PassiveHealthRegen:
                    stats.PassiveHealthRegen._Value = Math.Max(stats.PassiveHealthRegen._Value, increase);
                    break;
                default:
                    throw new InvalidOperationException("Unknown stat type to apply");
            }
        }
        public static void SetBloodMastery(User user, float Value, EntityManager entityManager)
        {
            ulong SteamID = user.PlatformId;
            bool isPlayerFound = DataStructures.PlayerBloodMastery.TryGetValue(SteamID, out var Mastery);
            float newExperience = Value + (isPlayerFound ? Mastery.Value : 0);
            int newLevel = ConvertXpToLevel(newExperience);
            bool leveledUp = isPlayerFound && newLevel > Mastery.Key;

            if (leveledUp && newLevel > MaxBloodMasteryLevel)
            {
                newLevel = MaxBloodMasteryLevel;
                newExperience = ConvertLevelToXp(MaxBloodMasteryLevel);
            }

            KeyValuePair<int, float> newMastery = new KeyValuePair<int, float>(newLevel, newExperience);
            if (isPlayerFound)
            {
                DataStructures.PlayerBloodMastery[SteamID] = newMastery;
            }
            else
            {
                DataStructures.PlayerBloodMastery.Add(SteamID, newMastery);
            }

            DataStructures.SavePlayerBloodMastery();
            NotifyPlayer(entityManager, user, Value, leveledUp, newLevel);
        }
        private static void NotifyPlayer(EntityManager entityManager, User user, float gainedXP, bool leveledUp, int newLevel)
        {
            ulong steamID = user.PlatformId;
            gainedXP = (int)gainedXP;  // Convert to integer if necessary
            int levelProgress = GetLevelProgress(steamID);  // Calculate the current progress to the next level

            if (leveledUp)
            {
                // Directly using the newLevel parameter since it's already calculated
                ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"<color=red>Sanguimancy</color> improved to [<color=white>{newLevel}</color>]");
            }
            else
            {
                if (DataStructures.PlayerBools.TryGetValue(steamID, out var bools) && bools["BloodLogging"])
                {
                    ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"+<color=yellow>{gainedXP}</color> <color=red>sanguimancy</color> (<color=white>{levelProgress}%</color>)");
                }
            }
        }

        private static int ConvertXpToLevel(float xp)
        {
            return (int)(BloodMasteryConstant * Math.Sqrt(xp));
        }

        private static float ConvertLevelToXp(int level)
        {
            return (int)Math.Pow(level / BloodMasteryConstant, BloodMasteryXPPower);
        }

        private static int GetLevelProgress(ulong steamID)
        {
            if (!DataStructures.PlayerBloodMastery.TryGetValue(steamID, out var mastery))
                return 0; // Return 0 if no mastery data found

            float currentXP = mastery.Value;
            int currentLevel = ConvertXpToLevel(currentXP);
            int nextLevelXP = (int)ConvertLevelToXp(currentLevel + 1);
            // Correcting to show progress between the current and next level
            int percent = (int)((currentXP - ConvertLevelToXp(currentLevel)) / (nextLevelXP - ConvertLevelToXp(currentLevel)) * 100);
            return percent;
        }
    }
}