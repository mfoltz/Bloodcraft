# Testing Roadmap

This living roadmap groups verification targets by architectural bucket and tracks which scenarios already have regression coverage versus those still requiring focused tests. Update this document whenever gameplay systems evolve or new failure patterns emerge.

## Utilities (Pure Helpers)

Stateless helpers and cache managers that translate configuration or compute derived values.

| Scenario | Primary Code | Existing / Planned Tests | Status |
| --- | --- | --- | --- |
| Configuration string parsing trims whitespace and skips invalid tokens | [Configuration.ParseIntegersFromString](../../Utilities/Configuration.cs) / [Configuration.ParseEnumsFromString](../../Utilities/Configuration.cs) | [ConfigurationParsingTests](../tests/Utilities/ConfigurationParsingTests.cs) | ✅ Covered |
| Player progression cache seeds from `DataService` and updates in-place for level/prestige changes | [Progression.PlayerProgressionCacheManager](../../Utilities/Progression.cs) | [PlayerProgressionCacheManagerTests](../tests/Utilities/PlayerProgressionCacheManagerTests.cs) | ✅ Covered |
| Quest reward and starter kit extraction warns on mismatched counts and populates command dictionaries | [Configuration.GetQuestRewardItems](../../Utilities/Configuration.cs) / [Configuration.GetStarterKitItems](../../Utilities/Configuration.cs) | [ConfigurationTests](../tests/Utilities/ConfigurationTests.cs) (verifies config strings hydrate runtime dictionaries, capture warnings, and ignore duplicates) | ✅ Covered |
| Class spell cooldown wiring enumerates parsed prefab lists without duplicate side effects | [Configuration.GetClassSpellCooldowns](../../Utilities/Configuration.cs) | [ClassSpellCooldownConfigurationTests](../tests/Utilities/ClassSpellCooldownConfigurationTests.cs) (covers cache refresh and deduped indices; still lacks an end-to-end Unity cooldown grid smoke test) | ✅ Covered |
| Experience conversion helpers clamp group share distance and respect share level bounds | [Progression.GetExperienceRecipients](../../Utilities/Progression.cs) (exp share radius/level checks) | [ExperienceShareRulesTests](../tests/Utilities/ExperienceShareRulesTests.cs) (validates toggle/level gating; integration for the live UserActivityGridSystem distance checks remains outstanding) | ✅ Covered |

`ConfigurationTests` drives deterministic overrides through `ConfigService` so the quest reward and starter kit dictionaries are populated exactly as the configuration strings describe. The fixture asserts that mismatched list lengths raise warnings, duplicate prefab entries keep their first quantity, and both dictionaries end up in predictable states—guarding the full config-to-runtime translation pipeline.

The experience-share helpers now run through `ExperienceShareRulesTests`, but the distance gating still rides on `Progression`'s dependency on `SystemService.UserActivityGridSystem`. We still need a lightweight Unity harness that can spawn an activity grid and validate the `GetExperienceRecipients` radius filtering end-to-end once more of the share pipeline migrates out of live systems.

## Services (Configuration / Data Access)

Glue that binds configuration, persistence, and player session state. These components typically orchestrate Unity data and must be validated with controlled fixtures.

| Scenario | Primary Code | Existing / Planned Tests | Status |
| --- | --- | --- | --- |
| Config values hydrate from overrides and coerce to correct primitive types | [ConfigService.GetConfigValue<T>](../../Services/ConfigService.cs) | [ConfigServiceTests](../tests/Services/ConfigServiceTests.cs) | ✅ Covered |
| Persistence suppression scope properly nests and restores writes | [DataService.SuppressPersistence](../../Services/DataService.cs) | [DataServicePersistenceScopeTests](../tests/Services/DataServicePersistenceScopeTests.cs) (confirms depth/locking semantics; still need host-level verification that background saves stay paused) | ✅ Covered |
| Player connection/disconnection caches online state and triggers eclipse cleanup | [PlayerService.HandleConnection](../../Services/PlayerService.cs) / [PlayerService.HandleDisconnection](../../Services/PlayerService.cs) | [PlayerServiceConnectionTests](../tests/Services/PlayerServiceConnectionTests.cs) (exercises cache resets and eclipse cleanup; matchmaking hooks that depend on live GameAssembly are still integration-only) | ✅ Covered |
| Query services resolve entities by prefab/category without leaking temporary arrays | [QueryService](../../Services/QueryService.cs) | [QueryServiceLookupTests](../tests/Services/QueryServiceLookupTests.cs) (patches constructors to assert query modifiers; end-to-end validation against the full Unity activity grid is still pending) | ✅ Covered |
| System bootstrap respects config toggles when registering listeners | [SystemService.InitializeSystems](../../Services/SystemService.cs) | [SystemBootstrapperTests](../tests/Services/SystemBootstrapperTests.cs) (verifies the registration toggles; requires future coverage that instantiates a real `WorldBootstrap` pipeline) | ✅ Covered |

* DataService persistence fixtures run inside a sandboxed temporary directory scope so tests stay isolated from real BepInEx persistence folders.

## Systems (Leveling, Expertise, Blood, etc.)

Gameplay systems that apply progression logic or orchestrate combat hooks. These scenarios often require seeded dictionaries and Harmony patches to emulate the runtime environment.

| Scenario | Primary Code | Existing / Planned Tests | Status |
| --- | --- | --- | --- |
| Leveling XP savings raise player level, clamp at max, and preserve rested XP timestamps | [LevelingSystem.SaveLevelingExperience](../../Systems/Leveling/LevelingSystem.cs) / [LevelingSystem.UpdateMaxRestedXP](../../Systems/Leveling/LevelingSystem.cs) | [LevelingSystemTests](../tests/Systems/Leveling/LevelingSystemTests.cs) | ✅ Covered |
| Weapon expertise accumulation respects prestige reducers and max caps | [WeaponSystem.SaveExpertiseExperience](../../Systems/Expertise/WeaponSystem.cs) | [WeaponSystemTests](../tests/Systems/Expertise/WeaponSystemTests.cs) | ✅ Covered |
| Blood legacy progression respects configured stat choices and clamps at max level | [BloodSystem.SaveBloodExperience](../../Systems/Legacies/BloodSystem.cs) | [BloodSystemTests](../tests/Systems/Legacies/BloodSystemTests.cs) | ✅ Covered |
| Prestige reducers adjust XP gains when players hit max level in group kills | [LevelingSystem.ProcessExperience](../../Systems/Leveling/LevelingSystem.cs) | _Gap — add_ `LevelingExperienceShareTests.cs` | ⛔ Not covered |
| Prestige reducers compound with rested XP and group multipliers during XP awards | [LevelingSystem.ProcessExperienceGain](../../Systems/Leveling/LevelingSystem.cs) | [LevelingPrestigeTests](../tests/Systems/Leveling/LevelingPrestigeTests.cs) | ✅ Covered |
| Familiar leveling shares XP with parties and differentiates VBlood/docile targets | [FamiliarLevelingSystem.ProcessFamiliarExperience](../../Systems/Familiars/FamiliarLevelingSystem.cs) | _Gap — add_ `FamiliarLevelingSystemTests.cs` | ⛔ Not covered |
| Profession XP gain and cap enforcement across handlers | [ProfessionSystem.SaveProfessionExperience](../../Systems/Professions/ProfessionSystem.cs) | _Gap — add_ `ProfessionSystemExperienceTests.cs` | ⛔ Not covered |
| Fishing profession bonuses route fish drops, mutant grease, and SCT toggles | [ProfessionSystem.GiveProfessionBonus](../../Systems/Professions/ProfessionSystem.cs) | [ProfessionBonusTests](../tests/Systems/Professions/ProfessionBonusTests.cs) | ✅ Covered |

### Factory Pattern Sandbox

`.codex/tests/Systems/Factory` hosts isolated fixtures for experimenting with the upcoming factory orchestration layer. The current focus is validating the `PrimalWarEventWork` and `QuestTargetWork` flows as reference implementations that demonstrate how work units register themselves, acquire dependencies, and emit results without requiring a live Unity world. Their unit suites now stream deterministic entity handles through `SystemContext.ForEachEntity` so the injectable gate/portal processors and quest target cache updaters are exercised during `OnUpdate`. These fixtures should evolve alongside the planned `FactorySystemBase` migration, preserving registrar-based assertions as the primary verification tool while deferring any world bootstrap until the factory stack stabilizes.

The sandbox now ships with a Unity-backed harness at `.codex/tests/Systems/Factory/Integration/FactoryWorldHarness.cs`. The harness spins up a managed `Unity.Entities.World`, injects a specialized `TestVSystem<TWork>` that surfaces the system's entity handles and registered refresh delegates, and disposes the world after each test. It requires the Unity.Entities assemblies provided by the VRising packages referenced from `Bloodcraft.csproj`; run the tests through `dotnet test` after executing `.codex/install.sh` to ensure the Entities binaries are restored. New integration suites can instantiate `FactoryWorldHarness<TWork>` with a custom work factory to seed delegates or state, seed the world with minimal entities/components, call `Update()`, and assert against the exposed handles or refresh actions.

The sandbox now includes `DeathEventAggregationWork`, which mirrors the live `DeathEventListenerSystem` patch. It captures the death-event query, registers the progression lookups (movement, block-feed, trader, unit level, minion, VBlood source), and exposes helper hooks so progression tests can emulate familiar resets and legacy feed-kill checks without spinning up a Unity world.

`FamiliarEquipmentWork` extends the fixture coverage into the familiar servant pipeline by modelling the servant equipment/transfer handlers alongside teleport cleanup. The tests assert that every query observed in the Harmony patches is requested, the `BlockFeedBuff` lookup and mocked network-ID map are wired, and teleport debug events record the expected familiar return targets so that equipment gating and cleanup can be validated without a Unity runtime. New `OnUpdate` tests stream fake event handles through `ForEachEntity`, exercising the injectable validators, stat refreshers, and teleport-return callbacks that mirror the Harmony patches.

`FamiliarTeleportReturnWork` isolates the teleport debug hook exercised by [`PlayerTeleportSystemPatch`](../../Patches/PlayerTeleportSystemPatch.cs). The fixture mirrors the debug-event query, registers the `PlayerTeleportDebugEvent`/`FromCharacter` lookups, and exposes delegates for resolving and returning active familiars so teleport-driven dismissals can be verified without booting the live system.

`FamiliarBehaviourStateWork` captures the behaviour transition handling introduced in [`BehaviourStateChangedSystemPatch`](../../Patches/BehaviourStateChangedSystemPatch.cs). The fixture documents the behaviour-state changed query, registers lookups for `BehaviourTreeStateChangedEvent`, `BehaviourTreeState`, `BlockFeedBuff`, and `Follower`, and exposes delegates mirroring `Familiars.HandleFamiliarMinions` plus `Familiars.TryReturnFamiliar` so return versus idle transitions can be validated without the Harmony patch.

`FamiliarBindingWork` mirrors the binding flow implemented by [`FamiliarBindingSystem`](../../Systems/Familiars/FamiliarBindingSystem.cs), documenting the gate/binding queries, component lookups for stat/faction/buff updates, and the persistence delegates that feed equipment and battle matchmaking. The tests assert the registrar wiring, persistence hooks, and injectable stat/battle delegates so that binding scenarios can be validated in isolation.

`CraftingProgressionWork` expands the sandbox into the crafting progression hooks covered by `ReactToInventoryChangedSystemPatch` and `CraftingSystemPatches`. The fixture models the inventory-obtained query plus the forge, workstation, and prison crafting queries, registers the `InventoryConnection`/`QueuedWorkstationCraftAction` lookups, and exposes clan/job data sources so profession XP and quest propagation logic stay testable without relying on live Unity state.

`AbilitySlotWork` captures the `ReplaceAbilityOnSlotSystem` Harmony prefix so we can validate the entity query, the `ReplaceAbilityOnSlotBuff` buffer registration, prefab lookup wiring, and the unarmed/shift/lock spell branches. This fixture demonstrates the migration path toward a native factory system where the prefix logic becomes an injectable work unit instead of a Harmony patch.

`AbilityRunScriptsWork` mirrors the cooldown and familiar automation logic wired through [`AbilityRunScriptsSystemPatch`](../../Patches/AbilityRunScriptsSystemPatch.cs). The fixture exposes the post-cast and waypoint queries, registers the cooldown/familiar lookups, and surfaces delegates that represent the shapeshift registry plus familiar dismissal hooks so factory tests can assert class spell cooldowns and auto-dismiss behaviour without a live server.

`SpawnBuffWork` extends the sandbox to the `ScriptSpawnServer` and `UnitSpawnerReactSystem` patches. The fixture publishes the shapeshift/blood-bolt/werewolf prefab tables, requests the player/familiar/minion lookups, and exposes injectable delegates so tests can assert ability cooldown and stat refresh behaviour without relying on the live systems.

`FamiliarMinionSpawnWork` captures the familiar minion tracking implemented by [`LinkMinionToOwnerOnSpawnSystemPatch`](../../Patches/LinkMinionToOwnerOnSpawnSystemPatch.cs). It mirrors the spawn query, wires the owner/minion/block-feed lookups, and surfaces injectable hooks for resolving the active familiar along with the lifetime scheduler so tests can exercise the familiar-minion dictionary without a Unity world.

`FamiliarImprisonmentWork` mirrors the imprisonment cleanup flow handled by [`ImprisonedBuffSystemPatch`](../../Patches/ImprisonedBuffSystemPatch.cs). It captures the imprisoned-buff query, registers the buff/CharmSource/block-feed lookups, and exposes component-removal plus destruction hooks so factory tests can assert familiars are released and destroyed when the patch removes their BlockFeed state.

`SecureChatWork` captures the eclipse chat interception sequence, detailing the `ChatMessageEvent` query, the read-only registrar wiring, and the injectable regex/HMAC helpers that validate message authenticity. These notes ensure the encrypted messaging flow and cryptographic hooks remain transparent within the roadmap.

## Maintenance Notes

* Revisit this roadmap after introducing new systems (e.g., Eclipse events, Primal War tweaks) to capture regression hotspots early.
* When a bug fix is implemented without coverage, record the failing scenario here before writing the test to prevent forgetting the regression.
* Align planned test filenames with the `.codex/tests` layout shown above to keep discovery tooling consistent.
