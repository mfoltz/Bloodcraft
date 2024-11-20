using Bloodcraft.Services;
using Bloodcraft.Systems.Leveling;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using VampireCommandFramework;
using static Bloodcraft.Services.PlayerService;
using User = ProjectM.Network.User;

namespace Bloodcraft.Utilities;
internal static class PlayerUtilities
{
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;

    static readonly float ShareDistance = ConfigService.ExpShareDistance;

    static readonly bool Parties = ConfigService.PlayerParties;

    static readonly PrefabGUID DraculaVBlood = new(-327335305);
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
    public static HashSet<Entity> GetDeathParticipants(Entity source)
    {
        float3 sourcePosition = source.Read<Translation>().Value;
        User sourceUser = source.GetUser();
        string playerName = sourceUser.CharacterName.Value;

        Entity clanEntity = sourceUser.ClanEntity.GetEntityOnServer();
        HashSet<Entity> players = [source]; // use hashset to prevent double gains processing

        if (Parties)
        {
            List<HashSet<string>> playerParties = new([..DataService.PlayerDictionaries.playerParties.Values]);

            foreach (HashSet<string> party in playerParties)
            {
                if (party.Contains(playerName)) // find party with death source player name
                {
                    foreach (string partyMember in party)
                    {
                        if (partyMember.TryGetPlayerInfo(out PlayerInfo playerInfo) && playerInfo.User.IsConnected)
                        {
                            float distance = UnityEngine.Vector3.Distance(sourcePosition, playerInfo.CharEntity.Read<Translation>().Value);

                            if (distance > ShareDistance) continue;
                            else players.Add(playerInfo.CharEntity);
                        }
                    }

                    break; // break to avoid cases where there might be more than one party with same character name to account for checks that would prevent that happening failing
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

                    if (distance > ShareDistance) continue;
                    else players.Add(player);
                }
            }
        }

        return players;
    }
    public static bool ConsumedDracula(Entity userEntity)
    {
        if (userEntity.TryGetComponent(out ProgressionMapper progressionMapper))
        {
            Entity progressionEntity = progressionMapper.ProgressionEntity.GetEntityOnServer();

            if (progressionEntity.Has<UnlockedVBlood>())
            {
                var buffer = progressionEntity.ReadBuffer<UnlockedVBlood>();

                foreach (UnlockedVBlood unlockedVBlood in buffer)
                {
                    if (unlockedVBlood.VBlood.Equals(DraculaVBlood))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
    public class PartyUtilities
    {
        public static void HandlePartyAdd(ChatCommandContext ctx, ulong ownerId, string playerName)
        {
            PlayerInfo playerInfo = PlayerCache.FirstOrDefault(kvp => kvp.Key.ToLower() == playerName.ToLower()).Value;

            if (playerInfo.UserEntity.Exists())
            {
                if (playerInfo.User.PlatformId == ownerId)
                {
                    LocalizationService.HandleReply(ctx, "Can't add yourself to your own party!");
                    return;
                }

                playerName = playerInfo.User.CharacterName.Value;

                if (InvitesEnabled(playerInfo.User.PlatformId))
                {
                    AddPlayerToParty(ctx, ownerId, playerName);
                }
                else
                {
                    LocalizationService.HandleReply(ctx, $"<color=green>{playerName}</color> does not have party invites enabled.");
                }
            }
            else
            {
                LocalizationService.HandleReply(ctx, "Couldn't find player...");
            }
        }
        static bool InvitesEnabled(ulong steamId)
        {
            if (GetPlayerBool(steamId, "Grouping"))
            {
                SetPlayerBool(steamId, "Grouping", false);

                return true;
            }

            return false;
        }
        static void AddPlayerToParty(ChatCommandContext ctx, ulong ownerId, string playerName)
        {
            string ownerName = ctx.Event.User.CharacterName.Value;

            // Check if the player is already in a party
            KeyValuePair<ulong, HashSet<string>> existingPartyEntry = DataService.PlayerDictionaries.playerParties.FirstOrDefault(entry => entry.Value.Contains(playerName));

            if (existingPartyEntry.Value != null)
            {
                LocalizationService.HandleReply(ctx, $"<color=green>{playerName}</color> is already in another party.");
                return;
            }

            if (!ownerId.TryGetPlayerParties(out HashSet<string> party) || party == null)
            {
                ownerId.SetPlayerParties([ownerName]);
            }

            if (CanAddPlayerToParty(party, playerName))
            {
                party.Add(playerName);
                ownerId.SetPlayerParties(party);

                LocalizationService.HandleReply(ctx, $"<color=green>{playerName}</color> added to party!");
            }
            else if (party.Count == ConfigService.MaxPartySize)
            {
                LocalizationService.HandleReply(ctx, $"Party is full, can't add <color=green>{playerName}</color>.");
            }
            else if (party.Contains(playerName))
            {
                LocalizationService.HandleReply(ctx, $"<color=green>{playerName}</color> is already in the party.");
            }
            else
            {
                LocalizationService.HandleReply(ctx, $"Couldn't add <color=green>{playerName}</color> to party...");
            }
        }
        static bool CanAddPlayerToParty(HashSet<string> party, string playerName)
        {
            return party.Count < ConfigService.MaxPartySize && !party.Contains(playerName);
        }
        public static void RemovePlayerFromParty(ChatCommandContext ctx, HashSet<string> party, string playerName)
        {
            ulong steamId = ctx.Event.User.PlatformId;

            PlayerInfo playerInfo = PlayerCache.FirstOrDefault(kvp => kvp.Key.ToLower() == playerName.ToLower()).Value;
            if (playerInfo.UserEntity.Exists() && party.Contains(playerInfo.User.CharacterName.Value))
            {
                party.Remove(playerInfo.User.CharacterName.Value);
                steamId.SetPlayerParties(party);

                LocalizationService.HandleReply(ctx, $"<color=green>{char.ToUpper(playerName[0]) + playerName[1..].ToLower()}</color> removed from party!");
            }
            else
            {
                LocalizationService.HandleReply(ctx, $"<color=green>{char.ToUpper(playerName[0]) + playerName[1..].ToLower()}</color> not found in party to remove...");
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
