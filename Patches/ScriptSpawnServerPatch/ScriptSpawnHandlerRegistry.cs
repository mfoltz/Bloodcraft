using System.Collections.Generic;
using Bloodcraft.Patches.ScriptSpawnServerPatch.Handlers;
using Bloodcraft.Systems.Legacies;
using Bloodcraft.Utilities;
using Bloodcraft.Resources;
using Stunlock.Core;

namespace Bloodcraft.Patches.ScriptSpawnServerPatch;

static class ScriptSpawnHandlerRegistry
{
    static readonly Dictionary<int, List<IScriptSpawnHandler>> _handlers = new();
    static readonly List<IScriptSpawnHandler> _globalHandlers = new();

    static ScriptSpawnHandlerRegistry()
    {
        Register(Buffs.EvolvedVampireBuff.GuidHash, new ShapeshiftAdjustmentHandler());
        Register(Buffs.CorruptedSerpentBuff.GuidHash, new ShapeshiftAdjustmentHandler());
        Register(Buffs.AncientGuardianBuff.GuidHash, new ShapeshiftAdjustmentHandler());

        Register(PrefabGUIDs.AB_Vampire_Dracula_BloodBoltSwarm_BeamTrigger.GuidHash, new BloodBoltTriggerHandler());
        Register(PrefabGUIDs.AB_Vampire_Dracula_BloodBoltSwarm_Trigger.GuidHash, new BloodBoltTriggerHandler());
        Register(PrefabGUIDs.AB_Vampire_Dracula_BloodBoltSwarm_TriggerDeadZonePunish.GuidHash, new BloodBoltTriggerHandler());

        Register(PrefabGUIDs.AB_Vampire_Dracula_BloodBoltSwarm_ChannelBuff.GuidHash, new BloodBoltCooldownHandler());

        Register(Buffs.BonusStatsBuff.GuidHash, new BonusStatBuffHandler());

        Register(Buffs.CastleManCombatBuff.GuidHash, new FamiliarCastleManHandler());

        Register(Buffs.StandardWerewolfBuff.GuidHash, new FamiliarShapeshiftHandler());
        Register(Buffs.VBloodWerewolfBuff.GuidHash, new FamiliarShapeshiftHandler());

        foreach (PrefabGUID buffGuid in BloodSystem.BloodBuffToBloodType.Keys)
        {
            Register(buffGuid.GuidHash, new BloodBuffHandler());
        }

        RegisterGlobal(new FriendlyDebuffHandler());
    }

    static void Register(int hash, IScriptSpawnHandler handler)
    {
        if (!_handlers.TryGetValue(hash, out var list))
        {
            list = new List<IScriptSpawnHandler>();
            _handlers[hash] = list;
        }
        list.Add(handler);
    }

    static void RegisterGlobal(IScriptSpawnHandler handler)
    {
        _globalHandlers.Add(handler);
    }

    public static IEnumerable<IScriptSpawnHandler> Resolve(int hash)
    {
        if (_handlers.TryGetValue(hash, out var list))
        {
            return list;
        }

        return [];
    }

    public static IEnumerable<IScriptSpawnHandler> GlobalHandlers => _globalHandlers;
}
