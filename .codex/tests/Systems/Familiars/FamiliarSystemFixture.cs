using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Services;
using Bloodcraft.Resources;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;
using FamiliarsUtilities = Bloodcraft.Utilities.Familiars;

namespace Bloodcraft.Tests.Systems.Familiars;

public sealed class FamiliarSystemFixture : IDisposable
{
    const string HarmonyId = "Bloodcraft.Tests.Systems.Familiars.FamiliarSystemFixture";

    static readonly ThreadLocal<List<InvocationRecord<ModifyUnitStatsCall>>> modifyUnitStatsCalls = new(() => new List<InvocationRecord<ModifyUnitStatsCall>>());
    static readonly ThreadLocal<List<InvocationRecord<ModifyBloodSourceCall>>> modifyBloodSourceCalls = new(() => new List<InvocationRecord<ModifyBloodSourceCall>>());
    static readonly ThreadLocal<List<InvocationRecord<RefreshStatsCall>>> refreshStatsCalls = new(() => new List<InvocationRecord<RefreshStatsCall>>());

    static readonly Harmony HarmonyInstance = new(HarmonyId);
    static bool isPatched;

    public FamiliarSystemFixture()
    {
        EnsurePatched();
        ClearLogs();
    }

    public static IReadOnlyList<InvocationRecord<ModifyUnitStatsCall>> ModifyUnitStatsCalls => GetModifyUnitStatsLog();

    public static IReadOnlyList<InvocationRecord<ModifyBloodSourceCall>> ModifyBloodSourceCalls => GetModifyBloodSourceLog();

    public static IReadOnlyList<InvocationRecord<RefreshStatsCall>> RefreshStatsCalls => GetRefreshStatsLog();

    public static void ClearLogs()
    {
        GetModifyUnitStatsLog().Clear();
        GetModifyBloodSourceLog().Clear();
        GetRefreshStatsLog().Clear();
    }

    void EnsurePatched()
    {
        if (isPatched)
        {
            return;
        }

        PatchTypeInitializer(typeof(FamiliarBindingSystem), nameof(FamiliarBindingSystemTypeInitializerPrefix));
        PatchTypeInitializer(typeof(Buffs), nameof(BuffsTypeInitializerPrefix));
        PatchTypeInitializer(typeof(FamiliarsUtilities), nameof(FamiliarsTypeInitializerPrefix));
        PatchTypeInitializer(typeof(FamiliarLevelingSystem), nameof(FamiliarLevelingSystemTypeInitializerPrefix));

        Patch(
            AccessTools.Method(typeof(FamiliarBindingSystem), nameof(FamiliarBindingSystem.ModifyUnitStats)),
            AccessTools.Method(typeof(FamiliarSystemFixture), nameof(LogModifyUnitStatsPrefix)),
            AccessTools.Method(typeof(FamiliarSystemFixture), nameof(LogModifyUnitStatsPostfix)));

        Patch(
            AccessTools.Method(typeof(FamiliarBindingSystem), nameof(FamiliarBindingSystem.ModifyBloodSource)),
            AccessTools.Method(typeof(FamiliarSystemFixture), nameof(LogModifyBloodSourcePrefix)),
            AccessTools.Method(typeof(FamiliarSystemFixture), nameof(LogModifyBloodSourcePostfix)));

        Patch(
            AccessTools.Method(typeof(Buffs), nameof(Buffs.RefreshStats)),
            AccessTools.Method(typeof(FamiliarSystemFixture), nameof(LogRefreshStatsPrefix)),
            AccessTools.Method(typeof(FamiliarSystemFixture), nameof(LogRefreshStatsPostfix)));

        isPatched = true;
    }

    static void Patch(MethodBase? target, MethodInfo prefix, MethodInfo postfix)
    {
        if (target == null)
        {
            throw new InvalidOperationException("Target method for Harmony patch could not be located.");
        }

        HarmonyInstance.Patch(target, prefix: new HarmonyMethod(prefix), postfix: new HarmonyMethod(postfix));
    }

    static void PatchTypeInitializer(Type type, string prefixName)
    {
        ConstructorInfo? typeInitializer = type.TypeInitializer;
        if (typeInitializer == null)
        {
            return;
        }

        MethodInfo? prefix = typeof(FamiliarSystemFixture).GetMethod(prefixName, BindingFlags.Static | BindingFlags.NonPublic);
        if (prefix == null)
        {
            throw new InvalidOperationException($"Failed to locate type initializer prefix '{prefixName}'.");
        }

        HarmonyInstance.Patch(typeInitializer, prefix: new HarmonyMethod(prefix));
    }

    public void Dispose()
    {
        if (isPatched)
        {
            HarmonyInstance.UnpatchSelf();
            isPatched = false;
        }

        ClearLogs();
    }

    static bool LogModifyUnitStatsPrefix(Entity familiar, int level, ulong steamId, int famKey, bool battle)
    {
        GetModifyUnitStatsLog().Add(new InvocationRecord<ModifyUnitStatsCall>(InvocationStage.Prefix, new ModifyUnitStatsCall(familiar, level, steamId, famKey, battle)));
        return false;
    }

    static void LogModifyUnitStatsPostfix(Entity familiar, int level, ulong steamId, int famKey, bool battle)
    {
        GetModifyUnitStatsLog().Add(new InvocationRecord<ModifyUnitStatsCall>(InvocationStage.Postfix, new ModifyUnitStatsCall(familiar, level, steamId, famKey, battle)));
    }

    static bool LogModifyBloodSourcePrefix(Entity familiar, int level)
    {
        GetModifyBloodSourceLog().Add(new InvocationRecord<ModifyBloodSourceCall>(InvocationStage.Prefix, new ModifyBloodSourceCall(familiar, level)));
        return false;
    }

    static void LogModifyBloodSourcePostfix(Entity familiar, int level)
    {
        GetModifyBloodSourceLog().Add(new InvocationRecord<ModifyBloodSourceCall>(InvocationStage.Postfix, new ModifyBloodSourceCall(familiar, level)));
    }

    static bool LogRefreshStatsPrefix(Entity entity)
    {
        GetRefreshStatsLog().Add(new InvocationRecord<RefreshStatsCall>(InvocationStage.Prefix, new RefreshStatsCall(entity)));
        return false;
    }

    static void LogRefreshStatsPostfix(Entity entity)
    {
        GetRefreshStatsLog().Add(new InvocationRecord<RefreshStatsCall>(InvocationStage.Postfix, new RefreshStatsCall(entity)));
    }

    static List<InvocationRecord<ModifyUnitStatsCall>> GetModifyUnitStatsLog()
    {
        return EnsureValue(modifyUnitStatsCalls);
    }

    static List<InvocationRecord<ModifyBloodSourceCall>> GetModifyBloodSourceLog()
    {
        return EnsureValue(modifyBloodSourceCalls);
    }

    static List<InvocationRecord<RefreshStatsCall>> GetRefreshStatsLog()
    {
        return EnsureValue(refreshStatsCalls);
    }

    static List<T> EnsureValue<T>(ThreadLocal<List<T>> storage)
    {
        var value = storage.Value;
        if (value == null)
        {
            value = new List<T>();
            storage.Value = value;
        }

        return value;
    }

    public readonly record struct ModifyUnitStatsCall(Entity Familiar, int Level, ulong SteamId, int FamiliarKey, bool IsBattle);

    public readonly record struct ModifyBloodSourceCall(Entity Familiar, int Level);

    public readonly record struct RefreshStatsCall(Entity Familiar);

    public readonly record struct InvocationRecord<T>(InvocationStage Stage, T Payload);

    public enum InvocationStage
    {
        Prefix,
        Postfix
    }

    static bool FamiliarBindingSystemTypeInitializerPrefix()
    {
        SetStaticField(typeof(FamiliarBindingSystem), "_familiarCombat", ConfigService.FamiliarCombat);
        SetStaticField(typeof(FamiliarBindingSystem), "_familiarPrestige", ConfigService.FamiliarPrestige);
        SetStaticField(typeof(FamiliarBindingSystem), "<EquipmentOnly>k__BackingField", ConfigService.EquipmentOnly);
        SetStaticField(typeof(FamiliarBindingSystem), "_maxFamiliarLevel", ConfigService.MaxFamiliarLevel);
        SetStaticField(typeof(FamiliarBindingSystem), "_familiarPrestigeStatMultiplier", ConfigService.FamiliarPrestigeStatMultiplier);

        SetStaticField(typeof(FamiliarBindingSystem), "PlayerBattleGroups", new ConcurrentDictionary<ulong, List<PrefabGUID>>());
        SetStaticField(typeof(FamiliarBindingSystem), "PlayerBattleFamiliars", new ConcurrentDictionary<ulong, List<Entity>>());

        return false;
    }

    static bool BuffsTypeInitializerPrefix()
    {
        SetStaticField(typeof(Buffs), "_buffMaxStacks", new Dictionary<PrefabGUID, int>());
        return false;
    }

    static bool FamiliarsTypeInitializerPrefix()
    {
        SetStaticField(typeof(FamiliarsUtilities), "_familiarCombat", ConfigService.FamiliarCombat);

        var equipmentMap = new Dictionary<FamiliarsUtilities.FamiliarEquipmentType, EquipmentType>
        {
            [FamiliarsUtilities.FamiliarEquipmentType.Chest] = EquipmentType.Chest,
            [FamiliarsUtilities.FamiliarEquipmentType.Weapon] = EquipmentType.Weapon,
            [FamiliarsUtilities.FamiliarEquipmentType.MagicSource] = EquipmentType.MagicSource,
            [FamiliarsUtilities.FamiliarEquipmentType.Footgear] = EquipmentType.Footgear,
            [FamiliarsUtilities.FamiliarEquipmentType.Legs] = EquipmentType.Legs,
            [FamiliarsUtilities.FamiliarEquipmentType.Gloves] = EquipmentType.Gloves
        };
        SetStaticField(typeof(FamiliarsUtilities), "_familiarEquipmentMap", equipmentMap);

        var vBloodMap = new Dictionary<string, PrefabGUID>
        {
            { "Mairwyn the Elementalist", new(-2013903325) },
            { "Clive the Firestarter", new(1896428751) },
            { "Rufus the Foreman", new(2122229952) },
            { "Grayson the Armourer", new(1106149033) },
            { "Errol the Stonebreaker", new(-2025101517) },
            { "Quincey the Bandit King", new(-1659822956) },
            { "Lord Styx the Night Champion", new(1112948824) },
            { "Gorecrusher the Behemoth", new(-1936575244) },
            { "Albert the Duke of Balaton", new(-203043163) },
            { "Matka the Curse Weaver", new(-910296704) },
            { "Alpha the White Wolf", new(-1905691330) },
            { "Terah the Geomancer", new(-1065970933) },
            { "Morian the Stormwing Matriarch", new(685266977) },
            { "Talzur the Winged Horror", new(-393555055) },
            { "Raziel the Shepherd", new(-680831417) },
            { "Vincent the Frostbringer", new(-29797003) },
            { "Octavian the Militia Captain", new(1688478381) },
            { "Meredith the Bright Archer", new(850622034) },
            { "Ungora the Spider Queen", new(-548489519) },
            { "Goreswine the Ravager", new(577478542) },
            { "Leandra the Shadow Priestess", new(939467639) },
            { "Cyril the Cursed Smith", new(326378955) },
            { "Bane the Shadowblade", new(613251918) },
            { "Kriig the Undead General", new(-1365931036) },
            { "Nicholaus the Fallen", new(153390636) },
            { "Foulrot the Soultaker", new(-1208888966) },
            { "Putrid Rat", new(-2039908510) },
            { "Jade the Vampire Hunter", new(-1968372384) },
            { "Tristan the Vampire Hunter", new(-1449631170) },
            { "Ben the Old Wanderer", new(109969450) },
            { "Beatrice the Tailor", new(-1942352521) },
            { "Frostmaw the Mountain Terror", new(24378719) },
            { "Terrorclaw the Ogre", new(-1347412392) },
            { "Keely the Frost Archer", new(1124739990) },
            { "Lidia the Chaos Archer", new(763273073) },
            { "Finn the Fisherman", new(-2122682556) },
            { "Azariel the Sunbringer", new(114912615) },
            { "Sir Magnus the Overseer", new(-26105228) },
            { "Baron du Bouchon the Sommelier", new(192051202) },
            { "Solarus the Immaculate", new(-740796338) },
            { "Kodia the Ferocious Bear", new(-1391546313) },
            { "Ziva the Engineer", new(172235178) },
            { "Adam the Firstborn", new(1233988687) },
            { "Angram the Purifier", new(106480588) },
            { "Voltatia the Power Master", new(2054432370) },
            { "Henry Blackbrew the Doctor", new(814083983) },
            { "Domina the Blade Dancer", new(-1101874342) },
            { "Grethel the Glassblower", new(910988233) },
            { "Christina the Sun Priestess", new(-99012450) },
            { "Maja the Dark Savant", new(1945956671) },
            { "Polora the Feywalker", new(-484556888) },
            { "Simon Belmont the Vampire Hunter", new(336560131) },
            { "General Valencia the Depraved", new(495971434) },
            { "Dracula the Immortal King", new(-327335305) },
            { "General Cassius the Betrayer", new(-496360395) },
            { "General Elena the Hollow", PrefabGUIDs.CHAR_Vampire_IceRanger_VBlood },
            { "Willfred the Village Elder", PrefabGUIDs.CHAR_WerewolfChieftain_Human },
            { "Sir Erwin the Gallant Cavalier", PrefabGUIDs.CHAR_Militia_Fabian_VBlood },
            { "Gaius the Cursed Champion", PrefabGUIDs.CHAR_Undead_ArenaChampion_VBlood },
            { "Stavros the Carver", PrefabGUIDs.CHAR_Blackfang_CarverBoss_VBlood },
            { "Dantos the Forgebinder", PrefabGUIDs.CHAR_Blackfang_Valyr_VBlood },
            { "Lucile the Venom Alchemist", PrefabGUIDs.CHAR_Blackfang_Lucie_VBlood },
            { "Jakira the Shadow Huntress", PrefabGUIDs.CHAR_Blackfang_Livith_VBlood },
            { "Megara the Serpent Queen", PrefabGUIDs.CHAR_Blackfang_Morgana_VBlood }
        };
        SetStaticField(typeof(FamiliarsUtilities), "VBloodNamePrefabGuidMap", vBloodMap);

        SetStaticField(typeof(FamiliarsUtilities), "AutoCallMap", new ConcurrentDictionary<Entity, Entity>());
        SetStaticField(typeof(FamiliarsUtilities.ActiveFamiliarManager), "_familiarActives", new ConcurrentDictionary<ulong, FamiliarsUtilities.ActiveFamiliarData>());

        return false;
    }

    static bool FamiliarLevelingSystemTypeInitializerPrefix()
    {
        SetStaticField(typeof(FamiliarLevelingSystem), "_familiarPrestige", ConfigService.FamiliarPrestige);
        SetStaticField(typeof(FamiliarLevelingSystem), "_unitFamiliarMultiplier", ConfigService.UnitFamiliarMultiplier);
        SetStaticField(typeof(FamiliarLevelingSystem), "_vBloodFamiliarMultiplier", ConfigService.VBloodFamiliarMultiplier);
        SetStaticField(typeof(FamiliarLevelingSystem), "_unitSpawnerMultiplier", ConfigService.UnitSpawnerMultiplier);
        SetStaticField(typeof(FamiliarLevelingSystem), "_levelingPrestigeReducer", ConfigService.LevelingPrestigeReducer);
        SetStaticField(typeof(FamiliarLevelingSystem), "_maxFamiliarLevel", ConfigService.MaxFamiliarLevel);

        return false;
    }

    static void SetStaticField(Type targetType, string fieldName, object? value)
    {
        FieldInfo? field = targetType.GetField(fieldName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (field == null)
        {
            throw new InvalidOperationException($"Field '{fieldName}' was not found on type '{targetType}'.");
        }

        field.SetValue(null, value);
    }
}
