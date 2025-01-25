using Bloodcraft.Services;
using Bloodcraft.Systems.Leveling;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using System.Collections;
using System.Collections.Concurrent;
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
    const float DAY_SECONDS = 86400f;
    const int EXO_PRESTIGES = 100;

    static readonly AssetGuid _assetGuid = AssetGuid.FromString("2a1f5c1b-5a50-4ff0-a982-ca37efb8f69d");
    static readonly PrefabGUID _exoCountdownSCT = new(106212079);
    static readonly float3 _yellow = new(1.0f, 1.0f, 0.0f);

    static readonly WaitForSeconds _secondDelay = new(1f);

    static readonly PrefabGUID _immortalBloodType = new(2010023718);

    static readonly ConcurrentDictionary<ulong, Blood> _storedPlayerBloods = [];
    public static bool CheckExoFormCharge(User user, ulong steamId)
    {
        UpdateExoFormChargeStored(steamId);

        if (steamId.TryGetPlayerExoFormData(out var exoFormData) && exoFormData.Value < BASE_DURATION)
        {
            ReplyNotEnoughCharge(user, steamId, exoFormData.Value);

            return false;
        }
        else if (steamId.TryGetPlayerExoFormData(out exoFormData) && exoFormData.Value >= BASE_DURATION)
        {
            return true;
        }

        return false;
    }
    public static void ReplyNotEnoughCharge(User user, ulong steamId, float value)
    {
        string timeRemaining = GetTimeUntilCharged(steamId, value);

        if (!string.IsNullOrEmpty(timeRemaining)) LocalizationService.HandleServerReply(EntityManager, user, $"Not enough energy to maintain form... (<color=yellow>{timeRemaining}</color>)");
    }
    static string GetTimeUntilCharged(ulong steamId, float value)
    {
        int exoLevel = steamId.TryGetPlayerPrestiges(out var prestiges) && prestiges.TryGetValue(PrestigeType.Exo, out int exoPrestiges) ? exoPrestiges : 0;
        float totalDuration = CalculateFormDuration(exoLevel);

        float chargeNeeded = BASE_DURATION - value;
        float ratioToTotal = chargeNeeded / totalDuration;
        float secondsRequired = DAY_SECONDS * ratioToTotal;

        TimeSpan timeSpan = TimeSpan.FromSeconds(secondsRequired);
        string timeRemaining;

        if (timeSpan.TotalHours >= 1)
        {
            timeRemaining = $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m {timeSpan.Seconds}s";
        }
        else
        {
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
            return 15f + (MAX_ADDED_DURATION / EXO_PRESTIGES) * (prestigeLevel);
        }

        return 0f;
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
            float timeInForm = buffEntity.Read<Age>().Value;

            KeyValuePair<DateTime, float> timeEnergyPair = new(DateTime.UtcNow, exoFormData.Value - timeInForm);
            steamId.SetPlayerExoFormData(timeEnergyPair);
        }
    }
    public static IEnumerator ExoFormCountdown(Entity buffEntity, Entity playerEntity, Entity userEntity, float countdownDelay)
    {
        yield return new WaitForSeconds(countdownDelay);

        float countdown = 5f;
        bool fullDuration = false;

        // Wait until there are 5 seconds left
        while (buffEntity.Exists() && countdown > 0f)
        {
            float3 targetPosition = playerEntity.Read<Translation>().Value;
            targetPosition = new float3(targetPosition.x, targetPosition.y + 1.5f, targetPosition.z);

            ScrollingCombatTextMessage.Create(
                EntityManager,
                EndSimulationEntityCommandBufferSystem.CreateCommandBuffer(),
                _assetGuid,
                targetPosition,
                _yellow,
                playerEntity,
                countdown,
                _exoCountdownSCT,
                userEntity
            );

            countdown--;
            yield return _secondDelay;

            if (countdown == 0f)
            {
                fullDuration = true;
            }
        }

        if (fullDuration) UpdateFullExoFormChargeUsed(playerEntity.GetSteamId());
    }
    public static void UpdateFullExoFormChargeUsed(ulong steamId)
    {
        KeyValuePair<DateTime, float> timeEnergyPair = new(DateTime.UtcNow, 0f);
        steamId.SetPlayerExoFormData(timeEnergyPair);
    }
    public static void HandleExoImmortal(Entity buffEntity, Entity playerCharacter)
    {
        Blood blood = playerCharacter.Read<Blood>();
        ulong steamId = playerCharacter.GetSteamId();

        if (_storedPlayerBloods.TryRemove(steamId, out Blood storedBlood))
        {
            if (buffEntity.Has<ChangeBloodOnGameplayEvent>())
            {
                var buffer = buffEntity.ReadBuffer<ChangeBloodOnGameplayEvent>();

                ChangeBloodOnGameplayEvent changeBloodOnGameplayEvent = buffer[0];

                changeBloodOnGameplayEvent.BloodValue = storedBlood.Value;
                changeBloodOnGameplayEvent.BloodQuality = storedBlood.Quality;
                changeBloodOnGameplayEvent.BloodType = storedBlood.BloodType;
                changeBloodOnGameplayEvent.GainBloodType = GainBloodType.Consumable;

                buffer[0] = changeBloodOnGameplayEvent;
            }
        }
        else if (_storedPlayerBloods.TryAdd(steamId, blood))
        {
            if (buffEntity.Has<ChangeBloodOnGameplayEvent>())
            {
                var buffer = buffEntity.ReadBuffer<ChangeBloodOnGameplayEvent>();

                ChangeBloodOnGameplayEvent changeBloodOnGameplayEvent = buffer[0];

                changeBloodOnGameplayEvent.BloodValue = 100f;
                changeBloodOnGameplayEvent.BloodQuality = 100f;
                changeBloodOnGameplayEvent.BloodType = _immortalBloodType;
                changeBloodOnGameplayEvent.GainBloodType = GainBloodType.Consumable;

                buffer[0] = changeBloodOnGameplayEvent;
            }
        }
    }
}
