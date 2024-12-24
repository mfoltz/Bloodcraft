using Bloodcraft.Services;
using ProjectM;
using ProjectM.Network;
using ProjectM.Shared;
using Stunlock.Core;
using System.Collections.Concurrent;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using VampireCommandFramework;
using static Bloodcraft.Patches.LinkMinionToOwnerOnSpawnSystemPatch;
using static Bloodcraft.Services.DataService.FamiliarPersistence;
using static Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarBuffsManager;
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
    static EntityCommandBufferSystem EntityCommandBufferSystem => SystemService.EntityCommandBufferSystem;

    public static readonly ConcurrentDictionary<Entity, Entity> AutoCallMap = [];

    static readonly PrefabGUID _defaultEmoteBuff = new(-988102043);
    static readonly PrefabGUID _combatBuff = new(581443919);
    static readonly PrefabGUID _pvPCombatBuff = new(697095869);
    static readonly PrefabGUID _dominateBuff = new(-1447419822);
    static readonly PrefabGUID _takeFlightBuff = new(1205505492);
    static readonly PrefabGUID _inkCrawlerDeathBuff = new(1273155981);

    static readonly float3 _southFloat3 = new(0f, 0f, -1f);
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

        if (!followers.IsEmpty) // if buffer not empty check here first, only need the rest if familiar is disabled via call/dismiss since that removes from followerBuffer to enable waygate use and such
        {
            foreach (FollowerBuffer follower in followers)
            {
                Entity familiar = follower.Entity._Entity;
                if (familiar.Has<BlockFeedBuff>()) return familiar;
                else if (steamId.TryGetFamiliarActives(out var actives))
                {
                    PrefabGUID prefabGUID = familiar.GetPrefabGUID();

                    if (actives.FamKey == prefabGUID.GuidHash) return familiar;
                }
            }
        }
        else if (HasDismissed(steamId, out Entity familiar)) return familiar;

        return Entity.Null;
    }
    public static void HandleFamiliarMinions(Entity familiar) //  need to see if game will handle familiar minions as player minions without extra effort, that would be neat
    {
        if (FamiliarMinions.ContainsKey(familiar))
        {
            foreach (Entity minion in FamiliarMinions[familiar])
            {
                DestroyUtility.Destroy(EntityManager, minion);
            }

            FamiliarMinions.TryRemove(familiar, out var _);
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
        UnlockedFamiliarData data = LoadUnlockedFamiliars(steamId);

        if (int.TryParse(unit, out int prefabHash) && PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(new(prefabHash), out Entity prefabEntity))
        {
            // Add to set if valid
            if (!prefabEntity.ReadRO<PrefabGUID>().GetPrefabName().StartsWith("CHAR"))
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
                if (!prefabEntity.ReadRO<PrefabGUID>().GetPrefabName().StartsWith("CHAR"))
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
        float distance = UnityEngine.Vector3.Distance(familiar.GetPosition(), playerPosition);

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
        familiar.Write(new Translation { Value = position });
        familiar.Write(new LastTranslation { Value = position });

        Follower follower = familiar.ReadRO<Follower>();
        follower.Followed._Value = playerCharacter;
        familiar.Write(follower);

        if (ConfigService.FamiliarCombat)
        {
            AggroConsumer aggroConsumer = familiar.ReadRO<AggroConsumer>();
            aggroConsumer.Active._Value = true;
            familiar.Write(aggroConsumer);
        }

        data = (Entity.Null, data.FamKey);
        steamId.SetFamiliarActives(data);

        string message = "<color=yellow>Familiar</color> <color=green>enabled</color>!";
        LocalizationService.HandleServerReply(EntityManager, user, message);
    }
    public static void NothingLivesForever(this Entity familiar, float duration = FAMILIAR_LIFETIME)
    {
        if (Buffs.TryApplyBuff(familiar, _inkCrawlerDeathBuff) && familiar.TryGetBuff(_inkCrawlerDeathBuff, out Entity buffEntity))
        {
            buffEntity.With((ref LifeTime lifeTime) =>
            {
                lifeTime.Duration = duration;
            });
        }
    }
    public static void DismissFamiliar(Entity playerCharacter, Entity familiar, User user, ulong steamId, (Entity Familiar, int FamKey) data)
    {
        if (FamiliarMinions.ContainsKey(data.Familiar)) HandleFamiliarMinions(familiar);

        ResetAndDisableAggro(familiar);

        Follower follower = familiar.ReadRO<Follower>();
        follower.Followed._Value = Entity.Null;
        familiar.Write(follower);

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
        EntityCommandBuffer entityCommandBuffer = EntityCommandBufferSystem.CreateCommandBuffer();

        if (familiar.Has<AlertBuffer>()) entityCommandBuffer.SetBuffer<AlertBuffer>(familiar).Clear();
        if (familiar.Has<AggroDamageHistoryBufferElement>()) entityCommandBuffer.SetBuffer<AggroDamageHistoryBufferElement>(familiar).Clear();
        if (familiar.Has<AggroCandidateBufferElement>()) entityCommandBuffer.SetBuffer<AggroCandidateBufferElement>(familiar).Clear();
        if (familiar.Has<ExternalAggroBufferElement>()) entityCommandBuffer.SetBuffer<ExternalAggroBufferElement>(familiar).Clear();

        /*
        if (ServerGameManager.TryGetBuffer<AlertBuffer>(familiar, out var alertBuffer)) alertBuffer.Clear();
        if (ServerGameManager.TryGetBuffer<AggroDamageHistoryBufferElement>(familiar, out var damageHistoryBuffer)) damageHistoryBuffer.Clear();
        if (ServerGameManager.TryGetBuffer<AggroCandidateBufferElement>(familiar, out var aggroCandidateBuffer)) aggroCandidateBuffer.Clear();
        if (ServerGameManager.TryGetBuffer<ExternalAggroBufferElement>(familiar, out var externalAggroBuffer)) externalAggroBuffer.Clear();
        if (ServerGameManager.TryGetBuffer<AggroBuffer>(familiar, out var aggroBuffer)) aggroBuffer.Clear();
        */

        if (!familiar.Has<AggroConsumer>()) return;

        familiar.With((ref AggroConsumer aggroConsumer) =>
        {
            aggroConsumer.AggroTarget = NetworkedEntity.Empty;
            aggroConsumer.AlertTarget = NetworkedEntity.Empty;
        });
    }
    public static void ResetAndDisableAggro(Entity familiar)
    {
        EntityCommandBuffer entityCommandBuffer = EntityCommandBufferSystem.CreateCommandBuffer();

        if (familiar.Has<AlertBuffer>()) entityCommandBuffer.SetBuffer<AlertBuffer>(familiar).Clear();
        if (familiar.Has<AggroDamageHistoryBufferElement>()) entityCommandBuffer.SetBuffer<AggroDamageHistoryBufferElement>(familiar).Clear();
        if (familiar.Has<AggroCandidateBufferElement>()) entityCommandBuffer.SetBuffer<AggroCandidateBufferElement>(familiar).Clear();
        if (familiar.Has<ExternalAggroBufferElement>()) entityCommandBuffer.SetBuffer<ExternalAggroBufferElement>(familiar).Clear();

        /*
        if (ServerGameManager.TryGetBuffer<AlertBuffer>(familiar, out var alertBuffer)) alertBuffer.Clear();
        if (ServerGameManager.TryGetBuffer<AggroDamageHistoryBufferElement>(familiar, out var damageHistoryBuffer)) damageHistoryBuffer.Clear();
        if (ServerGameManager.TryGetBuffer<AggroCandidateBufferElement>(familiar, out var aggroCandidateBuffer)) aggroCandidateBuffer.Clear();
        if (ServerGameManager.TryGetBuffer<ExternalAggroBufferElement>(familiar, out var externalAggroBuffer)) externalAggroBuffer.Clear();
        if (ServerGameManager.TryGetBuffer<AggroBuffer>(familiar, out var aggroBuffer)) aggroBuffer.Clear();
        */

        if (!familiar.Has<AggroConsumer>()) return;

        familiar.With((ref AggroConsumer aggroConsumer) =>
        {
            aggroConsumer.Active._Value = false;
            aggroConsumer.AggroTarget = NetworkedEntity.Empty;
            aggroConsumer.AlertTarget = NetworkedEntity.Empty;
        });
    }
    public static void BindFamiliar(Entity character, Entity userEntity, ulong steamId, int boxIndex = -1)
    {
        User user = userEntity.ReadRO<User>();
        Entity familiar = FindPlayerFamiliar(character);

        if (familiar.Exists())
        {
            LocalizationService.HandleServerReply(EntityManager, user, "You already have an active familiar! Unbind it first.");
            return;
        }
        else if (character.HasBuff(_combatBuff) || character.HasBuff(_dominateBuff) || character.HasBuff(_takeFlightBuff) || character.HasBuff(_pvPCombatBuff))
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

                // SetPlayerBool(steamId, "Binding", true);

                data = new(Entity.Null, famKeys[boxIndex - 1]);
                steamId.SetFamiliarActives(data);

                SummonFamiliar(character, userEntity, famKeys[boxIndex - 1]);
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
                // SetPlayerBool(steamId, "Binding", true);
                steamId.SetFamiliarDefault(boxIndex);

                data = new(Entity.Null, famKeys[boxIndex - 1]);
                steamId.SetFamiliarActives(data);

                SummonFamiliar(character, userEntity, famKeys[boxIndex - 1]);
            }
        }
        else
        {
            LocalizationService.HandleServerReply(EntityManager, user, "Couldn't find familiar actives or familiar already active! If this doesn't seem right try using '<color=white>.fam reset</color>'.");
        }
    }
    public static void UnbindFamiliar(Entity character, Entity userEntity, ulong steamId)
    {
        User user = userEntity.ReadRO<User>();
        Entity familiar = FindPlayerFamiliar(character);

        if (familiar.Exists())
        {
            PrefabGUID prefabGUID = familiar.GetPrefabGUID();
            int famKey = prefabGUID.GuidHash;

            FamiliarBuffsData buffsData = LoadFamiliarBuffs(steamId);
            string shinyHexColor = "";

            // Check if the familiar has buffs and update the color based on RandomVisuals
            if (buffsData.FamiliarBuffs.ContainsKey(famKey))
            {
                // Look up the color from the RandomVisuals dictionary if it exists
                if (ShinyBuffColorHexMap.TryGetValue(new(buffsData.FamiliarBuffs[famKey].First()), out var hexColor))
                {
                    shinyHexColor = $"<color={hexColor}>";
                }
            }

            string message = !string.IsNullOrEmpty(shinyHexColor) ? $"<color=green>{prefabGUID.GetLocalizedName()}</color>{shinyHexColor}*</color> <color=#FFC0CB>unbound</color>!" : $"<color=green>{prefabGUID.GetLocalizedName()}</color> <color=#FFC0CB>unbound</color>!";

            if (FamiliarMinions.ContainsKey(familiar)) HandleFamiliarMinions(familiar);
            if (familiar.Has<Disabled>()) familiar.Remove<Disabled>();
            if (AutoCallMap.ContainsKey(character)) AutoCallMap.TryRemove(character, out var _);

            DestroyUtility.Destroy(EntityManager, familiar);
            ClearFamiliarActives(steamId);

            LocalizationService.HandleServerReply(EntityManager, user, message);
        }
        else
        {
            LocalizationService.HandleServerReply(EntityManager, user, "Couldn't find familiar to unbind, if this doesn't seem right try using '<color=white>.fam reset</color>'.");
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
            DamageValue = 500f,
            NextPlayerCombatBuffSpawnTime = float.MinValue,
            Entity = target,
            Weight = 1f,
            IsPlayer = true
        };

        aggroBuffer.Add(aggroBufferElement);
    }
    public static void FaceYourEnemy(Entity familiar, Entity target)
    {
        //Core.Log.LogInfo($"Setting direction for {familiar.GetPrefabGUID().LookupName()} | {target.GetPrefabGUID().LookupName()}");

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

            //Core.Log.LogInfo($"Applied emote buff to {familiar.GetPrefabGUID().LookupName()} with owner {buffEntity.GetOwner().GetPrefabGUID().LookupName()}");
        }
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

            if (!prestigeData.FamiliarPrestige.ContainsKey(famKey))
            {
                prestigeData.FamiliarPrestige[famKey] = new(0, []);
                SaveFamiliarPrestige(steamId, prestigeData);
            }
            else
            {
                prestiges = prestigeData.FamiliarPrestige[famKey].Key;
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

        if (!prestigeData.FamiliarPrestige.ContainsKey(actives.famKey))
        {
            prestigeData.FamiliarPrestige[actives.famKey] = new(0, []);
            SaveFamiliarPrestige(steamId, prestigeData);
        }
        else
        {
            prestiges = prestigeData.FamiliarPrestige[actives.famKey].Key;
        }

        LocalizationService.HandleReply(ctx, $"<color=green>{famName}</color>{(buffsData.FamiliarBuffs.ContainsKey(actives.famKey) ? $"{colorCode}*</color>" : "")} [<color=white>{level}</color>][<color=#90EE90>{prestiges}</color>] added to battle group (<color=white>{slotIndex + 1}</color>)!");
    }
}
