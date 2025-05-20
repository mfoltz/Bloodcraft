using Bloodcraft.Interfaces;
using Bloodcraft.Resources;
using Bloodcraft.Services;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using static Bloodcraft.Utilities.Familiars;
using static Bloodcraft.Utilities.Misc.PlayerBoolsManager;
using static Bloodcraft.Utilities.Shapeshifts;
using static Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarEquipmentManager;
using User = ProjectM.Network.User;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class EmoteSystemPatch
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly bool _familiars = ConfigService.FamiliarSystem;
    static readonly bool _familiarBattles = ConfigService.FamiliarBattles;
    static readonly bool _exoForm = ConfigService.ExoPrestiging;
    static readonly bool _familiarCombat = ConfigService.FamiliarCombat;
    static readonly bool _familiarPvP = ConfigService.FamiliarPvP;

    static readonly PrefabGUID _ignoredFaction = PrefabGUIDs.Faction_Ignored;
    static readonly PrefabGUID _playerFaction = PrefabGUIDs.Faction_Players;

    static readonly PrefabGUID _vanishBuff = Buffs.VanishBuff;
    static readonly PrefabGUID _dominateBuff = Buffs.DominateBuff;
    static readonly PrefabGUID _invulnerableBuff = Buffs.AdminInvulnerableBuff;
    static readonly PrefabGUID _pveCombatBuff = Buffs.PvECombatBuff;
    static readonly PrefabGUID _pvpCombatBuff = Buffs.PvPCombatBuff;
    static readonly PrefabGUID _takeFlightBuff = Buffs.TakeFlightBuff;
    static readonly PrefabGUID _exoFormBuff = Buffs.EvolvedVampireBuff;
    static readonly PrefabGUID _phasingBuff = Buffs.PhasingBuff;
    static readonly PrefabGUID _gateBossFeedCompleteBuff = Buffs.GateBossFeedCompleteBuff;

    static readonly PrefabGUID _waveAbilityGroup = PrefabGUIDs.AB_Emote_Vampire_Wave_AbilityGroup;
    static readonly PrefabGUID _saluteAbilityGroup = PrefabGUIDs.AB_Emote_Vampire_Salute_AbilityGroup;
    static readonly PrefabGUID _clapAbilityGroup = PrefabGUIDs.AB_Emote_Vampire_Clap_AbilityGroup;
    static readonly PrefabGUID _tauntAbilityGroup = PrefabGUIDs.AB_Emote_Vampire_Taunt_AbilityGroup;
    static readonly PrefabGUID _yesAbilityGroup = PrefabGUIDs.AB_Emote_Vampire_Yes_AbilityGroup;
    static readonly PrefabGUID _noAbilityGroup = PrefabGUIDs.AB_Emote_Vampire_No_AbilityGroup;
    static readonly PrefabGUID _beckonAbilityGroup = PrefabGUIDs.AB_Emote_Vampire_Beckon_AbilityGroup;
    static readonly PrefabGUID _bowAbilityGroup = PrefabGUIDs.AB_Emote_Vampire_Bow_AbilityGroup;

    static readonly PrefabGUID _disableAggroBuff = Buffs.DisableAggroBuff;
    static readonly PrefabGUID _interactModeBuff = Buffs.InteractModeBuff;
    public static IReadOnlyDictionary<PrefabGUID, Action<User, Entity, ulong>> EmoteActions => _emoteActions;
    static readonly Dictionary<PrefabGUID, Action<User, Entity, ulong>> _emoteActions = new()
    {
        { _waveAbilityGroup, CallDismiss }, 
        { _saluteAbilityGroup, CombatMode },   
        { _clapAbilityGroup, BindUnbind },    
        { _beckonAbilityGroup, InteractMode },
        // { _bowAbilityGroup, CycleShapeshift },
        { _tauntAbilityGroup, HandleShapeshift }
    };

    static readonly Dictionary<PrefabGUID, Action<(ulong, ulong)>> _matchActions = new()
    {
        { _yesAbilityGroup, AcceptBattle }, 
        { _noAbilityGroup, DeclineBattle }    
    };

    public static readonly HashSet<ulong> BlockShapeshift = [];
    public static readonly HashSet<(ulong, ulong)> BattleChallenges = [];

    [HarmonyPatch(typeof(EmoteSystem), nameof(EmoteSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(EmoteSystem __instance)
    {
        if (!Core._initialized) return;
        else if (!_familiars && !_exoForm) return;

        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);

        try
        {
            foreach (var entity in entities)
            {
                UseEmoteEvent useEmoteEvent = entity.Read<UseEmoteEvent>();
                FromCharacter fromCharacter = entity.Read<FromCharacter>();

                User user = fromCharacter.User.Read<User>();
                Entity playerCharacter = fromCharacter.Character;
                ulong steamId = user.PlatformId;

                if (_exoForm && useEmoteEvent.Action.Equals(_tauntAbilityGroup) && GetPlayerBool(steamId, SHAPESHIFT_KEY))
                {
                    if (_emoteActions.TryGetValue(useEmoteEvent.Action, out var action)) action.Invoke(user, playerCharacter, steamId);
                }
                else if (_familiarBattles && BattleChallenges.TryGetMatch(steamId, out var match) && (useEmoteEvent.Action.Equals(_yesAbilityGroup) || useEmoteEvent.Action.Equals(_noAbilityGroup)))
                {
                    if (_matchActions.TryGetValue(useEmoteEvent.Action, out var action)) action.Invoke(match);
                }
                else if (_familiars && GetPlayerBool(steamId, EMOTE_ACTIONS_KEY))
                {
                    if (playerCharacter.HasBuff(_dominateBuff) && EmoteActions.ContainsKey(useEmoteEvent.Action))
                    {
                        LocalizationService.HandleServerReply(EntityManager, user, "You can't use emote actions when using dominate form!");
                    }
                    else if (playerCharacter.HasBuff(_takeFlightBuff) && EmoteActions.ContainsKey(useEmoteEvent.Action))
                    {
                        LocalizationService.HandleServerReply(EntityManager, user, "You can't use emote actions when using bat form!");
                    }
                    else if (useEmoteEvent.Action.Equals(_beckonAbilityGroup) && playerCharacter.PlayerInCombat())
                    {
                        LocalizationService.HandleServerReply(EntityManager, user, "You can't interact with your familiar during combat!");
                    }
                    else if (EmoteActions.TryGetValue(useEmoteEvent.Action, out var action)) action.Invoke(user, playerCharacter, steamId);
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
    static void HandleShapeshift(User user, Entity playerCharacter, ulong steamId)
    {
        if (!ShapeshiftCache.TryGetShapeshiftBuff(steamId, out PrefabGUID shapeshiftBuff))
        {
            BlockShapeshift.Add(steamId);
            LocalizationService.HandleServerReply(EntityManager, user, "Select a form you've unlocked first! ('<color=white>.prestige sf [<color=orange>EvolvedVampire|CorruptedSerpent</color>]</color>')");
            return;
        }

        if (!playerCharacter.HasBuff(shapeshiftBuff))
        {
            if (playerCharacter.TryApplyAndGetBuff(_phasingBuff, out Entity buffEntity) && buffEntity.Has<BuffModificationFlagData>())
            {
                buffEntity.Remove<BuffModificationFlagData>();
            }
        }
        else if (playerCharacter.TryGetBuff(shapeshiftBuff, out Entity buffEntity))
        {
            BlockShapeshift.Add(steamId);
            playerCharacter.TryApplyBuff(_gateBossFeedCompleteBuff);
            buffEntity.DestroyBuff();
        }
    }
    public static void BindUnbind(User user, Entity playerCharacter, ulong steamId)
    {
        Entity familiar = GetActiveFamiliar(playerCharacter);

        if (familiar.Exists())
        {
            UnbindFamiliar(user, playerCharacter);
        }
        else
        {
            BindFamiliar(user, playerCharacter);
        }
    }
    public static void CallDismiss(User user, Entity playerCharacter, ulong steamId)
    {
        if (steamId.HasActiveFamiliar())
        {
            Entity familiar = GetActiveFamiliar(playerCharacter);

            if (familiar.Exists())
            {
                if (steamId.HasDismissedFamiliar())
                {
                    if (!_familiarPvP && playerCharacter.HasBuff(_pvpCombatBuff))
                    {
                        LocalizationService.HandleServerReply(EntityManager, user, "You can't call your familiar during PvP combat!");
                        return;
                    }
                    else CallFamiliar(playerCharacter, familiar, user, steamId);
                }
                else if (familiar.HasBuff(_interactModeBuff))
                {
                    InteractMode(user, playerCharacter, steamId);
                    DismissFamiliar(playerCharacter, familiar, user, steamId);
                }
                else
                {
                    DismissFamiliar(playerCharacter, familiar, user, steamId);
                }
            }
            else
            {
                LocalizationService.HandleServerReply(EntityManager, user, "Active familiar doesn't exist! If that doesn't seem right try using '<color=white>.fam reset</color>'.");
            }
        }
        else
        {
            LocalizationService.HandleServerReply(EntityManager, user, "Couldn't find active familiar...");
        }
    }
    public static void CombatMode(User user, Entity playerCharacter, ulong steamId)
    {
        if (!_familiarCombat)
        {
            LocalizationService.HandleServerReply(EntityManager, user, "Familiar combat is not enabled.");
            return;
        }

        if (playerCharacter.PlayerInCombat())
        {
            LocalizationService.HandleServerReply(EntityManager, user, "You can't toggle familiar combat mode during PvE/PvP combat!");
            return;
        }
        else if (steamId.HasActiveFamiliar())
        {
            Entity familiar = GetActiveFamiliar(playerCharacter);

            if (!familiar.Exists())
            {
                LocalizationService.HandleServerReply(EntityManager, user, "Couldn't find active familiar...");
                return;
            }
            else if (familiar.HasBuff(_invulnerableBuff))
            {
                familiar.TryRemoveBuff(buffPrefabGuid: _invulnerableBuff);
                familiar.SetFaction(_playerFaction);
                EnableAggro(familiar);
                Familiars.EnableAggroable(familiar);

                LocalizationService.HandleServerReply(EntityManager, user, "Familiar combat <color=green>enabled</color>.");
            }
            else
            {
                familiar.TryApplyBuff(_invulnerableBuff);
                familiar.SetFaction(_ignoredFaction);
                DisableAggro(familiar);
                Familiars.DisableAggroable(familiar);

                LocalizationService.HandleServerReply(EntityManager, user, "Familiar combat <color=red>disabled</color>.");
            }
        }
        else
        {
            LocalizationService.HandleServerReply(EntityManager, user, "Couldn't find active familiar...");
        }
    }
    public static void InteractMode(User user, Entity playerCharacter, ulong steamId)
    {
        ActiveFamiliarData activeFamiliarData = ActiveFamiliarManager.GetActiveFamiliarData(steamId);

        Entity familiar = activeFamiliarData.Familiar;
        Entity servant = activeFamiliarData.Servant;

        if (activeFamiliarData.Dismissed)
        {
            LocalizationService.HandleServerReply(EntityManager, user, "Can't interact with familiar when dismissed!");
            return;
        }
        else if (familiar.Exists() && servant.Exists())
        {
            if (familiar.HasBuff(_vanishBuff))
            {
                LocalizationService.HandleServerReply(EntityManager, user, "Can't interact with familiar when binding/unbinding!");
                return;
            }

            bool hasInteractBuff = familiar.HasBuff(_interactModeBuff);

            if (hasInteractBuff)
            {
                SaveFamiliarEquipment(steamId, activeFamiliarData.FamiliarId, GetFamiliarEquipment(activeFamiliarData.Servant));

                EnableAggro(familiar);
                familiar.TryRemoveBuff(buffPrefabGuid: _interactModeBuff);

                servant.With((ref Interactable interactable) =>
                {
                    interactable.Disabled = true;
                });

                servant.Add<Disabled>();
            }
            else
            {
                DisableAggro(familiar);
                familiar.TryApplyBuffInteractMode(_interactModeBuff);
                
                servant.Remove<Disabled>();

                servant.With((ref Interactable interactable) =>
                {
                    interactable.Disabled = false;
                });
            }
        }
    }
    static void AcceptBattle((ulong, ulong) match)
    {
        BattleService.Matchmaker.QueueMatch(match);
        BattleChallenges.Remove(match);
    }
    static void DeclineBattle((ulong, ulong) match)
    {
        BattleService.NotifyBothPlayers(match.Item1, match.Item2, "The challenge has been declined.");
        BattleChallenges.Remove(match);
    }
}
