using Cobalt.Core;
using HarmonyLib;
using ProjectM;
using Stunlock.Network;
using Unity.Entities;
using static Cobalt.Systems.Bloodline.BloodlineStatsSystem;
using static Cobalt.Systems.Weapon.WeaponStatsSystem;
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
                    { "MasteryLogging", false },
                    { "ExperienceLogging", false },
                    { "BloodlineLogging", false },
                    { "ExperienceShare", false }
                });
                DataStructures.SavePlayerBools();
            }
            

            if (!DataStructures.PlayerExperience.ContainsKey(steamId))
            {
                DataStructures.PlayerExperience.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                DataStructures.SavePlayerExperience();
            }
            if (!DataStructures.PlayerMastery.ContainsKey(steamId))
            {
                DataStructures.PlayerMastery.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                DataStructures.SavePlayerMastery();
            }
            if (!DataStructures.PlayerBloodline.ContainsKey(steamId))
            {
                DataStructures.PlayerBloodline.Add(steamId, new KeyValuePair<int, float>(0, 0f));
                DataStructures.SavePlayerBloodLine();
            }
            if (!DataStructures.PlayerWeaponStats.ContainsKey(steamId))
            {
                DataStructures.PlayerWeaponStats.Add(steamId, new PlayerWeaponStats());
                DataStructures.SavePlayerWeaponStats();
            }
            if (!DataStructures.PlayerBloodlineStats.ContainsKey(steamId))
            {
                DataStructures.PlayerBloodlineStats.Add(steamId, new PlayerBloodlineStats());
                DataStructures.SavePlayerBloodlineStats();
            }
            
        }
    }
}