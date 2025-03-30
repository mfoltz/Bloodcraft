using Bloodcraft.Utilities;
using ProjectM.Behaviours;
using System.Collections;
using Unity.Entities;
using UnityEngine;
using static Bloodcraft.Services.PlayerService;

namespace Bloodcraft.Services;

internal class FamiliarService
{
    static readonly WaitForSeconds _delay = new(10f);
    public FamiliarService()
    {
        DisabledFamiliarPositionUpdateRoutine().Start();
    }
    static IEnumerator DisabledFamiliarPositionUpdateRoutine()
    {
        while (true)
        {
            yield return _delay;

            foreach (var kvp in Familiars.ActiveFamiliarManager.ActiveFamiliars)
            {
                ulong steamId = kvp.Key;
                Entity familiar = kvp.Value.Familiar;

                if (!familiar.HasValue())
                {
                    yield return null;
                }
                else if (kvp.Value.Dismissed && steamId.TryGetPlayerInfo(out PlayerInfo playerInfo))
                {
                    Familiars.TryReturnFamiliar(playerInfo.CharEntity, familiar);
                    yield return null;
                }
            }
        }
    }
}

