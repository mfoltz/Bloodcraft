using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Entities;
using Unity.Transforms;

namespace Bloodcraft.Systems.Familiars;
internal static class FamiliarSummonUtilities
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static DebugEventsSystem DebugEventsSystem => Core.DebugEventsSystem;
    static GameDifficulty GameDifficulty => Core.ServerGameSettings.GameDifficulty;
    static FactionLookupSingleton FactionLookupSingleton => Core.FactionLookupSingleton;
    static PrefabCollectionSystem PrefabCollectionSystem => Core.PrefabCollectionSystem;
    static EntityCommandBufferSystem EntityCommandBufferSystem => Core.EntityCommandBufferSystem;

    static readonly float VBloodDamageMultiplier = Plugin.VBloodDamageMultiplier.Value;
    static readonly float PlayerVampireDamageMultiplier = Plugin.PlayerVampireDamageMultiplier.Value;
    static readonly float FamiliarStatMultiplier = Plugin.FamiliarPrestigeStatMultiplier.Value;
    static readonly bool FamiliarCombat = Plugin.FamiliarCombat.Value;

    static readonly PrefabGUID invulnerableBuff = new(-480024072);
    static readonly PrefabGUID ignoredFaction = new(-1430861195);
    static readonly PrefabGUID playerFaction = new(1106458752);
    static readonly PrefabGUID pvpProtBuff = new(1111481396);
    static readonly PrefabGUID playersMutant = new(2146780972);
    static readonly PrefabGUID farbaneMerchants = new(30052367);
    static readonly PrefabGUID unitTeamPrefab = new(-1434736744);
    static readonly PrefabGUID clanTeamPrefab = new(-501996644);

    public static Dictionary<PrefabGUID, int> FactionIndices = [];

    public static void SummonFamiliar(Entity character, Entity userEntity, int famKey)
    {
        EntityCommandBuffer entityCommandBuffer = EntityCommandBufferSystem.CreateCommandBuffer();

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

        DebugEventsSystem.SpawnDebugEvent(index, ref debugEvent, entityCommandBuffer, ref fromCharacter);
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
        //ModifyAggro(familiar);
        ModifyConvertable(familiar);
        ModifyCollision(familiar);
        ModifyDropTable(familiar);
        PreventDisableFamiliar(familiar);
        //ModifyBehaviour(familiar);
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

        ApplyBuffDebugEvent applyBuffDebugEvent = new()
        {
            BuffPrefabGUID = invulnerableBuff,
        };

        FromCharacter fromCharacter = new()
        {
            Character = familiar,
            User = player.Read<PlayerCharacter>().UserEntity,
        };

        DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
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
        FactionReference factionReference = familiar.Read<FactionReference>();
        factionReference.FactionGuid._Value = playerFaction;
        familiar.Write(factionReference);

        Follower follower = familiar.Read<Follower>();
        follower.Followed._Value = player;
        follower.ModeModifiable._Value = 0;
        familiar.Write(follower);

        /*
        Team playerTeam = player.Read<Team>();
        TeamReference teamReference = player.Read<TeamReference>();
        Entity playerTeamEntity = teamReference.Value._Value;
        TeamData teamData = playerTeamEntity.Read<TeamData>();

        Team familiarTeam = familiar.Read<Team>();
        TeamReference familiarTeamReference = familiar.Read<TeamReference>();
        Entity familiarTeamEntity = familiarTeamReference.Value._Value;

        foreach (Entity entity in PlayerService.PlayerCache.Values)
        {
            if (!EntityManager.Exists(entity)) continue;
        }
        
        //playerTeamEntity.LogComponentTypes();
        //familiarTeamEntity.LogComponentTypes();  copy of user team basically

        //if (familiarTeamEntity.Has<UserTeam>()) Core.Log.LogInfo($"Name on familiar UserTeam: {familiarTeamEntity.Read<UserTeam>().UserEntity.Read<User>().CharacterName.Value}");
        Core.Log.LogInfo($"Player: {playerTeam.Value}/{teamData.TeamValue} | Familiar: {familiarTeam.Value}/{familiarTeamData.TeamValue}");

        var teamAllies = playerTeamEntity.ReadBuffer<TeamAllies>();

        TeamAllies playerFamiliarTeam = new()
        {
            Value = Entity.Null
        };
        
        Core.Log.LogInfo("Player Allies|");
        for (int i = 0; i < teamAllies.Length; i++)
        {
            Entity allyEntity = teamAllies[i].Value;
            TeamData allyTeamData = allyEntity.Read<TeamData>();
            Core.Log.LogInfo($"{allyEntity.Read<PrefabGUID>().LookupName()}: {allyTeamData.TeamValue}");
            if (allyEntity.Read<PrefabGUID>().Equals(clanTeamPrefab) && playerFamiliarTeam.Value.Equals(Entity.Null))
            {
                playerFamiliarTeam = teamAllies[i];
            }
        }

        teamAllies = familiarTeamEntity.ReadBuffer<TeamAllies>();
        HashSet<TeamAllies> added = [];
        foreach (Entity entity in PlayerService.PlayerCache.Values)
        {
            if (!EntityManager.Exists(entity)) continue;
            TeamReference reference = entity.Read<TeamReference>();
            var alliesBuffer = reference.Value._Value.ReadBuffer<TeamAllies>();
            foreach (TeamAllies team in alliesBuffer)
            {
                if (EntityManager.Exists(team.Value) && team.Value.Read<PrefabGUID>().Equals(clanTeamPrefab) && !added.Contains(team) && !playerFamiliarTeam.Value.Equals(Entity.Null))
                {
                    alliesBuffer.Add(playerFamiliarTeam);
                    teamAllies.Add(team);
                    added.Add(team);
                }
            }
        }

        Core.Log.LogInfo("Familiar Allies|");
        for (int i = 0; i < teamAllies.Length; i++)
        {
            Entity allyEntity = teamAllies[i].Value;
            if (!EntityManager.Exists(allyEntity)) continue;
            TeamData allyTeamData = allyEntity.Read<TeamData>();
            Core.Log.LogInfo($"{allyEntity.Read<PrefabGUID>().LookupName()}: {allyTeamData.TeamValue}");
            //TeamUtility
        }

        foreach (Entity entity in PlayerService.PlayerCache.Values)
        {
            if (!EntityManager.Exists(entity)) continue;
            Entity teamEntity = entity.Read<TeamReference>().Value._Value;
            TeamData allyTeamData = teamEntity.Read<TeamData>();
            Core.Log.LogInfo($"{teamEntity.Read<PrefabGUID>().LookupName()}: {allyTeamData.TeamValue}");
            var alliesBuffer = teamEntity.ReadBuffer<TeamAllies>();
            for (int i = 0; i < alliesBuffer.Length; i++)
            {
                Entity allyEntity = alliesBuffer[i].Value;
                if (!EntityManager.Exists(allyEntity)) continue;
                TeamData allyData = allyEntity.Read<TeamData>();
                Core.Log.LogInfo($"{allyEntity.Read<PrefabGUID>().LookupName()}: {allyData.TeamValue}");
            }

            if (ServerGameManager.IsAllies(familiar, entity))
            {
                Core.Log.LogInfo($"{entity.Read<User>().CharacterName.Value} is an ally of the familiar");
            }
            else
            {
                Core.Log.LogInfo($"{entity.Read<User>().CharacterName.Value} is not an ally of the familiar");
            }
        }
        

        AggroConsumer aggroConsumer = familiar.Read<AggroConsumer>();
        aggroConsumer.Active._Value = false;
        familiar.Write(aggroConsumer);

        if (familiar.Has<GainAggroByAlert>()) familiar.Remove<GainAggroByAlert>();
        if (familiar.Has<GainAggroByVicinity>()) familiar.Remove<GainAggroByVicinity>();
        if (familiar.Has<GainAlertByVicinity>()) familiar.Remove<GainAlertByVicinity>();

        
        Aggroable aggroable = familiar.Read<Aggroable>();
        aggroable.Value._Value = false;
        aggroable.DistanceFactor._Value = 0f;
        aggroable.AggroFactor._Value = 0f;
        familiar.Write(aggroable);
        */

        if (ServerGameManager.HasBuff(player, pvpProtBuff.ToIdentifier()))
        {
            UnitStats unitStats = familiar.Read<UnitStats>();
            unitStats.PvPProtected._Value = true;
            familiar.Write(unitStats);
        }

        var followerBuffer = player.ReadBuffer<FollowerBuffer>();
        followerBuffer.Add(new FollowerBuffer { Entity = NetworkedEntity.ServerEntity(familiar) });
    }
    public static void ModifyBloodSource(Entity familiar, int level)
    {
        BloodConsumeSource bloodConsumeSource = familiar.Read<BloodConsumeSource>();
        bloodConsumeSource.BloodQuality = level / (float)Plugin.MaxFamiliarLevel.Value * 100;
        bloodConsumeSource.CanBeConsumed = false;
        familiar.Write(bloodConsumeSource);
        familiar.Add<BlockFeedBuff>();
    }
    static void ModifyBehaviour(Entity familiar)
    {
        //BehaviourTreeNodeInstanceElement behaviourTreeNodeInstanceElement = familiar.Read<BehaviourTreeNodeInstanceElement>();
        //BehaviourTreeNodeInstance behaviourTreeNodeInstance = behaviourTreeNodeInstanceElement.Value;

        //BehaviourTreeInstance behaviourTreeInstance = familiar.Read<BehaviourTreeInstance>();
       
        //BehaviourTreeBinding behaviourTreeBinding = familiar.Read<BehaviourTreeBinding>();
        //Entity behaviourEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[behaviourTreeBinding.PrefabGUID];
        //BehaviourTree behaviourTree = behaviourEntity.Read<BehaviourTree>();
        //BehaviourTreeBlob* treeBlob = (BehaviourTreeBlob*)behaviourTree.Blob;
        //IntPtr nodePtr = treeBlob->Nodes;
        //Blackboard
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
        ModifiableBool modifiableBool = new() { _Value = false };
        CanPreventDisableWhenNoPlayersInRange canPreventDisable = new() { CanDisable = modifiableBool };
        EntityManager.AddComponentData(familiar, canPreventDisable);
    }

    /*
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
    */
    static void ModifyAggro(Entity familiar)
    {
        AggroConsumer aggroConsumer = familiar.Read<AggroConsumer>();
        aggroConsumer.MaxDistanceFromPreCombatPosition = 30f;
        //aggroConsumer.ProximityRadius = 0f;
        aggroConsumer.RemoveDelay = 10f;
        aggroConsumer.ProximityWeight = 0f;
        //aggroConsumer.RecieveAlerts = false;
        familiar.Write(aggroConsumer);

        if (familiar.Has<GainAggroByAlert>()) familiar.Remove<GainAggroByAlert>();
        if (familiar.Has<GainAggroByVicinity>()) familiar.Remove<GainAggroByVicinity>();
        if (familiar.Has<GainAlertByVicinity>()) familiar.Remove<GainAlertByVicinity>();
        if (familiar.Has<AggroModifiers>()) familiar.Remove<AggroModifiers>();
        if (familiar.Has<AlertModifiers>()) familiar.Remove<AlertModifiers>();
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
            if (Core.DataStructures.FamiliarActives.TryGetValue(platformId, out var data) && EntityManager.Exists(data.Familiar))
            {
                return data.Familiar;
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
        public static void ClearFamiliarActives(ulong steamId)
        {
            if (Core.DataStructures.FamiliarActives.TryGetValue(steamId, out var actives))
            {
                actives = (Entity.Null, 0);
                Core.DataStructures.FamiliarActives[steamId] = actives;
                Core.DataStructures.SavePlayerFamiliarActives();
            }
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
