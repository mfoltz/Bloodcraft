using Il2CppInterop.Runtime;
using ProjectM.Network;
using System.Collections;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Bloodcraft.Services;
internal class PlayerService
{
	static EntityManager EntityManager => Core.EntityManager;

	static readonly ComponentType[] UserComponent =
		[
			ComponentType.ReadOnly(Il2CppType.Of<User>()),
		];

	static EntityQuery UsersQuery;

	public static Dictionary<string, Entity> PlayerCache = []; //player name, player userEntity
	public PlayerService()
	{
		UsersQuery = EntityManager.CreateEntityQuery(UserComponent);
		Core.StartCoroutine(PlayerUpdateLoop());
	}
	static IEnumerator PlayerUpdateLoop()
    {
		WaitForSeconds wait = new(60);
        while (true)
        {
            PlayerCache = GetUsersEnumerable()
                .GroupBy(userEntity => userEntity.Read<User>().CharacterName.Value)
                .Select(group => group.First())
                .ToDictionary(user => user.Read<User>().CharacterName.Value, user => user);

            yield return wait;
        }
    }
    static IEnumerable<Entity> GetUsersEnumerable()
    {
        JobHandle handle = GetUsers(out NativeArray<Entity> userEntities, Allocator.TempJob);
        handle.Complete();
        try
        {
            foreach (Entity entity in userEntities)
            {
                if (EntityManager.Exists(entity))
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
    static JobHandle GetUsers(out NativeArray<Entity> userEntities, Allocator allocator = Allocator.TempJob)
    {
        userEntities = UsersQuery.ToEntityArray(allocator);
        return default;
    }
}
