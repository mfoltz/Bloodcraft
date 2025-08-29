using Bloodcraft.Resources;
using Bloodcraft.Utilities;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.CastleBuilding;
using ProjectM.Network;
using ProjectM.Shared.WarEvents;
using Stunlock.Core;
using System.Collections;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using VampireCommandFramework;

namespace Bloodcraft.Commands;
internal static class DevCommands
{
    static Coroutine _playSequences;
    static int _sequenceIndex = 0;

    [Command(name: "spawnsequence", shortHand: "sq", adminOnly: true, usage: ".sq [SequenceGUID] [Scale]", description: "Spawn specific sequences on target.")]
    public static void SpawnSequenceGuid(ChatCommandContext ctx, int sequenceGuid, float scale = 1f)
    {
        Entity playerCharacter = ctx.Event.SenderCharacterEntity;
        EntityInput entityInput = playerCharacter.GetInput();
        Entity hovered = entityInput.HoveredEntity;

        if (!hovered.Exists())
        {
            playerCharacter.PlaySequence(new(sequenceGuid));
        }
        else
        {
            hovered.PlaySequence(new(sequenceGuid));
        }
    }

    [Command(name: "spawnsequences", shortHand: "ssq", adminOnly: true, usage: ".ssq", description: "testing")]
    public static void SpawnSequenceGuids(ChatCommandContext ctx, int index = 0)
    {
        Entity entity = ctx.Event.SenderCharacterEntity.GetInput().HoveredEntity.Exists()
            ? ctx.Event.SenderCharacterEntity.GetInput().HoveredEntity
            : ctx.Event.SenderCharacterEntity;

        if (index >= 0)
            _sequenceIndex = index;

        _playSequences?.Stop();
        _playSequences = PlaySequences(entity).Start();
    }
    static IEnumerator PlaySequences(Entity playerCharacter)
    {
        var fields = typeof(SequenceGUIDs).GetFields(
                 System.Reflection.BindingFlags.Public |
                 System.Reflection.BindingFlags.Static);

        foreach (var field in fields.Skip(_sequenceIndex))
        {
            SequenceGUID sequenceGUID = (SequenceGUID)field.GetValue(null);
            playerCharacter.PlaySequence(sequenceGUID);
            _sequenceIndex++;

            yield return new WaitForSeconds(5f);
        }
    }

    static Entity _castleHeart;

    [Command(name: "stripteamstest", shortHand: "stt", adminOnly: true, usage: ".stt", description: "testing PvE raid mechanic ideas")]
    public static void RaidAggroTesting(ChatCommandContext ctx)
    {
        Entity playerCharacter = ctx.Event.SenderCharacterEntity;
        AddOrRemoveCastleTeamReferences(playerCharacter);
    }

    [Command(name: "spawnprefab", shortHand: "sp", adminOnly: true, usage: ".sp [PrefabGUID]", description: "testing")]
    public static void PrefabTesting(ChatCommandContext ctx, int guidHash)
    {
        PrefabGUID prefabGuid = new(guidHash);
        Entity prefab = prefabGuid.GetPrefabEntity();

        if (!prefab.Exists())
        {
            ctx.Reply($"Prefab with GUID {guidHash} not found!");
            return;
        }

        EntityInput entityInput = ctx.Event.SenderCharacterEntity.GetInput();
        float3 position = entityInput.AimPosition;

        Entity entity = Core.ServerGameManager.InstantiateEntityImmediate(Entity.Null, prefab);
        entity.SetPosition(position);

        if (prefabGuid.Equals(PrefabGUIDs.CHAR_Militia_Fabian_VBlood))
        {
        }
    }

    [Command(name: "buffhovered", shortHand: "bh", adminOnly: true, usage: ".bh [PrefabGUID]", description: "testing")]
    public static void BuffHovered(ChatCommandContext ctx, int guidHash)
    {
        PrefabGUID prefabGuid = new(guidHash);
        EntityInput entityInput = ctx.Event.SenderCharacterEntity.GetInput();
        Entity hovered = entityInput.HoveredEntity;

        if (!hovered.Exists())
        {
            ctx.Reply("No hovered entity!");
            return;
        }

        hovered.TryApplyBuff(prefabGuid);
    }

    [Command(name: "pathingtest", shortHand: "pt", adminOnly: true, usage: ".pt", description: "testing PvE raid mechanic ideas")]
    public static void RaidPathTesting(ChatCommandContext ctx)
    {
        Entity playerCharacter = ctx.Event.SenderCharacterEntity;
        Entity familiar = Familiars.GetActiveFamiliar(playerCharacter);

        EntityInput entityInput = playerCharacter.GetInput();
        float3 position = entityInput.AimPosition;

        if (!_castleHeart.Exists())
        {
            ctx.Reply("No Castle Heart!");
            return;
        }

        if (familiar.Exists())
        {
            familiar.With((ref Follower follower) =>
            {
                follower.Followed._Value = _castleHeart;
                follower.ModeModifiable._Value = 1;
            });
        }
        else
        {
            ctx.Reply("No Familiar Active!");
        }

        // followerBuffer.Add(new FollowerBuffer { Entity = NetworkedEntity.ServerEntity(familiar) });
    }

    [Command(name: "renametest", shortHand: "rt", adminOnly: true, usage: ".rt [Name]", description: "testing")]
    public static void RenameTest(ChatCommandContext ctx, string newName)
    {
        string oldName = ctx.Event.User.CharacterName.Value;
        Entity entity = ctx.Event.SenderCharacterEntity.GetInput().HoveredEntity.Exists()
            ? ctx.Event.SenderCharacterEntity.GetInput().HoveredEntity
            : ctx.Event.SenderCharacterEntity;

        entity.With((ref PlayerCharacter playerCharacter) => playerCharacter.Name = new(newName));

        // Core.Server.GetExistingSystemManaged<ServerConsoleCommandSystem>().RenamePlayerByPlatformId(steamId, newName);
        ctx.Reply($"Renamed '{oldName}' to '{newName}'!");
    }
    public readonly struct RankTitle
    {
        public char GreekLetter { get; }
        public string LeftDecoration { get; }
        public string RightDecoration { get; }

        public RankTitle(char greekLetter, string leftDecoration, string rightDecoration)
        {
            GreekLetter = greekLetter;
            LeftDecoration = leftDecoration;
            RightDecoration = rightDecoration;
        }

        public override string ToString()
        {
            // Optional: pad or space for style, tweak as needed.
            return $"{LeftDecoration}{GreekLetter}{RightDecoration}";
        }
    }

    [Command(name: "wareventschedule", shortHand: "wes", adminOnly: true, usage: ".wes", description: "testing")]
    public static void StartPrimalWarEvent(ChatCommandContext ctx)
    {
        ComponentType[] _componentTypes =
        [
            ComponentType.ReadOnly(Il2CppType.Of<WarEvent_StartEvent>()),
            ComponentType.ReadOnly(Il2CppType.Of<FromCharacter>()),
            ComponentType.ReadOnly(Il2CppType.Of<NetworkEventType>())
        ];

        NetworkEventType networkEventType = new()
        {
            EventId = NetworkEvents.EventId_WarEvent_StartEvent,
            IsAdminEvent = true,
            IsDebugEvent = true
        };

        WarEvent_StartEvent warEvent = new()
        {
            EventType = WarEventType.Primal,
            EnableAllGates = true
        };

        FromCharacter fromCharacter = new()
        {
            Character = ctx.Event.SenderCharacterEntity,
            User = ctx.Event.SenderUserEntity
        };

        Entity entity = Core.EntityManager.CreateEntity(_componentTypes);
        entity.Write(warEvent);
        entity.Write(fromCharacter);
        entity.Write(networkEventType);
    }
    static void AddOrRemoveCastleTeamReferences(Entity playerCharacter)
    {
        ComponentType[] castleStructureAllComponents =
        [
            ComponentType.ReadOnly(Il2CppType.Of<CastleHeartConnection>())
        ];

        EntityQuery castleStructureQuery = Core.EntityManager.BuildEntityQuery(castleStructureAllComponents);
        var castleStructures = castleStructureQuery.ToEntityArray(Allocator.Temp);
        int count = 0;

        try
        {
            foreach (Entity entity in castleStructures)
            {
                if (!entity.IsAllied(playerCharacter)) continue;
                else if (entity.Has<CastleHeart>() && !_castleHeart.Exists())
                {
                    _castleHeart = entity;
                    Core.Log.LogWarning($"[DevCommands] Found Castle Heart...");
                }
                else if (entity.TryGetComponent(out TeamReference teamReference))
                {
                    teamReference.Value._Value = Entity.Null;
                    count++;
                }
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogError($"Error in AddOrRemoveCastleTeamReferences: {ex}");
        }
        finally
        {
            Core.Log.LogInfo($"[DevCommands] Removed {count} TeamReference entities from Castle Structures!");
            castleStructures.Dispose();
            castleStructureQuery.Dispose();
        }
    }
}

