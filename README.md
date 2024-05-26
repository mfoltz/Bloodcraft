## Table of Contents

Note: leveling is probably too fast at the moment by default. Please test config values on local before using on a live server and adjust accordingly. Features are intended to be useable standalone without causing conflict but please let me know if you run into issues with this. Making progress tuning numbers here but still needs work.

Tentatively ready for public use, please reach out to @zfolmt on the V Rising modding Discord for support. Expertise is functional, professions are getting there (all professions provide at least one bonus, woodcutting/mining/harvesting bonus yields (x1 of drop per level) blacksmithing/tailoring/jewelcrafting bonus durability to crafted items (up to x2 at max) alchemy bonus consumable buff effects/lifetime (up to x2 at max), fishing x1 bonus random fish based on location every 20 levels per catch), leveling is functional, and blood legacies are functional (like weapon expertise with different stats now). Still needs refinement and have many plans for improvements, feedback and bug reports encouraged!

- [Features](#features)
- [Commands](#commands)
- [Configuration](#configuration)
- [Roadmap](#roadmap)

## Features

- **Weapon Expertise:** Enhances gameplay by introducing expertise in different weapon types and, in the case of unarmed, extra skills (unarmed also has stats as of second release). Experience for this is gained per kill based for equipped weapon.
- **Player Professions:** Adds various professions, allowing players to specialize and gain benefits from leveling the professions they like most. Proficiency is gained per resource broken, item crafted, or succesful catch.
- **Experience Leveling:** Implements a leveling system to replace traditional gearscore and provide a greater sense of progression. Experience for this is gained per kill.
- **Blood Legacies:** This is more like weapon expertise now with a different set of stats as of most recent push (5/26). Experience for this is gained per feed kill for equipped blood type.

## Commands

### Blood Commands
- `.getBloodLegacyProgress [Blood]`
  - Display progress in current blood legacy.
  - Shortcut: *.gbl [Blood]*
- `.logBloodLegacyProgress`
  - Enables or disables blood legacy logging.
  - Shortcut: *.log bl*
- `.chooseBloodStat [Blood] [Stat]`
  - Chooses 1 of 2 (maximum number of stat choices can be configured) total stats a blood will apply bonuses towards based on legacy.
  - Shortcut: *.cws [Blood] [Stat]*
- `.setBloodLegacy [Player] [Blood] [Level]` 🔒
  - Sets player blood legacy level.
  - Shorcut: *.sbl [Player] [Blood] [Level]*
- `.listBloodStats`
  - Lists blood stats available along with bonuses at max legacy.
  - Shortcut: *.lbs*
- `.resetBloodStats`
  - Resets stat choices for currently equipped blood for configurable item cost/quanity.
  - Shortcut: *.rbs*
- `.listBloodLegacies`
  - Lists blood legacies available.
  - Shortcut: *.lbl*

### Leveling Commands
- `.quickStart`
  - Completes GettingReadyForTheHunt.
  - Shortcut: *.start*
- `.getLevelingProgress`
  - Display current level progress.
  - Shortcut: *.get l*
- `.logLevelingProgress`
  - Enables or disables leveling experience logging.
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
- `.chooseWeaponStat [Weapon] [Stat]`
  - Chooses 1 of 2 (maximum number of stat choices can be configured) total stats a weapon will apply as bonuses towards based on expertise.
  - Shortcut: *.cws [Weapon] [Stat]*
- `.setWeaponExpertise [Player] [Weapon] [Level]` 🔒
  - Sets player weapon expertise level.
  - Shorcut: *.swe [Player] [Weapon] [Level]*
- `.listWeaponStats`
  - Lists weapon stats available along with bonuses at max expertise.
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
- **Unit Leveling Multiplier**: `UnitLevelingMultiplier` (float, default: 2.5)  
  Multiplier for experience gained from units.
- **VBlood Leveling Multiplier**: `VBloodLevelingMultiplier` (float, default: 5)  
  Multiplier for experience gained from VBloods.
- **Group Leveling Multiplier**: `GroupLevelingMultiplier` (float, default: 1)  
  Multiplier for experience gained from group kills.
- **Scaling Leveling Multiplier**: `LevelScalingMultiplier` (float, default: 0.025)  
  Scaling multiplier for tapering (reducing) experience gained at higher levels.

### Expertise System
- **Enable Expertise System**: `ExpertiseSystem` (bool, default: false)  
  Enable or disable the expertise system.
- **Enable Sanguimancy**: `Sanguimancy` (bool, default: false)  
  Enable or disable sanguimancy (unarmed expertise, note that expertise must also be enabled for this to work).
- **First Slot Unlock**: `FirstSlot` (int, default: 25)  
  Level to unlock first spell slot for unarmed.
- **Second Slot Unlock**: `SecondSlot` (int, default: 50)  
  Level to unlock second spell slot for unarmed.
- **Max Expertise Level**: `MaxExpertiseLevel` (int, default: 99)  
  Maximum level in weapon expertise.
- **Unit Expertise Multiplier**: `UnitExpertiseMultiplier` (float, default: 2)  
  Multiplier for expertise gained from units.
- **VBlood Expertise Multiplier**: `VBloodExpertiseMultiplier` (int, default: 5)  
  Multiplier for expertise gained from VBloods.
- **Expertise Stat Choices**: `ExpertiseStatChoices` (int, default: 2)  
  The maximum number of stat choices a player can choose for weapon expertise per weapon.
- **Reset Expertise Item**: `ResetExpertiseItem` (int, default: 0)  
  Item PrefabGUID cost for resetting expertise stats.
- **Reset Expertise Item Quantity**: `ResetExpertiseItemQuantity` (int, default: 0)  
  Quantity of item cost required for resetting expertise stats.

### Expertise Stats
- **Max Health**: `MaxHealth` (float, default: 250.0)  
  Base cap for max health.
- **Movement Speed**: `MovementSpeed` (float, default: 0.25)  
  Base cap for movement speed.
- **Primary Attack Speed**: `PrimaryAttackSpeed` (float, default: 0.25)  
  Base cap for primary attack speed.
- **Physical Lifeleech**: `PhysicalLifeLeech` (float, default: 0.15)  
  Base cap for physical lifeleech.
- **Spell Lifeleech**: `SpellLifeLeech` (float, default: 0.15)  
  Base cap for spell lifeleech.
- **Primary Lifeleech**: `PrimaryLifeleech` (float, default: 0.25)  
  Base cap for primary lifeleech.
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
  Multiplier for essence gained from units.
- **VBlood Legacy Multiplier**: `VBloodLegacyMultipler` (int, default: 15)  
  Multiplier for essence gained from VBloods.
- **Legacy Stat Choices**: `LegacyStatChoices` (int, default: 2)  
  The maximum number of stat choices a player can choose for weapon expertise per weapon.
- **Reset Legacy Item**: `ResetLegacyItem` (int, default: 0)  
  Item PrefabGUID cost for resetting legacy stats.
- **Reset Legacy Item Quantity**: `ResetLegacyItemQuantity` (int, default: 0)  
  Quantity of item cost required for resetting legacy stats.

### Legacy Stats
- **Healing Received**: `HealingReceived` (float, default: 0.25)  
  The base cap for healing received.
- **Damage Reduction**: `DamageReduction` (float, default: 0.10)  
  The base cap for damage reduction.
- **Physical Resistance**: `PhysicalResistance` (float, default: 0.20)  
  The base cap for physical resistance.
- **Spell Resistance**: `SpellResistance` (float, default: 0.20)  
  The base cap for spell resistance.
- **Blood Drain**: `BloodDrain` (float, default: 0.50)  
  The base cap for blood drain.
- **Crowd Control Reduction**: `CCReduction` (float, default: 0.25)  
  The base cap for crowd control reduction.
- **Spell Cooldown Recovery Rate**: `SpellCooldownRecoveryRate` (float, default: 0.15)  
  The base cap for spell cooldown recovery rate.
- **Weapon Cooldown Recovery Rate**: `WeaponCooldownRecoveryRate` (float, default: 0.15)  
  The base cap for weapon cooldown recovery rate.
- **Ultimate Cooldown Recovery Rate**: `UltimateCooldownRecoveryRate` (float, default: 0.20)  
  The base cap for ultimate cooldown recovery rate.
- **Minion Damage**: `MinionDamage` (float, default: 0.25)  
  The base cap for minion damage.
- **Shield Absorb**: `ShieldAbsorb` (float, default: 0.50)  
  The base cap for shield absorb.
- **Blood Efficiency**: `BloodEfficiency` (float, default: 0.10)  
  The base cap for blood efficiency.

### Profession System
- **Enable Profession System**: `ProfessionSystem` (bool, default: false)  
  Enable or disable the profession system.
- **Max Profession Level**: `MaxProfessionLevel` (int, default: 99)  
  Maximum level in professions.
- **Profession Multiplier**: `ProfessionMultiplier` (int, default: 10)  
  Multiplier for proficiency gained per action.

## Roadmap

These are things I'm working on in the immediate future, please feel free to suggest things you'd like to see.

- **Weapon Expertise:** Config options for allowed stats, additional stat choices (added stats 5/26)
- **Player Professions:** Config options to control bonuses per profession, more features per profession
- **Experience Leveling:** Config options to account for things like unit spawners, servant kills, etc
- **Blood Legacies:** Config options to customize blood benefits in some manner more similar to weapon expertise than just improving quality % (done 5/26)
