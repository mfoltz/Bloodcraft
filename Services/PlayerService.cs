using Il2CppInterop.Runtime;
using ProjectM.Network;
using System.Collections;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Bloodcraft.Services;
internal class PlayerService
{
	static readonly ComponentType[] UserComponent =
		[
			ComponentType.ReadOnly(Il2CppType.Of<User>()),
		];

	static EntityQuery ActiveUsersQuery;

	static EntityQuery AllUsersQuery;

	public static Dictionary<string, Entity> playerNameCache = []; //player name, player userEntity

	public static Dictionary<ulong, Entity> playerIdCache = []; //player steamID, player charEntity

	public PlayerService()
	{
		ActiveUsersQuery = Core.EntityManager.CreateEntityQuery(UserComponent);
		AllUsersQuery = Core.EntityManager.CreateEntityQuery(new EntityQueryDesc
		{
			All = UserComponent,
			Options = EntityQueryOptions.IncludeDisabledEntities
		});
		Core.StartCoroutine(CacheUpdateLoop());
	}
	static IEnumerator CacheUpdateLoop()
    {
		WaitForSeconds wait = new(60);
        while (true)
        {
			//Core.Log.LogInfo("Updating player cache...");

			IEnumerable<Entity> users = GetUsers();

			playerNameCache.Clear();
            playerNameCache = users
                .Select(userEntity => new { CharacterName = userEntity.Read<User>().CharacterName.Value, Entity = userEntity })
				.GroupBy(user => user.CharacterName)
				.Select(group => group.First())
				.ToDictionary(user => user.CharacterName, user => user.Entity); // playerName : userEntity

			playerIdCache.Clear();
			playerIdCache = users
                .Select(userEntity => new { SteamID = userEntity.Read<User>().PlatformId, Entity = userEntity.Read<User>().LocalCharacter._Entity })
				.GroupBy(user => user.SteamID)
				.Select(group => group.First())
				.ToDictionary(user => user.SteamID, user => user.Entity); // steamID : charEntity

			yield return wait;
        }
    }
	public static IEnumerable<Entity> GetUsers(bool includeDisabled = false)
	{
		NativeArray<Entity> userEntities = includeDisabled ? AllUsersQuery.ToEntityArray(Allocator.TempJob) : ActiveUsersQuery.ToEntityArray(Allocator.TempJob);
		try
		{
			foreach (Entity entity in userEntities)
			{
				if (Core.EntityManager.Exists(entity))
				{
					yield return entity;
				}
			}
		}
		finally
		{
			userEntities.Dispose();
		}
	}
    public static Entity GetUserByName(string playerName, bool includeDisabled = false)
	{
		Entity userEntity = GetUsers(includeDisabled).FirstOrDefault(entity => entity.Read<User>().CharacterName.Value.ToLower() == playerName.ToLower());
		return userEntity != Entity.Null ? userEntity : Entity.Null;
	}
}
