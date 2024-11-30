using Bloodcraft.Services;
using Bloodcraft.Systems.Legacies;
using Bloodcraft.Systems.Professions;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using static Il2CppSystem.Data.Common.ObjectStorage;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class BuffSystemSpawnPatches
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static DebugEventsSystem DebugEventsSystem => SystemService.DebugEventsSystem;

    static readonly GameModeType GameMode = SystemService.ServerGameSettingsSystem._Settings.GameModeType;

    static readonly bool EliteShardBearers = ConfigService.EliteShardBearers;
    static readonly bool Legacies = ConfigService.BloodSystem;
    static readonly bool Professions = ConfigService.ProfessionSystem;
    static readonly bool PotionStacking = ConfigService.PotionStacking;
    static readonly bool Familiars = ConfigService.FamiliarSystem;

    static readonly bool FamiliarPvP = ConfigService.FamiliarPvP;

    static readonly float MaxProfessionLevel = ConfigService.MaxProfessionLevel;

    static readonly PrefabGUID Solarus = new(-740796338);

    static readonly PrefabGUID DraculaReturnBuff = new(404387047);
    static readonly PrefabGUID ChannelHealBuff = new(478901515);
    static readonly PrefabGUID HighlordSwordSpawnBuff = new(-6635580);
    static readonly PrefabGUID HighlordSwordPermaBuff = new(-916946628);
    static readonly PrefabGUID HolyBeamPowerBuff = new(-1584595113);

    static readonly PrefabGUID PvPProtectedBuff = new(1111481396);
    static readonly PrefabGUID PvPCombatBuff = new(697095869);
    static readonly PrefabGUID PvECombatBuff = new(581443919);
    static readonly PrefabGUID PhasingBuff = new(-79611032);
    static readonly PrefabGUID GateBossFeedComplete = new(-354622715);

    static readonly PrefabGUID WitchPigTransformationBuff = new(1356064917);

    static readonly PrefabGUID WranglerPotionBuff = new(387154469);

    [HarmonyPatch(typeof(BuffSystem_Spawn_Server), nameof(BuffSystem_Spawn_Server.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(BuffSystem_Spawn_Server __instance)
    {
        if (!Core.hasInitialized) return;

        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out EntityOwner entityOwner) || !entityOwner.Owner.Exists() || !entity.TryGetComponent(out PrefabGUID prefabGUID)) continue;

                Entity buffTarget = entity.GetBuffTarget();
                if (!buffTarget.Exists()) continue;

                string prefabName = prefabGUID.LookupName().ToLower();

                //int guidHash = prefabGUID.GuidHash;
                //Core.Log.LogInfo($"BuffSystem_Spawn_Server: {prefabName}");

                if (prefabName.Contains("holybubble") && EliteShardBearers) // holy mortar effect for Solarus when eliteShardBearers active
                {
                    Entity character = buffTarget;

                    if (character.Read<PrefabGUID>().Equals(Solarus) && !ServerGameManager.HasBuff(character, HolyBeamPowerBuff))
                    {
                        ApplyBuffDebugEvent applyBuffDebugEvent = new()
                        {
                            BuffPrefabGUID = HolyBeamPowerBuff,
                        };

                        FromCharacter fromCharacter = new()
                        {
                            Character = character,
                            User = character
                        };

                        DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
                        if (ServerGameManager.TryGetBuff(character, HolyBeamPowerBuff.ToIdentifier(), out Entity buff))
                        {
                            if (buff.Has<LifeTime>())
                            {
                                var lifetime = buff.Read<LifeTime>();
                                lifetime.Duration = -1;
                                lifetime.EndAction = LifeTimeEndAction.None;
                                buff.Write(lifetime);
                            }
                        }
                    }
                }
                else if (Familiars && prefabGUID.Equals(WitchPigTransformationBuff) && buffTarget.Has<VBloodUnit>())
                {
                    DestroyUtility.Destroy(EntityManager, entity);
                }
                /*
                else if (Legacies && prefabGUID.Equals(GateBossFeedComplete) && buffTarget.TryGetPlayer(out Entity player))
                {
                    Blood blood = player.Read<Blood>();
                    ulong steamId = player.GetSteamId();

                    if (entity.Has<ChangeBloodOnGameplayEvent>())
                    {
                        var buffer = entity.ReadBuffer<ChangeBloodOnGameplayEvent>();

                        ChangeBloodOnGameplayEvent changeBlood = buffer[0];
                        changeBlood.BloodValue = blood.Value + 50f;
                        changeBlood.BloodQuality = Math.Min(blood.Quality, 100f);
                        changeBlood.BloodType = blood.BloodType;
                        changeBlood.GainBloodType = GainBloodType.Consumable;

                        buffer[0] = changeBlood;
                    }

                    BloodSystem.SkipBloodUpdate.Add(steamId);
                }
                */
                else if (Familiars && prefabGUID.Equals(PhasingBuff) && buffTarget.TryGetPlayer(out Entity player)) // teleport familiar to player after waygate and autoCall if was out before
                {
                    User user = player.GetUser();
                    ulong steamId = user.PlatformId;

                    if (steamId.TryGetFamiliarActives(out var data) && FamiliarUtilities.AutoCallMap.TryGetValue(player, out Entity familiar) && familiar.Exists())
                    {
                        FamiliarUtilities.CallFamiliar(player, familiar, user, steamId, data);
                        FamiliarUtilities.AutoCallMap.Remove(player);
                    }
                }
                else if (Familiars && prefabGUID.Equals(PvECombatBuff)) // return familiar when entering combat if far enough away
                {
                    if (buffTarget.TryGetPlayer(out player))
                    {
                        Entity familiar = FamiliarUtilities.FindPlayerFamiliar(player);

                        if (familiar.Exists())
                        {
                            familiar.With((ref Follower follower) =>
                            {
                                follower.ModeModifiable._Value = 1;
                            });

                            FamiliarUtilities.TryReturnFamiliar(player, familiar);
                        }
                    }
                }
                else if (Familiars && prefabGUID.Equals(PvPCombatBuff))
                {
                    if (buffTarget.TryGetPlayer(out player))
                    {
                        Entity userEntity = player.GetUserEntity();
                        ulong steamId = player.GetSteamId();

                        Entity familiar = FamiliarUtilities.FindPlayerFamiliar(player);

                        if (familiar.Exists())
                        {
                            familiar.With((ref Follower follower) =>
                            {
                                follower.ModeModifiable._Value = 1;
                            });

                            FamiliarUtilities.TryReturnFamiliar(player, familiar);
                            if (!FamiliarPvP) FamiliarUtilities.UnbindFamiliar(player, userEntity, steamId);
                        }
                    }
                }
                else if (Familiars && prefabName.Contains("emote_onaggro") && buffTarget.TryGetFollowedPlayer(out player))
                {
                    ulong steamId = player.GetSteamId();

                    if (!PlayerUtilities.GetPlayerBool(steamId, "VBloodEmotes"))
                    {
                        DestroyUtility.Destroy(EntityManager, entity);
                    }
                }
                else if (prefabName.Contains("consumable") && buffTarget.TryGetPlayer(out player)) // alchemy bonuses/potion stacking/familiar sharing
                {
                    ulong steamId = player.GetSteamId();

                    if (PotionStacking && !prefabName.Contains("holyresistance")) // stack t01/t02 potion effects
                    {
                        if (entity.Has<RemoveBuffOnGameplayEvent>()) entity.Remove<RemoveBuffOnGameplayEvent>();
                        if (entity.Has<RemoveBuffOnGameplayEventEntry>()) entity.Remove<RemoveBuffOnGameplayEventEntry>();
                    }

                    if (Familiars && !prefabGUID.Equals(WranglerPotionBuff))
                    {
                        Entity familiar = FamiliarUtilities.FindPlayerFamiliar(player);

                        if (familiar.Exists())
                        {
                            BuffUtilities.TryApplyBuff(familiar, prefabGUID);
                        }
                    }

                    if (Professions) // apply alchemy bonuses
                    {
                        IProfessionHandler handler = ProfessionHandlerFactory.GetProfessionHandler(prefabGUID, "alchemy");
                        int level = handler.GetProfessionData(steamId).Key;

                        if (entity.Has<LifeTime>())
                        {
                            LifeTime lifeTime = entity.Read<LifeTime>();
                            if (lifeTime.Duration != -1) lifeTime.Duration *= (float)(1 + (float)level / (float)MaxProfessionLevel);
                            entity.Write(lifeTime);
                        }

                        if (entity.Has<ModifyUnitStatBuff_DOTS>())
                        {
                            var buffer = entity.ReadBuffer<ModifyUnitStatBuff_DOTS>();
                            for (int i = 0; i < buffer.Length; i++)
                            {
                                ModifyUnitStatBuff_DOTS statBuff = buffer[i];
                                statBuff.Value *= (float)(1 + (float)level / (float)MaxProfessionLevel);
                                buffer[i] = statBuff;
                            }
                        }
                    }
                }
                else if (Familiars && buffTarget.TryGetFollowedPlayer(out player)) // cassius, drac, other weird boss phase stuff. ultimately checking for specific prefabs, chain with the above and just check at the end
                {
                    Entity familiar = FamiliarUtilities.FindPlayerFamiliar(player);

                    if (familiar.Exists())
                    {
                        if (prefabGUID.Equals(DraculaReturnBuff))
                        {
                            DestroyUtility.Destroy(EntityManager, entity);
                        }
                        else if (prefabGUID.Equals(HighlordSwordSpawnBuff))
                        {
                            if (ServerGameManager.TryGetBuff(familiar, HighlordSwordPermaBuff.ToIdentifier(), out Entity swordPermabuff))
                            {
                                if (swordPermabuff.Has<AmplifyBuff>()) swordPermabuff.Remove<AmplifyBuff>();
                            }
                        }
                        else if (prefabGUID.Equals(HolyBeamPowerBuff))
                        {
                            if (entity.Has<LifeTime>()) entity.Write(new LifeTime { Duration = 30f, EndAction = LifeTimeEndAction.Destroy });
                        }
                    }
                }
                else if (GameMode.Equals(GameModeType.PvE) && buffTarget.IsPlayer())
                {
                    Entity owner = entityOwner.Owner;

                    if (owner.IsPlayer() && !owner.Equals(buffTarget))
                    {
                        Buff buff = entity.Read<Buff>();
                        if (buff.BuffEffectType.Equals(BuffEffectType.Debuff)) DestroyUtility.Destroy(EntityManager, entity);
                    }
                    else if (Familiars)
                    {
                        if (owner.IsFollowingPlayer())
                        {
                            Buff buff = entity.Read<Buff>();
                            if (buff.BuffEffectType.Equals(BuffEffectType.Debuff)) DestroyUtility.Destroy(EntityManager, entity);
                        }
                        else if (owner.TryGetComponent(out entityOwner) && entityOwner.Owner.IsFollowingPlayer())
                        {
                            Buff buff = entity.Read<Buff>();
                            if (buff.BuffEffectType.Equals(BuffEffectType.Debuff)) DestroyUtility.Destroy(EntityManager, entity);
                        }
                    }
                }
                else if (GameMode.Equals(GameModeType.PvP) && buffTarget.IsPlayer())
                {
                    Entity owner = entityOwner.Owner;
                    bool pvpProtected = buffTarget.HasBuff(PvPProtectedBuff);

                    if (owner.IsPlayer() && !owner.Equals(buffTarget) && pvpProtected) 
                    {
                        Buff buff = entity.Read<Buff>();
                        if (buff.BuffEffectType.Equals(BuffEffectType.Debuff)) DestroyUtility.Destroy(EntityManager, entity);
                    }
                    else if (Familiars && pvpProtected)
                    {
                        if (owner.IsFollowingPlayer())
                        {
                            Buff buff = entity.Read<Buff>();
                            if (buff.BuffEffectType.Equals(BuffEffectType.Debuff)) DestroyUtility.Destroy(EntityManager, entity);
                        }
                        else if (owner.TryGetComponent(out entityOwner) && entityOwner.Owner.IsFollowingPlayer())
                        {
                            Buff buff = entity.Read<Buff>();
                            if (buff.BuffEffectType.Equals(BuffEffectType.Debuff)) DestroyUtility.Destroy(EntityManager, entity);
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

    // methods for refactoring since this patch is getting a bit heavy, #soonTM
    static void HandleGateBossFeed(Entity buffEntity)
    {
        if (buffEntity.GetBuffTarget().TryGetPlayer(out Entity player))
        {
            Blood blood = player.Read<Blood>();
            ulong steamId = player.GetSteamId();

            if (buffEntity.Has<ChangeBloodOnGameplayEvent>())
            {
                var buffer = buffEntity.ReadBuffer<ChangeBloodOnGameplayEvent>();

                ChangeBloodOnGameplayEvent changeBlood = buffer[0];
                changeBlood.BloodValue = blood.Value + 50f;
                changeBlood.BloodQuality = Math.Min(blood.Quality, 100f);
                changeBlood.BloodType = blood.BloodType;
                changeBlood.GainBloodType = GainBloodType.Consumable;

                buffer[0] = changeBlood;
            }

            BloodSystem.SkipBloodUpdate.Add(steamId);
        }
    }
    static void HandleEliteSolarusFinalPhase(Entity character)
    {
        if (character.Read<PrefabGUID>().Equals(Solarus) && !ServerGameManager.HasBuff(character, HolyBeamPowerBuff))
        {
            ApplyBuffDebugEvent applyBuffDebugEvent = new()
            {
                BuffPrefabGUID = HolyBeamPowerBuff,
            };

            FromCharacter fromCharacter = new()
            {
                Character = character,
                User = character
            };

            DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
            if (ServerGameManager.TryGetBuff(character, HolyBeamPowerBuff.ToIdentifier(), out Entity buff))
            {
                if (buff.Has<LifeTime>())
                {
                    var lifetime = buff.Read<LifeTime>();
                    lifetime.Duration = -1;
                    lifetime.EndAction = LifeTimeEndAction.None;
                    buff.Write(lifetime);
                }
            }
        }
    }
    static void HandleWaygatePhasing(Entity buffTarget)
    {
        if (buffTarget.TryGetPlayer(out Entity playerCharacter)) // teleport familiar to player after waygate and autoCall if was out before
        {
            User user = playerCharacter.GetUser();
            ulong steamId = user.PlatformId;

            if (steamId.TryGetFamiliarActives(out var data) && FamiliarUtilities.AutoCallMap.TryGetValue(playerCharacter, out Entity familiar) && familiar.Exists())
            {
                FamiliarUtilities.CallFamiliar(playerCharacter, familiar, user, steamId, data);
                FamiliarUtilities.AutoCallMap.Remove(playerCharacter);
            }
        }
    }
    static void HandlePvECombat(Entity buffTarget)
    {
        if (buffTarget.TryGetPlayer(out Entity playerCharacter))
        {
            Entity familiar = FamiliarUtilities.FindPlayerFamiliar(playerCharacter);

            if (EntityManager.Exists(familiar))
            {
                FamiliarUtilities.TryReturnFamiliar(playerCharacter, familiar);
            }
        }
    }
}
/* need to improve logic in this patch and finish switch statement but not tonight x_x
switch (guidHash)
{
    case -354622715:
        if (ConfigService.BloodSystem) HandleGateBossFeed(entity);
        break;
    case -79611032:
        if (ConfigService.FamiliarSystem) HandleWaygatePhasing(buffTarget);
        break;
    case 581443919:
        if (ConfigService.FamiliarSystem) HandlePvECombat(buffTarget);
        break;
}
*/