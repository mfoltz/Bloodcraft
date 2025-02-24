using Bloodcraft.Utilities;
using System.Collections;
using Unity.Entities;
using UnityEngine;
using static Bloodcraft.Services.PlayerService;

namespace Bloodcraft.Services;

internal class FamiliarService
{
    static readonly WaitForSeconds _delay = new(10f);

    static readonly Entity _nullCheck = Entity.Null;
    public FamiliarService()
    {
        DisabledFamiliarPositionUpdateRoutine().Start();
    }
    static IEnumerator DisabledFamiliarPositionUpdateRoutine()
    {
        while (true)
        {
            yield return _delay;

            foreach (var kvp in DataService.PlayerDictionaries._familiarActives)
            {
                ulong steamId = kvp.Key;
                Entity familiar = kvp.Value.Familiar;

                if (familiar.Equals(_nullCheck))
                {
                    yield return null;

                    continue;
                }
                else if (familiar.IsDisabled() && steamId.TryGetPlayerInfo(out PlayerInfo playerInfo))
                {
                    Familiars.TryReturnFamiliar(playerInfo.CharEntity, familiar);
                }

                yield return null;
            }
        }
    }
}

