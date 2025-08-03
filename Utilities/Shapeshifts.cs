using Bloodcraft.Interfaces;
using Bloodcraft.Resources;
using Bloodcraft.Services;
using ProjectM;
using ProjectM.Gameplay.Scripting;
using ProjectM.Network;
using Stunlock.Core;
using System.Collections;
using System.Collections.Concurrent;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Shapeshift = Bloodcraft.Interfaces.Shapeshift;

namespace Bloodcraft.Utilities;
internal static class Shapeshifts
{
    static EntityManager EntityManager => Core.EntityManager;
    static SystemService SystemService => Core.SystemService;
    static EndSimulationEntityCommandBufferSystem EndSimulationEntityCommandBufferSystem => SystemService.EndSimulationEntityCommandBufferSystem;
    static ReplaceAbilityOnSlotSystem ReplaceAbilityOnSlotSystem => SystemService.ReplaceAbilityOnSlotSystem;

    public const float BASE_DURATION = 15f;
    public const float MAX_ADDED_DURATION = 165f;
    const float EXO_COUNTDOWN = 5f;
    const float DAY_SECONDS = 86400f;
    const int EXO_PRESTIGES = 100;
    const float DELAY = 1f;

    static readonly AssetGuid _assetGuid = AssetGuid.FromString("2a1f5c1b-5a50-4ff0-a982-ca37efb8f69d");
    static readonly PrefabGUID _sctInfoWarning = PrefabGUIDs.SCT_Type_InfoWarning;
    static readonly float3 _yellow = new(1f, 1f, 0f);

    static readonly WaitForSeconds _secondDelay = new(DELAY);

    static readonly PrefabGUID _immortalBlood = PrefabGUIDs.BloodType_DraculaTheImmortal;
    static readonly PrefabGUID _frailedBlood = PrefabGUIDs.BloodType_None;
    public static IReadOnlyDictionary<PrefabGUID, IShapeshift> ShapeshiftForms => _shapeshiftForms;
    static readonly Dictionary<PrefabGUID, IShapeshift> _shapeshiftForms = new()
    {
        [Buffs.EvolvedVampireBuff] = new EvolvedVampire(),
        [Buffs.CorruptedSerpentBuff] = new CorruptedSerpent(),
        // [Buffs.AncientGuardianBuff] = new AncientGuardian()
    };
    public static IReadOnlyDictionary<ShapeshiftType, PrefabGUID> ShapeshiftBuffs => _shapeshiftBuffs;
    static readonly Dictionary<ShapeshiftType, PrefabGUID> _shapeshiftBuffs = new()
    {
        [ShapeshiftType.EvolvedVampire] = Buffs.EvolvedVampireBuff,
        [ShapeshiftType.CorruptedSerpent] = Buffs.CorruptedSerpentBuff
        // [ShapeshiftType.AncientGuardian] = Buffs.AncientGuardianBuff
    };
    public static class ShapeshiftRegistry
    {
        static readonly Lazy<List<IShapeshift>> _forms = new(() =>
        [
            new EvolvedVampire(),
            new CorruptedSerpent(),
            // new AncientGuardian()
        ]);

        static readonly Lazy<Dictionary<PrefabGUID, IShapeshift>> _buffToForm =
            new(() => _forms.Value.ToDictionary(f => f.ShapeshiftBuff));

        static readonly Lazy<Dictionary<PrefabGUID, IShapeshift>> _abilityGroupToForm =
            new(() =>
            {
                var map = new Dictionary<PrefabGUID, IShapeshift>();
                foreach (var form in _forms.Value)
                {
                    foreach (var ability in form.AbilityGroups)
                    {
                        if (!map.ContainsKey(ability))
                            map[ability] = form;
                    }
                }

                return map;
            });
        public static IEnumerable<IShapeshift> All => _forms.Value;
        public static bool TryGetByBuff(PrefabGUID buff, out IShapeshift form)
            => _buffToForm.Value.TryGetValue(buff, out form);
        public static bool TryGetByAbilityGroup(PrefabGUID abilityGroup, out IShapeshift form)
            => _abilityGroupToForm.Value.TryGetValue(abilityGroup, out form);
    }
    public static class ShapeshiftCache
    {
        public static IReadOnlyDictionary<ulong, PrefabGUID> PlayerShapeshifts => _playerShapeshifts;
        static readonly ConcurrentDictionary<ulong, PrefabGUID> _playerShapeshifts = [];
        public static void SetShapeshiftBuff(ulong steamId, ShapeshiftType shapeshiftType) => _playerShapeshifts[steamId] = GetShapeshiftBuff(shapeshiftType);
        public static bool TryGetShapeshiftBuff(ulong steamId, out PrefabGUID shapeshiftBuff) => _playerShapeshifts.TryGetValue(steamId, out shapeshiftBuff);
    }

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
        if (steamId.TryGetPlayerExoFormData(out var exoFormData) && buffEntity.TryGetComponent(out Age age))
        {
            KeyValuePair<DateTime, float> timeEnergyPair = new(DateTime.UtcNow, exoFormData.Value - age.Value);
            steamId.SetPlayerExoFormData(timeEnergyPair);
        }
    }
    public static IEnumerator ExoFormCountdown(Entity buffEntity, Entity playerCharacter, Entity userEntity, float countdownDelay)
    {
        yield return new WaitForSeconds(countdownDelay);

        float countdown = 5f;
        bool fullDuration = false;

        // Wait until there are 5 seconds left
        while (buffEntity.Exists() && countdown > 0f)
        {
            float3 targetPosition = playerCharacter.GetPosition();
            targetPosition = new float3(targetPosition.x, targetPosition.y + 1.5f, targetPosition.z);

            ScrollingCombatTextMessage.Create(
                EntityManager,
                EndSimulationEntityCommandBufferSystem.CreateCommandBuffer(),
                _assetGuid,
                targetPosition,
                _yellow,
                playerCharacter,
                countdown,
                _sctInfoWarning,
                userEntity
            );

            countdown--;
            yield return _secondDelay;

            if (countdown <= 0f)
            {
                fullDuration = true;
            }
        }

        if (fullDuration) UpdateFullExoFormChargeUsed(playerCharacter.GetSteamId());
    }
    public static void UpdateFullExoFormChargeUsed(ulong steamId)
    {
        KeyValuePair<DateTime, float> timeEnergyPair = new(DateTime.UtcNow, 0f);
        steamId.SetPlayerExoFormData(timeEnergyPair);
    }
    public static void TrueImmortal(Entity buffEntity, Entity playerCharacter)
    {
        Blood blood = playerCharacter.Read<Blood>();
        ulong steamId = playerCharacter.GetSteamId();
        var shapeshiftBuff = ShapeshiftBuffs.FirstOrDefault(buff => playerCharacter.HasBuff(buff.Value));
        bool hasValue = !shapeshiftBuff.Equals(default);

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
        else if (hasValue && _storedPlayerBloods.TryAdd(steamId, blood))
        {
            if (buffEntity.Has<ChangeBloodOnGameplayEvent>())
            {
                var buffer = buffEntity.ReadBuffer<ChangeBloodOnGameplayEvent>();

                ChangeBloodOnGameplayEvent changeBloodOnGameplayEvent = buffer[0];

                changeBloodOnGameplayEvent.BloodValue = 100f;
                changeBloodOnGameplayEvent.BloodQuality = 100f;
                changeBloodOnGameplayEvent.BloodType = _immortalBlood;
                changeBloodOnGameplayEvent.GainBloodType = GainBloodType.Consumable;

                buffer[0] = changeBloodOnGameplayEvent;
            }
        }
        else // 100% frailed as backup for server crashes or otherwise losing stored blood cache? good enough for devs good enough for me :p might not need this if checking for exoform buff in else if above but will see
        {
            if (buffEntity.Has<ChangeBloodOnGameplayEvent>())
            {
                var buffer = buffEntity.ReadBuffer<ChangeBloodOnGameplayEvent>();

                ChangeBloodOnGameplayEvent changeBloodOnGameplayEvent = buffer[0];

                changeBloodOnGameplayEvent.BloodValue = 100f;
                changeBloodOnGameplayEvent.BloodQuality = 100f;
                changeBloodOnGameplayEvent.BloodType = _frailedBlood;
                changeBloodOnGameplayEvent.GainBloodType = GainBloodType.Consumable;

                buffer[0] = changeBloodOnGameplayEvent;
            }
        }
    }
    public static void ModifyShapeshiftBuff(Entity buffEntity, Entity playerCharacter, PrefabGUID buffPrefabGuid)
    {
        Entity userEntity = playerCharacter.GetUserEntity();
        User user = userEntity.GetUser();
        ulong steamId = user.PlatformId;

        if (!ShapeshiftForms.TryGetValue(buffPrefabGuid, out var shapeshiftForm))
        {
            // Core.Log.LogWarning($"[ModifyShapeshiftBuff] No shapeshift for buff - {buffPrefabGuid.GetPrefabName()}");
            return;
        }

        float duration = steamId.TryGetPlayerExoFormData(out var exoFormData)
            ? exoFormData.Value
            : 0f;

        float bonusPhysicalPower = playerCharacter.TryGetComponent(out UnitStats unitStats)
            ? unitStats.SpellPower._Value
            : 0f;

        if (buffEntity.TryGetBuffer<ApplyBuffOnGameplayEvent>(out var applyBuffBuffer) && !applyBuffBuffer.IsEmpty)
        {
            ApplyBuffOnGameplayEvent applyBuffOnGameplayEvent = applyBuffBuffer[0];
            applyBuffOnGameplayEvent.Buff0 = PrefabGUID.Empty;
            applyBuffBuffer[0] = applyBuffOnGameplayEvent;
        }

        buffEntity.Add<ReplaceAbilityOnSlotData>();
        buffEntity.Add<Script_Buff_Shapeshift_DataShared>();

        buffEntity.AddWith((ref LifeTime lifeTime) =>
        {
            lifeTime.Duration = duration;
            lifeTime.EndAction = LifeTimeEndAction.Destroy;
        });

        buffEntity.AddWith((ref ChangeKnockbackResistanceBuff knockback) => knockback.KnockbackResistanceIndex = 6);

        if (!buffEntity.TryGetBuffer<ModifyUnitStatBuff_DOTS>(out var modifyStatsBuffer))
        {
            modifyStatsBuffer = buffEntity.AddBuffer<ModifyUnitStatBuff_DOTS>();
        }

        modifyStatsBuffer.Add(new ModifyUnitStatBuff_DOTS
        {
            StatType = UnitStatType.PhysicalPower,
            AttributeCapType = AttributeCapType.Uncapped,
            ModificationType = ModificationType.Add,
            Value = bonusPhysicalPower,
            Modifier = 1,
            IncreaseByStacks = false,
            ValueByStacks = 0,
            Priority = 0,
            Id = ModificationIDs.Create().NewModificationId()
        });

        buffEntity.With((ref Buff buff) => buff.BuffType = BuffType.Block);

        buffEntity.With((ref BuffCategory buffCategory) => buffCategory.Groups = BuffCategoryFlag.Shapeshift | BuffCategoryFlag.RemoveOnDisconnect);

        buffEntity.AddWith((ref AmplifyBuff amplifyBuff) => amplifyBuff.AmplifyModifier = -0.25f);

        if (!buffEntity.TryGetBuffer<ReplaceAbilityOnSlotBuff>(out var replaceAbilityBuffer))
        {
            replaceAbilityBuffer = buffEntity.AddBuffer<ReplaceAbilityOnSlotBuff>();
        }

        foreach (var (slot, ability) in shapeshiftForm.Abilities)
        {
            replaceAbilityBuffer.Add(new ReplaceAbilityOnSlotBuff
            {
                Target = ReplaceAbilityTarget.BuffTarget,
                Slot = slot,
                NewGroupId = ability,
                Priority = 99,
                CopyCooldown = true,
                CastBlockType = GroupSlotModificationCastBlockType.WholeCast
            });
        }

        ReplaceAbilityOnSlotSystem.OnUpdate();
        ExoFormCountdown(buffEntity, playerCharacter, userEntity, duration - EXO_COUNTDOWN).Start();
    }
    public static float GetShapeshiftAbilityCooldown<T>(PrefabGUID abilityGroup) where T : Shapeshift, new()
    {
        var shapeshift = new T();
        return shapeshift.TryGetCooldown(abilityGroup, out var cooldown) ? cooldown : 0f;
    }
    public static PrefabGUID GetShapeshiftBuff(ShapeshiftType shapeshiftType)
    {
        return ShapeshiftBuffs[shapeshiftType];
    }
    public static bool IsExoForm(this Entity playerCharacter)
    {
        return playerCharacter.HasBuff(Buffs.EvolvedVampireBuff)
            || playerCharacter.HasBuff(Buffs.CorruptedSerpentBuff);
    }
}
