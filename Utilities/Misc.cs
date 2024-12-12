using Bloodcraft.Services;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using static Bloodcraft.Services.PlayerService;
using User = ProjectM.Network.User;

namespace Bloodcraft.Utilities;
internal static class Misc
{
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly float ShareDistance = ConfigService.ExpShareDistance;

    static readonly bool Parties = ConfigService.PlayerParties;

    static readonly PrefabGUID DraculaVBlood = new(-327335305);
    public static bool GetPlayerBool(ulong steamId, string boolKey) // changed some default values in playerBools a while ago such that trues returned here are more easily/correctly interpreted, may need to revisit later <--- >_>
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
    public static string FormatTimespan(TimeSpan timeSpan)
    {
        string timeString = timeSpan.ToString(@"mm\:ss");
        return timeString;
    }

    /*
    public static bool EarnedPermaShroud()
    {
        if (UpdateBuffsBufferDestroyPatch.PrestigeBuffs.Contains(ShroudBuff) && !character.HasBuff(ShroudBuff)
    && steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(PrestigeType.Experience, out var experiencePrestiges) && experiencePrestiges > UpdateBuffsBufferDestroyPatch.PrestigeBuffs.IndexOf(ShroudBuff))
        {
            BuffUtilities.ApplyPermanentBuff(character, ShroudBuff);
        }
    }
    */

    /*
    public static void HandleModifiers(ref float dropChance, Entity player)
    {
        ulong steamId = player.GetSteamId();

        if (PlayerModifiers.ContainsKey(steamId)) dropChance *= PlayerModifiers[steamId];
    }
    */
}
