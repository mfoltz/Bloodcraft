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
internal static class ExoForm
{
    static EntityManager EntityManager => Core.EntityManager;
    static SystemService SystemService => Core.SystemService;
    static EndSimulationEntityCommandBufferSystem EndSimulationEntityCommandBufferSystem => SystemService.EndSimulationEntityCommandBufferSystem;

    public const float BASE_DURATION = 15f;
    public const float MAX_ADDED_DURATION = 165f;

    static readonly int _exoPrestiges = ConfigService.ExoPrestiges;

    static readonly AssetGuid _assetGuid = AssetGuid.FromString("2a1f5c1b-5a50-4ff0-a982-ca37efb8f69d");
    static readonly PrefabGUID _exoCountdownSCT = new(106212079);
    static readonly float3 _red = new(1f, 0f, 0f);

    static readonly WaitForSeconds _secondDelay = new(1f);
    public static bool CheckExoFormCharge(User user, ulong steamId) // also BuffUtilities? maybe ExoForm utilities or something, idk
    {
        UpdateExoFormChargeStored(steamId);

        if (steamId.TryGetPlayerExoFormData(out var exoFormData) && exoFormData.Value < BASE_DURATION)
        {
            ReplyNotEnoughCharge(user, steamId);

            return false;
        }
        else if (steamId.TryGetPlayerExoFormData(out exoFormData) && exoFormData.Value >= BASE_DURATION)
        {
            return true;
        }

        return false;
    }
    public static void ReplyNotEnoughCharge(User user, ulong steamId) // this should be in BuffUtilities or something, need to organize later
    {
        string timeRemaining = GetTimeUntilCharged(steamId);

        if (!string.IsNullOrEmpty(timeRemaining)) LocalizationService.HandleServerReply(EntityManager, user, $"Not enough energy to maintain form... (<color=yellow>{timeRemaining}</color>)");
    }
    static string GetTimeUntilCharged(ulong steamId) // same as method above, need to organize later
    {
        int exoLevel = steamId.TryGetPlayerPrestiges(out var prestiges) && prestiges.TryGetValue(PrestigeType.Exo, out int exoPrestiges) ? exoPrestiges : 0;
        float totalDuration = CalculateFormDuration(exoLevel);

        //float chargeNeeded = BaseDuration - exoFormData.Value; hmm this really shouldn't have been giving realistic values, need to check this out
        float chargeNeeded = BASE_DURATION;
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
        if (prestigeLevel == 1)
        {
            return 15f;
        }
        else if (prestigeLevel > 1)
        {
            return 15f + (MAX_ADDED_DURATION / _exoPrestiges) * (prestigeLevel);
        }

        return 0f;
    }
    public static IEnumerator ExoFormCountdown(Entity buffEntity, Entity playerEntity, Entity userEntity, float countdownDelay)
    {
        yield return new WaitForSeconds(countdownDelay);

        float countdown = 5f;
        bool fullDuration = false;

        // Wait until there are 5 seconds left
        while (buffEntity.Exists() && countdown > 0f)
        {
            float3 targetPosition = playerEntity.ReadRO<Translation>().Value;
            targetPosition = new float3(targetPosition.x, targetPosition.y + 1.5f, targetPosition.z);

            ScrollingCombatTextMessage.Create(
                EntityManager,
                EndSimulationEntityCommandBufferSystem.CreateCommandBuffer(),
                _assetGuid,
                targetPosition,
                _red,
                playerEntity,
                countdown,
                _exoCountdownSCT,
                userEntity
            );

            countdown--;
            yield return _secondDelay;

            if (countdown == 0f)
            {
                fullDuration = true; // Mark as used full duration
            }
        }

        if (fullDuration) UpdateFullExoFormChargeUsed(playerEntity.GetSteamId());
    }
    public static void UpdateExoFormChargeStored(ulong steamId)
    {
        if (steamId.TryGetPlayerExoFormData(out var exoFormData))
        {
            DateTime now = DateTime.UtcNow;

            int exoLevel = steamId.TryGetPlayerPrestiges(out var prestiges) && prestiges.TryGetValue(PrestigeType.Exo, out int exoPrestiges) ? exoPrestiges : 0;
            float totalDuration = CalculateFormDuration(exoLevel);

            float chargedEnergy = (float)(((now - exoFormData.Key).TotalSeconds / 86400) * totalDuration);
            float chargeStored = Mathf.Min(exoFormData.Value + chargedEnergy, totalDuration);

            KeyValuePair<DateTime, float> timeEnergyPair = new(now, chargeStored);
            steamId.SetPlayerExoFormData(timeEnergyPair);
        }
    }
    public static void UpdatePartialExoFormChargeUsed(Entity buffEntity, ulong steamId)
    {
        if (steamId.TryGetPlayerExoFormData(out var exoFormData))
        {
            float timeInForm = buffEntity.ReadRO<Age>().Value;

            KeyValuePair<DateTime, float> timeEnergyPair = new(DateTime.UtcNow, exoFormData.Value - timeInForm);
            steamId.SetPlayerExoFormData(timeEnergyPair);
        }
    }
    public static void UpdateFullExoFormChargeUsed(ulong steamId)
    {
        KeyValuePair<DateTime, float> timeEnergyPair = new(DateTime.UtcNow, 0f);
        steamId.SetPlayerExoFormData(timeEnergyPair);
    }
}
