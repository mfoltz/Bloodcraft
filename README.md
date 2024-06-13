## Table of Contents

Foundational features are fairly complete and stable at this point.

- [Features](#features)
- [Commands](#commands)
- [Configuration](#configuration)

## Features

- **Weapon Expertise:** Enhances gameplay by introducing expertise in different weapon types and optionally adds extra spell slots to unarmed. Expertise is gained per kill based on equipped weapon. Number of stat choices is configurable. Resetting choices is free by default but can be made to cost items. Can be further enhanced by activating soft OR hard synergies (classes) to encourage and/or restrict various playstyles.
- **Player Professions:** Adds various professions, allowing players to specialize and gain benefits from leveling the professions they like most. Proficiency is gained per resource broken, item crafted, or succesful catch. Mining/woodcutting/harvesting provide bonus yields per resource harvest, fishing provide bonus fish every 20 levels based on the area fished in, alchemy enhances consumable duration and effects, and blacksmithing/enchanting/tailoring increases durability in armors/weapons crafted. Do note you only receive experience for crafting and extra durability is only applied if you own the station/placed it if in a clan, plan on improving that and revamping these a bit in the future.
- **Experience Leveling:** Implements a leveling system to replace traditional gearscore and provide a greater sense of progression. Experience for this is gained per kill (should apply to all kills that can be linked back to the player) and can complete level 20 quest to track vbloods with .quickStart (.start). Still tuning this as needed but the config options should be there to support whatever kind of leveling experience you're looking to provide at this point (within reason :P). Experience sharing in clan or group has a max distance of 25f (4-5 floor lengths). Recently added options to control experience gain from units spawned via vermin nest and other unit spawners along with nerfing rift trash experience if you so desire. Can now tune how much experience higher level players receive from lower level kills as well.
- **Blood Legacies:** Provides further stat customization for legacies (blood types) similar to expertise with a different set of stats. Experience for this is gained per feed kill/execute for equipped blood type. Old blood quality enhancement option still exists for those who would like to use it, although do note if using prestige as well the quality gain will be based on legacy prestige level in that blood. Number of stat choices is configurable. Resetting choices is free by default but can be made to cost items. Can be further enhanced by activating soft OR hard synergies (classes) to encourage and/or restrict various playstyles.
- **Prestiging:** Prestige in leveling, expertise, and legacies! Each increment in leveling prestige will reduce experience rate gain for leveling by the leveling rate reducer (separate from the expertise/legacy rate reducer) and increase expertise/legacy rate gains by the rate multiplier. Prestiging in expertise/legacies will increase their max stat bonuses per level by the stat multiplier and will reduce rate gains in that individual prestige category by the rate reducer per increment. Configurable buffs will be permanently applied to the player each prestige level in experience (can skip levels by leaving prefab as 0). Prestiging is also configurably tied to unlocking extra spells for classes to use on shift (you can use any abilityGroup prefabs here but don't use normal player spells unless you like weird weapon cloning bugs when equipping jewels. also, some NPC spells don't really work as expected when used by players so do some testing before swapping these out and make sure they behave. can test spells with '.spell [1/2] [AbilityGroupPrefab]' to manually place them on unarmed)
- **Familiars:** Every kill of a valid unit is a chance to unlock that unit as a familiar, now including VBloods and summoners! VBloods can be toggled on/off and unit types can be configurably allowed (humans, demons, beasts, etc) although do note that some category bans will supercede VBlood checks (if humans are banned, Vincent will be banned as well even if VBloods are enabled); there is also a list for individual unit bans. They will level up, fight for you, and you can collect them all. Only one familiar can be summoned at a time. They can be toggled (called/dismissed) without fully unbinding them to quickly use waygates (attempting to use a waygate with a familiar out will dismiss it automatically now and using it will then summon the familiar again so it is there after teleporting) and perform other actions normally prohibited by having a follower like a charmed human. Each set can hold 10 and more sets will be created once the one being added to is full. You can choose the active set by name (default is FamiliarsList1, FamiliarsList2, etc), remove familiars from sets, move familiars between sets, and the sets can be renamed. To summon a familiar from your current set (which can be chosen with .cfs [Name]), .lf to see the units available and use .bind #; to choose a different one, use .unbind, and if it gets weird or buggy at any point .resetfams can be used to clear your active familiar data and followerBuffer. All units have the same stats as usual, however their phys/spell power starts at 10% at level 1 and will scale to 100% again when they reach max level. Base health is 500 and will scale with up to 2500 at max level. Traders, carriages, horses, crystals (looking at you dracula blood crystal thing) and werewolves are unavailable. Werewolves will be in the pool once I add handling for their transformations but the rest will likely stay banned (if you manually add one of them to your unlocks as an admin that's on you :P). Can't bind in combat or pvp combat.
- **Classes:** Soft synergies allows for the encouragement of stat choices without restricting choices while hard synergies will only allow the stats for that class to be chosen. Both soft or hard will apply the synergy multiplier to the max stat bonuses for that class for weapon expertise/blood legacies. Additionally, each class has a config spot for 4 blood buffs (the correct prefabs for this start with AB_BloodBuff_) that are granted every MaxPlayerLevel/#buffs while leveling up (with 4 buffs and 90 being the max, they'll be applied at 22, 44, 66, and 88) and apply permanently at max strength without regard to the blood quality or type the player possesses (can be skipped with 0 in the spot). They do not stack with buffs from the same blood type (if a player has the scholar free cast proc buff from class and also has 100% scholar blood, those chances will not stack) which maintains a sense of balance while allowing each class to have a persistent theme/identity that can be earned without excessive grinding. Classes also get extra spells to use on shift that are configurable, both the spell and at what level of prestige they are earned at but do keep in mind the considerations mentioned in the prestige section. Will probably be changing at least a few aspects of these new class systems in the future but am overall happy with where they're at in the sense that they allow for a lot of customization and can be made to feel as unique as you'd like. Tried to make decent thematic choices for the default blood buffs and spells per class but no promises :P. Changing classes should properly remove and apply buffs based on level.
- **Raid Monitor:** Detects castle breach events and takes note of the clans involved (raiding clan, castle-owner clan) and will punish others who try to interfere in the territory for the duration of the breach. Tested this as much as I could on my own for basic functioning and seemed good, should be able to handle multiple raids although I haven't specifically tried that yet. Can be toggled.
  
## Commands

### Blood Commands
- `.getBloodLegacyProgress [Blood]`
  - Display progress and bonuses in entered blood legacy.
  - Shortcut: *.gbl [Blood]*
- `.logBloodLegacyProgress`
  - Enables or disables blood legacy logging.
  - Shortcut: *.log bl*
- `.chooseBloodStat [Blood] [Stat]`
  - Chooses stat bonus to apply to a blood type that scales with that blood legacy level.
  - Shortcut: *.cbs [Blood] [Stat]*
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

### Leveling/Prestige Commands
- `.getLevelingProgress`
  - Display current level progress.
  - Shortcut: *.get l*
- `.logLevelingProgress`
  - Enables or disables leveling experience logging.
  - Shortcut: *.log l*
- `.setLevel [Player] [Level]` 🔒
  - Sets player experience level.
  - Shorcut: *.sl [Player] [Level]*
- `.quickStart`
  - Completes GettingReadyForTheHunt if not already completed. 
  - Shortcut: *.start*
- `.playerPrestige [PrestigeType]`
  - Handles all player prestiging. Must be at max configured level for experience, expertise, or legacies to be eligible.
  - Shortcut: *.prestige [PrestigeType]*
- `.getPrestige [PrestigeType]`
  - Shows information about player's prestige status and rates for entered type of prestige.
  - Shortcut: *.gp [PrestigeType]*
- `.resetPrestige [Name] [PrestigeType]` 🔒
  - Resets prestige type and removes buffs gained from prestiges in leveling if applicable.
  - Shortcut: *.rpr [Name] [PrestigeType]*
- `.listPlayerPrestiges`
  - Lists prestiges available to the player.
  - Shortcut: *.lpp*
- `.toggleGrouping`
  - Toggles being able to be invited to group with players not in clan for exp sharing.
  - Shortcut: *.grouping*
- `.groupAdd [Player]`
  - Adds player to group for exp sharing if they permit it in settings.
  - Shortcut: *.ga [Player]*
- `.groupRemove [Player]`
  - Removes player from group for exp sharing.
  - Shortcut: *.gr [Player]*
- `.groupDisband`
  - Disbands exp sharing group.
  - Shortcut: *.disband*
- `.chooseClass [Class]`
  - Chooses class (see options with .classes)
  - Shortcut: *.cc [Class]*
- `.chooseClassSpell [#]`
  - Choose class spell (see options with .lcs)
  - Shortcut: *.cs [#]*
- `.changeClass [Class]`
  - Changes to specified class.
  - Shortcut: *.change [Class]*
- `.listClasses`
  - List classes.
  - Shortcut: *.classes*
- `.listClassBuffs`
  - List class buffs.
  - Shortcut: *.lcb*
- `.listClassSpells`
  - List class spells.
  - Shortcut: *.classes*

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
 
### Familiar Commands
- `.bindFamiliar [#]`
  - Activates specified familiar from current list.
  - Shortcut: *.bind [#]*
- `.unbindFamiliar`
  - Destroys active familiar.
  - Shortcut: *.unbind*
- `.listFamiliars`
  - Lists unlocked familiars from current set.
  - Shortcut: *.lf*
- `.familiarSets`
  - Shows the available familiar lists.
  - Shorcut: *.famsets*
- `.chooseFamiliarSet [Name]`
  - Choose active set of familiars to bind from.
  - Shortcut: *.cfs [Name]*
- `.toggleFamiliar`
  - Calls or dismisses familar. Wave also does this if .emotes is toggled on.
  - Shortcut: *.toggle*
- `.toggleEmotes`
  - Toggles emote commands (only 1 right now) on.
  - Shortcut: *.emotes*
- `.listEmoteActions`
  - Lists emote actions and what they do.
  - Shortcut: *.le*
- `.getFamiliarLevel`
  - Display current familiar leveling progress.
  - Shortcut: *.get fl*
- `.setFamiliarLevel [Level]` 🔒
  - Set current familiar level. Rebind to force updating stats if that doesn't happen when this is used.
  - Shortcut: *.sf [Level]*
- `.getFamiliarStats`
  - Display current familiar stats (WIP).
  - Shortcut: *.get fs*
- `.resetFamiliars`
  - Resets (destroys) entities found in followerbuffer and clears familiar actives data.
  - Shortcut: *.resetfams*
 
## Configuration

### Leveling/Prestige Systems
- **Enable Leveling System**: `LevelingSystem` (bool, default: false)  
  Enable or disable the leveling system.
- **Enable Prestige System**: `PrestigeSystem` (bool, default: false)  
  Enable or disable the prestige system.
- **Max Player Level**: `MaxLevel` (int, default: 90)  
  The maximum level a player can reach.
- **Max Leveling Prestiges**: `MaxLevelingPrestiges` (int, default: 10)  
  The maximum number of prestiges a player can reach in leveling.
- **Prestige Rates Reducer**: `PrestigeRatesReducer` (float, default: 0.20)  
  Factor by which rates are reduced in expertise/legacy/experience per increment of prestige in expertise, legacy or experience.
- **Prestige Stat Multiplier**: `PrestigeStatMultiplier` (float, default: 0.25)  
  Factor by which stats are increased in expertise/legacy bonuses per increment of prestige in expertise/legacy.
- **Prestige Rates Multiplier**: `PrestigeRatesMultiplier` (float, default: 0.15)  
  Factor by which rates are increased in expertise/legacy per increment of prestige in leveling.
- **Starting Player Level**: `StartingLevel` (int, default: 0)  
  The starting level for players. Use this to skip the first few journal quests for now until I think of a better solution.
- **Unit Leveling Multiplier**: `UnitLevelingMultiplier` (float, default: 7.5)  
  Multiplier for experience gained from units.
- **VBlood Leveling Multiplier**: `VBloodLevelingMultiplier` (float, default: 15)  
  Multiplier for experience gained from VBloods.
- **Group Leveling Multiplier**: `GroupLevelingMultiplier` (float, default: 1)  
  Multiplier for experience gained from group kills.
- **Scaling Leveling Multiplier**: `LevelScalingMultiplier` (float, default: 0.025)  
  Scaling multiplier for tapering (reducing) experience gained at higher levels. Can be set to 0 if you don't want that.
- **Player Grouping**: `PlayerGrouping` (bool, default: false)  
  Enable or disable the ability to group with players not in your clan for experience sharing.
- **Max Group Size**: `MaxGroupSize` (int, default: 5)  
  The maximum number of players that can share experience in a group.

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
- **VBlood Expertise Multiplier**: `VBloodExpertiseMultiplier` (float, default: 5)  
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
- **Unit Legacy Multiplier**: `UnitLegacyMultiplier` (float, default: 5)  
  Multiplier for essence gained from units.
- **VBlood Legacy Multiplier**: `VBloodLegacyMultipler` (float, default: 15)  
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
- **Resource Yield**: `ResourceYield` (float, default: 0.25)  
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
- **Profession Multiplier**: `ProfessionMultiplier` (float, default: 10)  
  Multiplier for proficiency gained per action.

### Familiar System
- **Enable Familiar System**: `FamiliarSystem` (bool, default: false)  
  Enable or disable the familiar system.
- **Max Familiar Level**: `MaxFamiliarLevel` (int, default: 90)  
  Maximum level familiars can reach.
- **Unit Familiar Multiplier**: `UnitFamiliarMultiplier` (float, default: 5)  
  Multiplier for experience gained from units.
- **VBlood Familiar Multiplier**: `VBloodFamiliarMultiplier` (float, default: 15)  
  Multiplier for experience gained from VBloods.
- **Unit Unlock Chance**: `UnitUnlockChance` (float, default: 0.05)  
  The chance for a unit to unlock a familiar when killed.


