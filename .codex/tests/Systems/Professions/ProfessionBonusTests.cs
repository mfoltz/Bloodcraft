using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Xunit;

namespace Bloodcraft.Tests.Systems.Professions;

public sealed class ProfessionBonusTests
{
    [Fact]
    public void GiveProfessionBonus_AddsFishAndLogsWhenInventoryAccepts()
    {
        var harness = ProfessionBonusHarness.Create();
        harness.ProfessionLogging = true;
        harness.SctYield = true;

        const int fishingSpot = 11223344;
        const int fish = 99887766;

        harness.FishingDropsProvider = _ => new List<int> { fish };
        harness.TryAddInventory = (item, _) => true;

        harness.RunFishingBonus(fishingSpot, new Entity { Index = 1, Version = 1 }, steamId: 76561198000123456UL, level: 40);

        Assert.Contains((fish, 2), harness.AddedItems);
        Assert.Empty(harness.DroppedItems);

        int grease = harness.InventoryAttempts.Single(entry => entry.Item != fish).Item;
        Assert.Contains(harness.AddedItems, entry => entry.Item == grease && entry.Amount == 10);

        Assert.Contains(harness.ExperienceEvents, entry => entry.Resource == fish && Math.Abs(entry.Amount - 2f) < 0.001f);
        Assert.Contains(harness.BonusYieldSctEvents, amount => Math.Abs(amount - 2f) < 0.001f);
        Assert.Contains(harness.BonusYieldSctEvents, amount => Math.Abs(amount - 10f) < 0.001f);
    }

    [Fact]
    public void GiveProfessionBonus_DropsFishAndGreaseWhenInventoryIsFull()
    {
        var harness = ProfessionBonusHarness.Create();
        harness.ProfessionLogging = true;
        harness.SctYield = true;

        const int fishingSpot = 55667788;
        const int fish = 88776655;

        harness.FishingDropsProvider = _ => new List<int> { fish };
        harness.TryAddInventory = (_, _) => false;

        harness.RunFishingBonus(fishingSpot, new Entity { Index = 2, Version = 1 }, steamId: 76561198000987654UL, level: 24);

        Assert.DoesNotContain(harness.AddedItems, entry => entry.Item == fish);
        Assert.Contains(harness.DroppedItems, entry => entry.Item == fish && entry.Amount == 1);

        int grease = harness.InventoryAttempts.Single(entry => entry.Item != fish).Item;
        Assert.Contains(harness.DroppedItems, entry => entry.Item == grease && entry.Amount == 6);

        Assert.Contains(harness.ExperienceEvents, entry => entry.Resource == fish && Math.Abs(entry.Amount - 1f) < 0.001f);
        Assert.Contains(harness.BonusYieldSctEvents, amount => Math.Abs(amount - 1f) < 0.001f);
        Assert.Contains(harness.BonusYieldSctEvents, amount => Math.Abs(amount - 6f) < 0.001f);
    }

    sealed class ProfessionBonusHarness
    {
        const int FishStep = 20;
        const int GreaseStep = 4;
        const int MutantGreaseId = unchecked((int)0xDEADBEEF);

        ProfessionBonusHarness()
        {
            CreateDrop = (item, amount) => DroppedItems.Add((item, amount));
        }

        public static ProfessionBonusHarness Create() => new();

        public bool ProfessionLogging { get; set; }
        public bool SctYield { get; set; }
        public Func<int, IReadOnlyList<int>>? FishingDropsProvider { get; set; }
        public Func<int, int, bool>? TryAddInventory { get; set; }
        public Action<int, int>? CreateDrop { get; set; }

        public List<(int Item, int Amount)> InventoryAttempts { get; } = new();
        public List<(int Item, int Amount)> AddedItems { get; } = new();
        public List<(int Item, int Amount)> DroppedItems { get; } = new();
        public List<(int Resource, float Amount)> ExperienceEvents { get; } = new();
        public List<float> BonusYieldSctEvents { get; } = new();

        public void RunFishingBonus(int areaId, Entity playerCharacter, ulong steamId, int level)
        {
            IReadOnlyList<int> fishDrops = FishingDropsProvider?.Invoke(areaId)
                ?? throw new InvalidOperationException("Fishing drops provider must be configured for the test harness.");

            int fish = fishDrops.First();
            int bonusYield = level / FishStep;
            int mutantGrease = level / GreaseStep;

            if (bonusYield <= 0 && mutantGrease <= 0)
            {
                return;
            }

            if (bonusYield > 0)
            {
                bool success = TryAdd(fish, bonusYield);
                if (!success)
                {
                    CreateDrop?.Invoke(fish, bonusYield);
                }

                HandleBonusYield(fish, bonusYield);
                HandleMutantGrease(mutantGrease);
            }
            else
            {
                HandleMutantGrease(mutantGrease);
            }
        }

        bool TryAdd(int item, int amount)
        {
            bool success = TryAddInventory?.Invoke(item, amount) ?? true;
            InventoryAttempts.Add((item, amount));

            if (success)
            {
                AddedItems.Add((item, amount));
            }

            return success;
        }

        void HandleBonusYield(int resource, float amount)
        {
            if (ProfessionLogging)
            {
                ExperienceEvents.Add((resource, amount));
            }

            if (SctYield)
            {
                BonusYieldSctEvents.Add(amount);
            }
        }

        void HandleMutantGrease(int amount)
        {
            bool success = TryAdd(MutantGreaseId, amount);

            if (!success)
            {
                CreateDrop?.Invoke(MutantGreaseId, amount);
            }

            if (SctYield)
            {
                BonusYieldSctEvents.Add(amount);
            }
        }
    }
}
