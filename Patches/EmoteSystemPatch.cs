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

    static readonly PrefabGUID _ignoredFaction = new(-1430861195);
    static readonly PrefabGUID _playerFaction = new(1106458752);

    static readonly PrefabGUID _dominateBuff = new(-1447419822);
    static readonly PrefabGUID _invulnerableBuff = new(-480024072);
    static readonly PrefabGUID _combatBuff = new(581443919);
    static readonly PrefabGUID _pvpCombatBuff = new(697095869);
    static readonly PrefabGUID _takeFlightBuff = new(1205505492);
    static readonly PrefabGUID _exoFormBuff = new(-31099041);
    static readonly PrefabGUID _phasingBuff = new(-79611032);

    static readonly PrefabGUID _waveEmote = new(1177797340);
    static readonly PrefabGUID _saluteEmote = new(-370061286);
    static readonly PrefabGUID _clapEmote = new(-26826346);
    static readonly PrefabGUID _tauntEmote = new(-158502505);
    static readonly PrefabGUID _yesEmote = new(-1525577000);
    static readonly PrefabGUID _noEmote = new(-53273186);

    public static readonly Dictionary<PrefabGUID, Action<User, Entity, ulong>> EmoteActions = new()
    {
        { new(1177797340), CallDismiss }, // Wave
        { new(-370061286), CombatMode }, // Salute
        { new(-26826346), BindUnbind } // clap
    };

    static readonly Dictionary<PrefabGUID, Action<(ulong, ulong)>> _matchActions = new()
    {
        { new(-1525577000), AcceptBattle }, // Yes
        { new(-53273186), DeclineBattle }  // No
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

                if (_exoForm && useEmoteEvent.Action.Equals(_tauntEmote) && GetPlayerBool(steamId, EXO_FORM_KEY))
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
                        DestroyUtility.Destroy(EntityManager, buffEntity, DestroyDebugReason.TryRemoveBuff);
                    }
                }
                else if (_familiarBattles && BattleChallenges.TryGetMatch(steamId, out var match) && (useEmoteEvent.Action.Equals(_yesEmote) || useEmoteEvent.Action.Equals(_noEmote)))
                {
                    if (_matchActions.TryGetValue(useEmoteEvent.Action, out var action)) action.Invoke(match);
                }
                else if (_familiars && GetPlayerBool(steamId, EMOTE_ACTIONS_KEY))
                {
                    if (ServerGameManager.HasBuff(playerCharacter, _dominateBuff.ToIdentifier()) && EmoteActions.ContainsKey(useEmoteEvent.Action))
                    {
                        LocalizationService.HandleServerReply(EntityManager, user, "You can't use emote actions when using dominate form!");
                    }
                    else if (ServerGameManager.HasBuff(playerCharacter, _takeFlightBuff.ToIdentifier()) && EmoteActions.ContainsKey(useEmoteEvent.Action))
                    {
                        LocalizationService.HandleServerReply(EntityManager, user, "You can't use emote actions when using bat form!");
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
        Entity familiar = Familiars.FindPlayerFamiliar(playerCharacter);

        if (familiar.Exists())
        {
            Familiars.UnbindFamiliar(user, playerCharacter);
        }
        else
        {
            Familiars.BindFamiliar(user, playerCharacter);
        }
    }
    public static void CallDismiss(User user, Entity character, ulong steamId)
    {
        if (steamId.TryGetFamiliarActives(out var data) && !data.FamKey.Equals(0)) // 0 means no active familiar
        {
            Entity familiar = Familiars.FindPlayerFamiliar(character);

            if (familiar.Exists())
            {
                if (familiar.IsDisabled())
                {
                    Familiars.CallFamiliar(character, familiar, user, steamId, data);
                }
                else
                {
                    Familiars.DismissFamiliar(character, familiar, user, steamId, data);
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
    public static void CombatMode(User user, Entity character, ulong steamId) // need to tidy this up at some point
    {
        if (!_familiarCombat)
        {
            LocalizationService.HandleServerReply(EntityManager, user, "Familiar combat is not enabled.");
            return;
        }

        if (character.HasBuff(_combatBuff) || character.HasBuff(_pvpCombatBuff))
        {
            LocalizationService.HandleServerReply(EntityManager, user, "You can't toggle familiar combat mode during PvE/PvP combat!");
            return;
        }
        else if (steamId.TryGetFamiliarActives(out var data) && !data.FamKey.Equals(0)) // 0 means no active familiar
        {
            Entity familiar = Familiars.FindPlayerFamiliar(character); // return following entity matching Guidhash in FamiliarActives

            if (!familiar.Exists() && data.Familiar.Exists())
            {
                familiar = data.Familiar;
            }

            if (familiar == Entity.Null)
            {
                LocalizationService.HandleServerReply(EntityManager, user, "Couldn't find active familiar...");
                return;
            }

            if (familiar.HasBuff(_invulnerableBuff)) // remove and enable combat
            {
                // BuffUtility.BuffSpawner buffSpawner = BuffUtility.BuffSpawner.Create(ServerGameManager);
                // EntityCommandBuffer entityCommandBuffer = EntityCommandBufferSystem.CreateCommandBuffer();
                // BuffUtility.TryRemoveBuff(ref buffSpawner, entityCommandBuffer, _invulnerableBuff, familiar);

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
}
