## Table of Contents

Note: leveling is probably too fast at the moment by default. Please test config values on local before using on a live server and adjust accordingly.

Tentatively ready for public use, please reach out to @zfolmt on the V Rising modding Discord for support. Expertise is functional, professions are getting there (all professions provide at least one bonus except for fishing (WIP), woodcutting/mining/harvesting bonus yields blacksmithing/tailoring/jewelcrafting bonus durability to crafted items alchemy bonus consumable buff effects/lifetime), leveling is functional, and blood legacies are functional (each legacy level adds +1% to blood quality for that type when equipped, won't show higher quality after 100% but will still affect stats that scale with quality past 100%). Still needs refinement and have many plans for improvements, feedback and bug reports encouraged!

- [Features](#features)
- [Commands](#commands)
- [Configuration](#configuration)

## Features

- **Weapon Expertise:** Enhances gameplay by introducing expertise in different weapon types and, in the case of unarmed, extra skills. Experience for this is gained per kill based for equipped weapon.
- **Player Professions:** Adds various professions, allowing players to specialize and gain benefits from leveling the professions they like most. Experience for this is gained per resource broken, item crafted, or succesful catch.
- **Experience Leveling:** Implements a leveling system to replace traditional gearscore and provide a greater sense of progression. Experience for this is gained per kill.
- **Blood Legacies:** Players can increase their lineage in various bloodtypes, collecting essence and improving their potencies. Experience for this is gained per feed kill for equipped blood type.

## Commands

### Blood Commands
- `.getLegacyProgress`
  - Display progress in current blood lineage.
  - Shortcut: *.get b*
- `.logLegacyProgress`
  - Enables or disables blood legacy logging.
  - Shortcut: *.log b*
- `.setBloodLegacy [Player] [BloodType] [Level]` 🔒
  - Sets player blood legacy level.
  - Shorcut: *.sbl [Player] [BloodType] [Level]*
- `.listBloodTypes`
  - Lists blood legacies available.
  - Shortcut: *.lbt*

### Leveling Commands
- `.getLevelingProgress`
  - Display current level progress.
  - Shortcut: *.get l*
- `.logLevelingProgress`
  - Enables or disables blood legacy logging.
  - Shortcut: *.log l*
- `.setLevel [Player] [Level]` 🔒
  - Sets player experience level.
  - Shorcut: *.sl [Player] [Level]*

### Profession Commands
- `.getProfessionProgress [Profession]`
  - Display progress in entered profession.
  - Shortcut: *.gp [Profession]*
- `.logProfessionProgress`
  - Enables or disables profession progress logging (also controls if user is informed of bonus yields from profession levels).
  - Shortcut: *.log p*
- `.setProfessionLevel [Name] [Profession] [Level]` 🔒
  - Sets player profession level.
  - Shortcut: *.spl [Name] [Profession] [Level]*
- `.listProfessions`
  - Lists available professions.
  - Shortcut: *.lp*
    
### Weapon Commands
- `.getExpertiseProgress`
  - Display expertise progress for current weapon along with any bonus stats if applicable.
  - Shortcut: *.get e*
- `.logExpertiseProgress`
  - Enables or disables expertise logging.
  - Shortcut: *.log e*
- `.chooseWeaponStat [WeaponStat]`
  - Chooses 1 of 2 (maximum number of stat choices can be configured) total stats a weapon will apply as bonuses towards based on expertise.
  - Shortcut: *.cws [WeaponStat]*
- `.setWeaponExpertise [Player] [Weapon] [Level]` 🔒
  - Sets player weapon expertise level.
  - Shorcut: *.swe [Player] [Weapon] [Level]*
- `.listWeaponStats`
  - Lists weapon stats available.
  - Shortcut: *.lws*
- `.resetWeaponStats`
  - Resets stat choices for currently equipped weapon for configurable item cost/quanity.
  - Shortcut: *.rws*
- `.lockSpell`
  - Enables registering spells to use in unarmed slots if unarmed expertise (sanguimancy) is high enough (requirements for unlocked slots are configurable). Toggle, move spells to slots, then toggle again and switch to unarmed.
  - Shortcut: *.lock*
 
## Configuration

### Leveling System
- **Enable Leveling System**: `LevelingSystem` (bool, default: false)  
  Enable or disable the leveling system.
- **Max Player Level**: `MaxLevel` (int, default: 90)  
  The maximum level a player can reach.
- **Starting Player Level**: `StartingLevel` (int, default: 0)  
  The starting level for players. Use this to skip the first few journal quests for now until I think of a better solution.
- **Unit Leveling Multiplier**: `UnitLevelingMultiplier` (int, default: 5)  
  Multiplier for experience gained from units.
- **VBlood Leveling Multiplier**: `VBloodLevelingMultiplier` (int, default: 15)  
  Multiplier for experience gained from VBloods.
- **Group Leveling Multiplier**: `GroupLevelingMultiplier` (int, default: 1)  
  Multiplier for experience gained from group kills.

### Expertise System
- **Enable Expertise System**: `ExpertiseSystem` (bool, default: false)  
  Enable or disable the expertise system.
- **Enable Sanguimancy**: `Sanguimancy` (bool, default: false)  
  Enable or disable sanguimancy (unarmed expertise, note that expertise must also be enabled for this to work).
- **First Slot Unlock**: `FirstSlot` (int, default: 25)  
  Level to unlock first spell slot for unarmed.
- **Second Slot Unlock**: `SecondSlot` (int, default: 75)  
  Level to unlock second spell slot for unarmed.
- **Max Expertise Level**: `MaxExpertiseLevel` (int, default: 99)  
  Maximum level in weapon expertise.
- **Unit Expertise Multiplier**: `UnitExpertiseMultiplier` (int, default: 5)  
  Multiplier for expertise gained from units.
- **VBlood Expertise Multiplier**: `VBloodExpertiseMultiplier` (int, default: 15)  
  Multiplier for expertise gained from VBloods.
- **Max Number Stat Choices**: `MaxStatChoices` (int, default: 2)  
  The maximum number of stat choices a player can choose for weapon expertise per weapon.
- **Reset Item Cost**: `ResetStatsItem` (int, default: 0)  
  Item PrefabGUID cost for resetting weapon stats.
- **Reset Item Quantity**: `ResetStatsItemQuantity` (int, default: 0)  
  Quantity of item cost required for resetting stats.

### Expertise Stats
- **Physical Power**: `PhysicalPower` (float, default: 15.0)  
  Base cap for physical power.
- **Spell Power**: `SpellPower` (float, default: 15.0)  
  Base cap for spell power.
- **Physical Crit Chance**: `PhysicalCritChance` (float, default: 0.15)  
  Base cap for physical critical strike chance.
- **Physical Crit Damage**: `PhysicalCritDamage` (float, default: 0.75)  
  Base cap for physical critical strike damage.
- **Spell Crit Chance**: `SpellCritChance` (float, default: 0.15)  
  Base cap for spell critical strike chance.
- **Spell Crit Damage**: `SpellCritDamage` (float, default: 0.75)  
  Base cap for spell critical strike damage.

### Blood System
- **Enable Blood System**: `BloodSystem` (bool, default: false)  
  Enable or disable the blood legacy system.
- **Max Blood Level**: `MaxBloodLevel` (int, default: 99)  
  Maximum level in blood legacies.
- **Unit Legacy Multiplier**: `UnitLegacyMultiplier` (int, default: 5)  
  Multiplier for lineage gained from units.
- **VBlood Legacy Multiplier**: `VBloodLegacyMultipler` (int, default: 15)  
  Multiplier for lineage gained from VBloods.

### Profession System
- **Enable Profession System**: `ProfessionSystem` (bool, default: false)  
  Enable or disable the profession system.
- **Max Profession Level**: `MaxProfessionLevel` (int, default: 99)  
  Maximum level in professions.
- **Profession Multiplier**: `ProfessionMultiplier` (int, default: 10)  
  Multiplier for profession experience gained per action.
