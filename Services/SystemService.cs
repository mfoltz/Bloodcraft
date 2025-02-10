using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared.Systems;
using Unity.Entities;

namespace Bloodcraft.Services;
internal class SystemService(World world)
{
    readonly World _world = world ?? throw new ArgumentNullException(nameof(world));

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

    AttachParentIdSystem _attachParentIdSystem;
    public AttachParentIdSystem AttachParentIdSystem => _attachParentIdSystem ??= GetSystem<AttachParentIdSystem>();

    AbilitySpawnSystem _abilitySpawnSystem;
    public AbilitySpawnSystem AbilitySpawnSystem => _abilitySpawnSystem ??= GetSystem<AbilitySpawnSystem>();

    SpawnTeamSystem _spawnTeamSystem;
    public SpawnTeamSystem SpawnTeamSystem => _spawnTeamSystem ??= GetSystem<SpawnTeamSystem>();

    ModificationSystem _modificationSystem;
    public ModificationSystem ModificationSystem => _modificationSystem ??= GetSystem<ModificationSystem>();

    SetTeamOnSpawnSystem _setTeamOnSpawnSystem;
    public SetTeamOnSpawnSystem SetTeamOnSpawnSystem => _setTeamOnSpawnSystem ??= GetSystem<SetTeamOnSpawnSystem>();

    SpellModSyncSystem_Server _spellModSyncSystem_Server;
    public SpellModSyncSystem_Server SpellModSyncSystem_Server => _spellModSyncSystem_Server ??= GetSystem<SpellModSyncSystem_Server>();

    JewelSpawnSystem _jewelSpawnSystem;
    public JewelSpawnSystem JewelSpawnSystem => _jewelSpawnSystem ??= GetSystem<JewelSpawnSystem>();

    TraderPurchaseSystem _traderPurchaseSystem;
    public TraderPurchaseSystem TraderPurchaseSystem => _traderPurchaseSystem ??= GetSystem<TraderPurchaseSystem>();

    UpdateBuffsBuffer_Destroy _updateBuffsBuffer_Destroy;
    public UpdateBuffsBuffer_Destroy UpdateBuffsBuffer_Destroy => _updateBuffsBuffer_Destroy ??= GetSystem<UpdateBuffsBuffer_Destroy>();

    BuffSystem_Spawn_Server _buffSystem_Spawn_Server;
    public BuffSystem_Spawn_Server BuffSystem_Spawn_Server => _buffSystem_Spawn_Server ??= GetSystem<BuffSystem_Spawn_Server>();

    InstantiateMapIconsSystem_Spawn _instantiateMapIconsSystem_Spawn;
    public InstantiateMapIconsSystem_Spawn InstantiateMapIconsSystem_Spawn => _instantiateMapIconsSystem_Spawn ??= GetSystem<InstantiateMapIconsSystem_Spawn>();

    MapZoneCollectionSystem _mapZoneCollectionSystem;
    public MapZoneCollectionSystem MapZoneCollectionSystem => _mapZoneCollectionSystem ??= GetSystem<MapZoneCollectionSystem>();
    T GetSystem<T>() where T : ComponentSystemBase
    {
        return _world.GetExistingSystemManaged<T>() ?? throw new InvalidOperationException($"Failed to get {Il2CppType.Of<T>().FullName} from the Server...");
    }
}