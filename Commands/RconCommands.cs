using Bloodcraft.Services;
using Bloodcraft.Systems;
using Bloodcraft.Utilities;
using ScarletRCON.Shared;
using Stunlock.Core;
using Unity.Entities;
using static Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarUnlocksManager;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Utilities.Familiars;
using static Bloodcraft.Utilities.VEvents;

namespace Bloodcraft.Commands;

[RconCommandCategory("Bloodcraft")]
public static class RconCommands
{
    [RconCommand("familiar.add", "Admin add familiar RCON command.", "<playerName> <prefabGuid>")]
    public static string AddFamiliar(string playerName, string prefabGuid)
    {
        PlayerInfo playerInfo = GetPlayerInfo(playerName);
        ulong steamId = playerInfo.User.PlatformId;

        if (!playerInfo.CharEntity.Exists())
        {
            Core.Log.LogWarning($"[RCON] Attempted to add {prefabGuid} for {playerName} but they couldn't be found!");
            return false.ToString();
        }

        if (!ConfigService.FamiliarSystem)
        {
            Core.Log.LogWarning($"[RCON] Attempted to add {prefabGuid} for {playerName} but familiars are disabled!");
            return false.ToString();
        }

        bool hasResponse;
        if (steamId.TryGetFamiliarBox(out string activeSet) && !string.IsNullOrEmpty(activeSet))
        {
            ParseAddedFamiliar(playerInfo, steamId, prefabGuid, activeSet);
            hasResponse = true;
        }
        else
        {
            FamiliarUnlocksData unlocksData = LoadFamiliarUnlocksData(steamId);
            string lastListName = unlocksData.FamiliarUnlocks.Keys.LastOrDefault();

            if (string.IsNullOrEmpty(lastListName))
            {
                lastListName = $"box{unlocksData.FamiliarUnlocks.Count + 1}";
                unlocksData.FamiliarUnlocks[lastListName] = [];

                SaveFamiliarUnlocksData(steamId, unlocksData);
                ParseAddedFamiliar(playerInfo, steamId, prefabGuid, lastListName);
                hasResponse = true;
            }
            else
            {
                ParseAddedFamiliar(playerInfo, steamId, prefabGuid, lastListName);
                hasResponse = true;
            }
        }


        if (hasResponse)
            return $"Added familiar {new PrefabGUID(int.Parse(prefabGuid)).GetPrefabName()} for {playerName}!";

        return false.ToString();
    }

    [RconCommand("familiar.shiny", "Admin shiny buff RCON command.", "<playerName> <prefabGuid>")]
    public static string MakeSparkle(string playerName, string prefabGuid)
    {
        PlayerInfo playerInfo = GetPlayerInfo(playerName);
        Entity playerCharacter = playerInfo.CharEntity;

        if (!playerCharacter.Exists())
        {
            Core.Log.LogWarning($"[RCON] Attempted to apply shiny for {playerName} but they couldn't be found!");
            return false.ToString();
        }

        PrefabGUID buffGuid = PrefabGUID.Parse(prefabGuid);
        Entity prefab = buffGuid.GetPrefabEntity();

        if (!prefab.Exists())
        {
            Core.Log.LogWarning($"[RCON] Attempted to apply shiny {prefabGuid}->{buffGuid} for {playerName} but couldn't resolve prefab entity!");
            return false.ToString();
        }

        Buffs.HandleSparkleBuff(playerCharacter, buffGuid);
        return $"Applied shiny {buffGuid.GetPrefabName()} for {playerName}!";
    }

    [RconCommand("servant.upgrade", "Admin upgrade servant RCON command; returns string true if receipt w/ params found & has valid refund (resend RCON command with same params to ping upgrade status), otherwise returns string false for player not found or on dispatch.", "<playerName> <servantName>")]
    public static string UpgradeServant(string playerName, string servantName)
    {
        PlayerInfo playerInfo = GetPlayerInfo(playerName);
        Entity playerCharacter = playerInfo.CharEntity;

        if (!playerCharacter.Exists())
        {
            Core.Log.LogWarning($"[RCON] Attempted to upgrade servant for {playerName} but they couldn't be found!");
            return false.ToString();
        }

        if (HasRefund(new(playerName, servantName)))
        {
            return true.ToString();
        }

        ServantUpgradeEvent servantUpgradeEvent = new(playerName, servantName);
        Dispatch(servantUpgradeEvent);

        return false.ToString();
    }

    [RconCommand("status.health", "Returns startup readiness state summary.", "Remote-viewable readiness summary.")]
    public static string HealthStatus()
    {
        return StartupStateService.BuildJsonSummary();
    }

    [RconCommand("diagnostics.primal.start", "Attempts to queue a Primal Rift start event for harness diagnostics.", "Returns true:queued or a false:<reason> diagnostic result.")]
    public static string StartPrimalRifts()
    {
        if (!StartupStateService.IsReady())
        {
            return "false:server-not-ready";
        }

        if (!ConfigService.ElitePrimalRifts)
        {
            return "false:elite-primal-rifts-disabled";
        }

        return PrimalWarEventSystem.TryStartPrimalRiftsForDiagnostics(out string reason)
            ? $"true:{reason}"
            : $"false:{reason}";
    }
}
