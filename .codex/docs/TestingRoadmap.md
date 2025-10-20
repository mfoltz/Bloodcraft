# Testing Roadmap

This living roadmap groups verification targets by architectural bucket and tracks which scenarios already have regression coverage versus those still requiring focused tests. Update this document whenever gameplay systems evolve or new failure patterns emerge.

## Utilities (Pure Helpers)

Stateless helpers and cache managers that translate configuration or compute derived values.

| Scenario | Primary Code | Existing / Planned Tests | Status |
| --- | --- | --- | --- |
| Configuration string parsing trims whitespace and skips invalid tokens | [Configuration.ParseIntegersFromString](../../Utilities/Configuration.cs) / [Configuration.ParseEnumsFromString](../../Utilities/Configuration.cs) | [ConfigurationParsingTests](../tests/Utilities/ConfigurationParsingTests.cs) | ✅ Covered |
| Player progression cache seeds from `DataService` and updates in-place for level/prestige changes | [Progression.PlayerProgressionCacheManager](../../Utilities/Progression.cs) | [PlayerProgressionCacheManagerTests](../tests/Utilities/PlayerProgressionCacheManagerTests.cs) | ✅ Covered |
| Quest reward and starter kit extraction warns on mismatched counts and populates command dictionaries | [Configuration.GetQuestRewardItems](../../Utilities/Configuration.cs) / [Configuration.GetStarterKitItems](../../Utilities/Configuration.cs) | _Gap — add_ `QuestRewardConfigurationTests.cs` | ⛔ Not covered |
| Class spell cooldown wiring enumerates parsed prefab lists without duplicate side effects | [Configuration.GetClassSpellCooldowns](../../Utilities/Configuration.cs) | _Gap — add_ `ClassSpellCooldownConfigurationTests.cs` | ⛔ Not covered |
| Experience conversion helpers clamp group share distance and respect share level bounds | [Progression.GetExperienceRecipients](../../Utilities/Progression.cs) (exp share radius/level checks) | _Gap — add_ `ExperienceShareRulesTests.cs` | ⛔ Not covered |

## Services (Configuration / Data Access)

Glue that binds configuration, persistence, and player session state. These components typically orchestrate Unity data and must be validated with controlled fixtures.

| Scenario | Primary Code | Existing / Planned Tests | Status |
| --- | --- | --- | --- |
| Config values hydrate from overrides and coerce to correct primitive types | [ConfigService.GetConfigValue<T>](../../Services/ConfigService.cs) | [ConfigServiceTests](../tests/Services/ConfigServiceTests.cs) | ✅ Covered |
| Persistence suppression scope properly nests and restores writes | [DataService.SuppressPersistence](../../Services/DataService.cs) | _Gap — add_ `DataServicePersistenceScopeTests.cs` | ⛔ Not covered |
| Player connection/disconnection caches online state and triggers eclipse cleanup | [PlayerService.HandleConnection](../../Services/PlayerService.cs) / [PlayerService.HandleDisconnection](../../Services/PlayerService.cs) | _Gap — add_ `PlayerServiceConnectionTests.cs` | ⛔ Not covered |
| Query services resolve entities by prefab/category without leaking temporary arrays | [QueryService](../../Services/QueryService.cs) | _Gap — add_ `QueryServiceEntityLookupTests.cs` | ⛔ Not covered |
| System bootstrap respects config toggles when registering listeners | [SystemService.InitializeSystems](../../Services/SystemService.cs) | _Gap — add_ `SystemServiceInitializationTests.cs` | ⛔ Not covered |

## Systems (Leveling, Expertise, Blood, etc.)

Gameplay systems that apply progression logic or orchestrate combat hooks. These scenarios often require seeded dictionaries and Harmony patches to emulate the runtime environment.

| Scenario | Primary Code | Existing / Planned Tests | Status |
| --- | --- | --- | --- |
| Leveling XP savings raise player level, clamp at max, and preserve rested XP timestamps | [LevelingSystem.SaveLevelingExperience](../../Systems/Leveling/LevelingSystem.cs) / [LevelingSystem.UpdateMaxRestedXP](../../Systems/Leveling/LevelingSystem.cs) | [LevelingSystemTests](../tests/Systems/Leveling/LevelingSystemTests.cs) | ✅ Covered |
| Weapon expertise accumulation respects prestige reducers and max caps | [WeaponSystem.SaveExpertiseExperience](../../Systems/Expertise/WeaponSystem.cs) | [WeaponSystemTests](../tests/Systems/Expertise/WeaponSystemTests.cs) | ✅ Covered |
| Blood legacy progression respects configured stat choices and clamps at max level | [BloodSystem.SaveBloodExperience](../../Systems/Legacies/BloodSystem.cs) | [BloodSystemTests](../tests/Systems/Legacies/BloodSystemTests.cs) | ✅ Covered |
| Prestige reducers adjust XP gains when players hit max level in group kills | [LevelingSystem.ProcessExperience](../../Systems/Leveling/LevelingSystem.cs) | _Gap — add_ `LevelingExperienceShareTests.cs` | ⛔ Not covered |
| Familiar leveling shares XP with parties and differentiates VBlood/docile targets | [FamiliarLevelingSystem.ProcessFamiliarExperience](../../Systems/Familiars/FamiliarLevelingSystem.cs) | _Gap — add_ `FamiliarLevelingSystemTests.cs` | ⛔ Not covered |
| Profession XP gain and cap enforcement across handlers | [ProfessionSystem.SaveProfessionExperience](../../Systems/Professions/ProfessionSystem.cs) | _Gap — add_ `ProfessionSystemExperienceTests.cs` | ⛔ Not covered |

## Maintenance Notes

* Revisit this roadmap after introducing new systems (e.g., Eclipse events, Primal War tweaks) to capture regression hotspots early.
* When a bug fix is implemented without coverage, record the failing scenario here before writing the test to prevent forgetting the regression.
* Align planned test filenames with the `.codex/tests` layout shown above to keep discovery tooling consistent.
