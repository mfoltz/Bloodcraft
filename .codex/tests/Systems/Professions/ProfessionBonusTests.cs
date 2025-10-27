using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bloodcraft.Interfaces;
using Bloodcraft.Resources;
using Bloodcraft.Services;
using Bloodcraft.Systems.Professions;
using Bloodcraft.Tests.Support;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using Unity.Mathematics;
using Xunit;
using static Bloodcraft.Utilities.Misc.PlayerBoolsManager;
using static Bloodcraft.Utilities.Progression;

namespace Bloodcraft.Tests.Systems.Professions;

public sealed class ProfessionBonusTests : TestHost
{
    static readonly PrefabGUID FishingArea = new(112233445);
    static readonly Entity TargetEntity = new() { Index = 5, Version = 1 };
    static readonly Entity PlayerCharacter = new() { Index = 6, Version = 1 };
    static readonly Entity UserEntity = new() { Index = 7, Version = 1 };
    const ulong SteamId = 76561198000001000UL;

    protected override void ResetState()
    {
        base.ResetState();
        ConfigDirectoryShim.EnsureInitialized();
    }

    [Fact]
    public void GiveProfessionBonus_AddsFishAndLogsWhenInventoryAccepts()
    {
        var fish = new PrefabGUID(99887766);

        using var harness = ProfessionBonusHarness.Create();
        harness.ProfessionLogging = true;
        harness.SctYield = true;
        harness.TryAddInventory = (_, _) => true;
        harness.ConfigureFishingDrops(new[] { fish });
        harness.SetLocalizedName(fish, "Gleaming Goby");

        harness.RunFishingBonus(
            FishingArea,
            TargetEntity,
            PlayerCharacter,
            UserEntity,
            SteamId,
            level: 40);

        Assert.Contains((fish, 2), harness.AddedItems);
        Assert.DoesNotContain(harness.DroppedItems, entry => entry.Item == fish);

        PrefabGUID grease = PrefabGUIDs.Item_Ingredient_MutantGrease;
        Assert.Contains(harness.AddedItems, entry => entry.Item == grease && entry.Amount == 10);

        Assert.Contains(harness.InventoryAttempts, entry => entry.Item == fish && entry.Amount == 2);
        Assert.Contains(harness.InventoryAttempts, entry => entry.Item == grease && entry.Amount == 10);

        Assert.Contains(harness.ExperienceEvents, entry => entry.Resource == fish && Math.Abs(entry.Amount - 2f) < 0.001f);
        Assert.Single(harness.LocalizationMessages.Where(message => message.Contains("Gleaming Goby")));
        Assert.Contains(harness.LocalizationMessages, message => message.Contains("Mutant Grease"));

        Assert.Contains(harness.BonusYieldSctEvents, amount => Math.Abs(amount - 2f) < 0.001f);
        Assert.Contains(harness.BonusYieldSctEvents, amount => Math.Abs(amount - 10f) < 0.001f);
    }

    [Fact]
    public void GiveProfessionBonus_DropsFishAndGreaseWhenInventoryIsFull()
    {
        var fish = new PrefabGUID(88776655);

        using var harness = ProfessionBonusHarness.Create();
        harness.ProfessionLogging = true;
        harness.SctYield = true;
        harness.TryAddInventory = (_, _) => false;
        harness.ConfigureFishingDrops(new[] { fish });
        harness.SetLocalizedName(fish, "Silent Stinger");

        harness.RunFishingBonus(
            FishingArea,
            TargetEntity,
            PlayerCharacter,
            UserEntity,
            SteamId,
            level: 24);

        Assert.DoesNotContain(harness.AddedItems, entry => entry.Item == fish);
        Assert.Contains(harness.DroppedItems, entry => entry.Item == fish && entry.Amount == 1);

        PrefabGUID grease = PrefabGUIDs.Item_Ingredient_MutantGrease;
        Assert.Contains(harness.DroppedItems, entry => entry.Item == grease && entry.Amount == 6);

        Assert.Contains(harness.InventoryAttempts, entry => entry.Item == fish && entry.Amount == 1);
        Assert.Contains(harness.InventoryAttempts, entry => entry.Item == grease && entry.Amount == 6);

        Assert.Contains(harness.ExperienceEvents, entry => entry.Resource == fish && Math.Abs(entry.Amount - 1f) < 0.001f);
        Assert.Single(harness.LocalizationMessages.Where(message => message.Contains("Silent Stinger")));
        Assert.Contains(harness.LocalizationMessages, message => message.Contains("Mutant Grease"));

        Assert.Contains(harness.BonusYieldSctEvents, amount => Math.Abs(amount - 1f) < 0.001f);
        Assert.Contains(harness.BonusYieldSctEvents, amount => Math.Abs(amount - 6f) < 0.001f);
    }

    sealed class ProfessionBonusHarness : IDisposable
    {
        static readonly Harmony HarmonyInstance = new("Bloodcraft.Tests.Systems.Professions.ProfessionBonusTests");
        static readonly FieldInfo PrefabGuidNamesField = AccessTools.Field(typeof(LocalizationService), "_prefabGuidNames")!;
        static readonly FieldInfo PrefabMapField = AccessTools.Field(typeof(PrefabCollectionSystem), "_PrefabGuidToEntityMap")!;
        static readonly MethodInfo? PrefabMapAddMethod = PrefabMapField.FieldType.GetMethod("Add", new[] { typeof(PrefabGUID), typeof(Entity) });
        static readonly MethodInfo? PrefabMapRemoveMethod = PrefabMapField.FieldType.GetMethod("Remove", new[] { typeof(PrefabGUID) });
        static readonly MethodInfo? PrefabMapTryGetValueMethod = PrefabMapField.FieldType.GetMethod("TryGetValue", new[] { typeof(PrefabGUID), typeof(Entity).MakeByRefType() });
        static readonly PropertyInfo? PrefabMapIndexer = PrefabMapField.FieldType.GetProperty("Item", new[] { typeof(PrefabGUID) });
        static bool patched;

        static ProfessionBonusHarness? active;

        readonly DataStateScope dataScope;
        readonly IDisposable persistenceScope;
        readonly object? originalPrefabMap;
        readonly Dictionary<PrefabGUID, string?> localizationBaseline = new();
        readonly Dictionary<PrefabGUID, (bool HasValue, Entity Value)> prefabMapBaseline = new();

        ProfessionBonusHarness()
        {
            dataScope = new DataStateScope();
            persistenceScope = DataService.SuppressPersistence();
            originalPrefabMap = PrefabMapField.GetValue(null);

            if (originalPrefabMap is null)
            {
                object mapInstance = Activator.CreateInstance(PrefabMapField.FieldType)!;
                PrefabMapField.SetValue(null, mapInstance);
            }

            if (!patched)
            {
                InstallPatches();
                patched = true;
            }

            active = this;
        }

        public static ProfessionBonusHarness Create() => new();

        public bool ProfessionLogging { get; set; }
        public bool SctYield { get; set; }
        public Func<PrefabGUID, int, bool>? TryAddInventory { get; set; }
        public IReadOnlyList<PrefabGUID>? FishingDrops { get; private set; }

        public List<(PrefabGUID Item, int Amount)> InventoryAttempts { get; } = new();
        public List<(PrefabGUID Item, int Amount)> AddedItems { get; } = new();
        public List<(PrefabGUID Item, int Amount)> DroppedItems { get; } = new();
        public List<(PrefabGUID Resource, float Amount)> ExperienceEvents { get; } = new();
        public List<string> LocalizationMessages { get; } = new();
        public List<float> BonusYieldSctEvents { get; } = new();

        public void ConfigureFishingDrops(IReadOnlyList<PrefabGUID> drops)
        {
            FishingDrops = drops ?? throw new ArgumentNullException(nameof(drops));
        }

        public void SetLocalizedName(PrefabGUID prefab, string name)
        {
            object? existingMap = PrefabGuidNamesField.GetValue(null);
            if (existingMap is null)
            {
                existingMap = new Dictionary<PrefabGUID, string>();
                PrefabGuidNamesField.SetValue(null, existingMap);
            }

            var map = (IDictionary<PrefabGUID, string>)existingMap;
            if (!localizationBaseline.ContainsKey(prefab))
            {
                map.TryGetValue(prefab, out string? existing);
                localizationBaseline[prefab] = existing;
            }

            map[prefab] = name;
        }

        public void RunFishingBonus(PrefabGUID fishingSpot, Entity target, Entity playerCharacter, Entity userEntity, ulong steamId, int level)
        {
            if (FishingDrops is null)
            {
                throw new InvalidOperationException("Fishing drops must be configured before invoking the harness.");
            }

            active = this;

            try
            {
                SetPlayerState(steamId, level);
                SeedPrefabMap(fishingSpot);

                var handler = new FishingProfession();
                User user = default;
                ProfessionSystem.GiveProfessionBonus(target, fishingSpot, playerCharacter, userEntity, user, steamId, handler, delay: 0f);
            }
            finally
            {
                active = null;
            }
        }

        void SetPlayerState(ulong steamId, int level)
        {
            DataService.PlayerDictionaries._playerFishing[steamId] = new KeyValuePair<int, float>(level, ConvertLevelToXp(level));
            SetPlayerBool(steamId, PROFESSION_LOG_KEY, ProfessionLogging);
            SetPlayerBool(steamId, SCT_YIELD_KEY, SctYield);
        }

        void SeedPrefabMap(PrefabGUID fishingSpot)
        {
            object map = PrefabMapField.GetValue(null)!;

            if (!prefabMapBaseline.ContainsKey(fishingSpot))
            {
                Entity existingValue = default;
                bool found = false;

                if (PrefabMapTryGetValueMethod is not null)
                {
                    object[] parameters = new object[] { fishingSpot, existingValue };
                    found = PrefabMapTryGetValueMethod.Invoke(map, parameters) is bool result && result;
                    if (found)
                    {
                        existingValue = (Entity)parameters[1];
                    }
                }
                else if (PrefabMapIndexer is not null)
                {
                    try
                    {
                        existingValue = (Entity)PrefabMapIndexer.GetValue(map, new object[] { fishingSpot })!;
                        found = true;
                    }
                    catch
                    {
                        found = false;
                    }
                }

                prefabMapBaseline[fishingSpot] = (found, existingValue);
            }

            PrefabMapRemoveMethod?.Invoke(map, new object[] { fishingSpot });

            if (PrefabMapIndexer is not null)
            {
                PrefabMapIndexer.SetValue(map, new Entity { Index = 10, Version = 1 }, new object[] { fishingSpot });
            }
            else if (PrefabMapAddMethod is not null)
            {
                PrefabMapAddMethod.Invoke(map, new object[] { fishingSpot, new Entity { Index = 10, Version = 1 } });
            }
            else
            {
                throw new InvalidOperationException("Unsupported prefab map implementation.");
            }
        }

        public void Dispose()
        {
            RestoreLocalization();
            RestorePrefabMap();
            PrefabMapField.SetValue(null, originalPrefabMap);
            persistenceScope.Dispose();
            dataScope.Dispose();
            active = null;
        }

        void RestorePrefabMap()
        {
            if (prefabMapBaseline.Count == 0)
            {
                return;
            }

            object? map = PrefabMapField.GetValue(null) ?? originalPrefabMap;
            if (map is null)
            {
                return;
            }

            foreach (var (prefab, baseline) in prefabMapBaseline)
            {
                PrefabMapRemoveMethod?.Invoke(map, new object[] { prefab });

                if (!baseline.HasValue)
                {
                    continue;
                }

                if (PrefabMapIndexer is not null)
                {
                    PrefabMapIndexer.SetValue(map, baseline.Value, new object[] { prefab });
                }
                else if (PrefabMapAddMethod is not null)
                {
                    PrefabMapAddMethod.Invoke(map, new object[] { prefab, baseline.Value });
                }
            }

            prefabMapBaseline.Clear();
        }

        void RestoreLocalization()
        {
            if (localizationBaseline.Count == 0)
            {
                return;
            }

            var map = (IDictionary<PrefabGUID, string>)PrefabGuidNamesField.GetValue(null)!;
            foreach (var (prefab, value) in localizationBaseline)
            {
                if (value is null)
                {
                    map.Remove(prefab);
                }
                else
                {
                    map[prefab] = value;
                }
            }

            localizationBaseline.Clear();
        }

        static void InstallPatches()
        {
            HarmonyInstance.Patch(
                AccessTools.Method(typeof(ProfessionMappings), nameof(ProfessionMappings.GetFishingAreaDrops)),
                prefix: new HarmonyMethod(typeof(ProfessionBonusHarness), nameof(GetFishingDrops)));

            HarmonyInstance.Patch(
                AccessTools.Method(typeof(ServerGameManager), nameof(ServerGameManager.TryAddInventoryItem), new[] { typeof(Entity), typeof(PrefabGUID), typeof(int) }),
                prefix: new HarmonyMethod(typeof(ProfessionBonusHarness), nameof(TryAddInventoryPrefix)));

            HarmonyInstance.Patch(
                AccessTools.Method(typeof(InventoryUtilitiesServer), nameof(InventoryUtilitiesServer.CreateDropItem), new[] { typeof(EntityManager), typeof(Entity), typeof(PrefabGUID), typeof(int), typeof(Entity) }),
                prefix: new HarmonyMethod(typeof(ProfessionBonusHarness), nameof(CreateDropItemPrefix)));

            HarmonyInstance.Patch(
                AccessTools.Method(typeof(LocalizationService), nameof(LocalizationService.HandleServerReply)),
                prefix: new HarmonyMethod(typeof(ProfessionBonusHarness), nameof(HandleServerReplyPrefix)));

            HarmonyInstance.Patch(
                AccessTools.Method(typeof(ProfessionSystem), "HandleBonusYieldScrollingText", new[]
                {
                    typeof(Entity),
                    typeof(PrefabGUID),
                    typeof(AssetGuid),
                    typeof(Entity),
                    typeof(Entity),
                    typeof(float3),
                    typeof(float),
                    typeof(float).MakeByRefType()
                }),
                prefix: new HarmonyMethod(typeof(ProfessionBonusHarness), nameof(BonusYieldSctPrefix)));

            HarmonyInstance.Patch(
                AccessTools.Method(typeof(ProfessionSystem), "HandleExperienceAndBonusYield"),
                postfix: new HarmonyMethod(typeof(ProfessionBonusHarness), nameof(ExperiencePostfix)));
        }

        static bool GetFishingDrops(ref List<PrefabGUID> __result)
        {
            if (active is not { FishingDrops: { } drops })
            {
                return true;
            }

            __result = drops.ToList();
            return false;
        }

        static bool TryAddInventoryPrefix(ServerGameManager __instance, Entity character, PrefabGUID itemPrefabGuid, int amount, ref bool __result)
        {
            if (active is null)
            {
                return true;
            }

            Func<PrefabGUID, int, bool> tryAdd = active.TryAddInventory ?? ((_, _) => true);
            active.InventoryAttempts.Add((itemPrefabGuid, amount));
            bool success = tryAdd(itemPrefabGuid, amount);
            if (success)
            {
                active.AddedItems.Add((itemPrefabGuid, amount));
            }

            __result = success;
            return false;
        }

        static bool CreateDropItemPrefix(EntityManager entityManager, Entity owner, PrefabGUID prefabGuid, int quantity, Entity dropTarget)
        {
            if (active is null)
            {
                return true;
            }

            active.DroppedItems.Add((prefabGuid, quantity));
            return false;
        }

        static bool HandleServerReplyPrefix(EntityManager entityManager, User user, string message)
        {
            if (active is null)
            {
                return true;
            }

            active.LocalizationMessages.Add(message);
            return false;
        }

        static bool BonusYieldSctPrefix(Entity target, PrefabGUID sctPrefabGuid, AssetGuid assetGuid, Entity playerCharacter, Entity userEntity, float3 color, float bonusYield, ref float delay)
        {
            if (active is null)
            {
                return true;
            }

            active.BonusYieldSctEvents.Add(bonusYield);
            delay += 1f;
            return false;
        }

        static void ExperiencePostfix(User user, Entity userEntity, Entity playerCharacter, Entity target, PrefabGUID resource, string professionName, float bonusYield, bool professionLogging, bool sctYield, ref float delay)
        {
            if (active is null)
            {
                return;
            }

            active.ExperienceEvents.Add((resource, bonusYield));
        }
    }
}
