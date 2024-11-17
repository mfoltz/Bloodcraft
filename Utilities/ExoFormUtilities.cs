using Bloodcraft.Services;
using Bloodcraft.Systems.Leveling;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using System.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Bloodcraft.Utilities;
internal static class ExoFormUtilities
{
    static EntityManager EntityManager => Core.EntityManager;
    static SystemService SystemService => Core.SystemService;
    static EndSimulationEntityCommandBufferSystem EndSimulationEntityCommandBufferSystem => SystemService.EndSimulationEntityCommandBufferSystem;

    public const float BaseDuration = 15f;
    public const float MaxAddedDuration = 165f;

    static readonly int ExoPrestiges = ConfigService.ExoPrestiges;

    static readonly AssetGuid AssetGuid = AssetGuid.FromString("2a1f5c1b-5a50-4ff0-a982-ca37efb8f69d");
    static readonly float3 Red = new(1f, 0f, 0f);

    static readonly WaitForSeconds SecondDelay = new(1f);
    public static bool CheckExoFormCharge(User user, ulong steamId) // also BuffUtilities? maybe ExoForm utilities or something, idk
    {
        UpdateExoFormChargeStored(steamId);

        if (steamId.TryGetPlayerExoFormData(out var exoFormData) && exoFormData.Value < BaseDuration)
        {
            ReplyNotEnoughCharge(user, steamId, exoFormData);
            return false;
        }
        else if (steamId.TryGetPlayerExoFormData(out exoFormData) && exoFormData.Value >= BaseDuration)
        {
            return true;
        }

        return false;
    }
    public static void ReplyNotEnoughCharge(User user, ulong steamId, KeyValuePair<DateTime, float> exoFormData) // this should be in BuffUtilities or something, need to organize later
    {
        string timeRemaining = GetTimeUntilCharged(steamId, exoFormData);
        if (!string.IsNullOrEmpty(timeRemaining)) LocalizationService.HandleServerReply(EntityManager, user, $"Not enough energy to maintain form... (<color=yellow>{timeRemaining}</color>)");
    }
    static string GetTimeUntilCharged(ulong steamId, KeyValuePair<DateTime, float> exoFormData) // same as method above, need to organize later
    {
        int exoLevel = steamId.TryGetPlayerPrestiges(out var prestiges) && prestiges.TryGetValue(PrestigeType.Exo, out int exoPrestiges) ? exoPrestiges : 0;
        float totalDuration = CalculateFormDuration(exoLevel);

        //float chargeNeeded = BaseDuration - exoFormData.Value; hmm this really shouldn't have been giving realistic values, need to check this out
        float chargeNeeded = BaseDuration;
        float ratioToTotal = chargeNeeded / totalDuration;
        float secondsRequired = 86400f * ratioToTotal;

        // Convert seconds to hours, minutes, and seconds
        TimeSpan timeSpan = TimeSpan.FromSeconds(secondsRequired);
        string timeRemaining;

        // Format based on the amount of time left
        if (timeSpan.TotalHours >= 1)
        {
            // Display hours, minutes, and seconds if more than an hour remains
            timeRemaining = $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m {timeSpan.Seconds}s";
        }
        else
        {
            // Display only minutes and seconds if less than an hour remains
            timeRemaining = $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
        }

        return timeRemaining;
    }
    public static float CalculateFormDuration(int prestigeLevel)
    {
        // Linear scaling from 15s to 120s over 1-100 prestige levels
        if (prestigeLevel == 1)
        {
            return 15f;
        }
        else if (prestigeLevel > 1)
        {
            return 15f + (MaxAddedDuration / ExoPrestiges) * (prestigeLevel);
        }

        return 0f;
    }
    public static IEnumerator ExoFormCountdown(Entity buffEntity, Entity playerEntity, Entity userEntity, float countdownDelay)
    {
        yield return new WaitForSeconds(countdownDelay);

        float countdown = 5f;

        // Wait until there are 5 seconds left
        while (buffEntity.Exists() && countdown > 0f)
        {
            float3 targetPosition = playerEntity.Read<Translation>().Value;
            targetPosition = new float3(targetPosition.x, targetPosition.y + 1.5f, targetPosition.z);

            ScrollingCombatTextMessage.Create(
                EntityManager,
                EndSimulationEntityCommandBufferSystem.CreateCommandBuffer(),
                AssetGuid,
                targetPosition,
                Red,
                playerEntity,
                countdown,
                default,
                userEntity
            );

            countdown--;
            yield return SecondDelay;
        }

        UpdateFullExoFormChargeUsed(playerEntity.GetSteamId());
    }
    public static void UpdateExoFormChargeStored(ulong steamId)
    {
        // add energy based on last time form was exited till now
        if (steamId.TryGetPlayerExoFormData(out var exoFormData))
        {
            DateTime now = DateTime.UtcNow;

            int exoLevel = steamId.TryGetPlayerPrestiges(out var prestiges) && prestiges.TryGetValue(PrestigeType.Exo, out int exoPrestiges) ? exoPrestiges : 0;
            float totalDuration = CalculateFormDuration(exoLevel);

            // energy earned based on total duration from exo level times fraction of time passed in seconds per day?
            float chargedEnergy = (float)(((now - exoFormData.Key).TotalSeconds / 86400) * totalDuration);
            float chargeStored = Mathf.Min(exoFormData.Value + chargedEnergy, MaxAddedDuration + BaseDuration);

            KeyValuePair<DateTime, float> timeEnergyPair = new(now, chargeStored);
            steamId.SetPlayerExoFormData(timeEnergyPair);
        }
    }
    public static void UpdatePartialExoFormChargeUsed(Entity buffEntity, ulong steamId)
    {
        if (steamId.TryGetPlayerExoFormData(out var exoFormData))
        {
            int exoLevel = steamId.TryGetPlayerPrestiges(out var prestiges) && prestiges.TryGetValue(PrestigeType.Exo, out int exoPrestiges) ? exoPrestiges : 0;
            float timeInForm = buffEntity.Read<Age>().Value;

            // set stamp to start 'charging' energy, subtract energy used based on duration and exo level
            //Core.Log.LogInfo($"Time spent in form: {timeInForm} ({totalDuration})");

            KeyValuePair<DateTime, float> timeEnergyPair = new(DateTime.UtcNow, exoFormData.Value - timeInForm);
            steamId.SetPlayerExoFormData(timeEnergyPair);
        }
    }
    public static void UpdateFullExoFormChargeUsed(ulong steamId)
    {
        if (steamId.TryGetPlayerExoFormData(out var exoFormData))
        {
            KeyValuePair<DateTime, float> timeEnergyPair = new(DateTime.UtcNow, 0f);
            steamId.SetPlayerExoFormData(timeEnergyPair);
        }
    }
}
