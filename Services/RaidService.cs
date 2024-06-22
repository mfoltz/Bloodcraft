using ProjectM;
using ProjectM.CastleBuilding;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using System.Collections;
using Unity.Entities;
using Bloodcraft.Services;

namespace Bloodcraft.Services;
internal class RaidService
{
    static readonly bool PlayerAlliances = Plugin.PlayerAlliances.Value;

    static readonly bool DamageIntruders = Plugin.DamageIntruders.Value;
    static EntityManager EntityManager => Core.EntityManager;
    static DebugEventsSystem DebugEventsSystem => Core.DebugEventsSystem;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly PrefabGUID debuff = new(-1572696947);

    //static Dictionary<string, Entity> onlinePlayers = []; // player name and userEntity
    static Dictionary<Entity, HashSet<Entity>> raidParticipants = []; // castleHeart entity and players allowed in territory for the raid (owner clan, raiding clan, alliance members if applicable)

    static bool active = false;
    static DateTime lastMessage = DateTime.MinValue;
    public static void StartRaidMonitor(Entity raider, Entity breached)
    {
        Entity heartEntity = breached.Has<CastleHeartConnection>() ? breached.Read<CastleHeartConnection>().CastleHeartEntity._Entity : Entity.Null;
        
        if (!active) // if not active start monitor loop after clearing caches
        {
            Core.Log.LogInfo("Starting raid monitor...");

            raidParticipants.Clear(); // clear previous raid participants, this should be empty here anyway but just incase
            //onlinePlayers = GetUsers().Select(userEntity => new { CharacterName = userEntity.Read<User>().CharacterName.Value, Entity = userEntity }).ToDictionary(user => user.CharacterName, user => user.Entity); // get map of online player names to entities for easier handling
            
            if (!heartEntity.Equals(Entity.Null)) raidParticipants.TryAdd(heartEntity, GetRaidParticipants(raider, breached));
            
            Core.StartCoroutine(RaidMonitor());
        }
        else if (active) // if active update onlinePlayers and add new territory participants
        {
            //onlinePlayers = GetUsers().Select(userEntity => new { CharacterName = userEntity.Read<User>().CharacterName.Value, Entity = userEntity }).ToDictionary(user => user.CharacterName, user => user.Entity); // update player map
            if (!heartEntity.Equals(Entity.Null)) raidParticipants.TryAdd(heartEntity, GetRaidParticipants(raider, breached));
        }
    }
    static HashSet<Entity> GetRaidParticipants(Entity raider, Entity breached)
    {
        HashSet<Entity> participants = [];
        Dictionary<ulong, HashSet<string>> alliances = Core.DataStructures.PlayerAlliances;

        Entity playerUserEntity = breached.Read<UserOwner>().Owner._Entity;
        User playerUser = playerUserEntity.Read<User>(); // add alliance members of castle owner, should raid alliance members be included here? probably
        string playerName = playerUser.CharacterName.Value;

        Entity clanEntity = EntityManager.Exists(playerUser.ClanEntity._Entity) ? playerUser.ClanEntity._Entity : Entity.Null;

        if (!clanEntity.Equals(Entity.Null)) // add owner clan members to raid participants
        {
            var userBuffer = clanEntity.ReadBuffer<SyncToUserBuffer>();
            for (int i = 0; i < userBuffer.Length; i++)
            {
                participants.Add(userBuffer[i].UserEntity); // add clan members without checking if online since they might be in the territory and don't want them to take damage
            }
        }
        else // if no clan just add owner to raid participants
        {
            participants.Add(playerUserEntity);
        }

        if (PlayerAlliances && alliances.Values.Any(set => set.Contains(playerName)))
        {
            var members = alliances
                .Where(groupEntry => groupEntry.Value.Contains(playerUser.CharacterName.Value))
                .SelectMany(groupEntry => groupEntry.Value)
                .Where(name => PlayerService.playerCache.TryGetValue(name, out var _))
                .Select(name => PlayerService.playerCache[name])
                .ToHashSet();
            participants.UnionWith(members);
        }

        playerUserEntity = raider.Read<PlayerCharacter>().UserEntity;
        playerUser = playerUserEntity.Read<User>();
        playerName = playerUser.CharacterName.Value;

        clanEntity = EntityManager.Exists(playerUser.ClanEntity._Entity) ? playerUser.ClanEntity._Entity : Entity.Null;

        if (!clanEntity.Equals(Entity.Null)) // add raider clan members to raid participants
        {
            var userBuffer = clanEntity.ReadBuffer<SyncToUserBuffer>();
            for (int i = 0; i < userBuffer.Length; i++)
            {
                if (userBuffer[i].UserEntity.Read<User>().IsConnected) participants.Add(userBuffer[i].UserEntity); // for raiders only add clan members that are online
            }
        }
        else // if no clan just add raider to raid participants
        {
            participants.Add(playerUserEntity);
        }

        if (PlayerAlliances && alliances.Values.Any(set => set.Contains(playerName)))
        {
            var members = alliances
                .Where(groupEntry => groupEntry.Value.Contains(playerUser.CharacterName.Value))
                .SelectMany(groupEntry => groupEntry.Value)
                .Where(name => PlayerService.playerCache.TryGetValue(name, out var _))
                .Select(name => PlayerService.playerCache[name])
                .ToHashSet();
            participants.UnionWith(members);
        }

        return participants;
    }
    static IEnumerator RaidMonitor()
    {
        active = true;
        yield return null;
        while (true)
        {
            if (raidParticipants.Keys.Count == 0)
            {
                active = false;
                Core.Log.LogInfo("Stopping raid monitor...");
                yield break;
            }
            bool sendMessage = (DateTime.Now - lastMessage).TotalSeconds >= 10;
            List<Entity> heartEntities = [.. raidParticipants.Keys];
            foreach (KeyValuePair<string, Entity> player in PlayerService.playerCache) // validate player presence in raided territories
            {
                Entity userEntity = player.Value;
                User user = userEntity.Read<User>();
                Entity character = user.LocalCharacter._Entity;
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
                            if (DamageIntruders && !ServerGameManager.TryGetBuff(character, debuff.ToIdentifier(), out Entity _))
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
                                DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent); // apply green fire to interlopers and block healing
                                if (ServerGameManager.TryGetBuff(character, debuff.ToIdentifier(), out Entity debuffEntity))
                                {
                                    debuffEntity.Add<BlockHealBuff>();
                                    debuffEntity.Write(new BlockHealBuff { PercentageBlocked = 1f });
                                    if (debuffEntity.TryGetComponent(out LifeTime lifeTime))
                                    {
                                        lifeTime.Duration = 10f;
                                        debuffEntity.Write(lifeTime);
                                    }
                                    var buffer = debuffEntity.ReadBuffer<CreateGameplayEventsOnTick>();
                                    CreateGameplayEventsOnTick bufferEntry = buffer[0];
                                    bufferEntry.MaxTicks = 10;
                                    buffer[0] = bufferEntry;
                                }
                            }
                            if (sendMessage) LocalizationService.HandleServerReply(EntityManager, user, "You are not allowed in this territory during a raid.");                
                        }
                    });
                }
                yield return null;
            }
            if (sendMessage) lastMessage = DateTime.Now;
            yield return null;
        }
    }
}
