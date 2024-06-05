using ProjectM;
using ProjectM.Behaviours;
using ProjectM.Network;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Entities;
using Unity.Transforms;

namespace Bloodcraft.Systems.Familiars
{
    internal class FamiliarSummonSystem
    {
        static readonly PrefabGUID playerFaction = new(1106458752);
        public static void SummonFamiliar(Entity character, Entity userEntity, int famKey)
        {
            EntityCommandBufferSystem entityCommandBufferSystem = Core.EntityCommandBufferSystem;
            DebugEventsSystem debugEventsSystem = Core.DebugEventsSystem;
            EntityCommandBuffer entityCommandBuffer = entityCommandBufferSystem.CreateCommandBuffer();

            User user = userEntity.Read<User>();
            ulong steamId = user.PlatformId;
            int index = user.Index;
            int level = 1;
            FromCharacter fromCharacter = new() { Character = character, User = userEntity };

            if (Core.FamiliarExperienceManager.LoadFamiliarExperience(steamId).FamiliarExperience.TryGetValue(famKey, out var xpData) && xpData.Key > 1)
            {
                level = xpData.Key;
            }

            var debugEvent = new SpawnDebugEvent
            {
                PrefabGuid = new(famKey),
                Control = false,
                Roam = false,
                Team = SpawnDebugEvent.TeamEnum.Ally,
                Level = level,
                Position = character.Read<LocalToWorld>().Position
            };
            debugEventsSystem.SpawnDebugEvent(index, ref debugEvent, entityCommandBuffer, ref fromCharacter);
        }
        public static void HandleFamiliar(Entity player, Entity familiar)
        {
            UnitLevel unitLevel = familiar.Read<UnitLevel>();
            HandleFamiliarModifications(player, familiar, unitLevel.Level._Value);
        }
        public static void HandleFamiliarModifications(Entity player, Entity familiar, int level)
        {
            if (familiar.Has<BloodConsumeSource>()) ModifyBloodSource(familiar, level);
            ModifyFollowerAndTeam(player, familiar);
            ModifyDamageStats(familiar, level);
            ModifyAggro(familiar);
            if (familiar.Has<ServantConvertable>()) ModifyConvertable(familiar);
            ModifyCollision(familiar);
            ModifyDropTable(familiar);
        }
        
        static void ModifyFollowerAndTeam(Entity player, Entity familiar)
        {
            ModifyTeamBuff modifyTeamBuff = new ModifyTeamBuff { Source = ModifyTeamBuffAuthoring.ModifyTeamSource.OwnerTeam };
            familiar.Add<ModifyTeamBuff>();
            familiar.Write(modifyTeamBuff);

            FactionReference factionReference = familiar.Read<FactionReference>();
            factionReference.FactionGuid._Value = playerFaction;
            //factionReference.FactionGuid._Value = traderFaction;
            familiar.Write(factionReference);

            Follower follower = familiar.Read<Follower>();
            follower.Followed._Value = player;
            follower.ModeModifiable._Value = 0;
            familiar.Write(follower);
            
            UnitStats unitStats = familiar.Read<UnitStats>();
            unitStats.PvPProtected._Value = true;
            familiar.Write(unitStats);

            var buffer = player.ReadBuffer<FollowerBuffer>();
            buffer.Add(new FollowerBuffer { Entity = NetworkedEntity.ServerEntity(familiar) });
        }
        public static void ModifyBloodSource(Entity familiar, int level)
        {
            BloodConsumeSource bloodConsumeSource = familiar.Read<BloodConsumeSource>();
            bloodConsumeSource.BloodQuality = level / (float)Plugin.MaxFamiliarLevel.Value * 100;
            bloodConsumeSource.CanBeConsumed = false;
            familiar.Write(bloodConsumeSource);

            familiar.Add<BlockFeedBuff>();
        }
        public static void ModifyDamageStats(Entity familiar, int level)
        {
            float scalingFactor = 0.1f + (level / (float)Plugin.MaxFamiliarLevel.Value)*0.9f; // Calculate scaling factor
            float healthScalingFactor = 1.0f + (level / (float)Plugin.MaxFamiliarLevel.Value) * 4.0f; // Calculate scaling factor for max health

            /*
            DamageCategoryStats damageStats = familiar.Read<DamageCategoryStats>();
            damageStats.DamageVsUndeads._Value = scalingFactor;
            damageStats.DamageVsHumans._Value = scalingFactor;
            damageStats.DamageVsDemons._Value = scalingFactor;
            damageStats.DamageVsMechanical._Value = scalingFactor;
            damageStats.DamageVsBeasts._Value = scalingFactor;
            damageStats.DamageVsLightArmor._Value = scalingFactor;
            damageStats.DamageVsVBloods._Value = scalingFactor;
            damageStats.DamageVsMagic._Value = scalingFactor;
            damageStats.DamageVsVampires._Value = scalingFactor;
            familiar.Write(damageStats);
            */

            UnitStats unitStats = familiar.Read<UnitStats>();
            unitStats.PhysicalPower._Value *= scalingFactor;
            unitStats.SpellPower._Value *= scalingFactor;
            familiar.Write(unitStats);

            UnitLevel unitLevel = familiar.Read<UnitLevel>();
            unitLevel.Level._Value = level;
            familiar.Write(unitLevel);

            Health health = familiar.Read<Health>();
            int baseHealth = 300;
            health.MaxHealth._Value = baseHealth * healthScalingFactor;
            health.Value = health.MaxHealth._Value;
            familiar.Write(health);
        }
        static void ModifyUnitTier(Entity familiar, int level)
        {
            UnitLevelServerData unitLevelServerData = familiar.Read<UnitLevelServerData>();

            int tier;
            float threshold33 = Plugin.MaxFamiliarLevel.Value * 0.33f;
            float threshold66 = Plugin.MaxFamiliarLevel.Value * 0.66f;

            if (level == Plugin.MaxFamiliarLevel.Value)
            {
                tier = 3;
            }
            else if (level > threshold66)
            {
                tier = 2;
            }
            else if (level > threshold33)
            {
                tier = 1;
            }
            else
            {
                tier = 0;
            }
            unitLevelServerData.HealthUnitBaseStatsTypeInt._Value = tier;
            unitLevelServerData.UnitBaseStatsTypeInt._Value = tier;
            familiar.Write(unitLevelServerData);

            Core.Log.LogInfo("ModifyUnitTier complete...");
        }
        static void ModifyAggro(Entity familiar)
        {
            AggroConsumer aggroConsumer = familiar.Read<AggroConsumer>();
            aggroConsumer.MaxDistanceFromPreCombatPosition = 20f;
            aggroConsumer.ProximityRadius = 25f;
            familiar.Write(aggroConsumer);
            
        }
        static void ModifyConvertable(Entity familiar)
        {
            ServantConvertable convertable = familiar.Read<ServantConvertable>();
            convertable.ConvertToUnit = new(0);
            familiar.Write(convertable);

            if (familiar.Has<CharmSource>())
            {
                familiar.Remove<CharmSource>();
            }
        }
        static void ModifyCollision(Entity familiar)
        {
            DynamicCollision collision = familiar.Read<DynamicCollision>();
            collision.AgainstPlayers.RadiusOverride = -1f;
            familiar.Write(collision);
        }
        static void ModifyDropTable(Entity familiar)
        {
            if (!familiar.Has<DropTableBuffer>()) return;
            var buffer = familiar.ReadBuffer<DropTableBuffer>();
            for (int i = 0; i < buffer.Length; i++)
            {
                var item = buffer[i];
                item.DropTrigger = DropTriggerType.OnSalvageDestroy;
                buffer[i] = item;
            }
        }
        public static class FamiliarUtilities
        {
            public static Entity FindPlayerFamiliar(Entity characterEntity)
            {
                var followers = characterEntity.ReadBuffer<FollowerBuffer>();
                foreach (var follower in followers)
                {
                    PrefabGUID prefabGUID = follower.Entity._Entity.Read<PrefabGUID>();
                    ulong platformId = characterEntity.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                    if (Core.DataStructures.FamiliarActives.TryGetValue(platformId, out var data) && data.Item2.Equals(prefabGUID.GuidHash))
                    {
                        return follower.Entity._Entity;
                    }
                }
                return Entity.Null;
            }
        }

    }
}
