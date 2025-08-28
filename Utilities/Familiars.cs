using Bloodcraft.Patches;
using Bloodcraft.Resources;
using Bloodcraft.Services;
using ProjectM;
using ProjectM.Behaviours;
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
using static Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarBuffsManager;
using static Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarEquipmentManager;
using static Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarExperienceManager;
using static Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarPrestigeManager;
using static Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarUnlocksManager;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Systems.Familiars.FamiliarBindingSystem;
using static Bloodcraft.Systems.Familiars.FamiliarUnlockSystem;
using static Bloodcraft.Utilities.Familiars.ActiveFamiliarManager;
using static Bloodcraft.Utilities.Misc.PlayerBoolsManager;

namespace Bloodcraft.Utilities;
internal static class Familiars
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;

    static readonly bool _familiarCombat = ConfigService.FamiliarCombat;

    static readonly WaitForSeconds _bindingDelay = new(0.25f);
    static readonly WaitForSeconds _delay = new(2f);

    static readonly PrefabGUID _bonusStatsBuff = Buffs.BonusPlayerStatsBuff;
    static readonly PrefabGUID _defaultEmoteBuff = Buffs.DefaultEmoteBuff;
    static readonly PrefabGUID _pveCombatBuff = Buffs.PvECombatBuff;
    static readonly PrefabGUID _pvpCombatBuff = Buffs.PvPCombatBuff;
    static readonly PrefabGUID _dominateBuff = Buffs.DominateBuff;
    static readonly PrefabGUID _takeFlightBuff = Buffs.TakeFlightBuff;
    static readonly PrefabGUID _inkCrawlerDeathBuff = Buffs.InkCrawlerDeathBuff;
    static readonly PrefabGUID _invulnerableBuff = Buffs.AdminInvulnerableBuff;
    static readonly PrefabGUID _disableAggroBuff = Buffs.DisableAggroBuff;
    static readonly PrefabGUID _vanishBuff = Buffs.VanishBuff;
    static readonly PrefabGUID _interactModeBuff = Buffs.InteractModeBuff;

    static readonly PrefabGUID _spiritDouble = PrefabGUIDs.CHAR_Cursed_MountainBeast_SpiritDouble;
    static readonly PrefabGUID _highlordGroundSword = PrefabGUIDs.CHAR_Legion_HighLord_GroundSword;
    static readonly PrefabGUID _enchantedCross = PrefabGUIDs.CHAR_ChurchOfLight_EnchantedCross;

    static readonly PrefabGUID _itemSchematic = PrefabGUIDs.Item_Ingredient_Research_Schematic;

    static readonly float3 _southFloat3 = new(0f, 0f, -1f);

    const float BLOOD_QUALITY_IGNORE = 90f;
    public enum FamiliarEquipmentType
    {
        Chest,
        Weapon,
        MagicSource,
        Footgear,
        Legs,
        Gloves
    }

    static readonly Dictionary<FamiliarEquipmentType, EquipmentType> _familiarEquipmentMap = new()
    {
        { FamiliarEquipmentType.Chest, EquipmentType.Chest },
        { FamiliarEquipmentType.Weapon, EquipmentType.Weapon },
        { FamiliarEquipmentType.MagicSource, EquipmentType.MagicSource },
        { FamiliarEquipmentType.Footgear, EquipmentType.Footgear },
        { FamiliarEquipmentType.Legs, EquipmentType.Legs },
        { FamiliarEquipmentType.Gloves, EquipmentType.Gloves }
    };
    public static IReadOnlyDictionary<FamiliarEquipmentType, EquipmentType> FamiliarEquipmentMap => _familiarEquipmentMap;
    public class ActiveFamiliarData
    {
        public Entity Familiar { get; set; } = Entity.Null;
        public Entity Servant { get; set; } = Entity.Null;
        public int FamiliarId { get; set; } = 0;
        public bool Dismissed { get; set; } = false;
        public bool IsBinding { get; set; } = false;
    }
    public static class ActiveFamiliarManager
    {
        static readonly ConcurrentDictionary<ulong, ActiveFamiliarData> _familiarActives = [];
        public static IReadOnlyDictionary<ulong, ActiveFamiliarData> ActiveFamiliars => _familiarActives;
        public static ActiveFamiliarData GetActiveFamiliarData(ulong steamId)
        {
            if (!_familiarActives.TryGetValue(steamId, out var activeData))
            {
                activeData = CreateActiveFamiliarData(steamId);
            }

            return activeData;
        }
        public static void UpdateActiveFamiliarData(ulong steamId, Entity familiar, Entity servant, int familiarId, bool isDismissed = false)
        {
            ActiveFamiliarData data = new()
            {
                Familiar = familiar,
                Servant = servant,
                FamiliarId = familiarId,
                Dismissed = isDismissed
            };

            _familiarActives[steamId] = data;
        }
        static ActiveFamiliarData CreateActiveFamiliarData(ulong steamId)
        {
            var data = new ActiveFamiliarData();

            if (steamId.TryGetPlayerInfo(out PlayerInfo playerInfo))
            {
                Entity familiar = FindActiveFamiliar(playerInfo.CharEntity);
                Entity servant = FindFamiliarServant(familiar);

                data.Familiar = familiar;
                data.Servant = servant;
                data.FamiliarId = familiar.GetPrefabGuid().GuidHash;
            }

            _familiarActives[steamId] = data;
            return data;
        }
        public static void UpdateActiveFamiliarDismissed(ulong steamId, bool dismissed)
        {
            if (_familiarActives.TryGetValue(steamId, out var data))
            {
                data.Dismissed = dismissed;
                _familiarActives[steamId] = data;
            }
        }
        public static void UpdateActiveFamiliarBinding(ulong steamId, bool isBinding)
        {
            if (_familiarActives.TryGetValue(steamId, out var data))
            {
                data.IsBinding = isBinding;
                _familiarActives[steamId] = data;
            }
        }
        public static bool IsBinding(ulong steamId)
        {
            return _familiarActives.TryGetValue(steamId, out var data) && data.IsBinding;
        }
        public static bool HasActiveFamiliar(ulong steamId)
        {
            return _familiarActives.TryGetValue(steamId, out var data)
                && data.Familiar.Exists();
        }
        public static bool HasDismissedFamiliar(ulong steamId)
        {
            return _familiarActives.TryGetValue(steamId, out var data)
                && data.Familiar.Exists() && data.Dismissed;
        }
        public static void ResetActiveFamiliarData(ulong steamId)
        {
            _familiarActives[steamId] = new();
        }
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
        { "General Elena the Hollow", PrefabGUIDs.CHAR_Vampire_IceRanger_VBlood},
        { "Willfred the Village Elder", PrefabGUIDs.CHAR_WerewolfChieftain_Human},
        { "Sir Erwin the Gallant Cavalier", PrefabGUIDs.CHAR_Militia_Fabian_VBlood},
        { "Gaius the Cursed Champion", PrefabGUIDs.CHAR_Undead_ArenaChampion_VBlood},
        { "Stavros the Carver", PrefabGUIDs.CHAR_Blackfang_CarverBoss_VBlood},
        { "Dantos the Forgebinder", PrefabGUIDs.CHAR_Blackfang_Valyr_VBlood},
        { "Lucile the Venom Alchemist", PrefabGUIDs.CHAR_Blackfang_Lucie_VBlood},
        { "Jakira the Shadow Huntress", PrefabGUIDs.CHAR_Blackfang_Livith_VBlood},
        { "Megara the Serpent Queen", PrefabGUIDs.CHAR_Blackfang_Morgana_VBlood}
    };

    public static readonly ConcurrentDictionary<Entity, Entity> AutoCallMap = [];
    public static bool HasActiveFamiliar(this ulong steamId)
    {
        return ActiveFamiliarManager.HasActiveFamiliar(steamId);
    }
    public static bool HasDismissedFamiliar(this ulong steamId)
    {
        return ActiveFamiliarManager.HasDismissedFamiliar(steamId);
    }
    public static bool IsBinding(this ulong steamId)
    {
        return ActiveFamiliarManager.IsBinding(steamId);
    }
    public static Entity FindActiveFamiliar(Entity playerCharacter)
    {
        if (playerCharacter.TryGetBuffer<FollowerBuffer>(out var followers) && !followers.IsEmpty)
        {
            foreach (FollowerBuffer follower in followers)
            {
                Entity familiar = follower.Entity._Entity;
                if (familiar.Has<BlockFeedBuff>()) return familiar;
            }
        }

        return Entity.Null;
    }
    public static Entity FindFamiliarServant(Entity familiar)
    {
        if (familiar.TryGetBuffer<FollowerBuffer>(out var followers) && !followers.IsEmpty)
        {
            foreach (FollowerBuffer follower in followers)
            {
                Entity servant = follower.Entity._Entity;
                if (servant.Has<BlockFeedBuff>()) return servant;
            }
        }

        return Entity.Null;
    }
    public static Entity GetActiveFamiliar(Entity playerCharacter)
    {
        // ActiveFamiliarData activeFamiliarData;
        // if (!steamId.Equals(default)) activeFamiliarData = GetActiveFamiliarData(steamId);
        // else activeFamiliarData = GetActiveFamiliarData(playerCharacter.GetSteamId());
        ActiveFamiliarData activeFamiliarData = GetActiveFamiliarData(playerCharacter.GetSteamId());
        return activeFamiliarData.Familiar;
    }
    public static Entity GetFamiliarServant(Entity playerCharacter)
    {
        // ActiveFamiliarData activeFamiliarData;
        // if (!steamId.Equals(default)) activeFamiliarData = GetActiveFamiliarData(steamId);
        // else activeFamiliarData = GetActiveFamiliarData(playerCharacter.GetSteamId());
        ActiveFamiliarData activeFamiliarData = GetActiveFamiliarData(playerCharacter.GetSteamId());
        return activeFamiliarData.Servant;
    }
    public static Entity GetServantFamiliar(Entity servant)
    {
        if (servant.TryGetComponent(out Follower follower))
        {
            Entity familiar = follower.Followed._Value;
            if (familiar.Has<BlockFeedBuff>()) return familiar;
        }

        return Entity.Null;
    }
    public static Entity GetServantCoffin(Entity servant)
    {
        Entity coffin = Entity.Null;

        if (!servant.TryGetComponent(out ServantConnectedCoffin servantConnectedCoffin)) return coffin;
        else coffin = servantConnectedCoffin.CoffinEntity.GetEntityOnServer();

        return coffin;
    }
    public static void SyncFamiliarServant(Entity familiar, Entity servant)
    {
        float familiarHealth = familiar.GetMaxHealth();
        int familiarLevel = familiar.GetUnitLevel();
        (float physicalPower, float spellPower) = familiar.GetPowerTuple();

        servant.With((ref Health health) =>
        {
            health.MaxHealth._Value = familiarHealth;
            health.Value = familiarHealth;
        });

        servant.With((ref ServantPower servantPower) =>
        {
            servantPower.GearLevel = familiarLevel;
            servantPower.Power = physicalPower;
            servantPower.Expertise = 0f;
        });
    }
    public static IEnumerator FamiliarSyncDelayRoutine(Entity familiar, Entity servant)
    {
        yield return _bindingDelay;

        if (!familiar.Exists() || !servant.Exists()) yield break;

        float familiarHealth = familiar.GetMaxHealth();
        int familiarLevel = familiar.GetUnitLevel();
        (float physicalPower, float spellPower) = familiar.GetPowerTuple();

        servant.With((ref Health health) =>
        {
            health.MaxHealth._Value = familiarHealth;
            health.Value = familiarHealth;
        });

        servant.With((ref ServantPower servantPower) =>
        {
            servantPower.GearLevel = familiarLevel;
            servantPower.Power = physicalPower;
            servantPower.Expertise = 0f;
        });
    }
    public static void HandleFamiliarMinions(Entity familiar)
    {
        if (FamiliarMinions.TryRemove(familiar, out HashSet<Entity> familiarMinions))
        {
            foreach (Entity minion in familiarMinions)
            {
                minion.Destroy();
            }
        }
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

            data.FamiliarUnlocks[activeBox].Add(prefabHash);
            SaveFamiliarUnlocksData(steamId, data);

            LocalizationService.HandleReply(ctx, $"<color=green>{new PrefabGUID(prefabHash).GetLocalizedName()}</color> added to <color=white>{activeBox}</color>.");
        }
        else if (unit.StartsWith("char", StringComparison.CurrentCultureIgnoreCase)) // search for full and/or partial name match
        {
            // Try using TryGetValue for an exact match (case-sensitive)
            if (!PrefabCollectionSystem.SpawnableNameToPrefabGuidDictionary.TryGetValue(unit, out PrefabGUID match))
            {
                // If exact match is not found, do a case-insensitive search for full or partial matches
                foreach (var kvp in LocalizationService.PrefabGuidNames)
                {
                    // Check for a case-insensitive full match
                    if (kvp.Value.Equals(unit, System.StringComparison.CurrentCultureIgnoreCase))
                    {
                        match = kvp.Key; // Full match found
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

                data.FamiliarUnlocks[activeBox].Add(match.GuidHash);
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
    public static void TryReturnFamiliar(Entity playerCharacter, Entity familiar)
    {
        float3 playerPosition = playerCharacter.GetPosition();
        float distance = Vector3.Distance(familiar.GetPosition(), playerPosition);

        if (distance >= 25f)
        {
            PreventDisabled(familiar);
            ReturnFamiliar(playerPosition, familiar);
        }

        /*
        if (familiar.ShouldRelocate())
        {
            Core.Log.LogWarning($"Familiar {familiar.GetPrefabGuid().GetLocalizedName()} stuck, relocating!");
            PreventDisableFamiliar(familiar);
            ReturnFamiliar(playerPosition, familiar);
        }
        */
    }
    static bool ShouldRelocate(this Entity familiar)
    {
        if (familiar.TryGetComponent(out BehaviourTreeState behaviourTreeState) && behaviourTreeState.Value.Equals(GenericEnemyState.Relocate_Unstuck))
        {
            return true;
        }

        return false;
    }
    public static void SetPreCombatPosition(Entity playerCharacter, Entity familiar)
    {
        familiar.With((ref AggroConsumer aggroConsumer) => aggroConsumer.PreCombatPosition = playerCharacter.GetPosition());
    }
    public static void HandleFamiliarEnteringCombat(Entity playerCharacter, Entity familiar)
    {
        if (familiar.HasBuff(_interactModeBuff))
        {
            User user = playerCharacter.GetUser();
            ulong steamId = user.PlatformId;

            EmoteSystemPatch.InteractMode(user, playerCharacter, steamId);
        }

        familiar.With((ref Follower follower) => follower.ModeModifiable._Value = 1);

        SetPreCombatPosition(playerCharacter, familiar);
        TryReturnFamiliar(playerCharacter, familiar);
    }
    public static void ReturnFamiliar(float3 position, Entity familiar)
    {
        familiar.With((ref LastTranslation lastTranslation) => lastTranslation.Value = position);

        familiar.With((ref Translation translation) => translation.Value = position);

        familiar.With((ref AggroConsumer aggroConsumer) => aggroConsumer.PreCombatPosition = position);
    }

    // mmm these seem redundant? note to remove or otherwise rethink
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
    public static void CallFamiliar(Entity playerCharacter, Entity familiar, User user, ulong steamId)
    {
        familiar.Remove<Disabled>();
        PreventDisabled(familiar);

        float3 position = playerCharacter.GetPosition();
        ReturnFamiliar(position, familiar);

        familiar.With((ref Follower follower) =>
        {
            follower.Followed._Value = playerCharacter;
            follower.ModeModifiable._Value = 0; // leash until combat again, if still in combat see if this works to clear previous target? seems maybe to be doing that, noting for further... uh, notes
        });

        if (_familiarCombat && !familiar.HasBuff(_invulnerableBuff))
        {
            familiar.TryRemoveBuff(buffPrefabGuid: _disableAggroBuff);
        }

        UpdateActiveFamiliarDismissed(steamId, false);

        string message = "<color=yellow>Familiar</color> <color=green>enabled</color>!";
        LocalizationService.HandleServerReply(EntityManager, user, message);
    }
    public static void DismissFamiliar(Entity playerCharacter, Entity familiar, User user, ulong steamId)
    {
        if (familiar.HasBuff(_vanishBuff))
        {
            LocalizationService.HandleServerReply(EntityManager, user, "Can't dismiss familiar when binding/unbinding!");
            return;
        }

        HandleFamiliarMinions(familiar);

        familiar.With((ref Follower follower) => follower.Followed._Value = Entity.Null);

        var buffer = playerCharacter.ReadBuffer<FollowerBuffer>();
        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i].Entity._Entity.Equals(familiar))
            {
                buffer.RemoveAt(i);
                break;
            }
        }

        PreventDisabled(familiar);
        familiar.Add<Disabled>();

        UpdateActiveFamiliarDismissed(steamId, true);

        string message = "<color=yellow>Familiar</color> <color=red>disabled</color>!";
        LocalizationService.HandleServerReply(EntityManager, user, message);
    }
    public static string GetShinyInfo(FamiliarBuffsData buffsData, Entity familiar, int familiarId)
    {
        if (buffsData.FamiliarBuffs.ContainsKey(familiarId))
        {
            PrefabGUID shinyBuff = new(buffsData.FamiliarBuffs[familiarId].FirstOrDefault());

            if (ShinyBuffSpellSchools.TryGetValue(shinyBuff, out string spellSchool) && familiar.TryGetBuffStacks(shinyBuff, out Entity _, out int stacks))
            {
                return $"{spellSchool}[<color=white>{stacks}</color>]";
            }
        }

        return string.Empty;
    }
    public static void NothingLivesForever(this Entity unit, float duration = FAMILIAR_LIFETIME)
    {
        if (unit.TryApplyAndGetBuff(_inkCrawlerDeathBuff, out Entity buffEntity))
        {
            buffEntity.With((ref LifeTime lifeTime) => lifeTime.Duration = duration);

            PrefabGUID unitPrefabGuid = unit.GetPrefabGuid();

            if ((unitPrefabGuid.Equals(_spiritDouble) || unitPrefabGuid.Equals(_highlordGroundSword)) && unit.Has<Immortal>())
            {
                unit.With((ref Immortal immortal) => immortal.IsImmortal = false);
            }
        }
    }
    public static void DisableAggro(Entity familiar)
    {
        if (familiar.Has<AggroConsumer>())
        {
            familiar.With((ref AggroConsumer aggroConsumer) => aggroConsumer.Active._Value = false);
        }
    }
    public static void EnableAggro(Entity familiar)
    {
        if (familiar.Has<AggroConsumer>())
        {
            familiar.With((ref AggroConsumer aggroConsumer) => aggroConsumer.Active._Value = true);
        }
    }
    public static void EnableAggroable(this Entity entity)
    {
        if (entity.Has<Aggroable>())
        {
            entity.With((ref Aggroable aggroable) =>
            {
                aggroable.Value._Value = true;
                aggroable.DistanceFactor._Value = 1f;
                aggroable.AggroFactor._Value = 1f;
            });
        }
    }
    public static void DisableAggroable(this Entity entity)
    {
        if (entity.Has<Aggroable>())
        {
            entity.With((ref Aggroable aggroable) =>
            {
                aggroable.Value._Value = false;
                aggroable.DistanceFactor._Value = 0f;
                aggroable.AggroFactor._Value = 0f;
            });
        }
    }
    public static void BindFamiliar(User user, Entity playerCharacter, int boxIndex = -1)
    {
        ulong steamId = user.PlatformId;
        if (steamId.IsBinding())
        {
            LocalizationService.HandleServerReply(EntityManager, user, "Familiar binding already in progress!");
            return;
        }
        bool hasActive = steamId.HasActiveFamiliar();

        if (hasActive)
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
            LocalizationService.HandleServerReply(EntityManager, user, "Couldn't find active box! Use '<color=white>.fam listboxes</color>' and select one with '<color=white>.fam cb [BoxName]</color>");
            return;
        }
        else if (!hasActive && LoadFamiliarUnlocksData(steamId).FamiliarUnlocks.TryGetValue(box, out var famKeys))
        {
            if (boxIndex == -1 && steamId.TryGetBindingIndex(out boxIndex))
            {
                if (boxIndex < 1 || boxIndex > famKeys.Count)
                {
                    LocalizationService.HandleServerReply(EntityManager, user, $"Invalid index for active box, try binding or smartbind via command.");
                    return;
                }

                ActiveFamiliarManager.UpdateActiveFamiliarBinding(steamId, true);
                InstantiateFamiliarRoutine(user, playerCharacter, famKeys[boxIndex - 1]).Start();
            }
            else if (boxIndex == -1)
            {
                LocalizationService.HandleServerReply(EntityManager, user, $"Couldn't find binding preset, try binding or smartbind via command.");
            }
            else if (boxIndex < 1 || boxIndex > famKeys.Count)
            {
                LocalizationService.HandleServerReply(EntityManager, user, $"Invalid index, use <color=white>1</color>-<color=white>{famKeys.Count}</color>! (Active Box - <color=yellow>{box}</color>)");
            }
            else
            {
                steamId.SetBindingIndex(boxIndex);
                ActiveFamiliarManager.UpdateActiveFamiliarBinding(steamId, true);
                InstantiateFamiliarRoutine(user, playerCharacter, famKeys[boxIndex - 1]).Start();
            }
        }
        else
        {
            LocalizationService.HandleServerReply(EntityManager, user, "Couldn't find familiar actives or familiar already active! If this doesn't seem right try using '<color=white>.fam reset</color>'.");
        }
    }
    public static void UnbindFamiliar(User user, Entity playerCharacter, bool smartBind = false, int index = -1)
    {
        ulong steamId = user.PlatformId;

        if (steamId.IsBinding())
        {
            LocalizationService.HandleServerReply(EntityManager, user, "Cannot unbind while a binding is in progress!");
            return;
        }

        bool hasActive = steamId.HasActiveFamiliar();
        bool isDismissed = steamId.HasDismissedFamiliar();

        if (hasActive && !isDismissed)
        {
            Entity familiar = GetActiveFamiliar(playerCharacter);

            if (familiar.HasBuff(_interactModeBuff))
            {
                LocalizationService.HandleServerReply(EntityManager, user, "Can't unbind familiar right now! (interacting)");
                return;
            }

            familiar.TryApplyBuff(_vanishBuff);
            familiar.TryApplyBuff(_disableAggroBuff);
            familiar.TryRemoveBuff(buffPrefabGuid: _bonusStatsBuff);

            ActiveFamiliarManager.UpdateActiveFamiliarBinding(steamId, true);
            UnbindFamiliarDelayRoutine(user, playerCharacter, familiar, smartBind, index).Start();
        }
        else if (isDismissed)
        {
            LocalizationService.HandleServerReply(EntityManager, user, "Can't unbind familiar right now! (dismissed)");
        }
        else
        {
            LocalizationService.HandleServerReply(EntityManager, user, "Couldn't find familiar to unbind! If this doesn't seem right try using '<color=white>.fam reset</color>'.");
        }
    }
    static IEnumerator UnbindFamiliarDelayRoutine(User user, Entity playerCharacter, Entity familiar,
        bool smartBind = false, int index = -1)
    {
        yield return _delay;

        PrefabGUID prefabGuid = familiar.GetPrefabGuid();
        ulong steamId = user.PlatformId;
        if (prefabGuid.IsEmpty())
        {
            ActiveFamiliarManager.UpdateActiveFamiliarBinding(steamId, false);
            yield break;
        }

        int famKey = prefabGuid.GuidHash;

        FamiliarBuffsData buffsData = LoadFamiliarBuffsData(steamId);
        string shinyHexColor = "";

        if (buffsData.FamiliarBuffs.ContainsKey(famKey))
        {
            if (ShinyBuffColorHexes.TryGetValue(new(buffsData.FamiliarBuffs[famKey].First()), out var hexColor))
            {
                shinyHexColor = $"<color={hexColor}>";
            }
        }

        HandleFamiliarMinions(familiar);
        SaveFamiliarEquipment(steamId, famKey, UnequipFamiliar(playerCharacter));

        familiar.Remove<Disabled>();
        if (AutoCallMap.ContainsKey(playerCharacter)) AutoCallMap.TryRemove(playerCharacter, out var _);

        familiar.Destroy();
        ResetActiveFamiliarData(steamId);
        ActiveFamiliarManager.UpdateActiveFamiliarBinding(steamId, false);

        string message = !string.IsNullOrEmpty(shinyHexColor) ? $"<color=green>{prefabGuid.GetLocalizedName()}</color>{shinyHexColor}*</color> <color=#FFC0CB>unbound</color>!" : $"<color=green>{prefabGuid.GetLocalizedName()}</color> <color=#FFC0CB>unbound</color>!";
        LocalizationService.HandleServerReply(EntityManager, user, message);

        if (smartBind)
        {
            yield return _bindingDelay;

            BindFamiliar(user, playerCharacter, index);
        }
    }

    const float MAX_AGGRO_RANGE = 25f;
    const float DISTANCE_AGGRO_BASE = 100f;
    public static void SyncAggro(Entity playerCharacter, Entity familiar)
    {
        if (!playerCharacter.TryGetBuffer<InverseAggroBufferElement>(out var inverseAggroBuffer) || inverseAggroBuffer.IsEmpty) return;

        List<Entity> targets = [];

        foreach (InverseAggroBufferElement aggroBufferElement in inverseAggroBuffer)
        {
            Entity target = aggroBufferElement.Entity;
            if (target.Exists()) targets.Add(target);
        }

        AddToFamiliarAggroBuffer(playerCharacter, familiar, targets);
    }
    public static void AddToFamiliarAggroBuffer(Entity playerCharacter, Entity familiar, List<Entity> targets)
    {
        if (!familiar.TryGetBuffer<AggroBuffer>(out var buffer)) return;

        // Core.Log.LogWarning($"Adding to aggro buffer for {familiar.GetPrefabGuid().GetLocalizedName()}");

        List<Entity> entities = [];
        foreach (AggroBuffer aggroBufferEntry in buffer)
        {
            entities.Add(aggroBufferEntry.Entity);
        }

        foreach (Entity target in targets)
        {
            if (entities.Contains(target)) continue;
            else if (target.GetPrefabGuid().Equals(_enchantedCross)) continue;
            else if (target.TryGetComponent(out BloodConsumeSource bloodConsumeSource)
                && bloodConsumeSource.BloodQuality >= BLOOD_QUALITY_IGNORE) continue;

            float distance = Vector3.Distance(playerCharacter.GetPosition(), target.GetPosition());
            float distanceFactor = Mathf.Clamp01(1f - (distance / MAX_AGGRO_RANGE));

            float baseAggro = target.IsVBloodOrGateBoss() ? 400f : 100f;
            float aggroValue = baseAggro + (DISTANCE_AGGRO_BASE * distanceFactor);

            AggroBuffer aggroBufferElement = new()
            {
                DamageValue = aggroValue,
                Entity = target,
                Weight = 1f
            };

            buffer.Add(aggroBufferElement);
        }

        /*
        if (target.GetPrefabGuid().Equals(_enchantedCross)) return; // see if works to ignore

        bool targetInBuffer = false;

        foreach (AggroBuffer aggroBufferEntry in buffer)
        {
            if (aggroBufferEntry.Entity.Equals(target))
            {
                targetInBuffer = true;
                break;
            }
        }

        if (targetInBuffer) return;
        else if (target.TryGetComponent(out BloodConsumeSource bloodConsumeSource) 
            && bloodConsumeSource.BloodQuality >= BLOOD_QUALITY_IGNORE) return; // make sure this doesn't have unintended effects on targeting vBloods or something | seems good?

        float distance = Vector3.Distance(playerCharacter.GetPosition(), target.GetPosition());
        float distanceFactor = Mathf.Clamp01(1f - (distance / MAX_AGGRO_RANGE)); // Closer = 1, Far = 0

        float baseAggro = target.IsVBloodOrGateBoss() ? 400f : 100f;
        float aggroValue = baseAggro + (DISTANCE_AGGRO_BASE * distanceFactor);

        AggroBuffer aggroBufferElement = new()
        {
            DamageValue = aggroValue,
            Entity = target,
            Weight = 1f
        };

        buffer.Add(aggroBufferElement);
        */
    }
    public static void FaceYourEnemy(Entity familiar, Entity target)
    {
        if (familiar.Has<EntityInput>())
        {
            familiar.With((ref EntityInput entityInput) => entityInput.AimDirection = _southFloat3);
        }

        if (familiar.Has<TargetDirection>())
        {
            familiar.With((ref TargetDirection targetDirection) => targetDirection.AimDirection = _southFloat3);
        }

        if (familiar.TryApplyBuff(_defaultEmoteBuff) && familiar.TryGetBuff(_defaultEmoteBuff, out Entity buffEntity))
        {
            buffEntity.With((ref EntityOwner entityOwner) => entityOwner.Owner = target);
        }
    }
    public static bool EligibleForCombat(this Entity familiar)
    {
        return familiar.Exists() && !familiar.IsDisabled() && !familiar.HasBuff(_invulnerableBuff);
    }
    public static string GetFamiliarName(int familiarId, FamiliarBuffsData buffsData)
    {
        if (buffsData.FamiliarBuffs.ContainsKey(familiarId))
        {
            if (ShinyBuffColorHexes.TryGetValue(new(buffsData.FamiliarBuffs[familiarId].FirstOrDefault()), out string hexColor))
            {
                string colorCode = string.IsNullOrEmpty(hexColor) ? $"<color={hexColor}>" : string.Empty;
                return $"<color=green>{new PrefabGUID(familiarId).GetLocalizedName()}</color>{colorCode}*</color>";
            }
        }

        return $"<color=green>{new PrefabGUID(familiarId).GetLocalizedName()}</color>";
    }
    public static void HandleFamiliarPrestige(ChatCommandContext ctx, int clampedCost) // need to replace first block in command with this method but laterrrr
    {
        Entity playerCharacter = ctx.Event.SenderCharacterEntity;
        User user = ctx.User;

        ulong steamId = user.PlatformId;

        if (!steamId.HasActiveFamiliar())
        {
            LocalizationService.HandleReply(ctx, "Couldn't find active familiar!");
            return;
        }

        ActiveFamiliarData activeFamiliar = GetActiveFamiliarData(steamId);
        int familiarId = activeFamiliar.FamiliarId;

        FamiliarExperienceData xpData = LoadFamiliarExperienceData(steamId);
        FamiliarPrestigeData prestigeData = LoadFamiliarPrestigeData(steamId);

        if (!prestigeData.FamiliarPrestige.ContainsKey(familiarId))
        {
            prestigeData.FamiliarPrestige[familiarId] = 0;
            SaveFamiliarPrestigeData(steamId, prestigeData);
        }

        prestigeData = LoadFamiliarPrestigeData(steamId);

        if (prestigeData.FamiliarPrestige[familiarId] >= ConfigService.MaxFamiliarPrestiges)
        {
            LocalizationService.HandleReply(ctx, $"Your familiar has already prestiged the maximum number of times! (<color=white>{ConfigService.MaxFamiliarPrestiges}</color>)");
            return;
        }

        /*
        int value = -1;

        if (stats.Count < FamiliarPrestigeStats.Count) // if less than max stats parse entry and add if set doesnt already contain
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
        */

        if (ServerGameManager.TryRemoveInventoryItem(playerCharacter, _itemSchematic, clampedCost))
        {
            int prestigeLevel = prestigeData.FamiliarPrestige[familiarId] + 1;
            prestigeData.FamiliarPrestige[familiarId] = prestigeLevel;
            SaveFamiliarPrestigeData(steamId, prestigeData);

            Entity familiar = GetActiveFamiliar(playerCharacter);
            ModifyUnitStats(familiar, xpData.FamiliarExperience[familiarId].Key, steamId, familiarId);

            LocalizationService.HandleReply(ctx, $"Your familiar has prestiged [<color=#90EE90>{prestigeLevel}</color>]; the accumulated knowledge allowed them to retain their level!");

            /*
            if (value == -1)
            {
                LocalizationService.HandleReply(ctx, $"Your familiar has prestiged [<color=#90EE90>{prestigeLevel}</color>]; the accumulated knowledge allowed them to retain their level!");
            }
            else
            {
                LocalizationService.HandleReply(ctx, $"Your familiar has prestiged [<color=#90EE90>{prestigeLevel}</color>]; the accumulated knowledge allowed them to retain their level! (+<color=#00FFFF>{FamiliarPrestigeStats[value]}</color>)");
            }
            */
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Failed to remove schematics from your inventory!");
        }
    }
    public static IEnumerator HandleFamiliarShapeshiftRoutine(User user, Entity playerCharacter, Entity familiar)
    {
        yield return _delay;

        try
        {
            HandleModifications(user, playerCharacter, familiar);
        }
        catch (Exception ex)
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
    public static void DestroyFamiliarServant(Entity servant)
    {
        // Entity familiarServant = GetFamiliarServant(playerCharacter);
        // Entity servantCoffin = familiarServant.TryGetComponent(out ServantConnectedCoffin connectedCoffin) ? connectedCoffin.CoffinEntity.GetEntityOnServer() : Entity.Null;
        Entity coffin = GetServantCoffin(servant);

        /*
        if (servantCoffin.Exists())
        {
            servantCoffin.With((ref ServantCoffinstation coffinStation) =>
            {
                coffinStation.ConnectedServant._Entity = Entity.Null;
            });

            servantCoffin.Remove<Disabled>();
            servantCoffin.Destroy();
        }
        */

        // servant.Remove<Disabled>();
        StatChangeUtility.KillOrDestroyEntity(EntityManager, servant, Entity.Null, Entity.Null, Core.ServerTime, StatChangeReason.Default, true);
        // servant.Destroy();

        // servant.DropInventory();
        // servant.Destroy(VExtensions.DestroyMode.Delayed);

        if (coffin.Exists())
        {
            /*
            coffin.With((ref ServantCoffinstation coffinStation) =>
            {
                coffinStation.ConnectedServant._Entity = Entity.Null;
            });
            */

            coffin.Remove<Disabled>();
            coffin.Destroy();
        }
    }
}