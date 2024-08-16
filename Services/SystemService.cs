using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Scripting;
using ProjectM.Shared.Systems;
using Unity.Entities;

namespace Bloodcraft.Services;
public class SystemService(World world)
{
    // Server world reference
    readonly World _world = world ?? throw new ArgumentNullException(nameof(world));

    // System backing fields with lazy initialization
    DebugEventsSystem _debugEventsSystem;
    public DebugEventsSystem DebugEventsSystem => _debugEventsSystem ??= GetSystem<DebugEventsSystem>();

    PrefabCollectionSystem _prefabCollectionSystem;
    public PrefabCollectionSystem PrefabCollectionSystem => _prefabCollectionSystem ??= GetSystem<PrefabCollectionSystem>();

    ServerGameSettingsSystem _serverGameSettingsSystem;
    public ServerGameSettingsSystem ServerGameSettingsSystem => _serverGameSettingsSystem ??= GetSystem<ServerGameSettingsSystem>();

    ServerScriptMapper _serverScriptMapper;
    public ServerScriptMapper ServerScriptMapper => _serverScriptMapper ??= GetSystem<ServerScriptMapper>();

    ModifyUnitStatBuffSystem_Spawn _modifyUnitStatBuffSystem_Spawn;
    public ModifyUnitStatBuffSystem_Spawn ModifyUnitStatBuffSystem_Spawn => _modifyUnitStatBuffSystem_Spawn ??= GetSystem<ModifyUnitStatBuffSystem_Spawn>();

    ReplaceAbilityOnSlotSystem _replaceAbilityOnSlotSystem;
    public ReplaceAbilityOnSlotSystem ReplaceAbilityOnSlotSystem => _replaceAbilityOnSlotSystem ??= GetSystem<ReplaceAbilityOnSlotSystem>();

    EntityCommandBufferSystem _entityCommandBufferSystem;
    public EntityCommandBufferSystem EntityCommandBufferSystem => _entityCommandBufferSystem ??= GetSystem<EntityCommandBufferSystem>();

    ClaimAchievementSystem _claimAchievementSystem;
    public ClaimAchievementSystem ClaimAchievementSystem => _claimAchievementSystem ??= GetSystem<ClaimAchievementSystem>();

    GameDataSystem _gameDataSystem;
    public GameDataSystem GameDataSystem => _gameDataSystem ??= GetSystem<GameDataSystem>();

    ScriptSpawnServer _scriptSpawnServer;
    public ScriptSpawnServer ScriptSpawnServer => _scriptSpawnServer ??= GetSystem<ScriptSpawnServer>();

    CombatMusicSystem_Server _combatMusicSystem_Server;
    public CombatMusicSystem_Server CombatMusicSystem_Server => _combatMusicSystem_Server ??= GetSystem<CombatMusicSystem_Server>();

    MapZoneCollectionSystem _mapZoneCollectionSystem;
    public MapZoneCollectionSystem MapZoneCollectionSystem => _mapZoneCollectionSystem ??= GetSystem<MapZoneCollectionSystem>();

    // Generic method to get or create a system
    T GetSystem<T>() where T : ComponentSystemBase
    {
        return _world.GetExistingSystemManaged<T>() ?? throw new InvalidOperationException($"Failed to get {Il2CppType.Of<T>().FullName} from the Server...");
    }
}