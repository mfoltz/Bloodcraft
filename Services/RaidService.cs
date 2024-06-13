using ProjectM;
using ProjectM.CastleBuilding;
using ProjectM.Gameplay.Scripting;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using System.Collections;
using Unity.Entities;
using static Bloodcraft.Services.PlayerService;

namespace Bloodcraft.Services;
public class RaidService
{
    static EntityManager EntityManager => Core.EntityManager;
    static DebugEventsSystem DebugEventsSystem => Core.DebugEventsSystem;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly PrefabGUID debuff = new(-1572696947);

    static HashSet<Entity> onlinePlayers = [];
    public static Dictionary<Entity, HashSet<Entity>> raidParticipants = []; // castleHeart entity and players allowed in territory for the raid (owner clan, raiding clan)

    static bool active = false;
   
    public static void StartRaidMonitor(Entity raider, Entity breached)
    {
        Entity heartEntity = breached.Has<CastleHeartConnection>() ? breached.Read<CastleHeartConnection>().CastleHeartEntity._Entity : Entity.Null;

        if (!active) // if not active start monitor loop after clearing caches
        {
            Core.Log.LogInfo("Starting raid monitor...");

            raidParticipants.Clear();
            onlinePlayers = GetUsers().ToHashSet<Entity>();

            if (!heartEntity.Equals(Entity.Null)) raidParticipants.TryAdd(heartEntity, GetRaidParticipants(raider, breached));
            
            Core.StartCoroutine(MonitorInterlopers());
        }
        else if (active) // if active update onlinePlayers and add new territory participants
        {
            onlinePlayers = GetUsers().ToHashSet<Entity>();
            if (!heartEntity.Equals(Entity.Null)) raidParticipants.TryAdd(heartEntity, GetRaidParticipants(raider, breached));
        }
    }
    static HashSet<Entity> GetRaidParticipants(Entity raider, Entity breached)
    {
        HashSet<Entity> participants = [];

        Entity playerUserEntity = breached.Read<UserOwner>().Owner._Entity;
        User playerUser = playerUserEntity.Read<User>();

        Entity clanEntity = EntityManager.Exists(playerUser.ClanEntity._Entity) ? playerUser.ClanEntity._Entity : Entity.Null;

        if (!clanEntity.Equals(Entity.Null))
        {
            var userBuffer = clanEntity.ReadBuffer<SyncToUserBuffer>();
            for (int i = 0; i < userBuffer.Length; i++)
            {
                participants.Add(userBuffer[i].UserEntity);
            }
        }
        else
        {
            participants.Add(playerUserEntity);
        }

        playerUserEntity = raider.Read<PlayerCharacter>().UserEntity;
        playerUser = playerUserEntity.Read<User>();
        clanEntity = EntityManager.Exists(playerUser.ClanEntity._Entity) ? playerUser.ClanEntity._Entity : Entity.Null;

        if (!clanEntity.Equals(Entity.Null))
        {
            var userBuffer = clanEntity.ReadBuffer<SyncToUserBuffer>();
            for (int i = 0; i < userBuffer.Length; i++)
            {
                participants.Add(userBuffer[i].UserEntity);
            }
        }
        else
        {
            participants.Add(playerUserEntity);
        }

        return participants;
    }
    static IEnumerator MonitorInterlopers()
    {
        active = true;
        yield return null;
        while (true)
        {
            //float raidDuration = (float)DateTime.Now.Subtract(raidStart).TotalSeconds;
            if (raidParticipants.Keys.Count == 0)
            {
                active = false;
                onlinePlayers.Clear();
                Core.Log.LogInfo("Stopping raid monitor...");
                yield break;
            }

            foreach (Entity userEntity in onlinePlayers) // validate player presence in raided territories
            {
                List<Entity> heartEntities = [..raidParticipants.Keys];

                Entity character = userEntity.Read<User>().LocalCharacter._Entity;
                if (character.TryGetComponent(out TilePosition pos))
                {
                    heartEntities.ForEach(heartEntity =>
                    {
                        CastleHeart castleHeart = heartEntity.Read<CastleHeart>();
                        if (!castleHeart.IsSieged())
                        {
                            raidParticipants.Remove(heartEntity);
                            return;
                        }
                        if (!raidParticipants[heartEntity].Contains(userEntity) && CastleTerritoryExtensions.IsTileInTerritory(EntityManager, pos.Tile, ref castleHeart.CastleTerritoryEntity, out CastleTerritory _))
                        {
                            if (!ServerGameManager.TryGetBuff(character, debuff.ToIdentifier(), out Entity _))
                            {
                                ApplyBuffDebugEvent applyBuffDebugEvent = new()
                                {
                                    BuffPrefabGUID = debuff,
                                };
                                FromCharacter fromCharacter = new()
                                {
                                    Character = character,
                                    User = userEntity,
                                };
                                DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent); // apply green fire from iron mines w/e it's called
                            }
                        }
                    });
                }
                yield return null;
            }
            yield return null;
        }
    }
}
