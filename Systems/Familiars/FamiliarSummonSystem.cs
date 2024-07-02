using Bloodcraft.Systems.Leveling;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Steamworks;
using Stunlock.Core;
using Unity.Entities;
using Unity.Transforms;
using static Bloodcraft.Systems.Expertise.WeaponStats.WeaponStatManager;

namespace Bloodcraft.Systems.Familiars;
internal static class FamiliarSummonSystem
{
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static GameDifficulty GameDifficulty => Core.ServerGameSettings.GameDifficulty;
    static PrefabCollectionSystem PrefabCollectionSystem => Core.PrefabCollectionSystem;
    static readonly float VBloodDamageMultiplier = Plugin.VBloodDamageMultiplier.Value;
    static readonly float PlayerVampireDamageMultiplier = Plugin.PlayerVampireDamageMultiplier.Value;
    static readonly float FamiliarStatMultiplier = Plugin.FamiliarPrestigeStatMultiplier.Value;
    static readonly bool FamiliarCombat = Plugin.FamiliarCombat.Value;

    static readonly PrefabGUID invulnerableBuff = new(-480024072);
    static readonly PrefabGUID ignoredFaction = new(-1430861195);
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

        SpawnDebugEvent debugEvent = new()
        {
            PrefabGuid = new(famKey),
            Control = false,
            Roam = false,
            Team = SpawnDebugEvent.TeamEnum.Ally,
            Level = level,
            Position = character.Read<LocalToWorld>().Position,
            DyeIndex = 0
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
        ulong steamId = player.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
        int famKey = familiar.Read<PrefabGUID>().GuidHash;
        if (familiar.Has<BloodConsumeSource>()) ModifyBloodSource(familiar, level);
        ModifyFollowerAndTeam(player, familiar);
        ModifyDamageStats(familiar, level, steamId, famKey);
        ModifyAggro(familiar);
        ModifyConvertable(familiar);
        ModifyCollision(familiar);
        ModifyDropTable(familiar);
        PreventDisableFamiliar(familiar);
        if (!FamiliarCombat) DisableCombat(player, familiar);
    }
    static void DisableCombat(Entity player, Entity familiar)
    {
        FactionReference factionReference = familiar.Read<FactionReference>();
        factionReference.FactionGuid._Value = ignoredFaction;
        familiar.Write(factionReference);

        AggroConsumer aggroConsumer = familiar.Read<AggroConsumer>();
        aggroConsumer.Active._Value = false;
        familiar.Write(aggroConsumer);

        Aggroable aggroable = familiar.Read<Aggroable>();
        aggroable.Value._Value = false;
        aggroable.DistanceFactor._Value = 0f;
        aggroable.AggroFactor._Value = 0f;
        familiar.Write(aggroable);

        DebugEventsSystem debugEventsSystem = Core.DebugEventsSystem;
        ApplyBuffDebugEvent applyBuffDebugEvent = new()
        {
            BuffPrefabGUID = invulnerableBuff,
        };
        FromCharacter fromCharacter = new()
        {
            Character = familiar,
            User = player.Read<PlayerCharacter>().UserEntity,
        };
        debugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
        if (ServerGameManager.TryGetBuff(familiar, invulnerableBuff.ToIdentifier(), out Entity invlunerableBuff))
        {
            if (invlunerableBuff.Has<LifeTime>())
            {
                var lifetime = invlunerableBuff.Read<LifeTime>();
                lifetime.Duration = -1;
                lifetime.EndAction = LifeTimeEndAction.None;
                invlunerableBuff.Write(lifetime);
            }
        }
    }
    static void ModifyFollowerAndTeam(Entity player, Entity familiar)
    {
        ModifyTeamBuff modifyTeamBuff = new() { Source = ModifyTeamBuffAuthoring.ModifyTeamSource.OwnerTeam };
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

        var followerBuffer = player.ReadBuffer<FollowerBuffer>();
        followerBuffer.Add(new FollowerBuffer { Entity = NetworkedEntity.ServerEntity(familiar) });
        //AggroFactionMultiplierBufferElement aggro = new AggroFactionMultiplierBufferElement { FactionIndex = player.Read<Team>().FactionIndex, Multiplier = 0f };
        //var aggroBuffer = Core.EntityManager.AddBuffer<AggroFactionMultiplierBufferElement>(familiar);
        //aggroBuffer.Add(aggro);
    }
    public static void ModifyBloodSource(Entity familiar, int level)
    {
        BloodConsumeSource bloodConsumeSource = familiar.Read<BloodConsumeSource>();
        bloodConsumeSource.BloodQuality = level / (float)Plugin.MaxFamiliarLevel.Value * 100;
        bloodConsumeSource.CanBeConsumed = false;
        familiar.Write(bloodConsumeSource);
        familiar.Add<BlockFeedBuff>();
    }

    public enum FamiliarStatType
    {
        PhysicalCritChance,
        SpellCritChance,
        HealingReceived,
        PhysicalResistance,
        SpellResistance,
        CCReduction,
        ShieldAbsorb
    }

    public static readonly Dictionary<FamiliarStatType, float> familiarStatCaps = new()
            {
                {FamiliarStatType.PhysicalCritChance, 0.2f},
                {FamiliarStatType.SpellCritChance, 0.2f},
                {FamiliarStatType.HealingReceived, 0.5f},
                {FamiliarStatType.PhysicalResistance, 0.2f},
                {FamiliarStatType.SpellResistance, 0.2f},
                {FamiliarStatType.CCReduction, 0.5f},
                {FamiliarStatType.ShieldAbsorb, 1f}
            };
    public static void ModifyDamageStats(Entity familiar, int level, ulong steamId, int famKey)
    {
        float scalingFactor = 0.1f + (level / (float)Plugin.MaxFamiliarLevel.Value)*0.9f; // Calculate scaling factor
        float healthScalingFactor = 1.0f + (level / (float)Plugin.MaxFamiliarLevel.Value) * 4.0f; // Calculate scaling factor for max health

        int prestigeLevel = 0;
        List<FamiliarStatType> stats = [];

        if (Core.FamiliarPrestigeManager.LoadFamiliarPrestige(steamId).FamiliarPrestige.TryGetValue(famKey, out var prestigeData) && prestigeData.Key > 0)
        {
            prestigeLevel = prestigeData.Key;
            stats = prestigeData.Value;
        }

        if (!Plugin.FamiliarPrestige.Value)
        {
            prestigeLevel = 0;
            stats = [];
        }

        Entity original = PrefabCollectionSystem._PrefabGuidToEntityMap[familiar.Read<PrefabGUID>()];
        // get stats from original
        UnitStats unitStats = original.Read<UnitStats>();

        UnitStats familiarStats = familiar.Read<UnitStats>();
        familiarStats.PhysicalPower._Value = unitStats.PhysicalPower._Value * scalingFactor * (1 + prestigeLevel * FamiliarStatMultiplier);
        familiarStats.SpellPower._Value = unitStats.SpellPower._Value * scalingFactor * (1 + prestigeLevel * FamiliarStatMultiplier);

        foreach (FamiliarStatType stat in stats)
        {
            switch (stat)
            {
                case FamiliarStatType.PhysicalCritChance:
                    familiarStats.PhysicalCriticalStrikeChance._Value = familiarStatCaps[FamiliarStatType.PhysicalCritChance] * (1 + prestigeLevel * FamiliarStatMultiplier);
                    break;
                case FamiliarStatType.SpellCritChance:
                    familiarStats.SpellCriticalStrikeChance._Value = familiarStatCaps[FamiliarStatType.SpellCritChance] * (1 + prestigeLevel * FamiliarStatMultiplier);
                    break;
                case FamiliarStatType.HealingReceived:
                    familiarStats.HealingReceived._Value = familiarStatCaps[FamiliarStatType.HealingReceived] * (1 + prestigeLevel * FamiliarStatMultiplier);
                    break;
                case FamiliarStatType.PhysicalResistance:
                    familiarStats.PhysicalResistance._Value = familiarStatCaps[FamiliarStatType.PhysicalResistance] * (1 + prestigeLevel * FamiliarStatMultiplier);
                    break;
                case FamiliarStatType.SpellResistance:
                    familiarStats.SpellResistance._Value = familiarStatCaps[FamiliarStatType.SpellResistance] * (1 + prestigeLevel * FamiliarStatMultiplier);
                    break;
                case FamiliarStatType.CCReduction:
                    familiarStats.CCReduction._Value = (int)(familiarStatCaps[FamiliarStatType.CCReduction] * (1 + prestigeLevel * FamiliarStatMultiplier));
                    break;
                case FamiliarStatType.ShieldAbsorb:
                    familiarStats.ShieldAbsorbModifier._Value = unitStats.ShieldAbsorbModifier._Value + familiarStatCaps[FamiliarStatType.ShieldAbsorb] * (1 + prestigeLevel * FamiliarStatMultiplier);
                    break;
            }
        }
        familiar.Write(familiarStats);

        UnitLevel unitLevel = familiar.Read<UnitLevel>();
        unitLevel.Level._Value = level;
        familiar.Write(unitLevel);

        Health familiarHealth = familiar.Read<Health>();
        int baseHealth = 500;

        if (GameDifficulty.Equals(GameDifficulty.Hard))
        {
            baseHealth = 750;
        }

        familiarHealth.MaxHealth._Value = baseHealth * healthScalingFactor * (1 + prestigeLevel*FamiliarStatMultiplier);
        familiarHealth.Value = familiarHealth.MaxHealth._Value;
        familiar.Write(familiarHealth);

        if (VBloodDamageMultiplier != 1f)
        {
            DamageCategoryStats damageCategoryStats = familiar.Read<DamageCategoryStats>();
            if (damageCategoryStats.DamageVsVBloods._Value != VBloodDamageMultiplier)
            {
                damageCategoryStats.DamageVsVBloods._Value *= VBloodDamageMultiplier;
                familiar.Write(damageCategoryStats);
            }
        }
        if (PlayerVampireDamageMultiplier != 1f)
        {
            DamageCategoryStats damageCategoryStats = familiar.Read<DamageCategoryStats>();
            if (damageCategoryStats.DamageVsVampires._Value != PlayerVampireDamageMultiplier)
            {
                damageCategoryStats.DamageVsVampires._Value *= PlayerVampireDamageMultiplier;
                familiar.Write(damageCategoryStats);
            }
        }
        
        if (familiar.Has<MaxMinionsPerPlayerElement>()) // make vbloods summon?
        {
            familiar.Remove<MaxMinionsPerPlayerElement>();
        }
        if (familiar.Has<SpawnPrefabOnGameplayEvent>()) // stop pilots spawning from tanks
        {
            var buffer = familiar.ReadBuffer<SpawnPrefabOnGameplayEvent>();
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].SpawnPrefab.LookupName().ToLower().Contains("pilot"))
                {
                    familiar.Remove<SpawnPrefabOnGameplayEvent>();
                }
            }
        }
        if (familiar.Has<Immortal>())
        {
            familiar.Remove<Immortal>();
            if (!familiar.Has<ApplyBuffOnGameplayEvent>()) return;
            var buffer = familiar.ReadBuffer<ApplyBuffOnGameplayEvent>();
            for (int i = 0; i < buffer.Length; i++)
            {
                var item = buffer[i];
                if (item.Buff0.GuidHash.Equals(2144624015)) // no bubble for Solarus
                {
                    item.Buff0 = new(0);
                    buffer[i] = item;
                    break;
                }
            }
        }
    }
    static void PreventDisableFamiliar(Entity familiar)
    {
        ModifiableBool modifiableBool = new ModifiableBool { _Value = false };
        CanPreventDisableWhenNoPlayersInRange canPreventDisable = new() { CanDisable = modifiableBool };
        Core.EntityManager.AddComponentData(familiar, canPreventDisable);
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
        aggroConsumer.MaxDistanceFromPreCombatPosition = 30f;
        aggroConsumer.ProximityRadius = 25f;
        familiar.Write(aggroConsumer);   
    }
    static void ModifyConvertable(Entity familiar)
    {
        if (familiar.Has<ServantConvertable>())
        {
            familiar.Remove<ServantConvertable>();
        }
        if (familiar.Has<CharmSource>())
        {
            familiar.Remove<CharmSource>();
        }

        // testing
        //NameableInteractable nameableInteractable = new NameableInteractable { Name = "Bob", OnlyAllyRename = true, OnlyAllySee = false };
        //Core.EntityManager.SetComponentData(familiar, nameableInteractable);
        
    }
    static void ModifyCollision(Entity familiar)
    {
        DynamicCollision collision = familiar.Read<DynamicCollision>();
        collision.AgainstPlayers.RadiusOverride = -1f;
        collision.AgainstPlayers.HardnessThreshold._Value = 0f;
        collision.AgainstPlayers.PushStrengthMax._Value = 0f;
        collision.AgainstPlayers.PushStrengthMin._Value = 0f;
        collision.AgainstPlayers.RadiusVariation = 0f;
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
            ulong platformId = characterEntity.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
            if (Core.DataStructures.FamiliarActives.TryGetValue(platformId, out var data) && Core.EntityManager.Exists(data.Item1))
            {
                return data.Item1;
            }
            else
            {
                foreach (var follower in followers)
                {
                    PrefabGUID PrefabGUID = follower.Entity._Entity.Read<PrefabGUID>();
                    if (PrefabGUID.GuidHash.Equals(data.FamKey)) return follower.Entity._Entity;
                }
            }
            return Entity.Null;
        } 
    }
    public static bool TryParseFamiliarStat(string statType, out FamiliarStatType parsedStatType)
    {
        // Attempt to parse the prestigeType string to the PrestigeType enum.
        if (Enum.TryParse(statType, true, out parsedStatType))
        {
            return true; // Successfully parsed
        }

        // If the initial parse failed, try to find a matching PrestigeType enum value containing the input string.
        parsedStatType = Enum.GetValues(typeof(FamiliarStatType))
                                 .Cast<FamiliarStatType>()
                                 .FirstOrDefault(pt => pt.ToString().Contains(statType, StringComparison.OrdinalIgnoreCase));

        // Check if a valid enum value was found that contains the input string.
        if (!parsedStatType.Equals(default(FamiliarStatType)))
        {
            return true; // Found a matching enum value
        }

        // If no match is found, return false and set the out parameter to default value.
        parsedStatType = default;
        return false; // Parsing failed
    }
}
