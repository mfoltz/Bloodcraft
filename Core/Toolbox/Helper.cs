using Bloodstone.API;
using Il2CppInterop.Runtime;
using Il2CppSystem;
using ProjectM;
using ProjectM.Gameplay.Clan;
using ProjectM.Network;
using ProjectM.Shared;
using Unity;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using VCreate.Core.Services;
using VCreate.Data;
using Buff = ProjectM.Buff;

namespace VCreate.Core.Toolbox;

public static class Helper
{
    private struct MatchItem
    {
        public int Score;

        public Item Item;
    }

    public const int NO_DURATION = 0;

    public const int DEFAULT_DURATION = -1;

    public const int RANDOM_POWER = -1;

    public static DebugEventsSystem debugEventsSystem = VWorld.Server.GetExistingSystem<DebugEventsSystem>();

    public static NetworkIdSystem networkIdSystem = VWorld.Server.GetExistingSystem<NetworkIdSystem>();

    public static JewelSpawnSystem jewelSpawnSystem = VWorld.Server.GetExistingSystem<JewelSpawnSystem>();

    public static AdminAuthSystem adminAuthSystem = VWorld.Server.GetExistingSystem<AdminAuthSystem>();

    public static PrefabCollectionSystem prefabCollectionSystem = VWorld.Server.GetExistingSystem<PrefabCollectionSystem>();

    public static ClanSystem_Server clanSystem = VWorld.Server.GetExistingSystem<ClanSystem_Server>();

    public static EntityCommandBufferSystem entityCommandBufferSystem = VWorld.Server.GetExistingSystem<EntityCommandBufferSystem>();

    public static ServerBootstrapSystem serverBootstrapSystem = VWorld.Server.GetExistingSystem<ServerBootstrapSystem>();

    public static NativeHashSet<PrefabGUID> prefabGUIDs;

    public static System.Random random = new System.Random();

    public static PrefabGUID GetPrefabGUID(Entity entity)
    {
        EntityManager entityManager = VWorld.Server.EntityManager;
        PrefabGUID componentData = default;
        try
        {
            componentData = entityManager.GetComponentData<PrefabGUID>(entity);
            return componentData;
        }
        catch
        {
            componentData.GuidHash = 0;
        }

        return componentData;
    }

    public static void ResetCharacter(Entity Character)
    {
        ResetCooldown(Character);
        HealCharacter(Character);
        UnbuffCharacter(Character, Prefabs.Buff_InCombat_PvPVampire);
    }

    public static void ReviveCharacter(Entity Character, Entity User)
    {
        float3 position = Character.Read<LocalToWorld>().Position;
        ServerBootstrapSystem existingSystem = VWorld.Server.GetExistingSystem<ServerBootstrapSystem>();
        EntityCommandBufferSystem existingSystem2 = VWorld.Server.GetExistingSystem<EntityCommandBufferSystem>();
        EntityCommandBuffer commandBuffer = existingSystem2.CreateCommandBuffer();
        Nullable_Unboxed<float3> customSpawnLocation = default;
        customSpawnLocation.value = position;
        customSpawnLocation.has_value = true;
        Health componentData = Character.Read<Health>();
        if (BuffUtility.HasBuff(VWorld.Server.EntityManager, Character, Prefabs.Buff_General_Vampire_Wounded_Buff))
        {
            UnbuffCharacter(Character, Prefabs.Buff_General_Vampire_Wounded_Buff);
            componentData.Value = componentData.MaxHealth;
            componentData.MaxRecoveryHealth = componentData.MaxHealth;
            Character.Write(componentData);
        }

        if (componentData.IsDead)
        {
            existingSystem.RespawnCharacter(commandBuffer, User, customSpawnLocation, Character);
        }
    }

    public static void KillNearMouse(Entity Character, Entity User)
    {
        float3 aimPosition = User.Read<EntityInput>().AimPosition;
        Entity entity = VWorld.Server.EntityManager.CreateEntity(ComponentType.ReadWrite<FromCharacter>(), ComponentType.ReadWrite<ChangeHealthOfClosestToPositionDebugEvent>(), ComponentType.ReadWrite<NetworkEventType>(), ComponentType.ReadWrite<ReceiveNetworkEventTag>());
        entity.Write(new FromCharacter
        {
            Character = Character,
            User = User
        });
        entity.Write(new ChangeHealthOfClosestToPositionDebugEvent
        {
            Amount = -1000,
            Position = aimPosition
        });
        entity.Write(new NetworkEventType
        {
            IsAdminEvent = true,
            EventId = NetworkEvents.EventId_ChangeHealthOfClosestToPositionDebugEvent
        });
    }

    public static void SetPlayerBlood(Entity User, PrefabGUID bloodType, float quality = 100f)
    {
        NativeArray<Entity> prefabEntitiesByComponentTypes = GetPrefabEntitiesByComponentTypes<BloodConsumeSource>();
        Entity entity = prefabEntitiesByComponentTypes[0];
        BloodConsumeSource componentData = entity.Read<BloodConsumeSource>();
        PrefabGUID unitBloodType = componentData.UnitBloodType;
        componentData.UnitBloodType = bloodType;
        entity.Write(componentData);
        PrefabGUID source = entity.Read<PrefabGUID>();
        ConsumeBloodDebugEvent consumeBloodDebugEvent = default;
        consumeBloodDebugEvent.Amount = 100;
        consumeBloodDebugEvent.Quality = quality;
        consumeBloodDebugEvent.Source = source;
        ConsumeBloodDebugEvent clientEvent = consumeBloodDebugEvent;
        debugEventsSystem.ConsumeBloodEvent(User.Read<User>().Index, ref clientEvent);
        componentData.UnitBloodType = unitBloodType;
        entity.Write(componentData);
        prefabEntitiesByComponentTypes.Dispose();
    }

    public static NativeArray<Entity> GetPrefabEntitiesByComponentTypes<T1>()
    {
        EntityQueryOptions options = EntityQueryOptions.IncludePrefab;
        EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
        entityQueryDesc.All = new ComponentType[2]
        {
            new ComponentType(Il2CppType.Of<Prefab>()),
            new ComponentType(Il2CppType.Of<T1>())
        };
        entityQueryDesc.Options = options;
        EntityQueryDesc entityQueryDesc2 = entityQueryDesc;
        EntityQuery entityQuery = VWorld.Server.EntityManager.CreateEntityQuery(entityQueryDesc2);
        NativeArray<Entity> result = entityQuery.ToEntityArray(Allocator.Temp);
        entityQuery.Dispose();
        return result;
    }

    public static Entity CreateEntityWithComponents<T1>()
    {
        return VWorld.Server.EntityManager.CreateEntity(new ComponentType(Il2CppType.Of<T1>()));
    }

    public static Entity CreateEntityWithComponents<T1, T2>()
    {
        return VWorld.Server.EntityManager.CreateEntity(new ComponentType(Il2CppType.Of<T1>()), new ComponentType(Il2CppType.Of<T2>()));
    }

    public static Entity CreateEntityWithComponents<T1, T2, T3>()
    {
        return VWorld.Server.EntityManager.CreateEntity(new ComponentType(Il2CppType.Of<T1>()), new ComponentType(Il2CppType.Of<T2>()), new ComponentType(Il2CppType.Of<T3>()));
    }

    public static Entity CreateEntityWithComponents<T1, T2, T3, T4>()
    {
        return VWorld.Server.EntityManager.CreateEntity(new ComponentType(Il2CppType.Of<T1>()), new ComponentType(Il2CppType.Of<T2>()), new ComponentType(Il2CppType.Of<T3>()), new ComponentType(Il2CppType.Of<T4>()));
    }

    public static Entity CreateEntityWithComponents<T1, T2, T3, T4, T5>()
    {
        return VWorld.Server.EntityManager.CreateEntity(new ComponentType(Il2CppType.Of<T1>()), new ComponentType(Il2CppType.Of<T2>()), new ComponentType(Il2CppType.Of<T3>()), new ComponentType(Il2CppType.Of<T4>()), new ComponentType(Il2CppType.Of<T5>()));
    }

    public static Entity CreateEntityWithComponents<T1, T2, T3, T4, T5, T6>()
    {
        return VWorld.Server.EntityManager.CreateEntity(new ComponentType(Il2CppType.Of<T1>()), new ComponentType(Il2CppType.Of<T2>()), new ComponentType(Il2CppType.Of<T3>()), new ComponentType(Il2CppType.Of<T4>()), new ComponentType(Il2CppType.Of<T5>()), new ComponentType(Il2CppType.Of<T6>()));
    }

    public static NativeArray<Entity> GetEntitiesByComponentTypes<T1>(bool includeDisabled = false, bool includeSpawn = false)
    {
        EntityQueryOptions options = EntityQueryOptions.Default;

        // Add the IncludeDisabled option if includeDisabled is true
        if (includeDisabled)
        {
            options |= EntityQueryOptions.IncludeDisabled;
        }

        // Add the IncludePrefab option if includeSpawn is true
        if (includeSpawn)
        {
            options |= EntityQueryOptions.IncludePrefab;  // Assuming 'IncludeSpawn' refers to including prefabs
        }

        EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
        entityQueryDesc.All = new ComponentType[1]
        {
            new ComponentType(Il2CppType.Of<T1>())
        };
        entityQueryDesc.Options = options;
        EntityQueryDesc entityQueryDesc2 = entityQueryDesc;
        EntityQuery entityQuery = VWorld.Server.EntityManager.CreateEntityQuery(entityQueryDesc2);
        NativeArray<Entity> result = entityQuery.ToEntityArray(Allocator.Temp);
        entityQuery.Dispose();
        return result;
    }

    public static NativeArray<Entity> GetEntitiesByComponentTypes<T1, T2>(bool includeDisabled = false)
    {
        EntityQueryOptions options = includeDisabled ? EntityQueryOptions.IncludeDisabled : EntityQueryOptions.Default;
        EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
        entityQueryDesc.All = new ComponentType[2]
        {
            new ComponentType(Il2CppType.Of<T1>()),
            new ComponentType(Il2CppType.Of<T2>())
        };
        entityQueryDesc.Options = options;
        EntityQueryDesc entityQueryDesc2 = entityQueryDesc;
        EntityQuery entityQuery = VWorld.Server.EntityManager.CreateEntityQuery(entityQueryDesc2);
        NativeArray<Entity> result = entityQuery.ToEntityArray(Allocator.Temp);
        entityQuery.Dispose();
        return result;
    }

    public static NativeArray<Entity> GetEntitiesByComponentTypes<T1, T2, T3>(bool includeDisabled = false)
    {
        EntityQueryOptions options = includeDisabled ? EntityQueryOptions.IncludeDisabled : EntityQueryOptions.Default;
        EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
        entityQueryDesc.All = new ComponentType[3]
        {
            new ComponentType(Il2CppType.Of<T1>()),
            new ComponentType(Il2CppType.Of<T2>()),
            new ComponentType(Il2CppType.Of<T3>())
        };
        entityQueryDesc.Options = options;
        EntityQueryDesc entityQueryDesc2 = entityQueryDesc;
        EntityQuery entityQuery = VWorld.Server.EntityManager.CreateEntityQuery(entityQueryDesc2);
        NativeArray<Entity> result = entityQuery.ToEntityArray(Allocator.Temp);
        entityQuery.Dispose();
        return result;
    }

    public static List<Entity> GetClanMembersByUser(Entity User, bool includeStartingUser = true)
    {
        List<Entity> list = new List<Entity>();
        if (includeStartingUser)
        {
            list.Add(User);
        }

        NativeArray<Entity> entitiesByComponentTypes = GetEntitiesByComponentTypes<ClanRole>();
        Team team = User.Read<Team>();
        NativeArray<Entity>.Enumerator enumerator = entitiesByComponentTypes.GetEnumerator();
        while (enumerator.MoveNext())
        {
            Entity current = enumerator.Current;
            if (current != User && current.Read<Team>().Value == team.Value)
            {
                list.Add(current);
            }
        }

        return list;
    }

    public static void ClearInventory(Entity Character, bool all = false)
    {
        int num = 9;
        if (all)
        {
            num = 0;
        }

        for (int i = num; i < 36; i++)
        {
            InventoryUtilitiesServer.ClearSlot(VWorld.Server.EntityManager, Character, i);
        }
    }

    public static void UnlockResearch(FromCharacter fromCharacter)
    {
        debugEventsSystem.UnlockAllResearch(fromCharacter);
    }

    public static void UnlockVBloods(FromCharacter fromCharacter)
    {
        debugEventsSystem.UnlockAllVBloods(fromCharacter);
    }

    public static void UnlockAchievements(FromCharacter fromCharacter)
    {
        debugEventsSystem.CompleteAllAchievements(fromCharacter);
    }

    public static void UnlockContent(FromCharacter fromCharacter)
    {
        SetUserContentDebugEvent setUserContentDebugEvent = default;
        setUserContentDebugEvent.Value = UserContentFlags.EarlyAccess | UserContentFlags.DLC_DraculasRelics_EA | UserContentFlags.DLC_FoundersPack_EA | UserContentFlags.GiveAway_Razer01 | UserContentFlags.Halloween2022 | UserContentFlags.DLC_Gloomrot;
        SetUserContentDebugEvent clientEvent = setUserContentDebugEvent;
        debugEventsSystem.SetUserContentDebugEvent(fromCharacter.User.Read<User>().Index, ref clientEvent, ref fromCharacter);
    }

    public static void UnlockWaypoints(Entity User)
    {
        DynamicBuffer<UnlockedWaypointElement> dynamicBuffer = VWorld.Server.EntityManager.AddBuffer<UnlockedWaypointElement>(User);
        dynamicBuffer.Clear();
        ComponentType componentType = new ComponentType(Il2CppType.Of<ChunkWaypoint>());
        NativeArray<Entity>.Enumerator enumerator = VWorld.Server.EntityManager.CreateEntityQuery(componentType).ToEntityArray(Allocator.Temp).GetEnumerator();
        while (enumerator.MoveNext())
        {
            Entity current = enumerator.Current;
            UnlockedWaypointElement elem = default;
            elem.Waypoint = current.Read<NetworkId>();
            dynamicBuffer.Add(elem);
        }
    }

    public static void UnlockAll(FromCharacter fromCharacter)
    {
        UnlockResearch(fromCharacter);
        UnlockVBloods(fromCharacter);
        UnlockAchievements(fromCharacter);
        UnlockWaypoints(fromCharacter.User);
        UnlockContent(fromCharacter);
    }

    public static void RenamePlayer(FromCharacter fromCharacter, string newName)
    {
        NetworkId target = fromCharacter.User.Read<NetworkId>();
        RenameUserDebugEvent renameUserDebugEvent = default;
        renameUserDebugEvent.NewName = newName;
        renameUserDebugEvent.Target = target;
        RenameUserDebugEvent clientEvent = renameUserDebugEvent;
        debugEventsSystem.RenameUser(fromCharacter, clientEvent);
    }

    public static void ResetAllServants(Team playerTeam)
    {
        ComponentType componentType = new ComponentType(Il2CppType.Of<ServantCoffinstation>());
        NativeArray<Entity>.Enumerator enumerator = VWorld.Server.EntityManager.CreateEntityQuery(componentType).ToEntityArray(Allocator.Temp).GetEnumerator();
        while (enumerator.MoveNext())
        {
            Entity current = enumerator.Current;
            try
            {
                if (current.Read<Team>().Value == playerTeam.Value)
                {
                    Entity entity = current.Read<ServantCoffinstation>().ConnectedServant._Entity;
                    ServantEquipment componentData = entity.Read<ServantEquipment>();
                    componentData.Reset();
                    entity.Write(componentData);
                    StatChangeUtility.KillEntity(VWorld.Server.EntityManager, entity, Entity.Null, 0.0, killImmortals: true);
                }
            }
            catch (System.Exception)
            {
            }
        }
    }


    public static void RepairGear(Entity Character, bool repair = true)
    {
        Equipment equipment = Character.Read<Equipment>();
        NativeList<Entity> equipment2 = new NativeList<Entity>(Allocator.Temp);
        equipment.GetAllEquipmentEntities(equipment2);
        NativeArray<Entity>.Enumerator enumerator = equipment2.GetEnumerator();
        while (enumerator.MoveNext())
        {
            Entity current = enumerator.Current;
            if (current.Has<Durability>())
            {
                Durability componentData = current.Read<Durability>();
                if (repair)
                {
                    componentData.Value = componentData.MaxDurability;
                }
                else
                {
                    componentData.Value = 0f;
                }

                current.Write(componentData);
            }
        }

        equipment2.Dispose();
        for (int i = 0; i < 36; i++)
        {
            if (!InventoryUtilities.TryGetItemAtSlot(VWorld.Server.EntityManager, Character, i, out var item))
            {
                continue;
            }

            Entity entity = item.ItemEntity._Entity;
            if (entity.Has<Durability>())
            {
                Durability componentData2 = entity.Read<Durability>();
                if (repair)
                {
                    componentData2.Value = componentData2.MaxDurability;
                }
                else
                {
                    componentData2.Value = 0f;
                }

                entity.Write(componentData2);
            }
        }
    }

    public static bool TryGetPrefabGUIDFromString(string buffNameOrId, out PrefabGUID prefabGUID)
    {
        if (prefabCollectionSystem.NameToPrefabGuidDictionary.ContainsKey(buffNameOrId))
        {
            prefabGUID = prefabCollectionSystem.NameToPrefabGuidDictionary[buffNameOrId];
            return true;
        }

        if (int.TryParse(buffNameOrId, out var result))
        {
            PrefabGUID prefabGUID2 = new PrefabGUID(result);
            if (prefabCollectionSystem.PrefabGuidToNameDictionary.ContainsKey(prefabGUID2))
            {
                prefabGUID = prefabGUID2;
                return true;
            }
        }

        prefabGUID = default;
        return false;
    }

    public static bool TryGetItemPrefabGUIDFromString(string needle, out PrefabGUID prefabGUID)
    {
        List<MatchItem> list = new List<MatchItem>();
        foreach (Item giveableItem in Items.GiveableItems)
        {
            int num = IsSubsequence(needle, giveableItem.OverrideName.ToLower() + "s");
            if (num != -1)
            {
                list.Add(new MatchItem
                {
                    Score = num,
                    Item = giveableItem
                });
            }
        }

        foreach (Item giveableItem2 in Items.GiveableItems)
        {
            int num2 = IsSubsequence(needle, giveableItem2.FormalPrefabName.ToLower() + "s");
            if (num2 != -1)
            {
                list.Add(new MatchItem
                {
                    Score = num2,
                    Item = giveableItem2
                });
            }
        }

        if (int.TryParse(needle, out var result))
        {
            foreach (Item giveableItem3 in Items.GiveableItems)
            {
                if (result == giveableItem3.PrefabGUID.GuidHash)
                {
                    list.Add(new MatchItem
                    {
                        Score = int.MaxValue,
                        Item = giveableItem3
                    });
                }
            }
        }

        MatchItem matchItem = list.OrderByDescending((m) => m.Score).FirstOrDefault();
        if (matchItem.Item != null)
        {
            prefabGUID = matchItem.Item.PrefabGUID;
            return true;
        }

        prefabGUID = default;
        return false;
    }

    public static bool AddItemToInventory(Entity recipient, string needle, int amount, out Entity entity, bool equip = true)
    {
        if (TryGetItemPrefabGUIDFromString(needle, out var prefabGUID))
        {
            return AddItemToInventory(recipient, prefabGUID, amount, out entity, equip);
        }

        entity = default;
        return false;
    }

    public static bool AddItemToInventory(Entity recipient, PrefabGUID guid, int amount, out Entity entity, bool equip = true)
    {
        GameDataSystem existingSystem = VWorld.Server.GetExistingSystem<GameDataSystem>();
        AddItemSettings addItemSettings = AddItemSettings.Create(VWorld.Server.EntityManager, existingSystem.ItemHashLookupMap);
        addItemSettings.EquipIfPossible = equip;
        AddItemResponse addItemResponse = InventoryUtilitiesServer.TryAddItem(addItemSettings, recipient, guid, amount);
        if (addItemResponse.Success)
        {
            entity = addItemResponse.NewEntity;
            return true;
        }

        entity = default;
        return false;
    }

    public static void KillCharacter(Entity Character)
    {
        StatChangeUtility.KillEntity(VWorld.Server.EntityManager, Character, Character, 0.0, killImmortals: true);
    }

    public static bool BuffCharacter(Entity character, PrefabGUID buff, int duration = -1, bool persistsThroughDeath = false)
    {
        return BuffPlayer(character, GetAnyUser(), buff, duration, persistsThroughDeath);
    }

    private static Entity GetAnyUser()
    {
        NativeArray<Entity>.Enumerator enumerator = GetEntitiesByComponentTypes<User>().GetEnumerator();
        if (enumerator.MoveNext())
        {
            return enumerator.Current;
        }

        return Entity.Null;
    }

    public static bool BuffPlayer(Entity character, Entity user, PrefabGUID buff, int duration = -1, bool persistsThroughDeath = false)
    {
        List<PrefabGUID> list = new List<PrefabGUID>
        {
            Prefabs.AB_Interact_UseRelic_Behemoth_Buff,
            Prefabs.AB_Interact_UseRelic_Manticore_Buff,
            Prefabs.AB_Interact_UseRelic_Monster_Buff,
            Prefabs.AB_Interact_UseRelic_Paladin_Buff
        };
        DebugEventsSystem existingSystem = VWorld.Server.GetExistingSystem<DebugEventsSystem>();
        ApplyBuffDebugEvent applyBuffDebugEvent = default;
        applyBuffDebugEvent.BuffPrefabGUID = buff;
        ApplyBuffDebugEvent applyBuffDebugEvent2 = applyBuffDebugEvent;
        FromCharacter fromCharacter = default;
        fromCharacter.User = user;
        fromCharacter.Character = character;
        FromCharacter from = fromCharacter;
        if (!BuffUtility.TryGetBuff(VWorld.Server.EntityManager, character, buff, out var result))
        {
            existingSystem.ApplyBuff(from, applyBuffDebugEvent2);
            if (BuffUtility.TryGetBuff(VWorld.Server.EntityManager, character, buff, out result))
            {
                if (list.Contains(buff))
                {
                    if (result.Has<CreateGameplayEventsOnSpawn>())
                    {
                        result.Remove<CreateGameplayEventsOnSpawn>();
                    }

                    if (result.Has<GameplayEventListeners>())
                    {
                        result.Remove<GameplayEventListeners>();
                    }
                }

                if (persistsThroughDeath)
                {
                    result.Add<Buff_Persists_Through_Death>();
                    if (result.Has<RemoveBuffOnGameplayEvent>())
                    {
                        result.Remove<RemoveBuffOnGameplayEvent>();
                    }

                    if (result.Has<RemoveBuffOnGameplayEventEntry>())
                    {
                        result.Remove<RemoveBuffOnGameplayEventEntry>();
                    }
                }

                if (duration > 0 && duration != -1)
                {
                    if (result.Has<LifeTime>())
                    {
                        LifeTime componentData = result.Read<LifeTime>();
                        componentData.Duration = duration;
                        result.Write(componentData);
                    }
                }
                else if (duration == 0)
                {
                    if (result.Has<LifeTime>())
                    {
                        LifeTime componentData2 = result.Read<LifeTime>();
                        componentData2.Duration = -1f;
                        componentData2.EndAction = LifeTimeEndAction.None;
                        result.Write(componentData2);
                    }

                    if (result.Has<RemoveBuffOnGameplayEvent>())
                    {
                        result.Remove<RemoveBuffOnGameplayEvent>();
                    }

                    if (result.Has<RemoveBuffOnGameplayEventEntry>())
                    {
                        result.Remove<RemoveBuffOnGameplayEventEntry>();
                    }
                }

                return true;
            }

            return false;
        }

        return false;
    }

    public static bool BuffPlayerByName(string characterName, PrefabGUID buff, int duration = -1, bool persistsThroughDeath = false)
    {
        if (PlayerService.TryGetUserFromName(characterName, out var User))
        {
            Entity entity = User.Read<User>().LocalCharacter._Entity;
            return BuffPlayer(entity, User, buff, duration, persistsThroughDeath);
        }

        return false;
    }

    public static void UnbuffCharacter(Entity Character, PrefabGUID buffGUID)
    {
        if (BuffUtility.TryGetBuff(VWorld.Server.EntityManager, Character, buffGUID, out var result))
        {
            DestroyUtility.Destroy(VWorld.Server.EntityManager, result, DestroyDebugReason.TryRemoveBuff);
        }
    }

    public static void CreateClanForPlayer(Entity User)
    {
        Entity entity = CreateEntityWithComponents<ClanEvents_Client.CreateClan_Request, FromCharacter>();
        entity.Write(new ClanEvents_Client.CreateClan_Request
        {
            ClanMotto = "",
            ClanName = User.Read<User>().CharacterName
        });
        entity.Write(new FromCharacter
        {
            User = User,
            Character = User.Read<User>().LocalCharacter._Entity
        });
    }

    public static bool TryGetClanEntityFromPlayer(Entity User, out Entity ClanEntity)
    {
        Entity value = User.Read<TeamReference>().Value._Value;
        if (value.ReadBuffer<TeamAllies>().Length > 0)
        {
            ClanEntity = User.Read<TeamReference>().Value._Value.ReadBuffer<TeamAllies>()[0].Value;
            return true;
        }

        ClanEntity = default;
        return false;
    }

    public static void AddPlayerToPlayerClan(Entity User1, Entity User2)
    {
        if (TryGetClanEntityFromPlayer(User2, out var ClanEntity))
        {
            Entity entity = CreateEntityWithComponents<ClanInviteRequest_Server>();
            entity.Write(new ClanInviteRequest_Server
            {
                FromUser = User2,
                ToUser = User1,
                ClanEntity = ClanEntity
            });
            Entity entity2 = CreateEntityWithComponents<ClanEvents_Client.ClanInviteResponse, FromCharacter>();
            entity2.Write(new ClanEvents_Client.ClanInviteResponse
            {
                Response = InviteRequestResponse.Accept,
                ClanId = ClanEntity.Read<NetworkId>()
            });
            entity2.Write(new FromCharacter
            {
                User = User1,
                Character = User2.Read<User>().LocalCharacter._Entity
            });
        }
    }

    public static void RemoveFromClan(Entity User)
    {
        EntityCommandBuffer commandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
        if (TryGetClanEntityFromPlayer(User, out var ClanEntity))
        {
            clanSystem.LeaveClan(commandBuffer, ClanEntity, User, ClanSystem_Server.LeaveReason.Leave);
        }
    }

    public static void ClearExtraBuffs(Entity player)
    {
        DynamicBuffer<BuffBuffer> buffer = VWorld.Server.EntityManager.GetBuffer<BuffBuffer>(player);
        List<string> list = new List<string> { "BloodBuff", "SetBonus", "EquipBuff", "Combat", "VBlood_Ability_Replace", "Shapeshift", "Interact", "AB_Consumable" };
        NativeArray<BuffBuffer>.Enumerator enumerator = buffer.GetEnumerator();
        while (enumerator.MoveNext())
        {
            BuffBuffer current = enumerator.Current;
            bool flag = true;
            foreach (string item in list)
            {
                if (current.PrefabGuid.LookupName().Contains(item))
                {
                    flag = false;
                    break;
                }
            }

            if (flag)
            {
                DestroyUtility.Destroy(VWorld.Server.EntityManager, current.Entity, DestroyDebugReason.TryRemoveBuff);
            }
        }

        if (!player.Read<Equipment>().IsEquipped(Prefabs.Item_Cloak_Main_ShroudOfTheForest, out var _) && BuffUtility.HasBuff(VWorld.Server.EntityManager, player, Prefabs.EquipBuff_ShroudOfTheForest))
        {
            UnbuffCharacter(player, Prefabs.EquipBuff_ShroudOfTheForest);
        }
    }

    public static void ClearConsumablesAndShards(Entity player)
    {
        ClearConsumables(player);
        ClearShards(player);
    }

    public static void ClearConsumables(Entity player)
    {
        DynamicBuffer<BuffBuffer> buffer = VWorld.Server.EntityManager.GetBuffer<BuffBuffer>(player);
        List<string> list = new List<string> { "Consumable" };
        NativeArray<BuffBuffer>.Enumerator enumerator = buffer.GetEnumerator();
        while (enumerator.MoveNext())
        {
            BuffBuffer current = enumerator.Current;
            bool flag = false;
            foreach (string item in list)
            {
                if (current.PrefabGuid.LookupName().Contains(item))
                {
                    flag = true;
                    break;
                }
            }

            if (flag)
            {
                DestroyUtility.Destroy(VWorld.Server.EntityManager, current.Entity, DestroyDebugReason.TryRemoveBuff);
            }
        }
    }

    public static void ClearShards(Entity player)
    {
        DynamicBuffer<BuffBuffer> buffer = VWorld.Server.EntityManager.GetBuffer<BuffBuffer>(player);
        List<string> list = new List<string> { "UseRelic" };
        NativeArray<BuffBuffer>.Enumerator enumerator = buffer.GetEnumerator();
        while (enumerator.MoveNext())
        {
            BuffBuffer current = enumerator.Current;
            bool flag = false;
            foreach (string item in list)
            {
                if (current.PrefabGuid.LookupName().Contains(item))
                {
                    flag = true;
                    break;
                }
            }

            if (flag)
            {
                DestroyUtility.Destroy(VWorld.Server.EntityManager, current.Entity, DestroyDebugReason.TryRemoveBuff);
            }
        }
    }

    public static void RemoveStackFromPlayer(Entity player, PrefabGUID buffGUID)
    {
        if (BuffUtility.TryGetBuff(VWorld.Server.EntityManager, player, buffGUID, out var result))
        {
            Buff componentData = result.Read<Buff>();
            int stacks = componentData.Stacks;
            stacks--;
            if (stacks <= 0)
            {
                UnbuffCharacter(player, buffGUID);
                return;
            }

            componentData.Stacks = (byte)stacks;
            result.Write(componentData);
        }
    }

    public static Entity GetUserFromCharacter(Entity character)
    {
        FixedString64 name = character.Read<PlayerCharacter>().Name;
        VariousMigratedDebugEventsSystem existingSystem = VWorld.Server.GetExistingSystem<VariousMigratedDebugEventsSystem>();
        if (existingSystem.TryFindUserWithCharacterName(name, out var result))
        {
            return result;
        }

        return Entity.Null;
    }

    public static void UnbanUserBySteamID(ulong platformId)
    {
        KickBanSystem_Server existingSystem = VWorld.Server.GetExistingSystem<KickBanSystem_Server>();
        existingSystem._LocalBanList.Remove(platformId);
        existingSystem._LocalBanList.Save();
        Entity entity = VWorld.Server.EntityManager.CreateEntity(ComponentType.ReadWrite<FromCharacter>(), ComponentType.ReadWrite<BanEvent>());
        entity.Write(new BanEvent
        {
            PlatformId = platformId,
            Unban = true
        });
    }

    public static void MakeAdminPermanently(Entity Character, Entity User)
    {
        User componentData = User.Read<User>();
        if (!adminAuthSystem._LocalAdminList.Contains(User.Read<User>().PlatformId))
        {
            adminAuthSystem._LocalAdminList.Add(User.Read<User>().PlatformId);
        }

        adminAuthSystem._LocalAdminList.Save();
        User.Add<AdminUser>();
        User.Write(new AdminUser
        {
            AuthMethod = AdminAuthMethod.Authenticated,
            Level = AdminLevel.SuperAdmin
        });
        componentData.IsAdmin = true;
        User.Write(componentData);
        Entity entity = VWorld.Server.EntityManager.CreateEntity(ComponentType.ReadWrite<FromCharacter>(), ComponentType.ReadWrite<AdminAuthEvent>());
        entity.Write(new FromCharacter
        {
            Character = Character,
            User = User
        });
    }

    public static void DisableAdminPermanently(Entity Character, Entity User)
    {
        User componentData = User.Read<User>();
        if (adminAuthSystem._LocalAdminList.Contains(User.Read<User>().PlatformId))
        {
            adminAuthSystem._LocalAdminList.Remove(User.Read<User>().PlatformId);
            adminAuthSystem._LocalAdminList.Save();
        }

        if (User.Has<AdminUser>())
        {
            User.Remove<AdminUser>();
        }

        componentData.IsAdmin = false;
        Entity entity = VWorld.Server.EntityManager.CreateEntity(ComponentType.ReadWrite<FromCharacter>(), ComponentType.ReadWrite<DeauthAdminEvent>());
        entity.Write(new FromCharacter
        {
            Character = Character,
            User = User
        });
        User.Write(componentData);
    }

    public static void EnableSneakyAdmin(Entity Character, Entity User)
    {
        User.Add<AdminUser>();
        User.Write(new AdminUser
        {
            AuthMethod = AdminAuthMethod.Authenticated,
            Level = AdminLevel.SuperAdmin
        });
        User componentData = User.Read<User>();
        componentData.IsAdmin = false;
        User.Write(componentData);
    }

    public static bool ToggleAdmin(Entity Character, Entity User)
    {
        try
        {
            adminAuthSystem._LocalAdminList.RefreshLocal(forceRefresh: true);
        }
        catch (System.Exception ex)
        {
            Debug.Log(ex.ToString());
        }

        bool flag = false;
        if (adminAuthSystem._LocalAdminList.Contains(User.Read<User>().PlatformId))
        {
            flag = true;
        }

        try
        {
            if (flag)
            {
                Debug.Log($"Disabling admin permanently: {User.Read<User>().PlatformId}");
                DisableAdminPermanently(Character, User);
                return false;
            }

            Debug.Log("Attempting to make admin permanently");
            MakeAdminPermanently(Character, User);
            return true;
        }
        catch (System.Exception ex2)
        {
            Debug.Log(ex2.ToString());
        }

        return false;
    }

    public static void TeleportPlayer(Entity Character, Entity User, float3 position)
    {
        Entity entity = VWorld.Server.EntityManager.CreateEntity(ComponentType.ReadWrite<FromCharacter>(), ComponentType.ReadWrite<PlayerTeleportDebugEvent>());
        entity.Write(new FromCharacter
        {
            User = User,
            Character = Character
        });
        entity.Write(new PlayerTeleportDebugEvent
        {
            Position = new float3(position.x, position.y, position.z),
            Target = PlayerTeleportDebugEvent.TeleportTarget.Self
        });
    }

    public static void ResetCooldown(Entity PlayerCharacter)
    {
        NativeArray<AbilityGroupSlotBuffer>.Enumerator enumerator = VWorld.Server.EntityManager.GetBuffer<AbilityGroupSlotBuffer>(PlayerCharacter).GetEnumerator();
        while (enumerator.MoveNext())
        {
            Entity entity = enumerator.Current.GroupSlotEntity._Entity;
            Entity entity2 = entity.Read<AbilityGroupSlot>().StateEntity._Entity;
            if (entity2.Index > 0 && entity2.Read<PrefabGUID>().GuidHash != 0)
            {
                if (entity2.Has<AbilityChargesState>())
                {
                    AbilityChargesState componentData = entity2.Read<AbilityChargesState>();
                    componentData.CurrentCharges = entity2.Read<AbilityChargesData>().MaxCharges;
                    componentData.ChargeTime = 0f;
                    entity2.Write(componentData);
                }

                NativeArray<AbilityStateBuffer>.Enumerator enumerator2 = entity2.ReadBuffer<AbilityStateBuffer>().GetEnumerator();
                while (enumerator2.MoveNext())
                {
                    Entity entity3 = enumerator2.Current.StateEntity._Entity;
                    AbilityCooldownState componentData2 = entity3.Read<AbilityCooldownState>();
                    componentData2.CooldownEndTime = 0.0;
                    entity3.Write(componentData2);
                }
            }
        }
    }

    public static void HealCharacter(Entity Character)
    {
        Health componentData = Character.Read<Health>();
        componentData.Value = componentData.MaxHealth;
        componentData.MaxRecoveryHealth = componentData.MaxHealth;
        Character.Write(componentData);
    }

    public static void SpawnUnit(Entity user, PrefabGUID unit, int count, float2 position, float minRange = 1f, float maxRange = 2f, float duration = -1f)
    {
        Translation componentData = VWorld.Server.EntityManager.GetComponentData<Translation>(user);
        float3 spawnBasePosition = new float3(position.x, componentData.Value.y, position.y);
        UnitSpawnerUpdateSystem existingSystem = VWorld.Server.GetExistingSystem<UnitSpawnerUpdateSystem>();
        existingSystem.SpawnUnit(Entity.Null, unit, spawnBasePosition, count, minRange, maxRange, duration);
    }

    public static void UpgradeVampireHorse(FromCharacter fromCharacter, float speed, float acceleration, float rotation)
    {
        NativeArray<Entity> nativeArray = VWorld.Server.EntityManager.CreateEntityQuery(new ComponentType(Il2CppType.Of<NameableInteractable>()), new ComponentType(Il2CppType.Of<Mountable>()), new ComponentType(Il2CppType.Of<Immortal>())).ToEntityArray(Allocator.Temp);
        try
        {
            NativeArray<Entity>.Enumerator enumerator = nativeArray.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Entity current = enumerator.Current;
                NativeArray<AttachedBuffer>.Enumerator enumerator2 = VWorld.Server.EntityManager.GetBuffer<AttachedBuffer>(current).GetEnumerator();
                while (enumerator2.MoveNext())
                {
                    AttachedBuffer current2 = enumerator2.Current;
                    if (current2.Entity.Has<EntityOwner>())
                    {
                        EntityOwner entityOwner = current2.Entity.Read<EntityOwner>();
                        if (entityOwner == fromCharacter.Character)
                        {
                            Mountable componentData = current.Read<Mountable>();
                            componentData.MaxSpeed = speed;
                            componentData.Acceleration = acceleration;
                            componentData.RotationSpeed = rotation * 10f;
                            current.Write(componentData);
                            break;
                        }
                    }
                }
            }

            nativeArray.Dispose();
        }
        catch (System.Exception)
        {
        }
    }

    private static int IsSubsequence(string needle, string haystack)
    {
        int i = 0;
        int num = 0;
        int num2 = 0;
        for (int j = 0; j < needle.Length; j++)
        {
            for (; i < haystack.Length && haystack[i] != needle[j]; i++)
            {
            }

            if (i == haystack.Length)
            {
                return -1;
            }

            if (j > 0 && needle[j - 1] == haystack[i - 1])
            {
                num2++;
            }
            else
            {
                if (num2 > num)
                {
                    num = num2;
                }

                num2 = 1;
            }

            i++;
        }

        if (num2 > num)
        {
            num = num2;
        }

        return num;
    }

    public static void SetNewTargetForUser(Entity SourceUser, Entity TargetCharacter)
    {
    }
}

