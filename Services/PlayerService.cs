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

    static readonly WaitForSeconds Delay = new(60);

	static readonly ComponentType[] UserComponent =
		[
			ComponentType.ReadOnly(Il2CppType.Of<User>()),
		];

	static EntityQuery UsersQuery;

	public static Dictionary<string, Entity> UserCache = []; //player name, player userEntity
    public static Dictionary<ulong, Entity> CharacterCache = []; // playername, player characterEntity
	public PlayerService()
	{
		UsersQuery = EntityManager.CreateEntityQuery(UserComponent);
		Core.StartCoroutine(PlayerUpdateLoop());
	}
	static IEnumerator PlayerUpdateLoop()
    {
        while (true)
        {
            var userData = GetUsersEnumerable()
                .GroupBy(userEntity => new
                {
                    CharacterName = userEntity.Read<User>().CharacterName.Value,
                    userEntity.Read<User>().PlatformId
                })
                .Select(group => new
                {
                    group.Key.CharacterName,
                    group.Key.PlatformId,
                    UserEntity = group.First(),
                    LocalCharacterEntity = group.First().Read<User>().LocalCharacter._Entity
                })
                .ToList();

            UserCache = userData
                .GroupBy(data => data.CharacterName)
                .ToDictionary(group => group.Key, group => group.First().UserEntity);

            CharacterCache = userData
                .GroupBy(data => data.PlatformId)
                .ToDictionary(group => group.Key, group => group.First().LocalCharacterEntity);

            yield return Delay;
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
