using Bloodcraft.Systems.Familiars;
using Bloodcraft.Utilities;
using Il2CppInterop.Runtime;
using ProjectM;
using System.Collections;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Utilities.EntityQueries;
using static Bloodcraft.Utilities.Familiars;

namespace Bloodcraft.Services;
internal class FamiliarService
{
    static EntityManager EntityManager => Core.EntityManager;

    static readonly WaitForSeconds _delay = new(10f);

    static readonly ComponentType[] _familiarAllComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<Follower>()),
        ComponentType.ReadOnly(Il2CppType.Of<TeamReference>()),
        ComponentType.ReadOnly(Il2CppType.Of<BlockFeedBuff>())
    ];

    static QueryDesc _familiarQueryDesc;

    static bool _shouldDestroy = true;
    public FamiliarService()
    {
        _familiarQueryDesc = EntityManager.CreateQueryDesc(_familiarAllComponents, options: EntityQueryOptions.IncludeDisabled);
        DisabledFamiliarPositionUpdateRoutine().Start();
    }
    static IEnumerator DisabledFamiliarPositionUpdateRoutine()
    {
        if (_shouldDestroy) DestroyFamiliars();

        while (true)
        {
            yield return _delay;

            foreach (var (steamId, familiarData) in ActiveFamiliarManager.ActiveFamiliars)
            {
                if (!steamId.TryGetPlayerInfo(out var playerInfo)) continue;
                else if (familiarData.Dismissed)
                {
                    TryReturnFamiliar(playerInfo.CharEntity, familiarData.Familiar);
                }

                yield return null;
            }
        }
    }
    static void DestroyFamiliars()
    {
        var entities = _familiarQueryDesc.EntityQuery.ToEntityArray(Allocator.Temp);

        try
        {
            foreach (Entity entity in entities)
            {
                Entity servant = FindFamiliarServant(entity);

                if (servant.Exists())
                {
                    FamiliarBindingSystem.RemoveDropTable(servant);
                    StatChangeUtility.KillOrDestroyEntity(EntityManager, servant, Entity.Null, Entity.Null, Core.ServerTime, StatChangeReason.Default, true);
                }

                if (entity.Exists())
                {
                    FamiliarBindingSystem.RemoveDropTable(entity);
                    StatChangeUtility.KillOrDestroyEntity(EntityManager, entity, Entity.Null, Entity.Null, Core.ServerTime, StatChangeReason.Default, true);
                }
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogWarning($"[PlayerService] DestroyFamiliars() - {ex}");
        }

        _shouldDestroy = false;
    }
}

