using Bloodcraft.Services;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using System.Collections;
using System.Collections.Concurrent;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using VampireCommandFramework;
using static Bloodcraft.Patches.LinkMinionToOwnerOnSpawnSystemPatch;
using static Bloodcraft.Services.DataService.FamiliarPersistence;
using static Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarBuffsManager;
using static Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarExperienceManager;
using static Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarPrestigeManager;
using static Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarUnlocksManager;
using static Bloodcraft.Systems.Familiars.FamiliarLevelingSystem;
using static Bloodcraft.Systems.Familiars.FamiliarSummonSystem;
using static Bloodcraft.Systems.Familiars.FamiliarUnlockSystem;
using static Bloodcraft.Utilities.Misc.PlayerBoolsManager;

namespace Bloodcraft.Utilities;
internal static class Familiars
{
    static EntityManager EntityManager => Core.EntityManager;
    static SystemService SystemService => Core.SystemService;
    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;
    static GameDataSystem GameDataSystem => SystemService.GameDataSystem;

    static readonly bool _familiarCombat = ConfigService.FamiliarCombat;

    static readonly WaitForSeconds _smartBindDelay = new(0.1f);
    static readonly WaitForSeconds _delay = new(1f);

    const float AGGRO_BUFF_DURATION = 1f;

    static readonly PrefabGUID _defaultEmoteBuff = new(-988102043);
    static readonly PrefabGUID _combatBuff = new(581443919);
    static readonly PrefabGUID _pvpCombatBuff = new(697095869);
    static readonly PrefabGUID _dominateBuff = new(-1447419822);
    static readonly PrefabGUID _takeFlightBuff = new(1205505492);
    static readonly PrefabGUID _inkCrawlerDeathBuff = new(1273155981);
    static readonly PrefabGUID _invulnerableBuff = new(-480024072);
    static readonly PrefabGUID _disableAggroBuff = new(1934061152); // Buff_Illusion_Mosquito_DisableAggro
    static readonly PrefabGUID _vanishBuff = new(1595547018);       // AB_Bandit_Thief_Rush_Buff

    static readonly PrefabGUID _targetSwallowedBuff = new(-915145807);
    static readonly PrefabGUID _hasSwallowedBuff = new(1457576969);

    static readonly PrefabGUID _spiritDouble = new(-935560085);
    static readonly PrefabGUID _highlordGroundSword = new(-1266036232);

    static readonly float3 _southFloat3 = new(0f, 0f, -1f);
    public enum FamiliarEquipmentType
    {
        Headgear,
        Chest,
        Weapon,
        MagicSource,
        Footgear,
        Legs,
        Gloves
    }

    public static readonly ConcurrentDictionary<Entity, Entity> AutoCallMap = [];
    public static readonly ConcurrentDictionary<Entity, Entity> FamiliarServantMap = [];
    public static readonly ConcurrentDictionary<Entity, Entity> FamiliarHorseMap = [];
    public static void ClearFamiliarActives(ulong steamId)
    {
        if (steamId.TryGetFamiliarActives(out var actives))
        {
            actives = (Entity.Null, 0);
            steamId.SetFamiliarActives(actives);
        }
    }
    public static Entity FindPlayerFamiliar(Entity character)
    {
        if (!character.Has<FollowerBuffer>()) return Entity.Null;

        var followers = character.ReadBuffer<FollowerBuffer>();
        ulong steamId = character.GetSteamId();

        if (!followers.IsEmpty)
        {
            foreach (FollowerBuffer follower in followers)
            {
                Entity familiar = follower.Entity._Entity;

                if (familiar.Has<BlockFeedBuff>()) return familiar;
                else if (steamId.TryGetFamiliarActives(out var actives))
                {
                    PrefabGUID prefabGuid = familiar.GetPrefabGuid();
                    if (actives.FamKey == prefabGuid.GuidHash) return familiar;
                }
            }
        }
        else if (HasDismissed(steamId, out Entity familiar)) return familiar;

        return Entity.Null;
    }
    public static void HandleFamiliarMinions(Entity familiar) //  need to see if game will handle familiar minions as player minions without extra effort, that would be neat
    {
        if (FamiliarMinions.ContainsKey(familiar) && FamiliarMinions.TryRemove(familiar, out HashSet<Entity> familiarMinions))
        {
            foreach (Entity minion in familiarMinions)
            {
                minion.Destroy();
            }
        }
    }
    public static bool HasDismissed(ulong steamId, out Entity familiar)
    {
        familiar = Entity.Null;

        if (steamId.TryGetFamiliarActives(out var actives) && actives.Familiar.Exists())
        {
            familiar = actives.Familiar;
            return true;
        }

        return false;
    }
    public static void ParseAddedFamiliar(ChatCommandContext ctx, ulong steamId, string unit, string activeSet = "")
    {
        FamiliarUnlocksData data = LoadUnlockedFamiliars(steamId);

        if (int.TryParse(unit, out int prefabHash) && PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(new(prefabHash), out Entity prefabEntity))
        {
            // Add to set if valid
            if (!prefabEntity.Read<PrefabGUID>().GetPrefabName().StartsWith("CHAR"))
            {
                LocalizationService.HandleReply(ctx, "Invalid unit prefab (match found but does not start with CHAR/char).");
                return;
            }

            data.UnlockedFamiliars[activeSet].Add(prefabHash);
            SaveUnlockedFamiliars(steamId, data);

            LocalizationService.HandleReply(ctx, $"<color=green>{new PrefabGUID(prefabHash).GetLocalizedName()}</color> added to <color=white>{activeSet}</color>.");
        }
        else if (unit.ToLower().StartsWith("char")) // search for full and/or partial name match
        {
            // Try using TryGetValue for an exact match (case-sensitive)
            if (!PrefabCollectionSystem.NameToPrefabGuidDictionary.TryGetValue(unit, out PrefabGUID match))
            {
                // If exact match is not found, do a case-insensitive search for full or partial matches
                foreach (var kvp in PrefabCollectionSystem.NameToPrefabGuidDictionary)
                {
                    // Check for a case-insensitive full match
                    if (kvp.Key.Equals(unit, StringComparison.OrdinalIgnoreCase))
                    {
                        match = kvp.Value; // Full match found
                        break;
                    }
                }
            }

            // verify prefab is a char unit
            if (!match.IsEmpty() && PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(match, out prefabEntity))
            {
                if (!prefabEntity.Read<PrefabGUID>().GetPrefabName().StartsWith("CHAR"))
                {
                    LocalizationService.HandleReply(ctx, "Invalid unit name (match found but does not start with CHAR/char).");
                    return;
                }

                data.UnlockedFamiliars[activeSet].Add(match.GuidHash);
                SaveUnlockedFamiliars(steamId, data);

                LocalizationService.HandleReply(ctx, $"<color=green>{match.GetLocalizedName()}</color> (<color=yellow>{match.GuidHash}</color>) added to <color=white>{activeSet}</color>.");
            }
            else
            {
                LocalizationService.HandleReply(ctx, "Invalid unit name (no full or partial matches).");
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Invalid prefab (not an integer) or name (does not start with CHAR/char).");
        }
    }
    public static void TryReturnFamiliar(Entity player, Entity familiar)
    {
        float3 playerPosition = player.GetPosition();
        float distance = Vector3.Distance(familiar.GetPosition(), playerPosition);

        if (distance >= 25f)
        {
            ReturnFamiliar(playerPosition, familiar);
        }
    }
    public static void ReturnFamiliar(float3 position, Entity familiar)
    {
        familiar.With((ref LastTranslation lastTranslation) =>
        {
            lastTranslation.Value = position;
        });

        familiar.With((ref Translation translation) =>
        {
            translation.Value = position;
        });

        ResetAggro(familiar);
    }
    public static void ToggleShinies(ChatCommandContext ctx, ulong steamId)
    {
        TogglePlayerBool(steamId, "FamiliarVisual");
        LocalizationService.HandleReply(ctx, GetPlayerBool(steamId, "FamiliarVisual") ? "Shiny familiars <color=green>enabled</color>." : "Shiny familiars <color=red>disabled</color>.");
    }
    public static void ToggleVBloodEmotes(ChatCommandContext ctx, ulong steamId)
    {
        TogglePlayerBool(steamId, "VBloodEmotes");
        LocalizationService.HandleReply(ctx, GetPlayerBool(steamId, "VBloodEmotes") ? "VBlood Emotes <color=green>enabled</color>." : "VBlood Emotes <color=red>disabled</color>.");
    }
    public static bool TryParseFamiliarStat(string statType, out FamiliarStatType parsedStatType)
    {
        parsedStatType = default;

        if (Enum.TryParse(statType, true, out parsedStatType))
        {
            return true;
        }
        else
        {
            parsedStatType = Enum.GetValues(typeof(FamiliarStatType))
                .Cast<FamiliarStatType>()
                .FirstOrDefault(pt => pt.ToString().Contains(statType, StringComparison.OrdinalIgnoreCase));

            if (!parsedStatType.Equals(default(FamiliarStatType)))
            {
                return true;
            }
        }

        return false;
    }
    public static void CallFamiliar(Entity playerCharacter, Entity familiar, User user, ulong steamId, (Entity Familiar, int FamKey) data)
    {
        familiar.Remove<Disabled>();

        float3 position = playerCharacter.GetPosition();
        familiar.SetPosition(position);

        familiar.With((ref Follower follower) =>
        {
            follower.Followed._Value = playerCharacter;
        });

        if (_familiarCombat && !familiar.HasBuff(_invulnerableBuff))
        {
            familiar.TryRemoveBuff(_disableAggroBuff);
        }

        data = (Entity.Null, data.FamKey);
        steamId.SetFamiliarActives(data);

        string message = "<color=yellow>Familiar</color> <color=green>enabled</color>!";
        LocalizationService.HandleServerReply(EntityManager, user, message);
    }
    public static void NothingLivesForever(this Entity unit, float duration = FAMILIAR_LIFETIME)
    {
        if (unit.TryApplyAndGetBuff(_inkCrawlerDeathBuff, out Entity buffEntity))
        {
            buffEntity.With((ref LifeTime lifeTime) =>
            {
                lifeTime.Duration = duration;
            });

            PrefabGUID unitPrefabGuid = unit.GetPrefabGuid();

            if ((unitPrefabGuid.Equals(_spiritDouble) || unitPrefabGuid.Equals(_highlordGroundSword)) && unit.Has<Immortal>())
            {
                unit.With((ref Immortal immortal) =>
                {
                    immortal.IsImmortal = false;
                });
            }
        }
    }
    public static void DismissFamiliar(Entity playerCharacter, Entity familiar, User user, ulong steamId, (Entity Familiar, int FamKey) data)
    {
        HandleFamiliarMinions(familiar);
        ResetAndDisableAggro(familiar);

        // Follower follower = familiar.ReadRO<Follower>();
        // follower.Followed._Value = Entity.Null;
        // familiar.Write(follower);

        familiar.With((ref Follower follower) =>
        {
            follower.Followed._Value = Entity.Null;
        });

        var buffer = playerCharacter.ReadBuffer<FollowerBuffer>();
        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i].Entity._Entity.Equals(familiar))
            {
                buffer.RemoveAt(i);
                break;
            }
        }

        familiar.Add<Disabled>();

        data = (familiar, data.FamKey); // entity stored when dismissed
        steamId.SetFamiliarActives(data);

        string message = "<color=yellow>Familiar</color> <color=red>disabled</color>!";
        LocalizationService.HandleServerReply(EntityManager, user, message);
    }
    public static void ResetAggro(Entity familiar)
    {
        /*
        EntityCommandBuffer entityCommandBuffer = EntityCommandBufferSystem.CreateCommandBuffer();

        if (familiar.Has<AlertBuffer>()) entityCommandBuffer.SetBuffer<AlertBuffer>(familiar).Clear();
        if (familiar.Has<AggroDamageHistoryBufferElement>()) entityCommandBuffer.SetBuffer<AggroDamageHistoryBufferElement>(familiar).Clear();
        if (familiar.Has<AggroCandidateBufferElement>()) entityCommandBuffer.SetBuffer<AggroCandidateBufferElement>(familiar).Clear();
        if (familiar.Has<ExternalAggroBufferElement>()) entityCommandBuffer.SetBuffer<ExternalAggroBufferElement>(familiar).Clear();
        
        if (ServerGameManager.TryGetBuffer<AlertBuffer>(familiar, out var alertBuffer)) alertBuffer.Clear();
        if (ServerGameManager.TryGetBuffer<AggroDamageHistoryBufferElement>(familiar, out var damageHistoryBuffer)) damageHistoryBuffer.Clear();
        if (ServerGameManager.TryGetBuffer<AggroCandidateBufferElement>(familiar, out var aggroCandidateBuffer)) aggroCandidateBuffer.Clear();
        if (ServerGameManager.TryGetBuffer<ExternalAggroBufferElement>(familiar, out var externalAggroBuffer)) externalAggroBuffer.Clear();
        if (ServerGameManager.TryGetBuffer<AggroBuffer>(familiar, out var aggroBuffer)) aggroBuffer.Clear();   

        if (!familiar.Has<AggroConsumer>()) return;

        familiar.With((ref AggroConsumer aggroConsumer) =>
        {
            aggroConsumer.AggroTarget = NetworkedEntity.Empty;
            aggroConsumer.AlertTarget = NetworkedEntity.Empty;
        });
        */

        if (!familiar.Has<AggroConsumer>()) return;
        else if (familiar.TryApplyBuff(_disableAggroBuff))
        {
            familiar.TryApplyBuffWithLifeTime(_disableAggroBuff, AGGRO_BUFF_DURATION);
        }
    }
    public static void ResetAndDisableAggro(Entity familiar)
    {
        /*
        EntityCommandBuffer entityCommandBuffer = EntityCommandBufferSystem.CreateCommandBuffer();

        if (familiar.Has<AlertBuffer>()) entityCommandBuffer.SetBuffer<AlertBuffer>(familiar).Clear();
        if (familiar.Has<AggroDamageHistoryBufferElement>()) entityCommandBuffer.SetBuffer<AggroDamageHistoryBufferElement>(familiar).Clear();
        if (familiar.Has<AggroCandidateBufferElement>()) entityCommandBuffer.SetBuffer<AggroCandidateBufferElement>(familiar).Clear();
        if (familiar.Has<ExternalAggroBufferElement>()) entityCommandBuffer.SetBuffer<ExternalAggroBufferElement>(familiar).Clear();

        
        if (ServerGameManager.TryGetBuffer<AlertBuffer>(familiar, out var alertBuffer)) alertBuffer.Clear();
        if (ServerGameManager.TryGetBuffer<AggroDamageHistoryBufferElement>(familiar, out var damageHistoryBuffer)) damageHistoryBuffer.Clear();
        if (ServerGameManager.TryGetBuffer<AggroCandidateBufferElement>(familiar, out var aggroCandidateBuffer)) aggroCandidateBuffer.Clear();
        if (ServerGameManager.TryGetBuffer<ExternalAggroBufferElement>(familiar, out var externalAggroBuffer)) externalAggroBuffer.Clear();
        if (ServerGameManager.TryGetBuffer<AggroBuffer>(familiar, out var aggroBuffer)) aggroBuffer.Clear();
        */

        if (!familiar.Has<AggroConsumer>()) return;
        else
        {
            familiar.TryApplyBuff(_disableAggroBuff);
        }
    }
    public static void BindFamiliar(User user, Entity character, int boxIndex = -1)
    {
        ulong steamId = user.PlatformId;
        Entity familiar = FindPlayerFamiliar(character);

        if (familiar.Exists())
        {
            LocalizationService.HandleServerReply(EntityManager, user, "You already have an active familiar! Unbind it first.");
            return;
        }
        else if (character.HasBuff(_combatBuff) || character.HasBuff(_dominateBuff) || character.HasBuff(_takeFlightBuff) || character.HasBuff(_pvpCombatBuff))
        {
            LocalizationService.HandleServerReply(EntityManager, user, "You can't bind a familiar during combat or when using certain forms! (dominating presence, bat)");
            return;
        }

        string set = steamId.TryGetFamiliarBox(out set) ? set : "";
        if (string.IsNullOrEmpty(set))
        {
            LocalizationService.HandleServerReply(EntityManager, user, "You don't have a box selected! Use '<color=white>.fam boxes</color>' to see available boxes. Select a box with '<color=white>.fam cb [BoxName]</color>");
            return;
        }
        else if (steamId.TryGetFamiliarActives(out var data) && !data.Familiar.Exists() && data.FamKey.Equals(0) && LoadUnlockedFamiliars(steamId).UnlockedFamiliars.TryGetValue(set, out var famKeys))
        {
            if (boxIndex == -1 && steamId.TryGetFamiliarDefault(out boxIndex)) // use preset when invoked without index parameter
            {
                if (boxIndex < 1 || boxIndex > famKeys.Count) // validate index from default option
                {
                    LocalizationService.HandleServerReply(EntityManager, user, $"Invalid box index for current box, try binding manually again first.");
                    return;
                }

                data = new(Entity.Null, famKeys[boxIndex - 1]);
                steamId.SetFamiliarActives(data);

                InstantiateFamiliarImmediate(user, character, famKeys[boxIndex - 1]);
            }
            else if (boxIndex == -1)
            {
                LocalizationService.HandleServerReply(EntityManager, user, $"Couldn't find binding preset, bind to a familiar via command at least once first!");
                return;
            }
            else if (boxIndex < 1 || boxIndex > famKeys.Count) // validate input from user
            {
                LocalizationService.HandleServerReply(EntityManager, user, $"Invalid choice, please use <color=white>1</color> to <color=white>{famKeys.Count}</color> (Current Box: <color=yellow>{set}</color>)");
                return;
            }
            else
            {
                steamId.SetFamiliarDefault(boxIndex);

                data = new(Entity.Null, famKeys[boxIndex - 1]);
                steamId.SetFamiliarActives(data);

                InstantiateFamiliarImmediate(user, character, famKeys[boxIndex - 1]);
            }
        }
        else
        {
            LocalizationService.HandleServerReply(EntityManager, user, "Couldn't find familiar actives or familiar already active! If this doesn't seem right try using '<color=white>.fam reset</color>'.");
        }
    }
    public static void UnbindFamiliar(User user, Entity playerCharacter, bool smartBind = false, int index = -1)
    {
        Entity familiar = FindPlayerFamiliar(playerCharacter);
        bool hasActive = user.PlatformId.TryGetFamiliarActives(out var actives) && !actives.FamKey.Equals(0);

        if (hasActive && familiar.Exists())
        {
            familiar.TryApplyBuff(_vanishBuff);
            familiar.TryApplyBuff(_disableAggroBuff);
            UnbindFamiliarDelayRoutine(user, playerCharacter, familiar, smartBind, index).Start();
        }
        else
        {
            LocalizationService.HandleServerReply(EntityManager, user, "Couldn't find familiar to unbind, if this doesn't seem right try using '<color=white>.fam reset</color>'.");
        }
    }
    static IEnumerator UnbindFamiliarDelayRoutine(User user, Entity playerCharacter, Entity familiar, bool smartBind = false, int index = -1)
    {
        yield return _delay;

        PrefabGUID prefabGuid = familiar.GetPrefabGuid();

        ulong steamId = user.PlatformId;
        int famKey = prefabGuid.GuidHash;

        FamiliarBuffsData buffsData = LoadFamiliarBuffs(steamId);
        string shinyHexColor = "";

        if (buffsData.FamiliarBuffs.ContainsKey(famKey))
        {
            if (ShinyBuffColorHexMap.TryGetValue(new(buffsData.FamiliarBuffs[famKey].First()), out var hexColor))
            {
                shinyHexColor = $"<color={hexColor}>";
            }
        }

        HandleFamiliarMinions(familiar);
        FamiliarEquipmentManager.SaveFamiliarEquipment(steamId, famKey, UnequipFamiliar(familiar));

        if (familiar.Has<Disabled>()) familiar.Remove<Disabled>();
        if (AutoCallMap.ContainsKey(playerCharacter)) AutoCallMap.TryRemove(playerCharacter, out var _);

        familiar.Destroy();
        ClearFamiliarActives(steamId);

        string message = !string.IsNullOrEmpty(shinyHexColor) ? $"<color=green>{prefabGuid.GetLocalizedName()}</color>{shinyHexColor}*</color> <color=#FFC0CB>unbound</color>!" : $"<color=green>{prefabGuid.GetLocalizedName()}</color> <color=#FFC0CB>unbound</color>!";
        LocalizationService.HandleServerReply(EntityManager, user, message);

        if (smartBind)
        {
            yield return _smartBindDelay;

            BindFamiliar(user, playerCharacter, index);
        }
    }
    public static void AddToFamiliarAggroBuffer(Entity familiar, Entity target)
    {
        if (!familiar.Has<AggroBuffer>()) return;

        var aggroBuffer = familiar.ReadBuffer<AggroBuffer>();
        bool targetInBuffer = false;

        foreach (AggroBuffer aggroBufferEntry in aggroBuffer)
        {
            if (aggroBufferEntry.Entity.Equals(target))
            {
                targetInBuffer = true;

                break;
            }
        }

        if (targetInBuffer) return;
        AggroBuffer aggroBufferElement = new()
        {
            DamageValue = 500f, // Dreadhorn reference :p
            NextPlayerCombatBuffSpawnTime = float.MinValue,
            Entity = target,
            Weight = 1f,
            IsPlayer = true
        };

        aggroBuffer.Add(aggroBufferElement);
    }
    public static void FaceYourEnemy(Entity familiar, Entity target)
    {
        if (familiar.Has<EntityInput>())
        {
            familiar.With((ref EntityInput entityInput) =>
            {
                entityInput.AimDirection = _southFloat3;
            });
        }

        if (familiar.Has<TargetDirection>())
        {
            familiar.With((ref TargetDirection targetDirection) =>
            {
                targetDirection.AimDirection = _southFloat3;
            });
        }

        if (Buffs.TryApplyBuff(familiar, _defaultEmoteBuff) && familiar.TryGetBuff(_defaultEmoteBuff, out Entity buffEntity))
        {
            buffEntity.With((ref EntityOwner entityOwner) =>
            {
                entityOwner.Owner = target;
            });
        }
    }
    public static bool IsEligibleForCombat(this Entity familiar)
    {
        return familiar.Exists() && !familiar.IsDisabled() && !familiar.HasBuff(_invulnerableBuff);
    }
    public static void BuildBattleGroupDetailsReply(ulong steamId, FamiliarBuffsData buffsData, FamiliarPrestigeData prestigeData, List<int> battleGroup, ref List<string> familiars)
    {
        foreach (int famKey in battleGroup)
        {
            if (famKey == 0) continue;

            PrefabGUID famPrefab = new(famKey);
            string famName = famPrefab.GetLocalizedName();
            string colorCode = "<color=#FF69B4>"; // Default color for the asterisk

            int level = GetFamiliarExperience(steamId, famKey).Key;
            int prestiges = 0;

            // Check if the familiar has buffs and update the color based on RandomVisuals
            if (buffsData.FamiliarBuffs.ContainsKey(famKey))
            {
                // Look up the color from the RandomVisuals dictionary if it exists
                if (ShinyBuffColorHexMap.TryGetValue(new(buffsData.FamiliarBuffs[famKey][0]), out var hexColor))
                {
                    colorCode = $"<color={hexColor}>";
                }
            }

            if (!prestigeData.FamiliarPrestiges.ContainsKey(famKey))
            {
                prestigeData.FamiliarPrestiges[famKey] = new(0, []);
                SaveFamiliarPrestige(steamId, prestigeData);
            }
            else
            {
                prestiges = prestigeData.FamiliarPrestiges[famKey].Key;
            }

            familiars.Add($"<color=white>{battleGroup.IndexOf(famKey) + 1}</color>: <color=green>{famName}</color>{(buffsData.FamiliarBuffs.ContainsKey(famKey) ? $"{colorCode}*</color>" : "")} [<color=white>{level}</color>][<color=#90EE90>{prestiges}</color>]");
        }
    }
    public static void HandleBattleGroupDetailsReply(ChatCommandContext ctx, ulong steamId, List<int> battleGroup)
    {
        if (battleGroup.Any())
        {
            FamiliarBuffsData buffsData = LoadFamiliarBuffs(steamId);
            FamiliarPrestigeData prestigeData = LoadFamiliarPrestige(steamId);
            List<string> familiars = [];

            BuildBattleGroupDetailsReply(steamId, buffsData, prestigeData, battleGroup, ref familiars);

            string familiarReply = string.Join(", ", familiars);
            LocalizationService.HandleReply(ctx, $"Battle Group - {familiarReply}");
            return;
        }
        else
        {
            LocalizationService.HandleReply(ctx, "No familiars in battle group yet!");
            return;
        }
    }
    public static void HandleBattleGroupAddAndReply(ChatCommandContext ctx, ulong steamId, List<int> battleGroup, (Entity familiar, int famKey) actives, int slotIndex)
    {
        if (battleGroup.Contains(actives.famKey))
        {
            ctx.Reply("Familiar already in battle group!");
            return;
        }

        battleGroup[slotIndex] = (actives.famKey);
        steamId.SetFamiliarBattleGroup(battleGroup);

        FamiliarBuffsData buffsData = LoadFamiliarBuffs(steamId);
        FamiliarPrestigeData prestigeData = LoadFamiliarPrestige(steamId);

        int level = GetFamiliarExperience(steamId, actives.famKey).Key;
        if (level == 0) level = 1;
        int prestiges = 0;

        PrefabGUID famPrefab = new(actives.famKey);
        string famName = famPrefab.GetLocalizedName();
        string colorCode = "<color=#FF69B4>"; // Default color for the asterisk

        // Check if the familiar has buffs and update the color based on RandomVisuals
        if (buffsData.FamiliarBuffs.ContainsKey(actives.famKey))
        {
            // Look up the color from the RandomVisuals dictionary if it exists
            if (ShinyBuffColorHexMap.TryGetValue(new(buffsData.FamiliarBuffs[actives.famKey][0]), out var hexColor))
            {
                colorCode = $"<color={hexColor}>";
            }
        }

        if (!prestigeData.FamiliarPrestiges.ContainsKey(actives.famKey))
        {
            prestigeData.FamiliarPrestiges[actives.famKey] = new(0, []);
            SaveFamiliarPrestige(steamId, prestigeData);
        }
        else
        {
            prestiges = prestigeData.FamiliarPrestiges[actives.famKey].Key;
        }

        LocalizationService.HandleReply(ctx, $"<color=green>{famName}</color>{(buffsData.FamiliarBuffs.ContainsKey(actives.famKey) ? $"{colorCode}*</color>" : "")} [<color=white>{level}</color>][<color=#90EE90>{prestiges}</color>] added to battle group (<color=white>{slotIndex + 1}</color>)!");
    }
    public static string GetFamiliarName(PrefabGUID familiarId, FamiliarBuffsData buffsData)
    {
        if (buffsData.FamiliarBuffs.ContainsKey(familiarId.GuidHash))
        {
            if (ShinyBuffColorHexMap.TryGetValue(new(buffsData.FamiliarBuffs[familiarId.GuidHash].FirstOrDefault()), out string hexColor))
            {
                string colorCode = string.IsNullOrEmpty(hexColor) ? $"<color={hexColor}>" : string.Empty;
                return $"<color=green>{familiarId.GetLocalizedName()}</color>{colorCode}*</color>";
            }
        }

        return $"<color=green>{familiarId.GetLocalizedName()}</color>";
    }
    public static void HandleFamiliarPrestige(ChatCommandContext ctx, string bonusStat, int levels = 0)
    {
        Entity playerCharacter = ctx.Event.SenderCharacterEntity;
        User user = ctx.User;

        ulong steamId = user.PlatformId;

        if (!steamId.TryGetFamiliarActives(out var data)) // check if familiar is active
        {
            LocalizationService.HandleReply(ctx, "Couldn't find active familiar for prestiging!");
            return;
        }

        FamiliarExperienceData xpData = LoadFamiliarExperienceData(steamId);
        FamiliarPrestigeData prestigeData = LoadFamiliarPrestige(steamId);

        if (!prestigeData.FamiliarPrestiges.ContainsKey(data.FamKey))
        {
            prestigeData.FamiliarPrestiges[data.FamKey] = new(0, []);
            SaveFamiliarPrestige(steamId, prestigeData);
        }

        prestigeData = LoadFamiliarPrestige(steamId);
        List<FamiliarStatType> stats = prestigeData.FamiliarPrestiges[data.FamKey].Value;

        if (prestigeData.FamiliarPrestiges[data.FamKey].Key >= ConfigService.MaxFamiliarPrestiges)
        {
            LocalizationService.HandleReply(ctx, $"Your familiar has already prestiged the maximum number of times! (<color=white>{ConfigService.MaxFamiliarPrestiges}</color>)");
            return;
        }

        if (stats.Count < FamiliarStatValues.Count) // if less than max stats, parse entry and add if set doesnt already contain
        {
            if (!TryParseFamiliarStat(bonusStat, out var stat))
            {
                var familiarStatsWithCaps = Enum.GetValues(typeof(FamiliarStatType))
                .Cast<FamiliarStatType>()
                .Select(stat =>
                    $"<color=#00FFFF>{stat}</color>: <color=white>{FamiliarStatValues[stat]}</color>")
                .ToArray();

                int halfLength = familiarStatsWithCaps.Length / 2;

                string familiarStatsLine1 = string.Join(", ", familiarStatsWithCaps.Take(halfLength));
                string familiarStatsLine2 = string.Join(", ", familiarStatsWithCaps.Skip(halfLength));

                LocalizationService.HandleReply(ctx, "Invalid stat, please choose from the following:");
                LocalizationService.HandleReply(ctx, $"Available familiar stats (1/2): {familiarStatsLine1}");
                LocalizationService.HandleReply(ctx, $"Available familiar stats (2/2): {familiarStatsLine2}");

                return;
            }
            else if (!stats.Contains(stat))
            {
                stats.Add(stat);
            }
            else
            {
                LocalizationService.HandleReply(ctx, $"Familiar already has <color=#00FFFF>{stat}</color> as a bonus stat, pick another.");
                return;
            }
        }
        else if (stats.Count >= FamiliarStatValues.Count && !string.IsNullOrEmpty(bonusStat))
        {
            LocalizationService.HandleReply(ctx, "Familiar already has max bonus stats, try again without entering a stat.");
            return;
        }

        int levelsNeeded = ConfigService.MaxFamiliarLevel - xpData.FamiliarLevels[data.FamKey].Key;
        int levelsToAdd = levels - levelsNeeded;

        KeyValuePair<int, float> newXP = new(++levelsToAdd, Progression.ConvertLevelToXp(++levelsToAdd)); // reset level to 1
        xpData.FamiliarLevels[data.FamKey] = newXP;
        SaveFamiliarExperienceData(steamId, xpData);

        int prestigeLevel = prestigeData.FamiliarPrestiges[data.FamKey].Key + 1;
        prestigeData.FamiliarPrestiges[data.FamKey] = new(prestigeLevel, stats);
        SaveFamiliarPrestige(steamId, prestigeData);

        Entity familiar = FindPlayerFamiliar(playerCharacter);

        ModifyUnitStats(familiar, newXP.Key, steamId, data.FamKey);
        LocalizationService.HandleReply(ctx, $"Your familiar has prestiged [<color=#90EE90>{prestigeLevel}</color>] and is now level <color=white>{newXP.Key}</color>!");
    }
    static List<int> UnequipFamiliar(Entity familiar)
    {
        if (FamiliarServantMap.TryRemove(familiar, out Entity servant) && servant.TryGetComponent(out ServantEquipment servantEquipment))
        {
            List<int> familiarEquipment = [];

            foreach (FamiliarEquipmentType familiarEquipmentType in Enum.GetValues(typeof(FamiliarEquipmentType)))
            {
                if (Enum.TryParse(familiarEquipmentType.ToString(), true, out EquipmentType equipmentType) && servantEquipment.IsEquipped(equipmentType))
                {
                    PrefabGUID equipmentPrefabGuid = servantEquipment.GetEquipmentItemId(equipmentType);
                    familiarEquipment.Add(equipmentPrefabGuid.GuidHash);
                }
                else
                {
                    familiarEquipment.Add(0);
                }
            }

            servant.Destroy();
            return familiarEquipment;
        }

        return [0, 0, 0, 0, 0, 0];
    }
    public static void EquipFamiliar(ulong steamId, Entity familiar, int famKey)
    {
        if (!FamiliarServantMap.TryGetValue(familiar, out Entity servant) || !servant.TryGetComponent(out ServantEquipment servantEquipment)) return;
        List<PrefabGUID> familiarEquipment = FamiliarEquipmentManager.GetFamiliarEquipment(steamId, famKey).Select(item => new PrefabGUID(item)).ToList();

        for (int i = 0; i < familiarEquipment.Count; i++)
        {
            if (familiarEquipment[i].HasValue())
            {
                FamiliarEquipmentType familiarEquipmentType = (FamiliarEquipmentType)i;

                if (Enum.TryParse(familiarEquipmentType.ToString(), true, out EquipmentType equipmentType))
                {
                    Entity itemEntity = InventoryUtilitiesServer.CreateInventoryItemEntity(EntityManager, GameDataSystem.ItemHashLookupMap, familiarEquipment[i]);

                    if (itemEntity.Exists()) servantEquipment.SetEquipped(EntityManager, servant, equipmentType, itemEntity, familiarEquipment[i]);
                    else
                    {
                        Core.Log.LogInfo($"Failed to equip familiar, itemEntity doesn't exist! {familiarEquipment[i]}");
                    }
                }
            }
        }
    }
}
