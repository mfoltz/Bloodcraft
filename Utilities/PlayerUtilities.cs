using Bloodcraft.Services;
using ProjectM.Network;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using ProjectM.Scripting;
using static Bloodcraft.Services.PlayerService;
using VampireCommandFramework;

namespace Bloodcraft.Utilities;
internal static class PlayerUtilities
{
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    public static bool GetPlayerBool(ulong steamId, string boolKey) // changed some default values in playerBools a while ago such that trues returned here are more easily/correctly interpreted, may need to revisit later
    {
        return steamId.TryGetPlayerBools(out var bools) && bools[boolKey];
    }
    public static void SetPlayerBool(ulong steamId, string boolKey, bool value)
    {
        if (steamId.TryGetPlayerBools(out var bools))
        {
            bools[boolKey] = value;
            steamId.SetPlayerBools(bools);
        }
    }
    public static void TogglePlayerBool(ulong steamId, string boolKey)
    {
        if (steamId.TryGetPlayerBools(out var bools))
        {
            bools[boolKey] = !bools[boolKey];
            steamId.SetPlayerBools(bools);
        }
    }
    public static HashSet<Entity> GetDeathParticipants(Entity source, Entity userEntity)
    {
        float3 sourcePosition = source.Read<Translation>().Value;
        User sourceUser = userEntity.Read<User>();
        string playerName = sourceUser.CharacterName.Value;

        Entity clanEntity = sourceUser.ClanEntity.GetEntityOnServer();
        HashSet<Entity> players = [source]; // use hashset to prevent double gains processing

        if (ConfigService.PlayerParties)
        {
            List<HashSet<string>> playerParties = new([..DataService.PlayerDictionaries.playerParties.Values]);

            foreach (HashSet<string> party in playerParties)
            {
                if (party.Contains(playerName)) // find party with death source player name
                {
                    foreach (string name in party)
                    {
                        if (name.TryGetPlayerInfo(out PlayerInfo playerInfo) && playerInfo.User.IsConnected)
                        {
                            float distance = UnityEngine.Vector3.Distance(sourcePosition, playerInfo.CharEntity.Read<Translation>().Value);

                            if (distance > ConfigService.ExpShareDistance) continue;
                            else players.Add(playerInfo.CharEntity);
                        }

                        /*
                        if (name.TryGetPlayerInfo(out PlayerInfo playerInfo))
                        {
                            float distance = UnityEngine.Vector3.Distance(sourcePosition, playerInfo.CharEntity.Read<Translation>().Value);

                            if (distance > ConfigService.ExpShareDistance) continue;
                            else players.Add(playerInfo.CharEntity);
                        }
                        */
                    }

                    break; // break to avoid cases where there might be more than one party with same character name although that shouldn't be able to happen in theory
                }
            }
        }

        if (!clanEntity.Exists()) return players;
        else if (ServerGameManager.TryGetBuffer<SyncToUserBuffer>(clanEntity, out var clanUserBuffer) && !clanUserBuffer.IsEmpty)
        {        
            foreach (SyncToUserBuffer clanUser in clanUserBuffer)
            {
                if (clanUser.UserEntity.TryGetComponent(out User user) && user.IsConnected)
                {
                    Entity player = user.LocalCharacter._Entity;
                    var distance = UnityEngine.Vector3.Distance(sourcePosition, player.Read<Translation>().Value);

                    if (distance > ConfigService.ExpShareDistance) continue;
                    else players.Add(player);
                }

                /*
                if (clanUser.UserEntity.TryGetComponent(out User user)) // for testing general functionality of method alone
                {
                    Entity player = user.LocalCharacter._Entity;
                    var distance = UnityEngine.Vector3.Distance(sourcePosition, player.Read<Translation>().Value);

                    if (distance > ConfigService.ExpShareDistance) continue;
                    else players.Add(player);
                }
                */
            }
        }

        return players;
    }
    public class PartyUtilities
    {
        public static void HandlePlayerParty(ChatCommandContext ctx, ulong ownerId, string name)
        {
            string playerKey = PlayerCache.Keys.FirstOrDefault(key => key.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(playerKey) && playerKey.TryGetPlayerInfo(out PlayerInfo playerInfo))
            {
                if (playerInfo.User.PlatformId == ownerId)
                {
                    LocalizationService.HandleReply(ctx, "Can't add yourself to your own party!");
                    return;
                }

                string playerName = playerInfo.User.CharacterName.Value;

                if (IsPlayerEligibleForParty(playerInfo.User.PlatformId, playerName))
                {
                    AddPlayerToParty(ctx, ownerId, playerName);
                }
                else
                {
                    LocalizationService.HandleReply(ctx, $"<color=green>{playerName}</color> does not have parties enabled or is already in a party.");
                }
            }
            else
            {
                LocalizationService.HandleReply(ctx, "Player not found...");
            }
        }
        public static bool IsPlayerEligibleForParty(ulong steamId, string playerName)
        {
            if (GetPlayerBool(steamId, "Grouping"))
            {
                if (!steamId.TryGetPlayerParties(out var parties) && !DataService.PlayerDictionaries.playerParties.Values.Any(party => party.Equals(playerName)))
                {
                    SetPlayerBool(steamId, "Grouping", false);

                    return true;
                }
            }

            return false;
        }
        public static void AddPlayerToParty(ChatCommandContext ctx, ulong ownerId, string playerName)
        {
            string ownerName = ctx.Event.User.CharacterName.Value;

            if (!ownerId.TryGetPlayerParties(out HashSet<string> party))
            {
                party = [];
                ownerId.SetPlayerParties(party);
            }

            if (CanAddPlayerToParty(party, playerName))
            {
                party.Add(playerName);

                if (!party.Contains(ownerName)) // add owner to party for ease of processing party elsewhere
                {
                    party.Add(ownerName);
                }

                ownerId.SetPlayerParties(party);

                LocalizationService.HandleReply(ctx, $"<color=green>{playerName}</color> added to party.");
            }
            else
            {
                LocalizationService.HandleReply(ctx, $"Party is full or <color=green>{playerName}</color> is already in the party.");
            }
        }
        static bool CanAddPlayerToParty(HashSet<string> party, string playerName)
        {
            return party.Count < ConfigService.MaxPartySize && !party.Contains(playerName);
        }
        public static void RemovePlayerFromParty(ChatCommandContext ctx, HashSet<string> party, string playerName)
        {
            ulong steamId = ctx.Event.User.PlatformId;
            string playerKey = PlayerCache.Keys.FirstOrDefault(key => key.Equals(playerName, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(playerKey) && party.FirstOrDefault(n => n.Equals(playerKey)) != null)
            {
                party.Remove(playerKey);
                steamId.SetPlayerParties(party);

                LocalizationService.HandleReply(ctx, $"<color=green>{char.ToUpper(playerName[0]) + playerName[1..].ToLower()}</color> removed from party.");
            }
            else
            {
                LocalizationService.HandleReply(ctx, $"<color=green>{char.ToUpper(playerName[0]) + playerName[1..].ToLower()}</color> not found in party.");
            }
        }
        public static void ListPartyMembers(ChatCommandContext ctx, Dictionary<ulong, HashSet<string>> playerParties)
        {
            ulong ownerId = ctx.Event.User.PlatformId;
            string playerName = ctx.Event.User.CharacterName.Value;

            HashSet<string> members = playerParties.ContainsKey(ownerId) ? playerParties[ownerId] : playerParties.Where(groupEntry => groupEntry.Value.Contains(playerName)).SelectMany(groupEntry => groupEntry.Value).ToHashSet();
            string replyMessage = members.Count > 0 ? string.Join(", ", members.Select(member => $"<color=green>{member}</color>")) : "No members in party.";

            LocalizationService.HandleReply(ctx, replyMessage);
        }
    }
}
