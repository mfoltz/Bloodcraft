using Bloodcraft.Services;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using static Bloodcraft.Utilities.Misc.PlayerBoolsManager;
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

    static readonly PrefabGUID _ignoredFaction = new(-1430861195);
    static readonly PrefabGUID _playerFaction = new(1106458752);

    static readonly PrefabGUID _dominateBuff = new(-1447419822);
    static readonly PrefabGUID _invulnerableBuff = new(-480024072);
    static readonly PrefabGUID _pveCombatBuff = new(581443919);
    static readonly PrefabGUID _pvpCombatBuff = new(697095869);
    static readonly PrefabGUID _takeFlightBuff = new(1205505492);
    static readonly PrefabGUID _exoFormBuff = new(-31099041);
    static readonly PrefabGUID _phasingBuff = new(-79611032);
    static readonly PrefabGUID _gateBossFeedCompleteBuff = new(-354622715);

    static readonly PrefabGUID _waveEmoteGroup = new(1177797340);
    static readonly PrefabGUID _saluteEmoteGroup = new(-370061286);
    static readonly PrefabGUID _clapEmoteGroup = new(-26826346);
    static readonly PrefabGUID _tauntEmoteGroup = new(-158502505);
    static readonly PrefabGUID _yesEmoteGroup = new(-1525577000);
    static readonly PrefabGUID _noEmoteGroup = new(-53273186);
    static readonly PrefabGUID _beckonEmoteGroup = new(-658066984);

    static readonly PrefabGUID _disableAggroBuff = new(1934061152); // Buff_Illusion_Mosquito_DisableAggro
    static readonly PrefabGUID _interactModeBuff = new(1520432556); // AB_Militia_HoundMaster_QuickShot_Buff

    public static readonly Dictionary<PrefabGUID, Action<User, Entity, ulong>> EmoteActions = new()
    {
        { new(1177797340), CallDismiss },    // Wave
        { new(-370061286), CombatMode },     // Salute
        { new(-26826346), BindUnbind },      // Clap
        { new(-658066984), InteractMode } // Beckon
    };

    static readonly Dictionary<PrefabGUID, Action<(ulong, ulong)>> _matchActions = new()
    {
        { new(-1525577000), AcceptBattle },  // Yes
        { new(-53273186), DeclineBattle }    // No
    };

    public static readonly HashSet<ulong> ExitingForm = [];
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

                if (_exoForm && useEmoteEvent.Action.Equals(_tauntEmoteGroup) && GetPlayerBool(steamId, EXO_FORM_KEY))
                {
                    if (!playerCharacter.HasBuff(_exoFormBuff))
                    {
                        playerCharacter.TryApplyBuff(_phasingBuff);

                        if (playerCharacter.TryGetBuff(_phasingBuff, out Entity buffEntity) && buffEntity.Has<BuffModificationFlagData>())
                        {
                            buffEntity.Remove<BuffModificationFlagData>();
                        }
                    }
                    else if (playerCharacter.TryGetBuff(_exoFormBuff, out Entity buffEntity))
                    {
                        ExitingForm.Add(steamId);
                        ExoForm.UpdatePartialExoFormChargeUsed(buffEntity, steamId);

                        playerCharacter.TryApplyBuff(_gateBossFeedCompleteBuff);
                        DestroyUtility.Destroy(EntityManager, buffEntity, DestroyDebugReason.TryRemoveBuff);
                    }
                }
                else if (_familiarBattles && BattleChallenges.TryGetMatch(steamId, out var match) && (useEmoteEvent.Action.Equals(_yesEmoteGroup) || useEmoteEvent.Action.Equals(_noEmoteGroup)))
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
                    else if (useEmoteEvent.Action.Equals(_beckonEmoteGroup) && playerCharacter.PlayerInCombat())
                    {
                        LocalizationService.HandleServerReply(EntityManager, user, "You can't interact with your familiar's inventory during combat!");
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
    public static void BindUnbind(User user, Entity playerCharacter, ulong steamId)
    {
        Entity familiar = Familiars.GetActiveFamiliar(playerCharacter);

        if (familiar.Exists())
        {
            Familiars.UnbindFamiliar(user, playerCharacter);
        }
        else
        {
            Familiars.BindFamiliar(user, playerCharacter);
        }
    }
    public static void CallDismiss(User user, Entity playerCharacter, ulong steamId)
    {
        if (steamId.TryGetFamiliarActives(out var data) && !data.FamKey.Equals(0)) // 0 means no active familiar
        {
            Entity familiar = Familiars.GetActiveFamiliar(playerCharacter);

            if (familiar.Exists())
            {
                if (familiar.IsDisabled())
                {
                    if (!_familiarPvP && playerCharacter.HasBuff(_pvpCombatBuff))
                    {
                        LocalizationService.HandleServerReply(EntityManager, user, "You can't call your familiar during PvP combat!");
                        return;
                    }
                    else Familiars.CallFamiliar(playerCharacter, familiar, user, steamId, data);
                }
                else if (familiar.HasBuff(_interactModeBuff))
                {
                    InteractMode(user, playerCharacter, steamId);
                    Familiars.DismissFamiliar(playerCharacter, familiar, user, steamId, data);
                }
                else
                {
                    Familiars.DismissFamiliar(playerCharacter, familiar, user, steamId, data);
                }
            }
            else
            {
                LocalizationService.HandleServerReply(EntityManager, user, "Active familiar not found! If that doesn't seem quite right try using '<color=white>.fam reset</color>'.");
            }
        }
        else
        {
            LocalizationService.HandleServerReply(EntityManager, user, "Couldn't find active familiar...");
        }
    }
    public static void CombatMode(User user, Entity playerCharacter, ulong steamId) // need to tidy this up at some point
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
        else if (steamId.TryGetFamiliarActives(out var data) && !data.FamKey.Equals(0)) // 0 means no active familiar
        {
            Entity familiar = Familiars.GetActiveFamiliar(playerCharacter); // return following entity matching Guidhash in FamiliarActives

            if (!familiar.Exists() && data.Familiar.Exists())
            {
                familiar = data.Familiar;
            }

            if (!familiar.Exists())
            {
                LocalizationService.HandleServerReply(EntityManager, user, "Couldn't find active familiar...");
                return;
            }

            if (familiar.HasBuff(_invulnerableBuff))
            {
                familiar.TryRemoveBuff(_invulnerableBuff);
                familiar.SetFaction(_playerFaction);
                familiar.EnableAggro();
                familiar.EnableAggroable();

                LocalizationService.HandleServerReply(EntityManager, user, "Familiar combat <color=green>enabled</color>.");
            }
            else
            {
                familiar.SetFaction(_ignoredFaction);
                familiar.DisableAggro();
                familiar.DisableAggroable();
                familiar.TryApplyBuff(_invulnerableBuff);

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
        Entity familiar = Familiars.GetActiveFamiliar(playerCharacter);
        Entity servant = Familiars.GetFamiliarServant(familiar);

        if (familiar.Exists() && servant.Exists())
        {
            bool hasInteractBuff = familiar.HasBuff(_interactModeBuff);

            if (hasInteractBuff)
            {
                //familiar.TryRemoveBuff(_disableAggroBuff);

                Familiars.EnableAggro(familiar);
                familiar.TryRemoveBuff(_interactModeBuff);

                familiar.With((ref DynamicCollision dynamicCollision) =>
                {
                    dynamicCollision.Immobile = false;
                });

                servant.With((ref Interactable interactable) =>
                {
                    interactable.Disabled = true;
                });

                servant.Add<Disabled>();
            }
            else if (!hasInteractBuff)
            {
                // familiar.TryApplyBuff(_disableAggroBuff);

                Familiars.DisableAggro(familiar);
                familiar.TryApplyBuffInteractMode(_interactModeBuff);

                familiar.With((ref DynamicCollision dynamicCollision) =>
                {
                    dynamicCollision.Immobile = true;
                });
                
                servant.Remove<Disabled>();

                servant.With((ref Interactable interactable) =>
                {
                    interactable.Disabled = false;
                });
            }
        }
    }
}
