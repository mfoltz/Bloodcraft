using Bloodcraft.Services;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using User = ProjectM.Network.User;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class EmoteSystemPatch
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static DebugEventsSystem DebugEventsSystem => SystemService.DebugEventsSystem;
    static EntityCommandBufferSystem EntityCommandBufferSystem => SystemService.EntityCommandBufferSystem;

    static readonly PrefabGUID IgnoredFaction = new(-1430861195);
    static readonly PrefabGUID PlayerFaction = new(1106458752);

    static readonly PrefabGUID DominateBuff = new(-1447419822);
    static readonly PrefabGUID InvulnerableBuff = new(-480024072);
    static readonly PrefabGUID CombatBuff = new(581443919);
    static readonly PrefabGUID PvPCombatBuff = new(697095869);
    static readonly PrefabGUID TakeFlightBuff = new(1205505492);
    static readonly PrefabGUID ExoFormBuff = new(-31099041);
    static readonly PrefabGUID PhasingBuff = new(-79611032);

    static readonly PrefabGUID WaveEmote = new(1177797340);
    static readonly PrefabGUID SaluteEmote = new(-370061286);
    static readonly PrefabGUID ClapEmote = new(-26826346);
    public static readonly PrefabGUID TauntEmote = new(-158502505);

    public static readonly Dictionary<PrefabGUID, Action<User, Entity, ulong>> actions = new()
    {
        { new(1177797340), CallDismiss }, // Wave
        { new(-370061286), CombatMode }, // Salute
        { new(-26826346), BindUnbind } // clap
        //{ new(-158502505), HandleExoForm} // taunt
    };

    public static readonly HashSet<ulong> ExitingForm = [];

    [HarmonyPatch(typeof(EmoteSystem), nameof(EmoteSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(EmoteSystem __instance)
    {
        if (!Core.hasInitialized) return;
        else if (!ConfigService.FamiliarSystem) return;

        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (var entity in entities)
            {
                UseEmoteEvent useEmoteEvent = entity.Read<UseEmoteEvent>();
                FromCharacter fromCharacter = entity.Read<FromCharacter>();

                User user = fromCharacter.User.Read<User>();
                Entity character = fromCharacter.Character;
                ulong steamId = user.PlatformId;

                if (useEmoteEvent.Action.Equals(TauntEmote) && PlayerUtilities.GetPlayerBool(steamId, "ExoForm"))
                {
                    if (!character.HasBuff(ExoFormBuff))
                    {
                        BuffUtilities.TryApplyBuff(character, PhasingBuff);
                    }
                    else if (character.TryGetBuff(ExoFormBuff, out Entity buffEntity))
                    {
                        ExitingForm.Add(steamId);
                        BuffUtilities.UpdatePartialExoFormChargeUsed(buffEntity, steamId);
                        DestroyUtility.Destroy(EntityManager, buffEntity);
                    }
                    //HandleExoForm(user, character, steamId);
                }
                else if (PlayerUtilities.GetPlayerBool(steamId, "Emotes"))
                {
                    if (ServerGameManager.HasBuff(character, DominateBuff.ToIdentifier()) && actions.ContainsKey(useEmoteEvent.Action))
                    {
                        LocalizationService.HandleServerReply(EntityManager, user, "You can't use emote actions when using dominate form!");
                    }
                    else if (ServerGameManager.HasBuff(character, TakeFlightBuff.ToIdentifier()) && actions.ContainsKey(useEmoteEvent.Action))
                    {
                        LocalizationService.HandleServerReply(EntityManager, user, "You can't use emote actions when using bat form!");
                    }
                    else if (actions.TryGetValue(useEmoteEvent.Action, out var action)) action.Invoke(user, character, steamId);
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
    public static void BindUnbind(User user, Entity character, ulong steamId)
    {
        Entity userEntity = character.GetUserEntity();
        Entity familiar = FamiliarUtilities.FindPlayerFamiliar(character);

        if (familiar.Exists())
        {
            FamiliarUtilities.UnbindFamiliar(character, userEntity, steamId);
        }
        else
        {
            FamiliarUtilities.BindFamiliar(character, userEntity, steamId);
        }
    }
    public static void CallDismiss(User user, Entity character, ulong steamId)
    {
        if (steamId.TryGetFamiliarActives(out var data) && !data.FamKey.Equals(0)) // 0 means no active familiar
        {
            Entity familiar = FamiliarUtilities.FindPlayerFamiliar(character);

            if (familiar.Exists())
            {
                if (familiar.IsDisabled())
                {
                    FamiliarUtilities.CallFamiliar(character, familiar, user, steamId, data);
                }
                else
                {
                    FamiliarUtilities.DismissFamiliar(character, familiar, user, steamId, data);
                }
            }
            else
            {
                LocalizationService.HandleServerReply(EntityManager, user, "No active familiar found...");
            }
        }
    }
    public static void CombatMode(User user, Entity character, ulong steamId)
    {
        if (!ConfigService.FamiliarCombat)
        {
            LocalizationService.HandleServerReply(EntityManager, user, "Familiar combat is not enabled.");
            return;
        }

        if (steamId.TryGetFamiliarActives(out var data) && !data.FamKey.Equals(0)) // 0 means no active familiar
        {
            Entity familiar = FamiliarUtilities.FindPlayerFamiliar(character); // return following entity matching Guidhash in FamiliarActives

            if (!familiar.Exists() && data.Familiar.Exists())
            {
                familiar = data.Familiar;
            }

            if (familiar == Entity.Null)
            {
                LocalizationService.HandleServerReply(EntityManager, user, "No active familiar found to enable/disable combat mode for.");
                return;
            }

            if (ServerGameManager.HasBuff(familiar, InvulnerableBuff.ToIdentifier())) // remove and enable combat
            {
                BuffUtility.BuffSpawner buffSpawner = BuffUtility.BuffSpawner.Create(ServerGameManager);
                EntityCommandBuffer entityCommandBuffer = EntityCommandBufferSystem.CreateCommandBuffer();
                BuffUtility.TryRemoveBuff(ref buffSpawner, entityCommandBuffer, InvulnerableBuff, familiar);

                FactionReference factionReference = familiar.Read<FactionReference>();
                factionReference.FactionGuid._Value = PlayerFaction;
                familiar.Write(factionReference);

                AggroConsumer aggroConsumer = familiar.Read<AggroConsumer>();
                aggroConsumer.Active._Value = true;
                familiar.Write(aggroConsumer);

                Aggroable aggroable = familiar.Read<Aggroable>();
                aggroable.Value._Value = true;
                aggroable.DistanceFactor._Value = 1f;
                aggroable.AggroFactor._Value = 1f;
                familiar.Write(aggroable);

                LocalizationService.HandleServerReply(EntityManager, user, "Familiar combat <color=green>enabled</color>.");
            }
            else // if not, disable combat
            {
                FactionReference factionReference = familiar.Read<FactionReference>();
                factionReference.FactionGuid._Value = IgnoredFaction;
                familiar.Write(factionReference);

                AggroConsumer aggroConsumer = familiar.Read<AggroConsumer>();
                aggroConsumer.Active._Value = false;
                aggroConsumer.AggroTarget._Entity = Entity.Null;
                aggroConsumer.AlertTarget._Entity = Entity.Null;
                familiar.Write(aggroConsumer);

                Aggroable aggroable = familiar.Read<Aggroable>();
                aggroable.Value._Value = false;
                aggroable.DistanceFactor._Value = 0f;
                aggroable.AggroFactor._Value = 0f;
                familiar.Write(aggroable);

                ApplyBuffDebugEvent applyBuffDebugEvent = new()
                {
                    BuffPrefabGUID = InvulnerableBuff,
                };

                FromCharacter fromCharacter = new()
                {
                    Character = familiar,
                    User = familiar,
                };

                DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
                if (ServerGameManager.TryGetBuff(familiar, InvulnerableBuff.ToIdentifier(), out Entity invlunerableBuff))
                {
                    if (invlunerableBuff.Has<LifeTime>())
                    {
                        var lifetime = invlunerableBuff.Read<LifeTime>();
                        lifetime.Duration = -1;
                        lifetime.EndAction = LifeTimeEndAction.None;
                        invlunerableBuff.Write(lifetime);
                    }
                }

                LocalizationService.HandleServerReply(EntityManager, user, "Familiar combat <color=red>disabled</color>.");
            }
        }
        else
        {
            LocalizationService.HandleServerReply(EntityManager, user, "No active familiar found to enable/disable combat mode for...");
        }
    }
    static void HandleExoForm(User user, Entity character, ulong steamId) // moved this to after the taunt buff being destroyed so the visual before transforming looks nicer but leaving here for now >_>
    {
        // check for cooldown here and other such qualifiers before proceeding, also charge at 15 seconds of form time a day for level 1 up to maxDuration seconds of form time at max exo
        BuffUtilities.UpdateExoFormChargeStored(steamId);

        if (steamId.TryGetPlayerExoFormData(out var exoFormData) && exoFormData.Value < BuffUtilities.BaseDuration)
        {
            int exoLevel = steamId.TryGetPlayerPrestiges(out var prestiges) && prestiges.TryGetValue(PrestigeType.Exo, out int exoPrestiges) ? exoPrestiges : 0;
            float totalDuration = BuffUtilities.CalculateFormDuration(exoLevel);

            float chargeNeeded = BuffUtilities.BaseDuration - exoFormData.Value;
            float ratioToTotal = chargeNeeded / totalDuration;
            float secondsRequired = 86400f * ratioToTotal;

            // Convert seconds to hours, minutes, and seconds
            TimeSpan timeSpan = TimeSpan.FromSeconds(secondsRequired);
            string timeRemaining;

            // Format based on the amount of time left
            if (timeSpan.TotalHours >= 1)
            {
                // Display hours, minutes, and seconds if more than an hour remains
                timeRemaining = $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m {timeSpan.Seconds}s";
            }
            else
            {
                // Display only minutes and seconds if less than an hour remains
                timeRemaining = $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
            }

            LocalizationService.HandleServerReply(EntityManager, user, $"Not enough energy to maintain form... (<color=yellow>{timeRemaining}</color> remaining)");
            return;
        }

        if (!character.HasBuff(ExoFormBuff))
        {
            BuffUtilities.TryApplyBuff(character, PhasingBuff);
            BuffUtilities.TryApplyBuff(character, ExoFormBuff);
            BuffUtilities.TryApplyBuff(character, PhasingBuff);
        }
        else if (character.TryGetBuff(ExoFormBuff, out Entity buffEntity))
        {
            BuffUtilities.UpdatePartialExoFormChargeUsed(buffEntity, steamId);
            DestroyUtility.Destroy(EntityManager, buffEntity);
        }
    }
}
