using Bloodcraft.Services;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using static Bloodcraft.Systems.Leveling.LevelingSystem;
using User = ProjectM.Network.User;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class DealDamageSystemPatch
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static DebugEventsSystem DebugEventsSystem => SystemService.DebugEventsSystem;

    static readonly GameModeType GameMode = SystemService.ServerGameSettingsSystem._Settings.GameModeType;

    static readonly Random Random = new();

    public static readonly Dictionary<ulong, DateTime> LastDamageTime = [];

    static readonly Dictionary<PlayerClass, PrefabGUID> ClassOnHitDebuffMap = new() // tier 1
    {
        { PlayerClass.BloodKnight, new(-1246704569) }, //leech
        { PlayerClass.DemonHunter, new(-1576512627) }, //static
        { PlayerClass.VampireLord, new(27300215) }, // chill
        { PlayerClass.ShadowBlade, new(348724578) }, // ignite
        { PlayerClass.ArcaneSorcerer, new(1723455773) }, // weaken
        { PlayerClass.DeathMage, new(-325758519) } // condemn
    };

    static readonly Dictionary<PlayerClass, PrefabGUID> ClassOnHitEffectMap = new() // tier 2
    {
        { PlayerClass.BloodKnight, new(2085766220) }, // lesser bloodrage
        { PlayerClass.DemonHunter, new(-737425100) }, // lesser stormshield
        { PlayerClass.VampireLord, new(620130895) }, // lesser frozenweapon
        { PlayerClass.ShadowBlade, new(763939566) }, // lesser powersurge
        { PlayerClass.ArcaneSorcerer, new(1433921398) }, // lesser aegis
        { PlayerClass.DeathMage, new(-2071441247) } // guardian block :p
    };

    static readonly PrefabGUID stormShield03 = new(1095865904);
    static readonly PrefabGUID stormShield02 = new(-1192885497);
    static readonly PrefabGUID stormShield01 = new(1044565673);
    static readonly PrefabGUID garlicDebuff = new(-1701323826);
    static readonly PrefabGUID silverDebuff = new(853298599);

    static readonly bool Classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;
    static readonly bool OnHitEffects = ConfigService.ClassSpellSchoolOnHitEffects;
    static readonly bool Familiars = ConfigService.FamiliarSystem;
    static readonly bool Quests = ConfigService.QuestSystem;

    static readonly float OnHitProcChance = ConfigService.OnHitProcChance;

    [HarmonyPatch(typeof(DealDamageSystem), nameof(DealDamageSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(DealDamageSystem __instance)
    {
        if (!Core.hasInitialized) return;

        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out DealDamageEvent dealDamageEvent) || !dealDamageEvent.SpellSource.TryGetComponent(out PrefabGUID sourcePrefabGUID) || !dealDamageEvent.SpellSource.TryGetComponent(out EntityOwner entityOwner)) continue;
                else if (sourcePrefabGUID.Equals(silverDebuff) || sourcePrefabGUID.Equals(garlicDebuff)) continue; // these both count for physical damage so need to check source prefab first
                else if (!dealDamageEvent.Target.Exists() || !dealDamageEvent.SpellSource.Exists()) continue; // checks are kind of excessive here but null entities here do weird things I don't want to have to figure out >_>
                
                //else if (dealDamageEvent.SpellSource.TryGetComponent(out PrefabGUID sourcePrefabGUID) && (sourcePrefabGUID.Equals(silverDebuff) || sourcePrefabGUID.Equals(garlicDebuff))) continue; // skip if source is silver or garlic
                //if (!dealDamageEvent.SpellSource.TryGetComponent(out EntityOwner entityOwner) || !entityOwner.Owner.Exists()) continue;
                
                if (entityOwner.Owner.TryGetFollowedPlayer(out Entity player)) // not sure if any fam besides raziel does this
                {
                    if (dealDamageEvent.Target.IsPlayer() && player.Equals(dealDamageEvent.Target))
                    {
                        EntityManager.DestroyEntity(entity); // need to destroy with main entityManager, destroyEvent not sufficient to prevent damage
                    }
                }
                else if (dealDamageEvent.MainType == MainDamageType.Holy) // do anti-healing from too much holy resist + damage reduction? 155 75.5% | 170 85% holy resist
                {
                    //Core.Log.LogInfo($"MainFactor: {dealDamageEvent.MainFactor} | Modifier: {dealDamageEvent.Modifier} | RawDamage: {dealDamageEvent.RawDamage} | RawDamagePercent: {dealDamageEvent.RawDamagePercent}");
                }
                //if (dealDamageEvent.MainType != MainDamageType.Physical && dealDamageEvent.MainType != MainDamageType.Spell) continue; // skip if source isn't phys/spell at this point
                else if (dealDamageEvent.MainType == MainDamageType.Physical || dealDamageEvent.MainType == MainDamageType.Spell)
                {
                    if (Quests && dealDamageEvent.Target.Has<YieldResourcesOnDamageTaken>() && entityOwner.Owner.TryGetPlayer(out player))
                    {
                        ulong steamId = player.GetSteamId();
                        LastDamageTime[steamId] = DateTime.UtcNow;
                    }
                    else if (OnHitEffects && dealDamageEvent.Target.Has<Movement>() && dealDamageEvent.Target.Has<Health>() && entityOwner.Owner.TryGetPlayer(out player) && !dealDamageEvent.Target.IsPlayer())
                    {
                        Entity userEntity = player.Read<PlayerCharacter>().UserEntity;
                        ulong steamId = userEntity.Read<User>().PlatformId;
                        
                        if (!Utilities.Classes.HasClass(steamId)) continue;
                        PlayerClass playerClass = Utilities.Classes.GetPlayerClass(steamId);

                        if (Random.NextDouble() <= OnHitProcChance)
                        {
                            PrefabGUID prefabGUID = ClassOnHitDebuffMap[playerClass];

                            if (ServerGameManager.HasBuff(dealDamageEvent.Target, prefabGUID.ToIdentifier()))
                            {
                                prefabGUID = ClassOnHitEffectMap[playerClass];

                                if (playerClass.Equals(PlayerClass.DemonHunter))
                                {
                                    if (!player.HasBuff(stormShield03))
                                    {
                                        if (!player.HasBuff(stormShield02))
                                        {
                                            if (!player.HasBuff(stormShield01))
                                            {
                                                Buffs.TryApplyBuff(player, stormShield01);
                                            }
                                            else if (player.TryGetBuff(stormShield01, out Entity stormShieldFirstBuff))
                                            {
                                                stormShieldFirstBuff.With((ref Age age) =>
                                                {
                                                    age.Value = 0f;
                                                });

                                                Buffs.TryApplyBuff(player, stormShield02);
                                            }
                                        }
                                        else if (player.TryGetBuff(stormShield02, out Entity stormShieldSecondBuff))
                                        {
                                            stormShieldSecondBuff.With((ref Age age) =>
                                            {
                                                age.Value = 0f;
                                            });

                                            Buffs.TryApplyBuff(player, stormShield03);
                                        }
                                    }
                                    else if (player.TryGetBuff(stormShield03, out Entity stormShieldThirdBuff))
                                    {
                                        stormShieldThirdBuff.With((ref Age age) =>
                                        {
                                            age.Value = 0f;
                                        });
                                    }
                                }
                                else
                                {
                                    Buffs.TryApplyBuff(player, prefabGUID);
                                }
                            }
                            else
                            {
                                if (Buffs.TryApplyBuff(dealDamageEvent.Target, prefabGUID) && dealDamageEvent.Target.TryGetBuff(prefabGUID, out Entity buffEntity))
                                {
                                    buffEntity.With((ref EntityOwner entityOwner) =>
                                    {
                                        entityOwner.Owner = player;
                                    });
                                }
                            }
                        }
                    }
                    else if (Familiars)
                    {
                        if (entityOwner.Owner.TryGetPlayer(out player) && GameMode.Equals(GameModeType.PvP) && dealDamageEvent.Target.IsPlayer())
                        {
                            Entity familiar = Utilities.Familiars.FindPlayerFamiliar(player);

                            if (familiar.Exists() && !familiar.IsDisabled())
                            {
                                Utilities.Familiars.AddToFamiliarAggroBuffer(familiar, dealDamageEvent.Target);
                            }
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
}
