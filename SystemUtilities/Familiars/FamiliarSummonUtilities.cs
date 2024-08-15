using Bloodcraft.Patches;
using Bloodcraft.Services;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Entities;
using Unity.Transforms;
using VampireCommandFramework;

namespace Bloodcraft.SystemUtilities.Familiars;
internal static class FamiliarSummonUtilities
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static DebugEventsSystem DebugEventsSystem => Core.DebugEventsSystem;

    static readonly GameDifficulty GameDifficulty = Core.ServerGameSettings.GameDifficulty;
    static PrefabCollectionSystem PrefabCollectionSystem => Core.PrefabCollectionSystem;
    static EntityCommandBufferSystem EntityCommandBufferSystem => Core.EntityCommandBufferSystem;

    static readonly float VBloodDamageMultiplier = Plugin.VBloodDamageMultiplier.Value;
    static readonly float PlayerVampireDamageMultiplier = Plugin.PlayerVampireDamageMultiplier.Value;
    static readonly float FamiliarStatMultiplier = Plugin.FamiliarPrestigeStatMultiplier.Value;
    static readonly int MaxFamiliarLevel = Plugin.MaxFamiliarLevel.Value;

    static readonly bool FamiliarPrestige = Plugin.FamiliarPrestige.Value;
    static readonly bool FamiliarCombat = Plugin.FamiliarCombat.Value;

    static readonly PrefabGUID invulnerableBuff = new(-480024072);
    static readonly PrefabGUID ignoredFaction = new(-1430861195);
    static readonly PrefabGUID playerFaction = new(1106458752);
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
            Team = SpawnDebugEvent.TeamEnum.Neutral, // try neutral, then write player team to fam?
            Level = 1,
            Position = character.Read<LocalToWorld>().Position,
            DyeIndex = 0
        };

        DebugEventsSystem.SpawnDebugEvent(index, ref debugEvent, entityCommandBuffer, ref fromCharacter);
    }
    public static bool HandleFamiliar(Entity player, Entity familiar)
    {
        User user = player.Read<PlayerCharacter>().UserEntity.Read<User>();
        try
        {
            int level = familiar.Read<UnitLevel>().Level._Value;
            int famKey = familiar.Read<PrefabGUID>().GuidHash;
            ulong steamId = user.PlatformId;

            if (Core.FamiliarExperienceManager.LoadFamiliarExperience(steamId).FamiliarExperience.TryGetValue(famKey, out var xpData) && xpData.Key > 1)
            {
                level = xpData.Key;
            }

            if (HandleFamiliarModifications(user, steamId, famKey, player, familiar, level)) return true;
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
    public static bool HandleFamiliarModifications(User user, ulong steamId, int famKey, Entity player, Entity familiar, int level)
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
            if (!FamiliarCombat) DisableCombat(player, familiar);
            if (Core.PlayerBool(steamId, "FamiliarVisual"))
            {
                Core.DataStructures.FamiliarBuffsData data = Core.FamiliarBuffsManager.LoadFamiliarBuffs(steamId);
                if (data.FamiliarBuffs.ContainsKey(famKey)) FamiliarPatches.HandleVisual(familiar, new(data.FamiliarBuffs[famKey][0]));
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
        factionReference.FactionGuid._Value = playerFaction; // need friendly to players hostile to everything else? also why does playerFaction target castles? also try to mess with these again like with wolves to double check
        familiar.Write(factionReference);

        Follower follower = familiar.Read<Follower>();
        follower.Followed._Value = player;
        follower.ModeModifiable._Value = 0;
        familiar.Write(follower);

        Team playerTeam = player.Read<Team>();
        Team familiarTeam = familiar.Read<Team>();

        familiarTeam.Value = playerTeam.Value;
        familiar.Write(familiarTeam);

        var followerBuffer = player.ReadBuffer<FollowerBuffer>();
        followerBuffer.Add(new FollowerBuffer { Entity = NetworkedEntity.ServerEntity(familiar) });
    }
    public static void ModifyBloodSource(Entity familiar, int level)
    {
        BloodConsumeSource bloodConsumeSource = familiar.Read<BloodConsumeSource>();
        bloodConsumeSource.BloodQuality = level / (float)MaxFamiliarLevel * 100;
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
        float scalingFactor = 0.1f + (level / (float)MaxFamiliarLevel)*0.9f; // Calculate scaling factor
        float healthScalingFactor = 1.0f + (level / (float)MaxFamiliarLevel) * 4.0f; // Calculate scaling factor for max health

        int prestigeLevel = 0;
        List<FamiliarStatType> stats = [];

        if (Core.FamiliarPrestigeManager.LoadFamiliarPrestige(steamId).FamiliarPrestige.TryGetValue(famKey, out var prestigeData) && prestigeData.Key > 0)
        {
            prestigeLevel = prestigeData.Key;
            stats = prestigeData.Value;
        }

        if (!FamiliarPrestige)
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
    public static class FamiliarUtilities
    {
        public static Entity FindPlayerFamiliar(Entity characterEntity)
        {
            var followers = characterEntity.ReadBuffer<FollowerBuffer>();
            ulong steamId = characterEntity.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;

            if (!followers.IsEmpty) // if buffer not empty check here first, only need the rest if familiar is disabled via call/dismiss since that removes from followerBuffer to enable waygate use and such
            {
                foreach (FollowerBuffer follower in followers)
                {
                    Entity familiar = follower.Entity._Entity;
                    if (!familiar.Has<CharmSource>() && familiar.Exists()) return familiar;
                }
            }
            else if (Core.DismissedFamiliar(steamId, out Entity familiar)) return familiar;
            return Entity.Null;
            /*
            else
            {
                foreach (var follower in followers)
                {
                    PrefabGUID PrefabGUID = follower.Entity._Entity.Read<PrefabGUID>();
                    if (PrefabGUID.GuidHash.Equals(data.FamKey)) return follower.Entity._Entity;
                }
            }
            */
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
        public static void HandleFamiliarMinions(Entity familiar)
        {
            if (FamiliarPatches.FamiliarMinions.ContainsKey(familiar))
            {
                foreach (Entity minion in FamiliarPatches.FamiliarMinions[familiar])
                {
                    DestroyUtility.CreateDestroyEvent(EntityManager, minion, DestroyReason.Default, DestroyDebugReason.None);
                }
                FamiliarPatches.FamiliarMinions.Remove(familiar);
            }
        }
        public static bool TryParseFamiliarStat(string statType, out FamiliarStatType parsedStatType)
        {
            if (Enum.TryParse(statType, true, out parsedStatType))
            {
                return true;
            }

            parsedStatType = Enum.GetValues(typeof(FamiliarStatType))
                .Cast<FamiliarStatType>()
                .FirstOrDefault(pt => pt.ToString().Contains(statType, StringComparison.OrdinalIgnoreCase));

            if (!parsedStatType.Equals(default(FamiliarStatType)))
            {
                return true;
            }

            parsedStatType = default;
            return false;
        }
        public static void ToggleShinies(ChatCommandContext ctx, ulong steamId)
        {
            if (Core.DataStructures.PlayerBools.TryGetValue(steamId, out var bools))
            {
                bools["FamiliarVisual"] = !bools["FamiliarVisual"];
                Core.DataStructures.PlayerBools[steamId] = bools;
                Core.DataStructures.SavePlayerBools();
                LocalizationService.HandleReply(ctx, bools["FamiliarVisual"] ? "Shiny familiars <color=green>enabled</color>." : "Shiny familiars <color=red>disabled</color>.");
            }
        }
        public static void ToggleVBloodEmotes(ChatCommandContext ctx, ulong steamId)
        {
            if (Core.DataStructures.PlayerBools.TryGetValue(steamId, out var bools))
            {
                bools["VBloodEmotes"] = !bools["VBloodEmotes"];
                Core.DataStructures.PlayerBools[steamId] = bools;
                Core.DataStructures.SavePlayerBools();
                LocalizationService.HandleReply(ctx, bools["VBloodEmotes"] ? "VBlood Emotes <color=green>enabled</color>." : "VBlood Emotes <color=red>disabled</color>.");
            }
        }
    }
}
/*   
//TeamReference familiarReference = familiar.Read<TeamReference>();

//if (UnitTeam == Entity.Null) UnitTeam = familiarReference.Value._Value;
        
// adding unitTeam entity to player team entity allies buffer doesnt seem to have an effect, will now add to user team allies buffer as well then check again
// seems to be some significance to LocalTeam, wonder if that refers to the user team or the player team? seems to control familiar bar being blue or beige which seems like a good indicator of being full-allied or not
// seems like adding the familiarTeamEntity to the player team allies buffer and user team allies buffer prevents skeleton minions from attacking the familiar without modifying the skeletons?
// but for some reason it triggers combat for the familiar and therefore the player when they die that doesn't go away until next encounter? weird
// would expect if this prevents minions from attacking would also work for servants but guess not   
// question is, why does making the familiar TeamReference entity the same as the player user team entity make the bar blue?
// maybe try stripping allies buffer at some point and see if it makes things attack each other

//TeamReference playerReference = player.Read<TeamReference>();
//TeamReference userReference = userEntity.Read<TeamReference>();
        
static readonly FactionEnum Hostile =
    FactionEnum.Bandits |
    FactionEnum.Undead |
    FactionEnum.Militia |
    FactionEnum.ChurchOfLum |
    FactionEnum.Harpy |
    FactionEnum.Werewolves |
    FactionEnum.NatureSpirit |
    FactionEnum.Plants |
    FactionEnum.Spiders |
    FactionEnum.VampireHunters |
    FactionEnum.Cursed |
    FactionEnum.Gloomrot |
    FactionEnum.Elementals |
    FactionEnum.Mutants |
    FactionEnum.Legion |
    FactionEnum.ChurchOfLum_Slaves |
    FactionEnum.ChurchOfLum_Slaves_Rioters |
    FactionEnum.WerewolvesHuman |
    FactionEnum.Spot_ShapeshiftHuman;

static readonly FactionEnum Friendly =
    FactionEnum.Players |
    FactionEnum.PlayerMutant |
    FactionEnum.PlayerCastlePrisoner |
    FactionEnum.Ignored |
    FactionEnum.Players_ShapeshiftHuman |
    FactionEnum.Spiders_Shapeshift;
public unsafe static void ModifyFaction(PrefabGUID factionPrefab)
{
    NativeArray<Entity> entities = FactionLookupSingleton.FactionPrefabEntityLookup;
    try
    {
        foreach (Entity entity in entities)
        {

            if (entity.Read<PrefabGUID>().Equals(factionPrefab))
            {
                // Get BlobAssetReference from Faction
                Faction faction = entity.Read<Faction>();
                BlobAssetReference<FactionBlobAsset> blobAssetReferenceFaction = faction.Data;
                FactionBlobAsset* factionBlobAsset = (FactionBlobAsset*)blobAssetReferenceFaction.GetUnsafePtr();

                // Allocate memory for the new FactionBlobAsset
                long size = UnsafeUtility.SizeOf<FactionBlobAsset>();
                byte* allocatedMemory = (byte*)UnsafeUtility.Malloc(size, 16, Allocator.Persistent);

                // Copy existing data
                UnsafeUtility.MemCpy(allocatedMemory, factionBlobAsset, size);

                // Cast the allocated memory to FactionBlobAsset
                FactionBlobAsset* newFactionBlobAsset = (FactionBlobAsset*)allocatedMemory;

                // Update faction values as desired
                newFactionBlobAsset->Hostile = Hostile;
                newFactionBlobAsset->Friendly = Friendly;

                // Create the new BlobAssetReference from the allocated memory
                BlobAssetReference<FactionBlobAsset> newBlobAssetReference = BlobAssetReference<FactionBlobAsset>.Create(newFactionBlobAsset, (int)size);

                // Assign the new BlobAssetReference back to the Faction component
                faction.Data = newBlobAssetReference;
                entity.Write(faction);

                // Free the temporary allocated memory and break
                UnsafeUtility.Free(allocatedMemory, Allocator.Persistent);
            }

            Faction faction = entity.Read<Faction>();
            BlobAssetReference<FactionBlobAsset> blobAssetReference = faction.Data;
            FactionBlobAsset* factionBlobAsset = (FactionBlobAsset*)blobAssetReference.GetUnsafePtr();

            PrefabGUID factionPrefab = entity.Read<PrefabGUID>();
            int factionIndex = factionBlobAsset->Index;
            FamiliarSummonUtilities.FactionIndices.TryAdd(factionPrefab, factionIndex);


            Array factions = Enum.GetValues(typeof(FactionEnum));
            Core.Log.LogInfo($"Faction: {entity.Read<PrefabGUID>().LookupName()} | Index: {factionBlobAsset->Index}");
            Core.Log.LogInfo($"Members: {string.Join(", ", CollectEnumValues(factionBlobAsset->Membership, factions))}");
            Core.Log.LogInfo($"Friendly: {string.Join(", ", CollectEnumValues(factionBlobAsset->Friendly, factions))}");
            Core.Log.LogInfo($"Hostile: {string.Join(", ", CollectEnumValues(factionBlobAsset->Hostile, factions))}");
            Core.Log.LogInfo($"Neutral: {string.Join(", ", CollectEnumValues(factionBlobAsset->Neutral, factions))}");

        }
    }
    catch (Exception e)
    {
        Core.Log.LogError($"Error: {e}");
    }
}
static List<string> CollectEnumValues(FactionEnum factionEnum, Array factions)
{
    List<string> values = [];
    foreach (FactionEnum faction in factions)
    {
        if (factionEnum.HasFlag(faction) && faction != FactionEnum.None)
        {
            values.Add(faction.ToString());
        }
    }
    return values;
}
*/