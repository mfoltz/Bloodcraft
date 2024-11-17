using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
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

    ModifyUnitStatBuffSystem_Destroy _modifyUnitStatBuffSystem_Destroy;
    public ModifyUnitStatBuffSystem_Destroy ModifyUnitStatBuffSystem_Destroy => _modifyUnitStatBuffSystem_Destroy ??= GetSystem<ModifyUnitStatBuffSystem_Destroy>();

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

    NameableInteractableSystem _nameableInteractableSystem;
    public NameableInteractableSystem NameableInteractableSystem => _nameableInteractableSystem ??= GetSystem<NameableInteractableSystem>();

    ActivateVBloodAbilitySystem _activateVBloodAbilitySystem;
    public ActivateVBloodAbilitySystem ActivateVBloodAbilitySystem => _activateVBloodAbilitySystem ??= GetSystem<ActivateVBloodAbilitySystem>();

    EndSimulationEntityCommandBufferSystem _endSimulationEntityCommandBufferSystem;
    public EndSimulationEntityCommandBufferSystem EndSimulationEntityCommandBufferSystem => _endSimulationEntityCommandBufferSystem ??= GetSystem<EndSimulationEntityCommandBufferSystem>();

    ReplaceAbilityOnSlotSystem _replaceAbilityOnSlotSystem;
    public ReplaceAbilityOnSlotSystem ReplaceAbilityOnSlotSystem => _replaceAbilityOnSlotSystem ??= GetSystem<ReplaceAbilityOnSlotSystem>();
    
    UnEquipItemSystem _unEquipItemSystem;
    public UnEquipItemSystem UnEquipItemSystem => _unEquipItemSystem ??= GetSystem<UnEquipItemSystem>();

    EquipItemSystem _equipItemSystem;
    public EquipItemSystem EquipItemSystem => _equipItemSystem ??= GetSystem<EquipItemSystem>();

    Update_ReplaceAbilityOnSlotSystem _updateReplaceAbilityOnSlotSystem;
    public Update_ReplaceAbilityOnSlotSystem Update_ReplaceAbilityOnSlotSystem => _updateReplaceAbilityOnSlotSystem ??= GetSystem<Update_ReplaceAbilityOnSlotSystem>();

    StatChangeSystem _statChangeSystem;
    public StatChangeSystem StatChangeSystem => _statChangeSystem ??= GetSystem<StatChangeSystem>();

    NetworkIdSystem.Singleton _networkIdSystem_Singleton;
    public NetworkIdSystem.Singleton NetworkIdSystem => _networkIdSystem_Singleton = ServerScriptMapper.GetSingleton<NetworkIdSystem.Singleton>();

    ServerBootstrapSystem _serverBootstrapSystem;
    public ServerBootstrapSystem ServerBootstrapSystem => _serverBootstrapSystem ??= GetSystem<ServerBootstrapSystem>();

    BehaviourTreeBindingSystem_Spawn _behaviourTreeBindingSystem;
    public BehaviourTreeBindingSystem_Spawn BehaviourTreeBindingSystem_Spawn => _behaviourTreeBindingSystem ??= GetSystem<BehaviourTreeBindingSystem_Spawn>();

    SpawnAbilityGroupSlotsSystem _spawnAbilityGroupSlotSystem;
    public SpawnAbilityGroupSlotsSystem SpawnAbilityGroupSlotSystem => _spawnAbilityGroupSlotSystem ??= GetSystem<SpawnAbilityGroupSlotsSystem>();
    T GetSystem<T>() where T : ComponentSystemBase
    {
        return _world.GetExistingSystemManaged<T>() ?? throw new InvalidOperationException($"Failed to get {Il2CppType.Of<T>().FullName} from the Server...");
    }
}