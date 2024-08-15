using Bloodcraft.SystemUtilities.Familiars;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.CastleBuilding;
using ProjectM.Network;
using ProjectM.Terrain;
using System.Collections;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Bloodcraft.Services;

internal class TeamService // do I need territory and floor check? thinking just territory actually
{
	static EntityManager EntityManager => Core.EntityManager;
    static MapZoneCollection MapZoneCollection = Core.MapZoneCollection;

    static readonly WaitForSeconds Delay = new(2.5f);
    static DateTime LastCacheUpdate;

    static readonly ComponentType[] CastleTerritoryComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<CastleTerritory>()),
        ComponentType.ReadOnly(Il2CppType.Of<MapZoneData>()),
        ComponentType.ReadOnly(Il2CppType.Of<CastleTerritoryBlocks>()),
        ComponentType.ReadOnly(Il2CppType.Of<CastleTerritoryTiles>())
    ];
    public enum EntityQueryType
    {
        CastleTerritories
    }

    static readonly Dictionary<EntityQueryType, EntityQuery> EntityQueries = new()
    {
        [EntityQueryType.CastleTerritories] = CastleTerritoryQuery
    };

    static EntityQuery CastleTerritoryQuery;

    static readonly Dictionary<int, Entity> CastleTerritoryCache = [];
	public TeamService()
	{
        CastleTerritoryQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = CastleTerritoryComponents,
            Options = EntityQueryOptions.IncludeDisabled
        });

		Core.StartCoroutine(TeamUpdateLoop());
	}
	static IEnumerator TeamUpdateLoop()
    {
        while (true)
        {
            Dictionary<string, Entity> users = new(PlayerService.UserCache); // shallow copy for processing online users by playerCharacter entity

            List<Entity> players = users
                .Where(user => user.Value.Read<User>().IsConnected)
                .Select(user => user.Value.Read<User>().LocalCharacter._Entity)
                .ToList();

            foreach (Entity entity in players)
            {
                NetworkInterpolated_Shared networkInterpolated_Shared = entity.Read<NetworkInterpolated_Shared>();
                if (MapZoneUtilities.TryGetMapZoneForWorldPos(EntityManager, ref MapZoneCollection, networkInterpolated_Shared.ServerPosition, out SpatialMapZoneData spatialMapZone))
                {
                    if (CastleTerritoryCache.TryGetValue(spatialMapZone.ZoneId.ZoneIndex, out Entity castleTerritory)) // if in a territory use original team value either from clan or the player's castke
                    {
                        if (entity.Read<PlayerCharacter>().SmartClanName.IsEmpty) // use castle team
                        {
                            foreach(var ally in entity.Read<TeamReference>().Value._Value.ReadBuffer<TeamAllies>())
                            {
                                if (ally.Value.Has<CastleTeam>())
                                {
                                    int castleTeam = ally.Value.Read<TeamData>().TeamValue;
                                    UpdateTeam(entity, castleTeam);
                                }
                            }
                        }
                        else // use clan team
                        {
                            int clanTeam = entity.Read<PlayerCharacter>().UserEntity.Read<User>().ClanEntity._Entity.Read<ClanTeam>().TeamValue;
                            UpdateTeam(entity, clanTeam);
                        }
                    }
                    else // if not in a territory use universal value
                    {
                        int universalTeam = 0;
                        UpdateTeam(entity, universalTeam);
                    }
                }
            }

            yield return Delay;
        }
    }
    static void UpdateTeam(Entity entity, int teamValue)
    {
        Entity teamEntity = entity.Read<TeamReference>().Value._Value;
        TeamData teamData = teamEntity.Read<TeamData>();
        teamData.TeamValue = teamValue;
        teamEntity.Write(teamData);

        Team team = entity.Read<Team>();
        team.Value = teamValue;
        entity.Write(team);

        Entity familiar = FamiliarSummonUtilities.FamiliarUtilities.FindPlayerFamiliar(entity);
        if (familiar.Exists())
        {
            familiar.Write(teamEntity);
            familiar.Write(team);
        }
    }
    static IEnumerable<Entity> GetEntitiesEnumerable(EntityQueryType entityQuery)
    {
        JobHandle handle = GetEntities(entityQuery, out NativeArray<Entity> entities, Allocator.TempJob);
        handle.Complete();
        try
        {
            foreach (Entity entity in entities)
            {
                if (EntityManager.Exists(entity))
                {
                    yield return entity;
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
    static JobHandle GetEntities(EntityQueryType entityQuery, out NativeArray<Entity> entities, Allocator allocator = Allocator.TempJob)
    {
        entities = EntityQueries[entityQuery].ToEntityArray(allocator);
        return default;
    }
}
