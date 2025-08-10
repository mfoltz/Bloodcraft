using Bloodcraft.Services;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class JewelSpawnSystemPatch
{
    static SystemService SystemService => Core.SystemService;
    static SpellModSyncSystem_Server SpellModSyncSystemServer => SystemService.SpellModSyncSystem_Server;

    static readonly System.Random _random = new();

    const float POWER_MIN = 0.25f;

    static readonly bool _extraRecipes = ConfigService.ExtraRecipes;

    static readonly PrefabGUID _gemCuttingTable = new(-21483617);
    static readonly PrefabGUID _advancedGrinder = new(-178579946);

    static readonly List<PrefabGUID> _jewelTemplates =
    [
        new(1412786604),  // Item_Jewel_Unholy_T04
        new(2023809276),  // Item_Jewel_Storm_T04
        new(97169184),    // Item_Jewel_Illusion_T04
        new(-147757377),  // Item_Jewel_Frost_T04
        new(-1796954295), // Item_Jewel_Chaos_T04
        new(271061481)    // Item_Jewel_Blood_T04
    ];

    [HarmonyPatch(typeof(JewelSpawnSystem), nameof(JewelSpawnSystem.OnUpdate))] // KillingTorcher's arena mod (DojoKTArena) was very helpful in constructing this patch, namely the jewel command!
    [HarmonyPostfix]
    static void OnUpdatePostfix(JewelSpawnSystem __instance)
    {
        if (!Core.IsReady) return;
        else if (!_extraRecipes) return;

        NativeArray<Entity> entities = __instance._JewelSpawnQuery.ToEntityArray(Allocator.Temp);

        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out PrefabGUID prefabGuid)
                    || !entity.TryGetComponent(out InventoryItem inventoryItem)
                    || !inventoryItem.ContainerEntity.TryGetComponent(out InventoryConnection inventoryConnection)
                    || !inventoryConnection.InventoryOwner.GetPrefabGuid().Equals(_gemCuttingTable)) continue;
                else if (!_jewelTemplates.Contains(prefabGuid)) continue;
                else if (entity.TryGetComponent(out SpellModSetComponent spellModSetComponent)
                    && entity.TryGetComponent(out JewelInstance jewelInstance))
                {
                    PrefabGUID abilityGroup = jewelInstance.OverrideAbilityType;
                    if (abilityGroup.IsEmpty()) abilityGroup = jewelInstance.Ability;

                    if (!Jewels.TryGetSpellMods(abilityGroup, out var spellMods))
                    {
                        Core.Log.LogWarning($"JewelSpawnSystemPatch: No spell mods found for ability group! ({abilityGroup})");
                        continue;
                    }

                    List<PrefabGUID> usedSpellMods = [];
                    SpellModSet spellModSet = spellModSetComponent.SpellMods;

                    if (Enumerable.Contains(spellMods, spellModSet.Mod0.Id)) usedSpellMods.Add(spellModSet.Mod0.Id);
                    if (Enumerable.Contains(spellMods, spellModSet.Mod1.Id)) usedSpellMods.Add(spellModSet.Mod1.Id);
                    if (Enumerable.Contains(spellMods, spellModSet.Mod2.Id)) usedSpellMods.Add(spellModSet.Mod2.Id);
                    if (Enumerable.Contains(spellMods, spellModSet.Mod3.Id)) usedSpellMods.Add(spellModSet.Mod3.Id);
                    if (Enumerable.Contains(spellMods, spellModSet.Mod4.Id)) usedSpellMods.Add(spellModSet.Mod4.Id);
                    if (Enumerable.Contains(spellMods, spellModSet.Mod5.Id)) usedSpellMods.Add(spellModSet.Mod5.Id);
                    if (Enumerable.Contains(spellMods, spellModSet.Mod6.Id)) usedSpellMods.Add(spellModSet.Mod6.Id);
                    if (Enumerable.Contains(spellMods, spellModSet.Mod7.Id)) usedSpellMods.Add(spellModSet.Mod7.Id);

                    List<PrefabGUID> unusedSpellMods = [..spellMods.Except(usedSpellMods)];
                    unusedSpellMods.Shuffle();

                    int availableSlots = 8 - spellModSet.Count;
                    List<PrefabGUID> newSpellMods = [..unusedSpellMods.Take(availableSlots)];

                    int assignedMods = 0;
                    for (int i = 4; i < 8 && assignedMods < newSpellMods.Count; i++)
                    {
                        PrefabGUID spellModPrefabGUID = newSpellMods[assignedMods];
                        float powerValue = GetRandomPower();
                        // float powerValue = Mathf.Clamp((float)_random.NextDouble(), 0.5f, 1f);

                        switch (i)
                        {
                            case 4:
                                if (spellModSet.Mod4.Id.IsEmpty())
                                {
                                    spellModSet.Mod4.Id = spellModPrefabGUID;
                                    spellModSet.Mod4.Power = powerValue;
                                    assignedMods++;
                                }
                                break;
                            case 5:
                                if (spellModSet.Mod5.Id.IsEmpty())
                                {
                                    spellModSet.Mod5.Id = spellModPrefabGUID;
                                    spellModSet.Mod5.Power = powerValue;
                                    assignedMods++;
                                }
                                break;
                            case 6:
                                if (spellModSet.Mod6.Id.IsEmpty())
                                {
                                    spellModSet.Mod6.Id = spellModPrefabGUID;
                                    spellModSet.Mod6.Power = powerValue;
                                    assignedMods++;
                                }
                                break;
                            case 7:
                                if (spellModSet.Mod7.Id.IsEmpty())
                                {
                                    spellModSet.Mod7.Id = spellModPrefabGUID;
                                    spellModSet.Mod7.Power = powerValue;
                                    assignedMods++;
                                }
                                break;
                        }
                    }

                    spellModSet.Count += (byte)newSpellMods.Count;
                    if (spellModSet.Count > 8) spellModSet.Count = 8;

                    SpellModSyncSystemServer.AddSpellMod(ref spellModSet);
                    spellModSetComponent.SpellMods = spellModSet;
                    entity.Write(spellModSetComponent);

                    SpellModSyncSystemServer.AddSpellMod(ref spellModSet);
                }
            }
        }
        finally
        {
            entities.Dispose();
        }

        /*
        entities = __instance._LegendaryItemSpawnQuery.ToEntityArray(Allocator.Temp);

        try
        {
            foreach (Entity entity in entities)
            {
                PrefabGUID prefabGUID = entity.GetPrefabGuid();

                if (entity.TryGetComponent(out LegendaryItemSpellModSetComponent legendaryItemSpellModSet))
                {
                    List<PrefabGUID> statMods = [..StatMods.Values];
                    List<PrefabGUID> usedStatMods = [];

                    SpellModSet statModSet = legendaryItemSpellModSet.StatMods;
                    SpellModSet spellModSet = legendaryItemSpellModSet.AbilityMods0;

                    if (statMods.Contains(statModSet.Mod0.Id)) usedStatMods.Add(statModSet.Mod0.Id);
                    if (statMods.Contains(statModSet.Mod1.Id)) usedStatMods.Add(statModSet.Mod1.Id);
                    if (statMods.Contains(statModSet.Mod2.Id)) usedStatMods.Add(statModSet.Mod2.Id);
                    if (statMods.Contains(statModSet.Mod3.Id)) usedStatMods.Add(statModSet.Mod3.Id);
                    if (statMods.Contains(statModSet.Mod4.Id)) usedStatMods.Add(statModSet.Mod4.Id);
                    if (statMods.Contains(statModSet.Mod5.Id)) usedStatMods.Add(statModSet.Mod5.Id);
                    if (statMods.Contains(statModSet.Mod6.Id)) usedStatMods.Add(statModSet.Mod6.Id);
                    if (statMods.Contains(statModSet.Mod7.Id)) usedStatMods.Add(statModSet.Mod7.Id);

                    List<PrefabGUID> unusedStatMods = [..statMods.Except(usedStatMods)];
                    unusedStatMods.Shuffle();

                    int availableSlots = 8 - statModSet.Count;
                    List<PrefabGUID> newStatMods = [..unusedStatMods.Take(availableSlots)];

                    int assignedMods = 0;
                    for (int i = 4; i < 8 && assignedMods < newStatMods.Count; i++)
                    {
                        PrefabGUID statModPrefabGuid = newStatMods[assignedMods];
                        // float powerValue = (float)_random.NextDouble();
                        float powerValue = Mathf.Clamp((float)_random.NextDouble(), 0.5f, 1f);

                        switch (i)
                        {
                            case 4:
                                if (statModSet.Mod4.Id.IsEmpty())
                                {
                                    statModSet.Mod4.Id = statModPrefabGuid;
                                    statModSet.Mod4.Power = powerValue;
                                    assignedMods++;
                                }
                                break;
                            case 5:
                                if (statModSet.Mod5.Id.IsEmpty())
                                {
                                    statModSet.Mod5.Id = statModPrefabGuid;
                                    statModSet.Mod5.Power = powerValue;
                                    assignedMods++;
                                }
                                break;
                            case 6:
                                if (statModSet.Mod6.Id.IsEmpty())
                                {
                                    statModSet.Mod6.Id = statModPrefabGuid;
                                    statModSet.Mod6.Power = powerValue;
                                    assignedMods++;
                                }
                                break;
                            case 7:
                                if (statModSet.Mod7.Id.IsEmpty())
                                {
                                    statModSet.Mod7.Id = statModPrefabGuid;
                                    statModSet.Mod7.Power = powerValue;
                                    assignedMods++;
                                }
                                break;
                        }
                    }

                    statModSet.Count += (byte)newStatMods.Count;
                    if (statModSet.Count > 8) statModSet.Count = 8;

                    SpellModSyncSystemServer.AddSpellMod(ref statModSet);
                    legendaryItemSpellModSet.StatMods = statModSet;

                    entity.Write(legendaryItemSpellModSet);

                    LegendaryItemSpellModSetComponent spellModSetComponent = entity.Read<LegendaryItemSpellModSetComponent>();

                    HandleSpellModSet(entity, ref spellModSetComponent, ref spellModSetComponent.StatMods, [..StatMods.Values], 0.5f);
                    HandleSpellModSet(entity, ref spellModSetComponent, ref spellModSetComponent.AbilityMods0, [..Utilities.Misc.SpellSchoolInfusionMap.SpellSchoolInfusions.Values], 1f);
                    HandleSpellModSet(entity, ref spellModSetComponent, ref spellModSetComponent.AbilityMods1, [..Utilities.Misc.SpellSchoolInfusionMap.SpellSchoolInfusions.Values], 1f);

                    __instance.InitializeLegendaryItemData(entity);
                    SpellModSyncSystemServer.OnUpdate();
                }
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogInfo(ex.ToString());
        }
        finally
        {
            entities.Dispose();
        }
        */
    }
    static void HandleSpellModSet(Entity entity, ref LegendaryItemSpellModSetComponent spellModSetComponent, ref SpellModSet spellModSet, List<PrefabGUID> setMods, float powerMin = 1f)
    {
        List<PrefabGUID> usedMods = [];

        if (setMods.Contains(spellModSet.Mod0.Id)) setMods.Add(spellModSet.Mod0.Id);
        if (setMods.Contains(spellModSet.Mod1.Id)) setMods.Add(spellModSet.Mod1.Id);
        if (setMods.Contains(spellModSet.Mod2.Id)) setMods.Add(spellModSet.Mod2.Id);
        if (setMods.Contains(spellModSet.Mod3.Id)) setMods.Add(spellModSet.Mod3.Id);
        if (setMods.Contains(spellModSet.Mod4.Id)) setMods.Add(spellModSet.Mod4.Id);
        if (setMods.Contains(spellModSet.Mod5.Id)) setMods.Add(spellModSet.Mod5.Id);
        if (setMods.Contains(spellModSet.Mod6.Id)) setMods.Add(spellModSet.Mod6.Id);
        if (setMods.Contains(spellModSet.Mod7.Id)) setMods.Add(spellModSet.Mod7.Id);

        List<PrefabGUID> unusedMods = [..setMods.Except(usedMods)];
        unusedMods.Shuffle();

        int availableSlots = 8 - spellModSet.Count;
        List<PrefabGUID> newMods = [..unusedMods.Take(availableSlots)];

        int assignedMods = 0;
        for (int i = 4; i < 8 && assignedMods < newMods.Count; i++)
        {
            PrefabGUID modPrefabGuid = newMods[assignedMods];
            float powerValue = Mathf.Clamp((float)_random.NextDouble(), powerMin, 1f);

            switch (i)
            {
                case 4:
                    if (spellModSet.Mod4.Id.IsEmpty())
                    {
                        spellModSet.Mod4.Id = modPrefabGuid;
                        spellModSet.Mod4.Power = powerValue;
                        assignedMods++;
                    }
                    break;
                case 5:
                    if (spellModSet.Mod5.Id.IsEmpty())
                    {
                        spellModSet.Mod5.Id = modPrefabGuid;
                        spellModSet.Mod5.Power = powerValue;
                        assignedMods++;
                    }
                    break;
                case 6:
                    if (spellModSet.Mod6.Id.IsEmpty())
                    {
                        spellModSet.Mod6.Id = modPrefabGuid;
                        spellModSet.Mod6.Power = powerValue;
                        assignedMods++;
                    }
                    break;
                case 7:
                    if (spellModSet.Mod7.Id.IsEmpty())
                    {
                        spellModSet.Mod7.Id = modPrefabGuid;
                        spellModSet.Mod7.Power = powerValue;
                        assignedMods++;
                    }
                    break;
            }
        }

        spellModSet.Count += (byte)newMods.Count;
        if (spellModSet.Count > 8) spellModSet.Count = 8;

        SpellModSyncSystemServer.AddSpellMod(ref spellModSet);
        spellModSetComponent.StatMods = spellModSet;

        entity.Write(spellModSetComponent);
    }
    static float GetRandomPower()
    {
        return POWER_MIN + (float)_random.NextDouble() * POWER_MIN;
    }
}
