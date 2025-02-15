using Bloodcraft.Services;
using Il2CppSystem;
using ProjectM;
using ProjectM.Gameplay.Scripting;
using ProjectM.Network;
using ProjectM.Scripting;
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
using static Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarPrestigeManager_V2;
using static Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarUnlocksManager;
using static Bloodcraft.Systems.Familiars.FamiliarSummonSystem;
using static Bloodcraft.Systems.Familiars.FamiliarUnlockSystem;
using static Bloodcraft.Utilities.Misc.PlayerBoolsManager;

namespace Bloodcraft.Utilities;
internal static class Familiars
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;
    static GameDataSystem GameDataSystem => SystemService.GameDataSystem;

    static readonly bool _familiarCombat = ConfigService.FamiliarCombat;

    static readonly WaitForSeconds _bindingDelay = new(0.25f);
    static readonly WaitForSeconds _delay = new(2f);

    const float AGGRO_BUFF_DURATION = 1f;

    static readonly PrefabGUID _defaultEmoteBuff = new(-988102043);
    static readonly PrefabGUID _pveCombatBuff = new(581443919);
    static readonly PrefabGUID _pvpCombatBuff = new(697095869);
    static readonly PrefabGUID _dominateBuff = new(-1447419822);
    static readonly PrefabGUID _takeFlightBuff = new(1205505492);
    static readonly PrefabGUID _inkCrawlerDeathBuff = new(1273155981);
    static readonly PrefabGUID _invulnerableBuff = new(-480024072);
    static readonly PrefabGUID _disableAggroBuff = new(1934061152);     // Buff_Illusion_Mosquito_DisableAggro
    static readonly PrefabGUID _vanishBuff = new(1595547018);           // AB_Bandit_Thief_Rush_Buff
    static readonly PrefabGUID _interactModeBuff = new(1520432556); // AB_Militia_HoundMaster_QuickShot_Buff

    static readonly PrefabGUID _targetSwallowedBuff = new(-915145807);
    static readonly PrefabGUID _hasSwallowedBuff = new(1457576969);

    static readonly PrefabGUID _spiritDouble = new(-935560085);
    static readonly PrefabGUID _highlordGroundSword = new(-1266036232);

    static readonly PrefabGUID _itemSchematic = new(2085163661);

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

    public static readonly Dictionary<string, PrefabGUID> VBloodNamePrefabGuidMap = new()
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
        { "Keely the Frost Archer", new(1124739990)},
        { "Lidia the Chaos Archer", new(763273073)},
        { "Finn the Fisherman", new(-2122682556)},
        { "Azariel the Sunbringer", new(114912615)},
        { "Sir Magnus the Overseer", new(-26105228)},
        { "Baron du Bouchon the Sommelier", new(192051202)},
        { "Solarus the Immaculate", new(-740796338)},
        { "Kodia the Ferocious Bear", new(-1391546313)},
        { "Ziva the Engineer", new(172235178)},
        { "Adam the Firstborn", new(1233988687)},
        { "Angram the Purifier", new(106480588)},
        { "Voltatia the Power Master", new(2054432370)},
        { "Henry Blackbrew the Doctor", new(814083983)},
        { "Domina the Blade Dancer", new(-1101874342)},
        { "Grethel the Glassblower", new(910988233)},
        { "Christina the Sun Priestess", new(-99012450)},
        { "Maja the Dark Savant", new(1945956671)},
        { "Polora the Feywalker", new(-484556888)},
        { "Simon Belmont the Vampire Hunter", new(336560131)},
        { "General Valencia the Depraved", new(495971434)},
        { "Dracula the Immortal King", new(-327335305)},
        { "General Cassius the Betrayer", new(-496360395)},
        { "General Elena the Hollow", new(795262842)}
    };

    public static readonly Dictionary<PrefabGUID, int> VBloodSpawnBuffTierMap = new()
    {
        {new(600470494), 1},    // Buff_General_Spawn_VBlood_AlphaWolf
        {new(148706785), 4},    // Buff_General_Spawn_VBlood_FirstBandits
        {new(-703593639), 7},   // Buff_General_Spawn_VBlood_EarlyGame
        {new(-184730451), 10},  // Buff_General_Spawn_VBlood_MidGame
        {new(-2071666138), 15}, // Buff_General_Spawn_VBlood_EndGame
        {new(-1163165749), 25}  // Buff_General_Spawn_VBlood_ShardBosses
    };

    public static readonly ConcurrentDictionary<Entity, Entity> AutoCallMap = [];
    public static readonly ConcurrentDictionary<Entity, Entity> FamiliarServantMap = [];
    // public static readonly ConcurrentDictionary<Entity, Entity> FamiliarHorseMap = [];
    public static void ClearFamiliarActives(ulong steamId)
    {
        steamId.SetFamiliarActives((Entity.Null, 0));
    }
    public static Entity GetActiveFamiliar(Entity playerCharacter)
    {
        ulong steamId = playerCharacter.GetSteamId();

        if (playerCharacter.TryGetBuffer<FollowerBuffer>(out var followers) && !followers.IsEmpty)
        {
            foreach (FollowerBuffer follower in followers)
            {
                Entity familiar = follower.Entity._Entity;

                if (familiar.Has<BlockFeedBuff>()) return familiar;
                else if (steamId.TryGetFamiliarActives(out var actives))
                {
                    PrefabGUID familiarId = familiar.GetPrefabGuid();
                    if (actives.FamKey == familiarId.GuidHash) return familiar;
                }
            }
        }
        else if (HasDismissed(steamId, out Entity familiar)) return familiar;

        return Entity.Null;
    }
    public static Entity GetFamiliarServant(Entity familiar)
    {
        if (FamiliarServantMap.TryGetValue(familiar, out Entity servant) && servant.Exists())
        {
            return servant;
        }

        return Entity.Null;
    }
    public static void HandleFamiliarMinions(Entity familiar)
    {
        if (FamiliarMinions.TryRemove(familiar, out HashSet<Entity> familiarMinions))
        {
            foreach (Entity minion in familiarMinions)
            {
                if (minion.Exists()) minion.Destroy();
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
    public static void ParseAddedFamiliar(ChatCommandContext ctx, ulong steamId, string unit, string activeBox = "")
    {
        FamiliarUnlocksData data = LoadFamiliarUnlocksData(steamId);

        if (int.TryParse(unit, out int prefabHash) && PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(new(prefabHash), out Entity prefabEntity))
        {
            // Add to set if valid
            if (!prefabEntity.Read<PrefabGUID>().GetPrefabName().StartsWith("CHAR"))
            {
                LocalizationService.HandleReply(ctx, "Invalid unit prefab (match found but does not start with CHAR/char).");
                return;
            }

            data.UnlockedFamiliars[activeBox].Add(prefabHash);
            SaveFamiliarUnlocksData(steamId, data);

            LocalizationService.HandleReply(ctx, $"<color=green>{new PrefabGUID(prefabHash).GetLocalizedName()}</color> added to <color=white>{activeBox}</color>.");
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
                    if (kvp.Key.Equals(unit, System.StringComparison.OrdinalIgnoreCase))
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

                data.UnlockedFamiliars[activeBox].Add(match.GuidHash);
                SaveFamiliarUnlocksData(steamId, data);

                LocalizationService.HandleReply(ctx, $"<color=green>{match.GetLocalizedName()}</color> (<color=yellow>{match.GuidHash}</color>) added to <color=white>{activeBox}</color>.");
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

        // ResetAggro(familiar);
    }
    public static void ToggleShinies(ChatCommandContext ctx, ulong steamId)
    {
        TogglePlayerBool(steamId, SHINY_FAMILIARS_KEY);
        LocalizationService.HandleReply(ctx, GetPlayerBool(steamId, SHINY_FAMILIARS_KEY) ? "Shiny familiars <color=green>enabled</color>." : "Shiny familiars <color=red>disabled</color>.");
    }
    public static void ToggleVBloodEmotes(ChatCommandContext ctx, ulong steamId)
    {
        TogglePlayerBool(steamId, VBLOOD_EMOTES_KEY);
        LocalizationService.HandleReply(ctx, GetPlayerBool(steamId, VBLOOD_EMOTES_KEY) ? "VBlood emotes <color=green>enabled</color>." : "VBlood emotes <color=red>disabled</color>.");
    }
    public static bool TryParseFamiliarStat(string statType, out FamiliarStatType parsedStatType)
    {
        parsedStatType = default;

        if (System.Enum.TryParse(statType, true, out parsedStatType))
        {
            return true;
        }
        else
        {
            parsedStatType = System.Enum.GetValues(typeof(FamiliarStatType))
                .Cast<FamiliarStatType>()
                .FirstOrDefault(pt => pt.ToString().Contains(statType, System.StringComparison.OrdinalIgnoreCase));

            if (!parsedStatType.Equals(default(FamiliarStatType)))
            {
                return true;
            }
        }

        return false;
    }
    public static void CallFamiliar(Entity playerCharacter, Entity familiar, User user, ulong steamId, (Entity Familiar, int FamKey) data)
    {
        if (familiar.IsDisabled()) familiar.Remove<Disabled>();
        if (familiar.Has<GetTranslationOnUpdate>()) familiar.Remove<GetTranslationOnUpdate>();

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
        // ResetAndDisableAggro(familiar);

        familiar.With((ref Follower follower) =>
        {
            follower.Followed._Value = Entity.Null;
        });

        familiar.AddWith((ref GetTranslationOnUpdate updateTranslation) =>
        {
            updateTranslation.Source = GetTranslationSource.Creator;
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
    public static void DisableAggro(Entity familiar)
    {
        if (familiar.Has<AggroConsumer>())
        {
            familiar.With((ref AggroConsumer aggroConsumer) =>
            {
                aggroConsumer.Active._Value = false;
            });
        }

        /*
        if (familiar.Has<Aggroable>())
        {
            familiar.With((ref Aggroable aggroable) =>
            {
                aggroable.Value._Value = false;
            });
        }
        */
    }
    public static void EnableAggro(Entity familiar)
    {
        if (familiar.Has<AggroConsumer>())
        {
            familiar.With((ref AggroConsumer aggroConsumer) =>
            {
                aggroConsumer.Active._Value = true;
            });
        }

        /*
        if (familiar.Has<Aggroable>())
        {
            familiar.With((ref Aggroable aggroable) =>
            {
                aggroable.Value._Value = true;
            });
        }
        */
    }
    public static void ResetAggro(Entity familiar)
    {
        if (!familiar.Has<AggroConsumer>()) return;
        else
        {
            familiar.TryApplyBuffWithLifeTimeDestroy(_disableAggroBuff, AGGRO_BUFF_DURATION);
        }
    }
    public static void ResetAndDisableAggro(Entity familiar)
    {
        if (!familiar.Has<AggroConsumer>()) return;
        else
        {
            familiar.TryApplyBuff(_disableAggroBuff);
        }
    }
    public static void BindFamiliar(User user, Entity playerCharacter, int boxIndex = -1)
    {
        ulong steamId = user.PlatformId;
        Entity familiar = GetActiveFamiliar(playerCharacter);

        if (familiar.Exists())
        {
            LocalizationService.HandleServerReply(EntityManager, user, "You already have an active familiar! Unbind that one first.");
            return;
        }
        else if (playerCharacter.HasBuff(_pveCombatBuff) || playerCharacter.HasBuff(_dominateBuff) || playerCharacter.HasBuff(_takeFlightBuff) || playerCharacter.HasBuff(_pvpCombatBuff))
        {
            LocalizationService.HandleServerReply(EntityManager, user, "You can't bind in combat or when using certain forms! (dominating presence, bat)");
            return;
        }

        string box = steamId.TryGetFamiliarBox(out box) ? box : string.Empty;

        if (string.IsNullOrEmpty(box))
        {
            LocalizationService.HandleServerReply(EntityManager, user, "No active box! Use '<color=white>.fam listboxes</color>' and select one with '<color=white>.fam cb [BoxName]</color>");
            return;
        }
        else if (steamId.TryGetFamiliarActives(out var data) && !data.Familiar.Exists() && data.FamKey.Equals(0) && LoadFamiliarUnlocksData(steamId).UnlockedFamiliars.TryGetValue(box, out var famKeys))
        {
            if (boxIndex == -1 && steamId.TryGetFamiliarBoxPreset(out boxIndex))
            {
                if (boxIndex < 1 || boxIndex > famKeys.Count)
                {
                    LocalizationService.HandleServerReply(EntityManager, user, $"Invalid index for active box, try binding or smartbind via command.");
                    return;
                }

                data = new(Entity.Null, famKeys[boxIndex - 1]);
                steamId.SetFamiliarActives(data);

                InstantiateFamiliar(user, playerCharacter, famKeys[boxIndex - 1]).Start();
            }
            else if (boxIndex == -1)
            {
                LocalizationService.HandleServerReply(EntityManager, user, $"Couldn't find binding preset, try binding or smartbind via command.");
                return;
            }
            else if (boxIndex < 1 || boxIndex > famKeys.Count)
            {
                LocalizationService.HandleServerReply(EntityManager, user, $"Invalid index, use <color=white>1</color>-<color=white>{famKeys.Count}</color>! (Active Box - <color=yellow>{box}</color>)");
                return;
            }
            else
            {
                steamId.SetFamiliarBoxPreset(boxIndex);

                data = new(Entity.Null, famKeys[boxIndex - 1]);
                steamId.SetFamiliarActives(data);

                InstantiateFamiliar(user, playerCharacter, famKeys[boxIndex - 1]).Start();
            }
        }
        else
        {
            LocalizationService.HandleServerReply(EntityManager, user, "Couldn't find familiar actives or familiar already active! If this doesn't seem right try using '<color=white>.fam reset</color>'.");
        }
    }
    public static void UnbindFamiliar(User user, Entity playerCharacter, bool smartBind = false, int index = -1)
    {
        Entity familiar = GetActiveFamiliar(playerCharacter);
        bool hasActive = user.PlatformId.TryGetFamiliarActives(out var actives) && !actives.FamKey.Equals(0);
        bool isDisabled = familiar.IsDisabled();

        if (hasActive && familiar.Exists() && !isDisabled)
        {
            familiar.TryApplyBuff(_vanishBuff);
            familiar.TryApplyBuff(_disableAggroBuff);

            UnbindFamiliarDelayRoutine(user, playerCharacter, familiar, smartBind, index).Start();
        }
        else if (isDisabled)
        {
            LocalizationService.HandleServerReply(EntityManager, user, "Can't unbind familiar when dismissed!");
        }
        else
        {
            LocalizationService.HandleServerReply(EntityManager, user, "Couldn't find familiar to unbind, if this doesn't seem right try using '<color=white>.fam reset</color>'.");
        }
    }
    static IEnumerator UnbindFamiliarDelayRoutine(User user, Entity playerCharacter, Entity familiar,
        bool smartBind = false, int index = -1)
    {
        yield return _delay;

        PrefabGUID prefabGuid = familiar.GetPrefabGuid();
        if (prefabGuid.IsEmpty())
        {
            yield break;
        }

        ulong steamId = user.PlatformId;
        int famKey = prefabGuid.GuidHash;

        FamiliarBuffsData buffsData = LoadFamiliarBuffsData(steamId);
        string shinyHexColor = "";

        if (buffsData.FamiliarBuffs.ContainsKey(famKey))
        {
            if (ShinyBuffColorHexMap.TryGetValue(new(buffsData.FamiliarBuffs[famKey].First()), out var hexColor))
            {
                shinyHexColor = $"<color={hexColor}>";
            }
        }

        HandleFamiliarMinions(familiar);
        // FamiliarEquipmentManager.SaveFamiliarEquipment(steamId, famKey, UnequipFamiliar(familiar));

        if (familiar.Has<Disabled>()) familiar.Remove<Disabled>();
        if (AutoCallMap.ContainsKey(playerCharacter)) AutoCallMap.TryRemove(playerCharacter, out var _);

        familiar.Destroy();
        ClearFamiliarActives(steamId);

        string message = !string.IsNullOrEmpty(shinyHexColor) ? $"<color=green>{prefabGuid.GetLocalizedName()}</color>{shinyHexColor}*</color> <color=#FFC0CB>unbound</color>!" : $"<color=green>{prefabGuid.GetLocalizedName()}</color> <color=#FFC0CB>unbound</color>!";
        LocalizationService.HandleServerReply(EntityManager, user, message);

        if (smartBind)
        {
            yield return _bindingDelay;

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
    } // should try using the native utility for this by ref for the aggroBuffer but also laterrrrrr
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

        if (familiar.TryApplyBuff(_defaultEmoteBuff) && familiar.TryGetBuff(_defaultEmoteBuff, out Entity buffEntity))
        {
            buffEntity.With((ref EntityOwner entityOwner) =>
            {
                entityOwner.Owner = target;
            });
        }
    }
    public static bool EligibleForCombat(this Entity familiar)
    {
        return familiar.Exists() && !familiar.IsDisabled() && !familiar.HasBuff(_invulnerableBuff);
    }
    public static void BuildBattleGroupDetailsReply(ulong steamId, FamiliarBuffsData buffsData, FamiliarPrestigeData_V2 prestigeData, List<int> battleGroup, ref List<string> familiars)
    {
        foreach (int famKey in battleGroup)
        {
            if (famKey == 0) continue;

            PrefabGUID famPrefab = new(famKey);
            string famName = famPrefab.GetLocalizedName();
            string colorCode = "<color=#FF69B4>"; // Default color for the asterisk

            int level = Systems.Familiars.FamiliarLevelingSystem.GetFamiliarExperience(steamId, famKey).Key;
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
                SaveFamiliarPrestigeData_V2(steamId, prestigeData);
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
        if (battleGroup.Any(x => x != 0))
        {
            FamiliarBuffsData buffsData = LoadFamiliarBuffsData(steamId);
            FamiliarPrestigeData_V2 prestigeData = LoadFamiliarPrestigeData_V2(steamId);
            List<string> familiars = [];

            BuildBattleGroupDetailsReply(steamId, buffsData, prestigeData, battleGroup, ref familiars);

            string familiarReply = string.Join(", ", familiars);
            LocalizationService.HandleReply(ctx, $"Battle Group - {familiarReply}");
            return;
        }
        else
        {
            LocalizationService.HandleReply(ctx, "No familiars added to battle group!");
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

        FamiliarBuffsData buffsData = LoadFamiliarBuffsData(steamId);
        FamiliarPrestigeData_V2 prestigeData = LoadFamiliarPrestigeData_V2(steamId);

        int level = Systems.Familiars.FamiliarLevelingSystem.GetFamiliarExperience(steamId, actives.famKey).Key;
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
            SaveFamiliarPrestigeData_V2(steamId, prestigeData);
        }
        else
        {
            prestiges = prestigeData.FamiliarPrestige[actives.famKey].Key;
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
    public static void HandleFamiliarPrestige(ChatCommandContext ctx, string statType, int clampedCost, int levels = 0) // need to replace first block in command with this method but laterrrr
    {
        Entity playerCharacter = ctx.Event.SenderCharacterEntity;
        User user = ctx.User;

        ulong steamId = user.PlatformId;

        if (!steamId.TryGetFamiliarActives(out var data)) // check if familiar is active
        {
            LocalizationService.HandleReply(ctx, "Couldn't find active familiar!");
            return;
        }

        FamiliarExperienceData xpData = LoadFamiliarExperienceData(steamId);
        FamiliarPrestigeData_V2 prestigeData = LoadFamiliarPrestigeData_V2(steamId);

        if (!prestigeData.FamiliarPrestige.ContainsKey(data.FamKey))
        {
            prestigeData.FamiliarPrestige[data.FamKey] = new(0, []);
            SaveFamiliarPrestigeData_V2(steamId, prestigeData);
        }

        prestigeData = LoadFamiliarPrestigeData_V2(steamId);
        List<int> stats = prestigeData.FamiliarPrestige[data.FamKey].Value;

        if (prestigeData.FamiliarPrestige[data.FamKey].Key >= ConfigService.MaxFamiliarPrestiges)
        {
            LocalizationService.HandleReply(ctx, $"Your familiar has already prestiged the maximum number of times! (<color=white>{ConfigService.MaxFamiliarPrestiges}</color>)");
            return;
        }

        int value = -1;

        if (stats.Count < FamiliarPrestigeStats.Count) // if less than max stats, parse entry and add if set doesnt already contain
        {
            if (int.TryParse(statType, out value))
            {
                int length = FamiliarPrestigeStats.Count;

                if (value < 1 || value > length)
                {
                    LocalizationService.HandleReply(ctx, $"Invalid familiar prestige stat type, use '<color=white>.fam lst</color>' to see options.");
                    return;
                }

                --value;

                if (!stats.Contains(value))
                {
                    stats.Add(value);
                }
                else
                {
                    LocalizationService.HandleReply(ctx, $"Familiar already has <color=#00FFFF>{FamiliarPrestigeStats[value]}</color> (<color=yellow>{value + 1}</color>) from prestiging, use '<color=white>.fam lst</color>' to see options.");
                    return;
                }
            }
            else
            {
                LocalizationService.HandleReply(ctx, $"Invalid familiar prestige stat, use '<color=white>.fam lst</color>' to see options.");
                return;
            }
        }
        else if (stats.Count >= FamiliarPrestigeStats.Count && !string.IsNullOrEmpty(statType))
        {
            LocalizationService.HandleReply(ctx, "Familiar already has all prestige stats! ('<color=white>.fam pr</color>' instead of '<color=white>.fam pr [PrestigeStat]</color>')");
            return;
        }

        int levelsNeeded = ConfigService.MaxFamiliarLevel - xpData.FamiliarExperience[data.FamKey].Key;
        int levelsToAdd = levels - levelsNeeded;

        if (ServerGameManager.TryRemoveInventoryItem(playerCharacter, _itemSchematic, clampedCost))
        {
            KeyValuePair<int, float> newXP = new(++levelsToAdd, Progression.ConvertLevelToXp(++levelsToAdd)); // reset level to 1
            xpData.FamiliarExperience[data.FamKey] = newXP;
            SaveFamiliarExperienceData(steamId, xpData);

            int prestigeLevel = prestigeData.FamiliarPrestige[data.FamKey].Key + 1;
            prestigeData.FamiliarPrestige[data.FamKey] = new(prestigeLevel, stats);
            SaveFamiliarPrestigeData_V2(steamId, prestigeData);

            Entity familiar = GetActiveFamiliar(playerCharacter);

            ModifyUnitStats(familiar, newXP.Key, steamId, data.FamKey);

            if (value == -1)
            {
                LocalizationService.HandleReply(ctx, $"Your familiar has prestiged [<color=#90EE90>{prestigeLevel}</color>] and is now level <color=white>{newXP.Key}</color>!");
            }
            else
            {
                LocalizationService.HandleReply(ctx, $"Your familiar has prestiged [<color=#90EE90>{prestigeLevel}</color>] and is now level <color=white>{newXP.Key}</color>! (+<color=#00FFFF>{FamiliarPrestigeStats[value]}</color>)");
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Failed to remove schematics from your inventory! This shouldn't happen at this point and you may want to inform the developer.");
        }
    }
    public static List<int> UnequipFamiliar(Entity familiar)
    {
        if (FamiliarServantMap.TryRemove(familiar, out Entity servant) && servant.TryGetComponent(out ServantEquipment servantEquipment))
        {
            List<int> familiarEquipment = [];

            foreach (FamiliarEquipmentType familiarEquipmentType in System.Enum.GetValues(typeof(FamiliarEquipmentType)))
            {
                if (System.Enum.TryParse(familiarEquipmentType.ToString(), true, out EquipmentType equipmentType) && servantEquipment.IsEquipped(equipmentType))
                {
                    PrefabGUID equipmentPrefabGuid = servantEquipment.GetEquipmentItemId(equipmentType);
                    familiarEquipment.Add(equipmentPrefabGuid.GuidHash);
                    Core.Log.LogInfo($"Unequipped familiar equipment - {equipmentPrefabGuid.GetPrefabName()}");
                }
                else
                {
                    familiarEquipment.Add(0);
                    Core.Log.LogInfo($"No equipment matching type - {equipmentType}");
                }
            }

            servant.Destroy();
            return familiarEquipment;
        }

        return [0, 0, 0, 0, 0, 0, 0];
    }
    public static void EquipFamiliar(ulong steamId, Entity playerCharacter, int famKey, Entity servant)
    {
        // Entity servant = GetFamiliarServant(familiar);
        if (!servant.TryGetComponent(out ServantEquipment servantEquipment)) return;

        List<PrefabGUID> familiarEquipment = FamiliarEquipmentManager.GetFamiliarEquipment(steamId, famKey).Select(item => new PrefabGUID(item)).ToList();

        FromCharacter fromCharacter = new()
        {
            Character = playerCharacter,
            User = playerCharacter.GetUserEntity()
        };

        NetworkEventType networkEventType = new()
        {
            EventId = NetworkEvents.EventId_EquipServantItemEvent,
            IsAdminEvent = false,
            IsDebugEvent = false
        };

        for (int i = 0; i < familiarEquipment.Count; i++)
        {
            if (familiarEquipment[i].HasValue())
            {
                FamiliarEquipmentType familiarEquipmentType = (FamiliarEquipmentType)i;
                Core.Log.LogInfo($"Equipping familiar equipment - {familiarEquipment[i].GetPrefabName()}");

                if (System.Enum.TryParse(familiarEquipmentType.ToString(), true, out EquipmentType equipmentType))
                {
                    // var itemDataMap = GameDataSystem.ItemHashLookupMap;
                    // Entity itemEntity = InventoryUtilitiesServer.CreateInventoryItemEntity(EntityManager, itemDataMap, familiarEquipment[i]);
                    AddItemResponse addItemResponse = ServerGameManager.TryAddInventoryItem(servant, familiarEquipment[i], 1);
                    // Nullable_Unboxed<EntityManager> entityManager = Nullable_Unboxed<EntityManager>.Unbox(EntityManager.BoxIl2CppObject());

                    // if (addItemResponse.Success) servantEquipment.CreateItemEquippedEvent(entityManager, servant, equipmentType, EquipmentChangedEventType.Equipped, addItemResponse.NewEntity, familiarEquipment[i]);
                    if (addItemResponse.Success) // NetworkEvents.EventId_EquipServantItemFromInventoryEvent
                    {
                        // [Info   :Bloodcraft]  All: ProjectM.Network.FromCharacter,ProjectM.Network.EquipServantItemFromInventoryEvent
                        int slotIndex = InventoryUtilities.TryGetItemSlot(EntityManager, servant, addItemResponse.NewEntity, out int slotId) ? slotId : -1;

                        Entity entity = Core.EntityManager.CreateEntity(
                        [
                            ComponentType.ReadOnly<NetworkEventType>(),
                            ComponentType.ReadOnly<FromCharacter>(),
                            ComponentType.ReadOnly<EquipServantItemEvent>()
                        ]);

                        EquipServantItemEvent equipServantItemEvent = new()
                        {
                            SlotIndex = slotIndex,
                            ToEntity = servant.GetNetworkId(),
                        };

                        entity.Write(fromCharacter);
                        entity.Write(networkEventType);
                        entity.Write(equipServantItemEvent);

                        Core.Log.LogInfo($"Slot equipped - {slotIndex}");
                    }
                    else
                    {
                        Core.Log.LogInfo($"Failed to equip familiar, addItemResponse not a success! {familiarEquipment[i]} | {addItemResponse.Result}");
                    }
                }
                else
                {
                    Core.Log.LogInfo($"Failed to equip familiar, equipment type not found! {familiarEquipmentType}");
                }
            }
        }

        servant.Add<Disabled>();
    }
    public static IEnumerator HandleFamiliarShapeshiftRoutine(User user, Entity playerCharacter, Entity familiar)
    {
        yield return _delay;

        try
        {
            HandleModifications(user, playerCharacter, familiar);
        }
        catch (System.Exception ex)
        {
            Core.Log.LogWarning(ex);
        }
    }
    public static void HandleFamiliarCastleMan(Entity buffEntity)
    {
        buffEntity.Remove<ScriptSpawn>();
        buffEntity.Remove<ScriptUpdate>();
        buffEntity.Remove<ScriptDestroy>();
        buffEntity.Remove<Script_Buff_ModifyDynamicCollision_DataServer>();
        buffEntity.Remove<Script_Castleman_AdaptLevel_DataShared>();
    }
}
