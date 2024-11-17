using Bloodcraft.Utilities;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using System.Collections;
using Unity.Entities;
using UnityEngine;

namespace Bloodcraft.Services;
internal class PlayerService
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly WaitForSeconds Delay = new(60);

    //static bool abilityGroupSlotBuffersCleared = false;

    static readonly ComponentType[] UserComponent =
    [
        ComponentType.ReadOnly(Il2CppType.Of<User>()),
    ];

    static EntityQuery UserQuery;

    public static readonly Dictionary<string, PlayerInfo> PlayerCache = [];

    public static readonly Dictionary<string, PlayerInfo> OnlineCache = [];
    public struct PlayerInfo(Entity userEntity = default, Entity charEntity = default, User user = default)
    {
        public User User { get; set; } = user;
        public Entity UserEntity { get; set; } = userEntity;
        public Entity CharEntity { get; set; } = charEntity;
    }
    public PlayerService()
    {
        UserQuery = EntityManager.CreateEntityQuery(UserComponent);
        Core.StartCoroutine(PlayerUpdateLoop());
    }
    static IEnumerator PlayerUpdateLoop()
    {
        while (true)
        {
            PlayerCache.Clear();
            OnlineCache.Clear();

            var players = EntityUtilities.GetEntitiesEnumerable(UserQuery);
            players
                .Select(userEntity =>
                {
                    var user = userEntity.Read<User>();
                    var playerName = user.CharacterName.Value;
                    var steamId = user.PlatformId.ToString(); // Assuming User has a SteamId property
                    var characterEntity = user.LocalCharacter._Entity;

                    return new
                    {
                        PlayerNameEntry = new KeyValuePair<string, PlayerInfo>(
                            playerName, new PlayerInfo(userEntity, characterEntity, user)),
                        SteamIdEntry = new KeyValuePair<string, PlayerInfo>(
                            steamId, new PlayerInfo(userEntity, characterEntity, user))
                    };
                })
                .SelectMany(entry => new[] { entry.PlayerNameEntry, entry.SteamIdEntry })
                .GroupBy(entry => entry.Key)
                .ToDictionary(group => group.Key, group => group.First().Value)
                .ForEach(kvp =>
                {
                    PlayerCache[kvp.Key] = kvp.Value; // Add to PlayerCache

                    if (kvp.Value.User.IsConnected) // Add to OnlinePlayerCache if connected
                    {
                        OnlineCache[kvp.Key] = kvp.Value;
                    }
                });

            /*
            if (!abilityGroupSlotBuffersCleared)
            {
                HashSet<Entity> processedPlayers = [];

                foreach (PlayerInfo player in PlayerCache.Values)
                {
                    if (!processedPlayers.Contains(player.CharEntity) && player.CharEntity.Exists() && ServerGameManager.TryGetBuffer<AbilityGroupSlotBuffer>(player.CharEntity, out var buffer))
                    {
                        foreach (AbilityGroupSlotBuffer abilityGroupSlotBuffer in buffer)
                        {
                            Entity abilityGroupSlotEntity = abilityGroupSlotBuffer.GroupSlotEntity.GetEntityOnServer();

                            if (abilityGroupSlotEntity.Exists() && abilityGroupSlotEntity.TryGetComponent(out AbilityGroupSlot abilityGroupSlot) && abilityGroupSlot.SlotId > 8)
                            {
                                DestroyUtility.Destroy(EntityManager, abilityGroupSlotEntity);
                            }
                        }

                        buffer.RemoveRange(9, buffer.Length);
                        processedPlayers.Add(player.CharEntity);
                    }
                }

                abilityGroupSlotBuffersCleared = true;
            }
            */

            yield return Delay;
        }
    }
}
