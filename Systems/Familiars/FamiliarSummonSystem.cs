using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using ProjectM.Gameplay.Scripting;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using static Bloodcraft.Services.DataService.FamiliarPersistence;
using static Bloodcraft.Utilities.Progression;

namespace Bloodcraft.Systems.Familiars;
internal static class FamiliarSummonSystem
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static DebugEventsSystem DebugEventsSystem => SystemService.DebugEventsSystem;
    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;
    static EntityCommandBufferSystem EntityCommandBufferSystem => SystemService.EntityCommandBufferSystem;

    static readonly GameDifficulty GameDifficulty = SystemService.ServerGameSettingsSystem.Settings.GameDifficulty;
    static readonly GameModeType GameMode = SystemService.ServerGameSettingsSystem._Settings.GameModeType;

    public const float FAMILIAR_LIFETIME = 240f;

    static readonly int MaxFamiliarLevel = ConfigService.MaxFamiliarLevel;
    static readonly float FamiliarPrestigeStatMultiplier = ConfigService.FamiliarPrestigeStatMultiplier;
    static readonly float VBloodDamageMultiplier = ConfigService.VBloodDamageMultiplier;

    static readonly bool FamiliarPrestige = ConfigService.FamiliarPrestige;

    static readonly PrefabGUID InvulnerableBuff = new(-480024072);

    static readonly PrefabGUID DivineAngel = new(-1737346940);

    static readonly PrefabGUID IgnoredFaction = new(-1430861195);
    static readonly PrefabGUID PlayerFaction = new(1106458752);

    static readonly PrefabGUID HideStaffBuff = new(2053361366);
    public static void SummonFamiliar(Entity character, Entity userEntity, int famKey)
    {
        EntityCommandBuffer entityCommandBuffer = EntityCommandBufferSystem.CreateCommandBuffer();

        User user = userEntity.Read<User>();
        int index = user.Index;

        FromCharacter fromCharacter = new() { Character = character, User = userEntity };

        SpawnDebugEvent debugEvent = new()
        {
            PrefabGuid = new(famKey),
            Control = false,
            Roam = false,
            Team = SpawnDebugEvent.TeamEnum.Ally,
            Level = 1,
            Position = character.Read<LocalToWorld>().Position,
            DyeIndex = 0
        };

        DebugEventsSystem.SpawnDebugEvent(index, ref debugEvent, entityCommandBuffer, ref fromCharacter);
    }
    public static void SummonFamiliarForBattle(Entity character, Entity userEntity, PrefabGUID familiarPrefabGUID, float3 position)
    {
        EntityCommandBuffer entityCommandBuffer = EntityCommandBufferSystem.CreateCommandBuffer();

        User user = userEntity.Read<User>();
        int index = user.Index;

        FromCharacter fromCharacter = new() { Character = character, User = userEntity };

        SpawnDebugEvent debugEvent = new()
        {
            PrefabGuid = familiarPrefabGUID,
            Control = false,
            Roam = false,
            Team = SpawnDebugEvent.TeamEnum.Ally,
            Level = 1,
            Position = position,
            DyeIndex = 0
        };

        DebugEventsSystem.SpawnDebugEvent(index, ref debugEvent, entityCommandBuffer, ref fromCharacter);
    }
    public static bool HandleFamiliar(Entity player, Entity familiar)
    {
        User user = player.Read<PlayerCharacter>().UserEntity.Read<User>();

        try
        {
            UnitLevel unitLevel = familiar.Read<UnitLevel>();
            int level = unitLevel.Level._Value;
            int famKey = familiar.Read<PrefabGUID>().GuidHash;
            ulong steamId = user.PlatformId;
            
            FamiliarExperienceData famData = FamiliarExperienceManager.LoadFamiliarExperience(steamId);
            level = famData.FamiliarExperience.TryGetValue(famKey, out var xpData) ? xpData.Key : 0;

            if (level == 0)
            {
                level = 1;

                KeyValuePair<int, float> newXP = new(1, ConvertLevelToXp(1));
                famData.FamiliarExperience[famKey] = newXP;

                FamiliarExperienceManager.SaveFamiliarExperience(steamId, famData);
            }

            if (ModifyFamiliar(user, steamId, famKey, player, familiar, level)) return true;
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogError($"Error during familiar modifications for {user.CharacterName.Value}: {ex}");
            
            return false;
        }
    }
    public static bool HandleFamiliarForBattle(Entity player, Entity familiar)
    {
        User user = player.Read<PlayerCharacter>().UserEntity.Read<User>();

        try
        {
            UnitLevel unitLevel = familiar.Read<UnitLevel>();
            int level = unitLevel.Level._Value;
            int famKey = familiar.Read<PrefabGUID>().GuidHash;
            ulong steamId = user.PlatformId;

            FamiliarExperienceData famData = FamiliarExperienceManager.LoadFamiliarExperience(steamId);
            level = famData.FamiliarExperience.TryGetValue(famKey, out var xpData) ? xpData.Key : 0;

            if (level == 0)
            {
                level = 1;

                KeyValuePair<int, float> newXP = new(level, ConvertLevelToXp(level));
                famData.FamiliarExperience[famKey] = newXP;

                FamiliarExperienceManager.SaveFamiliarExperience(steamId, famData);
            }

            if (ModifyFamiliarForBattle(user, steamId, famKey, player, familiar, level)) return true;
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogError($"Error during familiar modifications for {user.CharacterName.Value}: {ex}");
            
            return false;
        }
    }
    public static bool ModifyFamiliar(User user, ulong steamId, int famKey, Entity player, Entity familiar, int level)
    {
        try
        {
            if (familiar.Has<BloodConsumeSource>()) ModifyBloodSource(familiar, level);
            ModifyFollowerAndTeam(player, familiar);
            ModifyDamageStats(familiar, level, steamId, famKey);
            ModifyConvertable(familiar);
            ModifyCollision(familiar);
            ModifyDropTable(familiar);
            PreventDisableFamiliar(familiar);

            if (!ConfigService.FamiliarCombat) DisableCombat(player, familiar);

            if (Misc.GetPlayerBool(steamId, "FamiliarVisual"))
            {
                FamiliarBuffsData data = FamiliarBuffsManager.LoadFamiliarBuffs(steamId);
                if (data.FamiliarBuffs.ContainsKey(famKey))
                {
                    PrefabGUID visualBuff = new(data.FamiliarBuffs[famKey][0]);
                    Buffs.HandleVisual(familiar, visualBuff);
                }
            }

            if (GameMode.Equals(GameModeType.PvP)) ManualAggroHandling(familiar);

            return true;
        }
        catch (Exception ex)
        {
            Core.Log.LogError($"Error during familiar modifications for {user.CharacterName.Value}: {ex}");
            
            return false;
        }
    }
    public static bool ModifyFamiliarForBattle(User user, ulong steamId, int famKey, Entity player, Entity familiar, int level)
    {
        try
        {
            if (familiar.Has<BloodConsumeSource>()) ModifyBloodSource(familiar, level);

            ModifyFactionAndAggro(familiar);
            ModifyDamageStatsForBattle(familiar, level, steamId, famKey);
            ModifyConvertable(familiar);
            ModifyCollision(familiar);
            ModifyDropTable(familiar);
            PreventDisableFamiliar(familiar);
            Utilities.Familiars.NothingLivesForever(familiar);

            if (Misc.GetPlayerBool(steamId, "FamiliarVisual"))
            {
                FamiliarBuffsData data = FamiliarBuffsManager.LoadFamiliarBuffs(steamId);

                if (data.FamiliarBuffs.ContainsKey(famKey))
                {
                    PrefabGUID visualBuff = new(data.FamiliarBuffs[famKey][0]);
                    Buffs.HandleVisual(familiar, visualBuff);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Core.Log.LogError($"Error during familiar modifications for {user.CharacterName.Value}: {ex}");

            return false;
        }
    }
    static void DisableCombat(Entity player, Entity familiar)
    {
        FactionReference factionReference = familiar.Read<FactionReference>();
        factionReference.FactionGuid._Value = IgnoredFaction;
        familiar.Write(factionReference);

        AggroConsumer aggroConsumer = familiar.Read<AggroConsumer>();
        aggroConsumer.Active._Value = false;
        familiar.Write(aggroConsumer);

        Aggroable aggroable = familiar.Read<Aggroable>();
        aggroable.Value._Value = false;
        aggroable.DistanceFactor._Value = 0f;
        aggroable.AggroFactor._Value = 0f;
        familiar.Write(aggroable);

        ApplyBuffDebugEvent applyBuffDebugEvent = new()
        {
            BuffPrefabGUID = InvulnerableBuff,
        };

        FromCharacter fromCharacter = new()
        {
            Character = familiar,
            User = player.Read<PlayerCharacter>().UserEntity,
        };

        DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
        if (ServerGameManager.TryGetBuff(familiar, InvulnerableBuff.ToIdentifier(), out Entity invlunerableBuff))
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
    static void ModifyFactionAndAggro(Entity familiar)
    {
        if (familiar.Has<FactionReference>())
        {
            familiar.With((ref FactionReference factionReference) =>
            {
                factionReference.FactionGuid._Value = PlayerFaction;
            });
        }

        if (familiar.Has<AggroConsumer>())
        {
            familiar.With((ref AggroConsumer aggroConsumer) =>
            {
                aggroConsumer.Active._Value = false;
            });
        }

        if (familiar.Has<Aggroable>())
        {
            familiar.With((ref Aggroable aggroable) =>
            {
                aggroable.Value._Value = false;
            });
        }

        if (!familiar.Has<BlockFeedBuff>()) familiar.Add<BlockFeedBuff>(); // general marker for fams
    }
    static void ModifyFollowerAndTeam(Entity player, Entity familiar)
    {
        if (familiar.Has<FactionReference>())
        {
            familiar.With((ref FactionReference factionReference) =>
            {
                factionReference.FactionGuid._Value = PlayerFaction;
            });
        }

        if (familiar.Has<Follower>())
        {
            familiar.With((ref Follower follower) =>
            {
                follower.Followed._Value = player;
                follower.ModeModifiable._Value = 0;
            });
        }

        if (familiar.Has<IsMinion>())
        {
            //familiar.Write(new IsMinion { Value = true }); // this or the entityOwner is sufficient apparently to stop player attacks, guess it was this? skellie priest summoning again as well
            // this prevents summoners from summoning so do not want
        }

        if (!familiar.Has<Minion>())
        {
            familiar.Add<Minion>(); //try taking this one off first and see if they summon things again, may also be related to entityOwner
            familiar.With((ref Minion minion) => minion.MasterDeathAction = MinionMasterDeathAction.Kill); // kill fam on owner death
        }

        if (familiar.Has<EntityOwner>())
        {
            familiar.Write(new EntityOwner { Owner = player }); //try taking this away? see if SCT persists and what else is affected, like if they now just die immediately when summoned -_- leaving that then
        }

        if (!familiar.Has<BlockFeedBuff>()) familiar.Add<BlockFeedBuff>();

        var followerBuffer = player.ReadBuffer<FollowerBuffer>();
        followerBuffer.Add(new FollowerBuffer { Entity = NetworkedEntity.ServerEntity(familiar) });
    }
    public static void ModifyBloodSource(Entity familiar, int level)
    {
        BloodConsumeSource bloodConsumeSource = familiar.Read<BloodConsumeSource>();
        bloodConsumeSource.BloodQuality = level / (float)MaxFamiliarLevel * 100;
        bloodConsumeSource.CanBeConsumed = false;
        familiar.Write(bloodConsumeSource);
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
    public static readonly Dictionary<FamiliarStatType, float> FamiliarStatValues = new()
    {
        {FamiliarStatType.PhysicalCritChance, 0.2f},
        {FamiliarStatType.SpellCritChance, 0.2f},
        {FamiliarStatType.HealingReceived, 0.5f},
        {FamiliarStatType.PhysicalResistance, 0.2f},
        {FamiliarStatType.SpellResistance, 0.2f},
        {FamiliarStatType.CCReduction, 0.5f},
        {FamiliarStatType.ShieldAbsorb, 1f}
    };
    public static void ModifyDamageStatsForBattle(Entity familiar, int level, ulong steamId, int famKey)
    {
        float scalingFactor = 0.25f + (level / (float)MaxFamiliarLevel) * 0.75f; // Calculate scaling factor for power and such
        float healthScalingFactor = 1.0f + ((level - 1) / (float)MaxFamiliarLevel) * 1.5f; // Calculate scaling factor for max health

        if (level == MaxFamiliarLevel) healthScalingFactor = 2.5f;

        int prestigeLevel = 0;
        List<FamiliarStatType> stats = [];

        if (FamiliarPrestige && FamiliarPrestigeManager.LoadFamiliarPrestige(steamId).FamiliarPrestige.TryGetValue(famKey, out var prestigeData) && prestigeData.Key > 0)
        {
            prestigeLevel = prestigeData.Key;
            stats = prestigeData.Value;
        }

        // get base stats from original unit prefab then apply scaling
        PrefabGUID prefabGUID = familiar.Read<PrefabGUID>();

        if (prefabGUID.GuidHash.Equals(1945956671) && familiar.TryGetBuff(HideStaffBuff, out Entity buffEntity))
        {
            DestroyUtility.Destroy(EntityManager, buffEntity, DestroyDebugReason.TryRemoveBuff);
        }

        Entity original = PrefabCollectionSystem._PrefabGuidToEntityMap[prefabGUID];
        UnitStats unitStats = original.Read<UnitStats>();

        UnitStats familiarStats = familiar.Read<UnitStats>();
        familiarStats.PhysicalPower._Value = unitStats.PhysicalPower._Value * scalingFactor * (1 + prestigeLevel * FamiliarPrestigeStatMultiplier);
        familiarStats.SpellPower._Value = unitStats.SpellPower._Value * scalingFactor * (1 + prestigeLevel * FamiliarPrestigeStatMultiplier);

        foreach (FamiliarStatType stat in stats)
        {
            switch (stat)
            {
                case FamiliarStatType.PhysicalCritChance:
                    familiarStats.PhysicalCriticalStrikeChance._Value = FamiliarStatValues[FamiliarStatType.PhysicalCritChance] * (1 + prestigeLevel * FamiliarPrestigeStatMultiplier);
                    break;
                case FamiliarStatType.SpellCritChance:
                    familiarStats.SpellCriticalStrikeChance._Value = FamiliarStatValues[FamiliarStatType.SpellCritChance] * (1 + prestigeLevel * FamiliarPrestigeStatMultiplier);
                    break;
                case FamiliarStatType.HealingReceived:
                    familiarStats.HealingReceived._Value = FamiliarStatValues[FamiliarStatType.HealingReceived] * (1 + prestigeLevel * FamiliarPrestigeStatMultiplier);
                    break;
                case FamiliarStatType.PhysicalResistance:
                    familiarStats.PhysicalResistance._Value = FamiliarStatValues[FamiliarStatType.PhysicalResistance] * (1 + prestigeLevel * FamiliarPrestigeStatMultiplier);
                    break;
                case FamiliarStatType.SpellResistance:
                    familiarStats.SpellResistance._Value = FamiliarStatValues[FamiliarStatType.SpellResistance] * (1 + prestigeLevel * FamiliarPrestigeStatMultiplier);
                    break;
                case FamiliarStatType.CCReduction:
                    familiarStats.CCReduction._Value = (int)(FamiliarStatValues[FamiliarStatType.CCReduction] * (1 + prestigeLevel * FamiliarPrestigeStatMultiplier));
                    break;
                case FamiliarStatType.ShieldAbsorb:
                    familiarStats.ShieldAbsorbModifier._Value = unitStats.ShieldAbsorbModifier._Value + FamiliarStatValues[FamiliarStatType.ShieldAbsorb] * (1 + prestigeLevel * FamiliarPrestigeStatMultiplier);
                    break;
            }
        }

        familiar.Write(familiarStats);

        UnitLevel unitLevel = familiar.Read<UnitLevel>();
        unitLevel.Level._Value = level;
        unitLevel.HideLevel = false;
        familiar.Write(unitLevel);

        Health familiarHealth = familiar.Read<Health>();
        int baseHealth = 500;

        if (GameDifficulty.Equals(GameDifficulty.Hard))
        {
            baseHealth = 750;
        }

        familiarHealth.MaxHealth._Value = baseHealth * healthScalingFactor;
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

        if (familiar.Has<SpawnPrefabOnGameplayEvent>()) // stop pilots spawning from gloomrot mechs
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

        if (prefabGUID.Equals(DivineAngel) && familiar.TryGetComponent(out Script_ApplyBuffUnderHealthThreshold_DataServer script_ApplyBuffUnderHealthThreshold_DataServer))
        {
            script_ApplyBuffUnderHealthThreshold_DataServer.NewBuffEntity = PrefabGUID.Empty;
            familiar.Write(script_ApplyBuffUnderHealthThreshold_DataServer);
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
    public static void ModifyDamageStats(Entity familiar, int level, ulong steamId, int famKey)
    {
        float scalingFactor = 0.1f + (level / (float)MaxFamiliarLevel) * 0.9f; // Calculate scaling factor for power and such
        float healthScalingFactor = 1.0f + ((level - 1) / (float)MaxFamiliarLevel) * 4.0f; // Calculate scaling factor for max health
        
        if (level == MaxFamiliarLevel) healthScalingFactor = 5.0f;

        int prestigeLevel = 0;
        List<FamiliarStatType> stats = [];

        if (FamiliarPrestige && FamiliarPrestigeManager.LoadFamiliarPrestige(steamId).FamiliarPrestige.TryGetValue(famKey, out var prestigeData) && prestigeData.Key > 0)
        {
            prestigeLevel = prestigeData.Key;
            stats = prestigeData.Value;
        }

        // get base stats from original unit prefab then apply scaling
        PrefabGUID prefabGUID = familiar.Read<PrefabGUID>();

        if (prefabGUID.GuidHash.Equals(1945956671) && familiar.TryGetBuff(HideStaffBuff, out Entity buffEntity))
        {
            DestroyUtility.Destroy(EntityManager, buffEntity, DestroyDebugReason.TryRemoveBuff);
        }

        Entity original = PrefabCollectionSystem._PrefabGuidToEntityMap[prefabGUID];
        UnitStats unitStats = original.Read<UnitStats>();

        UnitStats familiarStats = familiar.Read<UnitStats>();
        familiarStats.PhysicalPower._Value = unitStats.PhysicalPower._Value * scalingFactor * (1 + prestigeLevel * FamiliarPrestigeStatMultiplier);
        familiarStats.SpellPower._Value = unitStats.SpellPower._Value * scalingFactor * (1 + prestigeLevel * FamiliarPrestigeStatMultiplier);

        foreach (FamiliarStatType stat in stats)
        {
            switch (stat)
            {
                case FamiliarStatType.PhysicalCritChance:
                    familiarStats.PhysicalCriticalStrikeChance._Value = FamiliarStatValues[FamiliarStatType.PhysicalCritChance] * (1 + prestigeLevel * FamiliarPrestigeStatMultiplier);
                    break;
                case FamiliarStatType.SpellCritChance:
                    familiarStats.SpellCriticalStrikeChance._Value = FamiliarStatValues[FamiliarStatType.SpellCritChance] * (1 + prestigeLevel * FamiliarPrestigeStatMultiplier);
                    break;
                case FamiliarStatType.HealingReceived:
                    familiarStats.HealingReceived._Value = FamiliarStatValues[FamiliarStatType.HealingReceived] * (1 + prestigeLevel * FamiliarPrestigeStatMultiplier);
                    break;
                case FamiliarStatType.PhysicalResistance:
                    familiarStats.PhysicalResistance._Value = FamiliarStatValues[FamiliarStatType.PhysicalResistance] * (1 + prestigeLevel * FamiliarPrestigeStatMultiplier);
                    break;
                case FamiliarStatType.SpellResistance:
                    familiarStats.SpellResistance._Value = FamiliarStatValues[FamiliarStatType.SpellResistance] * (1 + prestigeLevel * FamiliarPrestigeStatMultiplier);
                    break;
                case FamiliarStatType.CCReduction:
                    familiarStats.CCReduction._Value = (int)(FamiliarStatValues[FamiliarStatType.CCReduction] * (1 + prestigeLevel * FamiliarPrestigeStatMultiplier));
                    break;
                case FamiliarStatType.ShieldAbsorb:
                    familiarStats.ShieldAbsorbModifier._Value = unitStats.ShieldAbsorbModifier._Value + FamiliarStatValues[FamiliarStatType.ShieldAbsorb] * (1 + prestigeLevel * FamiliarPrestigeStatMultiplier);
                    break;
            }
        }

        familiar.Write(familiarStats);

        UnitLevel unitLevel = familiar.Read<UnitLevel>();
        unitLevel.Level._Value = level;
        unitLevel.HideLevel = false;
        familiar.Write(unitLevel);

        Health familiarHealth = familiar.Read<Health>();
        int baseHealth = 500;

        if (GameDifficulty.Equals(GameDifficulty.Hard))
        {
            baseHealth = 750;
        }

        familiarHealth.MaxHealth._Value = baseHealth * healthScalingFactor;
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

        if (familiar.Has<SpawnPrefabOnGameplayEvent>()) // stop pilots spawning from gloomrot mechs
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

        if (prefabGUID.Equals(DivineAngel) && familiar.TryGetComponent(out Script_ApplyBuffUnderHealthThreshold_DataServer script_ApplyBuffUnderHealthThreshold_DataServer))
        {
            script_ApplyBuffUnderHealthThreshold_DataServer.NewBuffEntity = PrefabGUID.Empty;
            familiar.Write(script_ApplyBuffUnderHealthThreshold_DataServer);
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
        ModifiableBool modifiableBool = new() { _Value = false };
        CanPreventDisableWhenNoPlayersInRange canPreventDisable = new() { CanDisable = modifiableBool };
        EntityManager.AddComponentData(familiar, canPreventDisable);
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
    static void ManualAggroHandling(Entity familiar)
    {
        //if (familiar.Has<EntitiesInView_Server>()) familiar.Remove<EntitiesInView_Server>(); see if new handling still works without touching this

        familiar.With((ref AggroConsumer aggroConsumer) =>
        {
            aggroConsumer.ProximityRadius = 0f;
            aggroConsumer.ProximityWeight = 0f;
        });

        familiar.With((ref AlertModifiers alertModifiers) =>
        {
            alertModifiers.CircleRadiusFactor._Value = 0f;
            alertModifiers.ConeRadiusFactor._Value = 0f;
        });

        familiar.With((ref AggroModifiers aggroModifiers) =>
        {
            aggroModifiers.CircleRadiusFactor._Value = 0f;
            aggroModifiers.ConeRadiusFactor._Value = 0f;
        });

        familiar.With((ref GainAggroByVicinity gainAggroByVicinity) =>
        {
            gainAggroByVicinity.Value.AggroValue = 0f;
        });

        familiar.With((ref GainAlertByVicinity gainAlertByVicinity) =>
        {
            gainAlertByVicinity.Value.AggroValue = 0f;
        });
    }
}
