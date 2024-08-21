using Il2CppInterop.Runtime;
using ProjectM.Network;
using System.Collections;
using Unity.Entities;
using UnityEngine;
using static Bloodcraft.Utilities;

namespace Bloodcraft.Services;
internal class PlayerService
{
	static EntityManager EntityManager => Core.EntityManager;

    static readonly WaitForSeconds Delay = new(60);

	static readonly ComponentType[] UserComponent =
	[
		ComponentType.ReadOnly(Il2CppType.Of<User>()),
	];

	public static EntityQuery UserQuery;

	public Dictionary<string, Entity> UserCache = []; //player name, player userEntity
    //public Dictionary<(string PlayerName, ulong SteamId), (Entity UserEntity, Entity CharacterEntity)> PlayerCache = [];

    public PlayerService()
	{
		UserQuery = EntityManager.CreateEntityQuery(UserComponent);
		Core.StartCoroutine(PlayerUpdateLoop());
	}
	IEnumerator PlayerUpdateLoop()
    {
        while (true)
        {
            UserCache = GetEntitiesEnumerable(UserQuery)
                .GroupBy(userEntity => userEntity.Read<User>().CharacterName.Value)
                .Select(group => group.First())
                .ToDictionary(user => user.Read<User>().CharacterName.Value, user => user);

            yield return Delay;
        }
    }
}
