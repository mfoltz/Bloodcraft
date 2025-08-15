using Bloodcraft.Resources;
using Bloodcraft.Resources.Localization;
using Bloodcraft.Services;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using static Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarEquipmentManager;
using static Bloodcraft.Utilities.Familiars;
using static Bloodcraft.Utilities.Misc.PlayerBools;
using static Bloodcraft.Utilities.Shapeshifts;
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
                        LocalizationService.Reply(EntityManager, user, MessageKeys.EMOTE_ACTIONS_DOMINATE_FORM);
                    }
                    else if (playerCharacter.HasBuff(_takeFlightBuff) && EmoteActions.ContainsKey(useEmoteEvent.Action))
                    {
                        LocalizationService.Reply(EntityManager, user, MessageKeys.EMOTE_ACTIONS_BAT_FORM);
                    }
                    else if (useEmoteEvent.Action.Equals(_beckonAbilityGroup) && playerCharacter.PlayerInCombat())
                    {
                        LocalizationService.Reply(EntityManager, user, MessageKeys.FAMILIAR_INTERACT_COMBAT);
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
            LocalizationService.Reply(EntityManager, user, MessageKeys.SHAPESHIFT_SELECT_FORM);
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
            buffEntity.Destroy();
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
                        LocalizationService.Reply(EntityManager, user, MessageKeys.FAMILIAR_CALL_PVP_COMBAT);
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
                LocalizationService.Reply(EntityManager, user, MessageKeys.FAMILIAR_ACTIVE_NOT_EXIST);
            }
        }
        else
        {
            LocalizationService.Reply(EntityManager, user, MessageKeys.FAMILIAR_NOT_FOUND);
        }
    }
    public static void CombatMode(User user, Entity playerCharacter, ulong steamId)
    {
        if (!_familiarCombat)
        {
            LocalizationService.Reply(EntityManager, user, MessageKeys.FAMILIAR_COMBAT_NOT_ENABLED);
            return;
        }

        if (playerCharacter.PlayerInCombat())
        {
            LocalizationService.Reply(EntityManager, user, MessageKeys.FAMILIAR_COMBAT_TOGGLE_IN_COMBAT);
            return;
        }
        else if (steamId.HasActiveFamiliar())
        {
            Entity familiar = GetActiveFamiliar(playerCharacter);

            if (!familiar.Exists())
            {
                LocalizationService.Reply(EntityManager, user, MessageKeys.FAMILIAR_NOT_FOUND);
                return;
            }
            else if (familiar.HasBuff(_invulnerableBuff))
            {
                familiar.TryRemoveBuff(buffPrefabGuid: _invulnerableBuff);
                familiar.SetFaction(_playerFaction);
                EnableAggro(familiar);
                Familiars.EnableAggroable(familiar);

                LocalizationService.Reply(EntityManager, user, MessageKeys.FAMILIAR_COMBAT_ENABLED);
            }
            else
            {
                familiar.TryApplyBuff(_invulnerableBuff);
                familiar.SetFaction(_ignoredFaction);
                DisableAggro(familiar);
                Familiars.DisableAggroable(familiar);

                LocalizationService.Reply(EntityManager, user, MessageKeys.FAMILIAR_COMBAT_DISABLED);
            }
        }
        else
        {
            LocalizationService.Reply(EntityManager, user, MessageKeys.FAMILIAR_NOT_FOUND);
        }
    }
    public static void InteractMode(User user, Entity playerCharacter, ulong steamId)
    {
        ActiveFamiliarData activeFamiliarData = ActiveFamiliarManager.GetActiveFamiliarData(steamId);

        Entity familiar = activeFamiliarData.Familiar;
        Entity servant = activeFamiliarData.Servant;
        Entity coffin = GetServantCoffin(servant);

        if (activeFamiliarData.Dismissed)
        {
            LocalizationService.Reply(EntityManager, user, MessageKeys.FAMILIAR_INTERACT_DISMISSED);
            return;
        }
        else if (familiar.Exists() && servant.Exists() && coffin.Exists())
        {
            if (familiar.HasBuff(_vanishBuff))
            {
                LocalizationService.Reply(EntityManager, user, MessageKeys.FAMILIAR_INTERACT_BINDING);
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

                coffin.Add<Disabled>();
                servant.Add<Disabled>();
            }
            else
            {
                DisableAggro(familiar);
                familiar.TryApplyBuffInteractMode(_interactModeBuff);
                
                coffin.Remove<Disabled>();
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
