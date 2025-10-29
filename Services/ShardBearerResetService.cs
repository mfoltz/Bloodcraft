using System;
using System.Collections.Generic;
using Bloodcraft.Interfaces;
using Bloodcraft.Resources;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;

namespace Bloodcraft.Services;

/// <summary>
/// Coordinates the destruction of shard bearer VBlood entities.
/// </summary>
internal sealed class ShardBearerResetService
{
    static readonly PrefabGUID[] DefaultShardBearerPrefabs =
    [
        PrefabGUIDs.CHAR_Manticore_VBlood,
        PrefabGUIDs.CHAR_ChurchOfLight_Paladin_VBlood,
        PrefabGUIDs.CHAR_Gloomrot_Monster_VBlood,
        PrefabGUIDs.CHAR_Vampire_Dracula_VBlood,
        PrefabGUIDs.CHAR_Blackfang_Morgana_VBlood
    ];

    readonly IVBloodEntityContext entityContext;
    readonly Action<string> logWarning;
    readonly HashSet<PrefabGUID> shardBearerPrefabs;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShardBearerResetService"/> class.
    /// </summary>
    /// <param name="entityContext">The context used to enumerate and manipulate VBlood entities.</param>
    /// <param name="log">The logger used to report errors.</param>
    /// <param name="shardBearers">An optional custom set of shard bearer prefabs to reset.</param>
    public ShardBearerResetService(
        IVBloodEntityContext entityContext,
        Action<string> logWarning,
        IEnumerable<PrefabGUID> shardBearers = null)
    {
        this.entityContext = entityContext ?? throw new ArgumentNullException(nameof(entityContext));
        this.logWarning = logWarning ?? throw new ArgumentNullException(nameof(logWarning));

        IEnumerable<PrefabGUID> prefabs = shardBearers ?? DefaultShardBearerPrefabs;
        shardBearerPrefabs = new HashSet<PrefabGUID>(prefabs);
    }

    /// <summary>
    /// Resets the configured shard bearer entities by destroying them.
    /// </summary>
    public void ResetShardBearers()
    {
        try
        {
            foreach (Entity entity in entityContext.EnumerateVBloodEntities())
            {
                if (!entityContext.TryGetPrefabGuid(entity, out PrefabGUID prefabGuid))
                {
                    continue;
                }

                if (!shardBearerPrefabs.Contains(prefabGuid))
                {
                    continue;
                }

                entityContext.Destroy(entity);
            }
        }
        catch (Exception ex)
        {
            logWarning($"[ResetShardBearers] error: {ex}");
        }
    }

    /// <summary>
    /// Gets the default collection of shard bearer prefab GUIDs.
    /// </summary>
    public static IReadOnlyCollection<PrefabGUID> DefaultShardBearers => DefaultShardBearerPrefabs;
}
