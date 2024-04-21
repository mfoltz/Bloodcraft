using Cobalt.Core;
using HarmonyLib;
using ProjectM;
using Stunlock.Network;
using Unity.Entities;
using static Cobalt.Systems.WeaponStatsSystem;
using User = ProjectM.Network.User;

namespace VPlus.Hooks
{
    [HarmonyPatch]
    public class ServerBootstrapPatches
    {
        [HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUserConnected))]
        [HarmonyPrefix]
        private static unsafe void OnUserConnectedPrefix(ServerBootstrapSystem __instance, NetConnectionId netConnectionId)
        {
            int userIndex = __instance._NetEndPointToApprovedUserIndex[netConnectionId];
            ServerBootstrapSystem.ServerClient serverClient = __instance._ApprovedUsersLookup[userIndex];
            Entity userEntity = serverClient.UserEntity;
            User user = __instance.EntityManager.GetComponentData<User>(userEntity);
            Entity playerEntity = user.LocalCharacter.GetEntityOnServer();
            ulong steamId = user.PlatformId;

            if (!DataStructures.PlayerBools.ContainsKey(steamId))
            {
                DataStructures.PlayerBools.Add(steamId, new Dictionary<string, bool>
                {
                    { "MasteryLogging", false },
                    { "ExperienceLogging", false },
                    { "BloodlineLogging", false }
                });
                DataStructures.SavePlayerBools();
            }
            if (!DataStructures.PlayerExperience.ContainsKey(steamId))
            {
                DataStructures.PlayerExperience.Add(steamId, new KeyValuePair<int, float>(0, 0));
                DataStructures.SavePlayerExperience();
            }
            if (!DataStructures.PlayerMastery.ContainsKey(steamId))
            {
                DataStructures.PlayerMastery.Add(steamId, new KeyValuePair<int, DateTime>(0, DateTime.Now));
                DataStructures.SavePlayerMastery();
            }
            if (!DataStructures.PlayerStats.ContainsKey(steamId))
            {
                DataStructures.PlayerStats.Add(steamId, new PlayerStats());
                DataStructures.SavePlayerStats();
            }
        }
    }
}