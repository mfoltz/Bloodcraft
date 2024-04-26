using Cobalt.Core;
using HarmonyLib;
using ProjectM;
using Stunlock.Network;
using Unity.Entities;
using User = ProjectM.Network.User;

namespace Cobalt.Hooks
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
            ulong steamId = user.PlatformId;

            if (!DataStructures.PlayerBools.ContainsKey(steamId))
            {
                DataStructures.PlayerBools.Add(steamId, new Dictionary<string, bool>
                {
                    { "ExperienceLogging", false },
                    { "ExperienceShare", false },
                    { "ProfessionLogging", false },
                });
                DataStructures.SavePlayerBools();
            }

            if (!DataStructures.PlayerExperience.ContainsKey(steamId))
            {
                DataStructures.PlayerExperience.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                DataStructures.SavePlayerExperience();
            }

            if (!DataStructures.PlayerWoodcutting.ContainsKey(steamId))
            {
                DataStructures.PlayerWoodcutting.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                DataStructures.SavePlayerWoodcutting();
            }
            if (!DataStructures.PlayerMining.ContainsKey(steamId))
            {
                DataStructures.PlayerMining.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                DataStructures.SavePlayerMining();
            }
            if (!DataStructures.PlayerFishing.ContainsKey(steamId))
            {
                DataStructures.PlayerFishing.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                DataStructures.SavePlayerFishing();
            }
        }
    }
}