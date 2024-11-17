using Bloodcraft.Services;
using Bloodcraft.Systems.Legacies;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Stunlock.Network;
using Unity.Collections;
using Unity.Entities;
using static Bloodcraft.Services.DataService.FamiliarPersistence;
using static Bloodcraft.Services.PlayerService;
using User = ProjectM.Network.User;
using WeaponType = Bloodcraft.Systems.Expertise.WeaponType;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class ServerBootstrapSystemPatches
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;

    static readonly PrefabGUID InsideWoodenCoffin = new(381160212);
    static readonly PrefabGUID InsideStoneCoffin = new(569692162);

    static readonly bool Classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;

    static readonly Dictionary<string, bool> DefaultBools = new()
    {
        { "ExperienceLogging", false },
        { "QuestLogging", false },
        { "ProfessionLogging", false },
        { "ExpertiseLogging", false },
        { "BloodLogging", false },
        { "FamiliarLogging", false },
        { "SpellLock", false },
        { "ShiftLock", false },
        { "Grouping", false },
        { "Emotes", false },
        { "Binding", false },
        { "Kit", true },
        { "VBloodEmotes", true },
        { "FamiliarVisual", true},
        { "ShinyChoice", false },
        { "Reminders", true },
        { "ScrollingText", true},
        { "ExoForm", false}
    };

    [HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUserConnected))]
    [HarmonyPostfix]
    static void OnUserConnectedPostfix(ServerBootstrapSystem __instance, NetConnectionId netConnectionId)
    {
        int userIndex = __instance._NetEndPointToApprovedUserIndex[netConnectionId];
        ServerBootstrapSystem.ServerClient serverClient = __instance._ApprovedUsersLookup[userIndex];
        Entity userEntity = serverClient.UserEntity;
        User user = __instance.EntityManager.GetComponentData<User>(userEntity);
        ulong steamId = user.PlatformId;

        bool exists = false;
        Entity playerCharacter = user.LocalCharacter.GetEntityOnServer();

        if (playerCharacter.Exists())
        {
            exists = true;
        }

        if (!steamId.TryGetPlayerBools(out var bools))
        {
            steamId.SetPlayerBools(DefaultBools);
        }
        else
        {
            foreach (string key in DefaultBools.Keys)
            {
                if (!bools.ContainsKey(key))
                {
                    bools[key] = DefaultBools[key];
                }
            }

            steamId.SetPlayerBools(bools);
        }

        if (ConfigService.ProfessionSystem)
        {
            if (!steamId.TryGetPlayerWoodcutting(out var woodcutting))
            {
                steamId.SetPlayerWoodcutting(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerMining(out var rmining))
            {
                steamId.SetPlayerMining(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerFishing(out var fishing))
            {
                steamId.SetPlayerFishing(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerBlacksmithing(out var blacksmithing))
            {
                steamId.SetPlayerBlacksmithing(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerTailoring(out var tailoring))
            {
                steamId.SetPlayerTailoring(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerAlchemy(out var alchemy))
            {
                steamId.SetPlayerAlchemy(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerHarvesting(out var harvesting))
            {
                steamId.SetPlayerHarvesting(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerEnchanting(out var enchanting))
            {
                steamId.SetPlayerEnchanting(new KeyValuePair<int, float>(0, 0f));
            }
        }

        if (ConfigService.ExpertiseSystem)
        {
            if (!steamId.TryGetPlayerUnarmedExpertise(out var unarmed))
            {
                steamId.SetPlayerUnarmedExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerSpells(out var spells))
            {
                steamId.SetPlayerSpells((0, 0, 0));
            }

            if (!steamId.TryGetPlayerSwordExpertise(out var sword))
            {
                steamId.SetPlayerSwordExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerAxeExpertise(out var axe))
            {
                steamId.SetPlayerAxeExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerMaceExpertise(out var mace))
            {
                steamId.SetPlayerMaceExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerSpearExpertise(out var spear))
            {
                steamId.SetPlayerSpearExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerCrossbowExpertise(out var crossbow))
            {
                steamId.SetPlayerCrossbowExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerGreatSwordExpertise(out var greatSword))
            {
                steamId.SetPlayerGreatSwordExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerSlashersExpertise(out var slashers))
            {
                steamId.SetPlayerSlashersExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerPistolsExpertise(out var pistols))
            {
                steamId.SetPlayerPistolsExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerReaperExpertise(out var reaper))
            {
                steamId.SetPlayerReaperExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerLongbowExpertise(out var longbow))
            {
                steamId.SetPlayerLongbowExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerWhipExpertise(out var whip))
            {
                steamId.SetPlayerWhipExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerFishingPoleExpertise(out var fishingPole))
            {
                steamId.SetPlayerFishingPoleExpertise(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerWeaponStats(out var weaponStats))
            {
                weaponStats = [];
                foreach (WeaponType weaponType in Enum.GetValues<WeaponType>())
                {
                    weaponStats.Add(weaponType, []);
                }
                steamId.SetPlayerWeaponStats(weaponStats);  // Assuming the weapon stats are a list or similar collection.
            }
            else
            {
                foreach (WeaponType weaponType in Enum.GetValues<WeaponType>())
                {
                    if (!weaponStats.ContainsKey(weaponType)) weaponStats.Add(weaponType, []);
                }
            }
        }

        if (ConfigService.BloodSystem)
        {
            if (!steamId.TryGetPlayerWorkerLegacy(out var worker))
            {
                steamId.SetPlayerWorkerLegacy(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerWarriorLegacy(out var warrior))
            {
                steamId.SetPlayerWarriorLegacy(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerScholarLegacy(out var scholar))
            {
                steamId.SetPlayerScholarLegacy(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerRogueLegacy(out var rogue))
            {
                steamId.SetPlayerRogueLegacy(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerMutantLegacy(out var mutant))
            {
                steamId.SetPlayerMutantLegacy(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerVBloodLegacy(out var vBlood))
            {
                steamId.SetPlayerVBloodLegacy(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerDraculinLegacy(out var draculin))
            {
                steamId.SetPlayerDraculinLegacy(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerImmortalLegacy(out var immortal))
            {
                steamId.SetPlayerImmortalLegacy(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerCreatureLegacy(out var creature))
            {
                steamId.SetPlayerCreatureLegacy(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerBruteLegacy(out var brute))
            {
                steamId.SetPlayerBruteLegacy(new KeyValuePair<int, float>(0, 0f));
            }

            if (!steamId.TryGetPlayerBloodStats(out var bloodStats))
            {
                bloodStats = [];
                foreach (BloodType bloodType in Enum.GetValues<BloodType>())
                {
                    bloodStats.Add(bloodType, []);
                }
                steamId.SetPlayerBloodStats(bloodStats);
            }
            else
            {
                foreach (BloodType bloodType in Enum.GetValues<BloodType>())
                {
                    if (!bloodStats.ContainsKey(bloodType)) bloodStats.Add(bloodType, []);
                }
            }
        }

        if (ConfigService.LevelingSystem)
        {
            if (!steamId.TryGetPlayerExperience(out var experience))
            {
                steamId.SetPlayerExperience(new KeyValuePair<int, float>(ConfigService.StartingLevel, LevelingSystem.ConvertLevelToXp(ConfigService.StartingLevel)));
            }

            if (ConfigService.RestedXPSystem)
            {
                if (!steamId.TryGetPlayerRestedXP(out var restedData))
                {
                    steamId.SetPlayerRestedXP(new KeyValuePair<DateTime, float>(DateTime.UtcNow, 0));
                }
                else if (exists)
                {
                    float restedMultiplier = 0f;

                    if (ServerGameManager.HasBuff(playerCharacter, InsideWoodenCoffin)) restedMultiplier = 0.5f;
                    else if (ServerGameManager.HasBuff(playerCharacter, InsideStoneCoffin)) restedMultiplier = 1f;

                    DateTime lastLogout = restedData.Key;
                    TimeSpan timeOffline = DateTime.UtcNow - lastLogout;

                    if (timeOffline.TotalMinutes >= ConfigService.RestedXPTickRate && restedMultiplier != 0f && experience.Key < ConfigService.MaxLevel)
                    {
                        float currentRestedXP = restedData.Value;

                        int currentLevel = experience.Key;
                        int maxRestedLevel = Math.Min(ConfigService.RestedXPMax + currentLevel, ConfigService.MaxLevel);
                        float restedCap = LevelingSystem.ConvertLevelToXp(maxRestedLevel) - LevelingSystem.ConvertLevelToXp(currentLevel);

                        float earnedPerTick = ConfigService.RestedXPRate * restedCap;
                        float earnedRestedXP = (float)timeOffline.TotalMinutes / ConfigService.RestedXPTickRate * earnedPerTick * restedMultiplier;

                        currentRestedXP = Math.Min(currentRestedXP + earnedRestedXP, restedCap);
                        int roundedXP = (int)(Math.Round(currentRestedXP / 100.0) * 100);

                        //Core.Log.LogInfo($"Rested XP: {currentRestedXP} | Earned: {earnedRestedXP} | Rounded: {roundedXP} | Rested Cap: {restedCap} | Max Rested Level: {maxRestedLevel}");
                        steamId.SetPlayerRestedXP(new KeyValuePair<DateTime, float>(DateTime.UtcNow, currentRestedXP));
                        string message = $"+<color=#FFD700>{roundedXP}</color> <color=green>rested</color> <color=#FFC0CB>experience</color> earned from being logged out in your coffin!";
                        LocalizationService.HandleServerReply(EntityManager, user, message);
                    }
                }
            }

            if (exists) LevelingSystem.SetLevel(playerCharacter);
        }

        if (ConfigService.PrestigeSystem)
        {
            if (!steamId.TryGetPlayerPrestiges(out var prestiges))
            {
                var prestigeDict = new Dictionary<PrestigeType, int>();

                foreach (var prestigeType in Enum.GetValues<PrestigeType>())
                {
                    prestigeDict.Add(prestigeType, 0);
                }

                steamId.SetPlayerPrestiges(prestigeDict);
            }
            else
            {
                foreach (var prestigeType in Enum.GetValues<PrestigeType>())
                {
                    if (!prestiges.ContainsKey(prestigeType)) prestiges.Add(prestigeType, 0);
                }

                if (exists && prestiges.TryGetValue(PrestigeType.Experience, out int experiencePrestiges) && experiencePrestiges > 0)
                {
                    BuffUtilities.SanitizePrestigeBuffs(playerCharacter);
                }

                if (ConfigService.ExoPrestiging && exists && prestiges.TryGetValue(PrestigeType.Experience, out int exoPrestiges) && exoPrestiges > 0)
                {
                    PrestigeSystem.ResetDamageResistCategoryStats(playerCharacter); // undo old exo stuff

                    if (!steamId.TryGetPlayerExoFormData(out var exoFormData))
                    {
                        KeyValuePair<DateTime, float> timeEnergyPair = new(DateTime.UtcNow, ExoFormUtilities.CalculateFormDuration(exoPrestiges));
                        steamId.SetPlayerExoFormData(timeEnergyPair);
                    }
                }

                if (ConfigService.ExoPrestiging && !steamId.TryGetPlayerExoFormData(out var exoFormDataOther))
                {
                    KeyValuePair<DateTime, float> timeEnergyPair = new(DateTime.MaxValue, 0f);
                    steamId.SetPlayerExoFormData(timeEnergyPair);
                }
            }
        }

        if (ConfigService.FamiliarSystem)
        {
            if (!steamId.TryGetFamiliarActives(out var actives))
            {
                steamId.SetFamiliarActives(new(Entity.Null, 0));
            }

            if (!steamId.TryGetFamiliarBox(out var box))
            {
                steamId.SetFamiliarBox("");
            }

            FamiliarExperienceManager.SaveFamiliarExperience(steamId, FamiliarExperienceManager.LoadFamiliarExperience(steamId));
            FamiliarUnlocksManager.SaveUnlockedFamiliars(steamId, FamiliarUnlocksManager.LoadUnlockedFamiliars(steamId));

            if (exists) FamiliarUtilities.ClearBuffers(playerCharacter, steamId);
        }

        if (Classes)
        {
            if (!steamId.TryGetPlayerClasses(out var classes))
            {
                steamId.SetPlayerClasses([]);
            }

            if (!steamId.TryGetPlayerSpells(out var spells))
            {
                steamId.SetPlayerSpells((0, 0, 0));
            }

            if (exists)
            {
                if (ClassUtilities.HasClass(steamId) && playerCharacter.Has<BloodQualityBuff>())
                {
                    PrefabGUID bloodPrefab = playerCharacter.Read<Blood>().BloodType;
                    BloodType bloodType = BloodManager.GetCurrentBloodType(playerCharacter);

                    var bloodQualityBuffBuffer = playerCharacter.ReadBuffer<BloodQualityBuff>();
                    List<PrefabGUID> bloodQualityBuffs = [];

                    if (!bloodQualityBuffBuffer.IsEmpty)
                    {
                        foreach (BloodQualityBuff item in bloodQualityBuffBuffer)
                        {
                            bloodQualityBuffs.Add(item.BloodQualityBuffPrefabGuid);
                        }
                    }

                    LevelingSystem.PlayerClass playerClass = ClassUtilities.GetPlayerClass(steamId);

                    HashSet<PrefabGUID> classBuffsToRemove = [..UpdateBuffsBufferDestroyPatch.ClassBuffs.Where(keyValuePair => keyValuePair.Key != playerClass).SelectMany(keyValuePair => keyValuePair.Value)];
                    //HashSet<PrefabGUID> filteredClassBuffs = classBuffsToRemove.Where(buff => !UpdateBuffsBufferDestroyPatch.PrestigeBuffs.Contains(buff)).ToHashSet();

                    foreach (PrefabGUID classBuff in classBuffsToRemove)
                    {
                        if (ServerGameManager.TryGetBuff(playerCharacter, classBuff.ToIdentifier(), out Entity buffEntity))
                        {
                            if (bloodQualityBuffs.Contains(classBuff)) continue; // after filtering out class buffs the player should have, check against remaining buffs then see if they are supposed to have it based on blood type and destroy it if not?
                            else
                            {
                                //Core.Log.LogInfo($"{user.CharacterName.Value} class is {playerClass.ToString()} with blood type {bloodType.ToString()} and should not have {classBuff.LookupName()}, removing buff...");
                                DestroyUtility.Destroy(EntityManager, buffEntity, DestroyDebugReason.TryRemoveBuff);
                            }
                        }
                    }

                    if (playerCharacter.Has<VBloodAbilityBuffEntry>())
                    {
                        var vBloodAbilityBuffer = playerCharacter.ReadBuffer<VBloodAbilityBuffEntry>();

                        Dictionary<int, Entity> abilityBuffEntities = [];
                        bool firstFound = false;

                        // Traverse the buffer to find the first occurrence of SlotId == 3 and track duplicates
                        for (int i = 0; i < vBloodAbilityBuffer.Length; i++)
                        {
                            VBloodAbilityBuffEntry item = vBloodAbilityBuffer[i];

                            if (item.SlotId == 3)
                            {
                                if (!firstFound)
                                {
                                    firstFound = true; // Mark first occurrence
                                }
                                else
                                {
                                    abilityBuffEntities.Add(i, item.ActiveBuff); // Track duplicates for removal
                                }
                            }
                        }

                        // Reverse iterate and remove duplicates to avoid index shifting issues
                        if (abilityBuffEntities.Count > 0)
                        {
                            // Iterate over the abilityBuffEntities in reverse order of indexes
                            foreach (var entry in abilityBuffEntities.OrderByDescending(e => e.Key))
                            {
                                var index = entry.Key;
                                var entity = entry.Value;

                                if (vBloodAbilityBuffer.IsIndexWithinRange(index))
                                {
                                    //Core.Log.LogInfo($"Removing duplicate VBlood ability buff: {(entity.Has<PrefabGUID>() ? entity.Read<PrefabGUID>().LookupName() : "N/A")} | {entity} | {character}");

                                    vBloodAbilityBuffer.RemoveAt(index);
                                    DestroyUtility.Destroy(EntityManager, entity, DestroyDebugReason.TryRemoveBuff);
                                }
                            }
                        }
                    }
                }

                /*
                if (ServerGameManager.TryGetBuffer<AbilityGroupSlotBuffer>(playerCharacter, out var buffer) && !buffer.IsEmpty)
                {
                    
                    var prefabEntityMap = PrefabCollectionSystem._PrefabGuidToEntityMap;

                    for (int i = 0; i < buffer.Length; i++)
                    {
                        AbilityGroupSlotBuffer abilityGroupSlotBuffer = buffer[i];
                        AbilityGroupSlot
                        if (prefabEntityMap.TryGetValue(abilityGroupSlotBuffer.BaseAbilityGroupOnSlot, out Entity abilityPrefabEntity) && abilityPrefabEntity.Has<VBloodAbilityData>())
                        {
                            abilityGroupSlotBuffer.ShowOnBar = true;
                            buffer[i] = abilityGroupSlotBuffer;
                        }
                    }
                    

                    Dictionary<PrefabGUID, int> abilityGroupCount = [];
                    Dictionary<PrefabGUID, int> spellModSources = [];

                    int emptySpellModSource = 0;

                    foreach (AbilityGroupSlotBuffer abilityGroupSlotBuffer in buffer)
                    {
                        PrefabGUID abilityGroupPrefabGUID = abilityGroupSlotBuffer.BaseAbilityGroupOnSlot;
                        Entity abilityGroupSlotEntity = abilityGroupSlotBuffer.GroupSlotEntity.GetEntityOnServer();

                        if (!abilityGroupCount.ContainsKey(abilityGroupPrefabGUID))
                        {
                            abilityGroupCount.TryAdd(abilityGroupPrefabGUID, 1);

                            if (abilityGroupSlotEntity.Exists()) abilityGroupSlotEntity.LogComponentTypes();
                        }
                        else if (abilityGroupCount.ContainsKey(abilityGroupPrefabGUID))
                        {
                            abilityGroupCount[abilityGroupPrefabGUID]++;

                            if (abilityGroupSlotEntity.Exists()) abilityGroupSlotEntity.LogComponentTypes();
                        }
                        else if (abilityGroupPrefabGUID.IsEmpty() && abilityGroupSlotEntity.Exists()) abilityGroupSlotEntity.LogComponentTypes();

                        if (abilityGroupSlotEntity.TryGetComponent(out AbilityGroupSlot abilityGroupSlot))
                        {
                            Entity spellModsSource = abilityGroupSlot.SpellModsSource._Value;

                            try
                            {
                                if (spellModsSource.TryGetComponent(out PrefabGUID prefabGUID))
                                {
                                    if (!spellModSources.ContainsKey(prefabGUID))
                                    {
                                        spellModSources.TryAdd(prefabGUID, 1);
                                    }
                                    else
                                    {
                                        spellModSources[prefabGUID]++;
                                    }

                                    spellModsSource.LogComponentTypes();
                                }
                                else if (spellModsSource.Exists())
                                {
                                    spellModsSource.LogComponentTypes();
                                }
                                else
                                {
                                    emptySpellModSource++;
                                }
                            }
                            catch (Exception e)
                            {
                                Core.Log.LogError($"Error logging spellModsSource: {e}");
                            }
                        }
                    }

                    Core.Log.LogWarning("----- Ability Group Slot Buffer Analysis -----");

                    Core.Log.LogWarning("AbilityGroup Counts:");
                    foreach (var kvp in abilityGroupCount)
                    {
                        if (kvp.Key.IsEmpty())
                        {
                            Core.Log.LogInfo($"Empty AbilityGroups - {kvp.Key} | Count: {kvp.Value}");
                        }
                        else
                        {
                            Core.Log.LogInfo($"AbilityGroup: {kvp.Key.LookupName()} | Count: {kvp.Value}");
                        }
                    }

                    Core.Log.LogWarning("Spell Mod Sources:");
                    foreach (var kvp in spellModSources)
                    {
                        if (kvp.Key.IsEmpty())
                        {
                            Core.Log.LogInfo($"Empty SpellModSource - {kvp.Key} | Count: {kvp.Value}");
                        }
                        else
                        {
                            Core.Log.LogInfo($"SpellModSource: {kvp.Key.LookupName()} | Count: {kvp.Value}");
                        }
                    }

                    Core.Log.LogWarning("---------------------------------------------");
                }
                */
            }
        }

        if (ConfigService.ClientCompanion && exists)
        {
            PlayerInfo playerInfo = new()
            {
                CharEntity = playerCharacter,
                UserEntity = userEntity,
                User = user
            };

            if (!OnlineCache.ContainsKey(steamId.ToString())) OnlineCache.TryAdd(steamId.ToString(), playerInfo);
        }
    }

    [HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUserDisconnected))]
    [HarmonyPrefix]
    static void OnUserDisconnectedPrefix(ServerBootstrapSystem __instance, NetConnectionId netConnectionId)
    {
        int userIndex = __instance._NetEndPointToApprovedUserIndex[netConnectionId];
        ServerBootstrapSystem.ServerClient serverClient = __instance._ApprovedUsersLookup[userIndex];

        Entity userEntity = serverClient.UserEntity;
        User user = __instance.EntityManager.GetComponentData<User>(userEntity);
        Entity playerCharacter = user.LocalCharacter.GetEntityOnServer();
        ulong steamId = user.PlatformId;

        if (ConfigService.FamiliarSystem && playerCharacter.Exists())
        {
            FamiliarUtilities.ClearBuffers(playerCharacter, steamId);
        }

        if (ConfigService.LevelingSystem)
        {
            if (ConfigService.RestedXPSystem && steamId.TryGetPlayerRestedXP(out var restedData))
            {
                restedData = new KeyValuePair<DateTime, float>(DateTime.UtcNow, restedData.Value);
                steamId.SetPlayerRestedXP(restedData);
            }
        }

        if (ConfigService.ClientCompanion)
        {
            if (EclipseService.RegisteredUsersAndClientVersions.ContainsKey(steamId)) EclipseService.RegisteredUsersAndClientVersions.Remove(steamId);
        }
    }

    [HarmonyPatch(typeof(KickBanSystem_Server), nameof(KickBanSystem_Server.OnUpdate))] // treat this an OnUserDisconnected to account for player swaps
    [HarmonyPrefix]
    static void OnUserDisconnectedPrefix(KickBanSystem_Server __instance)
    {
        NativeArray<Entity> entities = __instance._KickQuery.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (entity.TryGetComponent(out KickEvent kickEvent))
                {
                    ulong steamId = kickEvent.PlatformId;

                    if (steamId.TryGetPlayerInfo(out PlayerInfo playerInfo) && playerInfo.CharEntity.Exists())
                    {
                        Entity character = playerInfo.CharEntity;

                        if (ConfigService.FamiliarSystem)
                        {
                            FamiliarUtilities.ClearBuffers(character, steamId);
                        }

                        if (ConfigService.LevelingSystem)
                        {
                            if (ConfigService.RestedXPSystem && steamId.TryGetPlayerRestedXP(out var restedData))
                            {
                                restedData = new KeyValuePair<DateTime, float>(DateTime.UtcNow, restedData.Value);
                                steamId.SetPlayerRestedXP(restedData);
                            }
                        }

                        if (ConfigService.ClientCompanion)
                        {
                            if (EclipseService.RegisteredUsersAndClientVersions.ContainsKey(steamId)) EclipseService.RegisteredUsersAndClientVersions.Remove(steamId);
                        }
                    }
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
}