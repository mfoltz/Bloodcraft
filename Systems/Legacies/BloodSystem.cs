﻿using Bloodcraft.Patches;
using Bloodcraft.Systems.Leveling;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;

namespace Bloodcraft.Systems.Legacy
{
    public class BloodSystem
    {
        static readonly float UnitLegacyMultiplier = Plugin.UnitExpertiseMultiplier.Value; // Expertise points multiplier from normal units
        static readonly int MaxBloodLevel = Plugin.MaxExpertiseLevel.Value; // maximum level
        static readonly float VBloodLegacyMultiplier = Plugin.VBloodLegacyMultipler.Value; // Expertise points multiplier from VBlood units
        static readonly float BloodConstant = 0.1f; // constant for calculating level from xp
        static readonly int BloodPower = 2; // power for calculating level from xp

        public enum BloodType
        {
            Worker,
            Warrior,
            Scholar,
            Rogue,
            Mutant,
            VBlood,
            None,
            GateBoss,
            Draculin,
            Immortal,
            Creature,
            Brute
        }
        public static readonly Dictionary<BloodType, PrestigeSystem.PrestigeType> BloodPrestigeMap = new()
        {
            { BloodType.Worker, PrestigeSystem.PrestigeType.WorkerLegacy },
            { BloodType.Warrior, PrestigeSystem.PrestigeType.WarriorLegacy },
            { BloodType.Scholar, PrestigeSystem.PrestigeType.ScholarLegacy },
            { BloodType.Rogue, PrestigeSystem.PrestigeType.RogueLegacy },
            { BloodType.Mutant, PrestigeSystem.PrestigeType.MutantLegacy },
            { BloodType.VBlood, PrestigeSystem.PrestigeType.VBloodLegacy },
            { BloodType.None, PrestigeSystem.PrestigeType.Experience }, // Assuming 'None' maps to general experience
            { BloodType.GateBoss, PrestigeSystem.PrestigeType.Experience }, // Example mapping
            { BloodType.Draculin, PrestigeSystem.PrestigeType.DraculinLegacy },
            { BloodType.Immortal, PrestigeSystem.PrestigeType.ImmortalLegacy },
            { BloodType.Creature, PrestigeSystem.PrestigeType.CreatureLegacy },
            { BloodType.Brute, PrestigeSystem.PrestigeType.BruteLegacy }
        };

        public static readonly Dictionary<PrefabGUID, BloodType> BuffToBloodTypeMap = new()
{
            { new PrefabGUID(-773025435), BloodType.Worker }, // yield bonus
            { new PrefabGUID(-804597757), BloodType.Warrior }, // phys bonus
            { new PrefabGUID(1934870645), BloodType.Scholar }, // spell bonus
            { new PrefabGUID(1201299233), BloodType.Rogue }, // crit bonus
            { new PrefabGUID(-1266262267), BloodType.Mutant }, // drain bonus
            { new PrefabGUID(560247144), BloodType.VBlood }, // vblood_0
            { new PrefabGUID(1558171501), BloodType.Draculin }, // speed bonus
            { new PrefabGUID(-488475343), BloodType.Immortal }, // phys/spell bonus
            { new PrefabGUID(894725875), BloodType.Creature }, // speed bonus
            { new PrefabGUID(-534491790), BloodType.Brute } // primary life leech
        };

        public static void UpdateLegacy(Entity Killer, Entity Victim)
        {
            EntityManager entityManager = Core.EntityManager;
            if (Killer == Victim || entityManager.HasComponent<Minion>(Victim) || !Victim.Has<BloodConsumeSource>() || !Victim.Has<UnitLevel>()) return;
            BloodConsumeSource bloodConsumeSource = Victim.Read<BloodConsumeSource>();
            Entity userEntity = entityManager.GetComponentData<PlayerCharacter>(Killer).UserEntity;
            int unitLevel = Victim.Read<UnitLevel>().Level;
            float BloodValue = 0;
            if (entityManager.HasComponent<VBloodConsumeSource>(Victim))
            {
                BloodValue = 10 * unitLevel * VBloodLegacyMultiplier;
            }
            else
            {
                BloodValue = bloodConsumeSource.BloodQuality / 10 * unitLevel * UnitLegacyMultiplier;
            }
            User user = entityManager.GetComponentData<User>(userEntity);
            ulong steamID = user.PlatformId;
            BloodSystem.BloodType bloodType = ModifyUnitStatBuffUtils.GetCurrentBloodType(Killer);
            if (bloodType.Equals(BloodType.None)) return;

            IBloodHandler handler = BloodHandlerFactory.GetBloodHandler(bloodType);
            if (handler != null)
            {
                // Check if the player leveled up
                var xpData = handler.GetLegacyData(steamID);

                if (Core.DataStructures.PlayerPrestiges.TryGetValue(steamID, out var prestiges) && prestiges.TryGetValue(BloodPrestigeMap[bloodType], out var PrestigeData) && PrestigeData > 0)
                {
                    float reductionFactor = 1.0f / (1 + PrestigeData * Plugin.PrestigeRatesReducer.Value);
                    BloodValue *= reductionFactor;
                }

                if (Core.DataStructures.PlayerPrestiges.TryGetValue(steamID, out var xpPrestige) && xpPrestige.TryGetValue(PrestigeSystem.PrestigeType.Experience, out var xpPrestigeLevel) && xpPrestigeLevel > 0)
                {
                    float gainFactor = (1 + xpPrestigeLevel * Plugin.PrestigeRatesMultiplier.Value);
                    BloodValue *= gainFactor;
                }


                float newExperience = xpData.Value + BloodValue;
                int newLevel = ConvertXpToLevel(newExperience);
                bool leveledUp = false;

                if (newLevel > xpData.Key)
                {
                    leveledUp = true;
                    if (newLevel > MaxBloodLevel)
                    {
                        newLevel = MaxBloodLevel;
                        newExperience = ConvertLevelToXp(MaxBloodLevel);
                    }
                }
                var updatedXPData = new KeyValuePair<int, float>(newLevel, newExperience);
                handler.UpdateLegacyData(steamID, updatedXPData);
                handler.SaveChanges();
                NotifyPlayer(entityManager, user, bloodType, BloodValue, leveledUp, newLevel, handler);
            }
        }

        public static void NotifyPlayer(EntityManager entityManager, User user, BloodSystem.BloodType bloodType, float gainedXP, bool leveledUp, int newLevel, IBloodHandler handler)
        {
            ulong steamID = user.PlatformId;
            gainedXP = (int)gainedXP; // Convert to integer if necessary
            int levelProgress = GetLevelProgress(steamID, handler); // Calculate the current progress to the next level

            string message;

            if (leveledUp)
            {
                message = $"<color=red>{bloodType}</color> legacy improved to [<color=white>{newLevel}</color>]";
                if (newLevel < MaxBloodLevel) ServerChatUtils.SendSystemMessageToClient(entityManager, user, message);
            }
            
            if (Core.DataStructures.PlayerBools.TryGetValue(steamID, out var bools) && bools["BloodLogging"])
            {
                message = $"+<color=yellow>{gainedXP}</color> <color=red>{bloodType}</color> <color=#FFC0CB>essence</color> (<color=white>{levelProgress}%</color>)";
                ServerChatUtils.SendSystemMessageToClient(entityManager, user, message);
            }
            
        }
        
        public static int GetLevelProgress(ulong SteamID, IBloodHandler handler)
        {
            float currentXP = GetXp(SteamID, handler);
            int currentLevelXP = ConvertLevelToXp(GetLevel(SteamID, handler));
            int nextLevelXP = ConvertLevelToXp(GetLevel(SteamID, handler) + 1);

            double neededXP = nextLevelXP - currentLevelXP;
            double earnedXP = nextLevelXP - currentXP;

            return 100 - (int)Math.Ceiling(earnedXP / neededXP * 100);
        }

        public static int ConvertXpToLevel(float xp)
        {
            // Assuming a basic square root scaling for experience to level conversion
            return (int)(BloodConstant * Math.Sqrt(xp));
        }

        public static int ConvertLevelToXp(int level)
        {
            // Reversing the formula used in ConvertXpToLevel for consistency
            return (int)Math.Pow(level / BloodConstant, BloodPower);
        }

        static float GetXp(ulong steamID, IBloodHandler handler)
        {
            var xpData = handler.GetLegacyData(steamID);
            return xpData.Value;
        }

        static int GetLevel(ulong steamID, IBloodHandler handler)
        {
            return ConvertXpToLevel(GetXp(steamID, handler));
        }

        public static BloodType GetBloodTypeFromPrefab(PrefabGUID blood)
        {
            string bloodCheck = blood.LookupName().ToString().ToLower();
            foreach (BloodType type in Enum.GetValues(typeof(BloodType)))
            {
                if (bloodCheck.Contains(type.ToString().ToLower()))
                {
                    return type;
                }
            }
            throw new InvalidOperationException("Unrecognized blood type");
        }
    }
}