## Table of Contents

UPDATED (7/5)


- [Features](#features)
- [Commands](#commands)
- [Configuration](#configuration)

## Sponsor this project

[![patreon](https://i.imgur.com/u6aAqeL.png)](https://www.patreon.com/join/4865914) | [![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/zfolmt)

## Features

- **Weapon Expertise:** Enhances gameplay by introducing expertise in different weapon types and optionally adds extra spell slots to unarmed. Expertise is gained per kill based on equipped weapon. Number of stat choices is configurable. Resetting choices is free by default but can be made to cost items. Can be further enhanced by activating soft OR hard synergies (classes) to encourage and/or restrict various playstyles.
- **Player Professions:** Adds various professions, allowing players to specialize and gain benefits from leveling the professions they like most. Proficiency is gained per resource broken, item crafted, or succesful catch. Mining/woodcutting/harvesting provide bonus yields per resource harvest, fishing provide bonus fish every 20 levels based on the area fished in, alchemy enhances consumable duration and effects, and blacksmithing/enchanting/tailoring increases durability in armors/weapons crafted. Do note you only receive experience for crafting and extra durability is only applied if you own the station/placed it if in a clan, plan on improving that and revamping these a bit in the future.
- **Experience Leveling:** Implements a leveling system to replace traditional gearscore and provide a greater sense of progression. Experience for this is gained per kill (should apply to all kills that can be linked back to the player) and can complete level 20 quest to track vbloods with .quickStart (.start). Still tuning this as needed but the config options should be there to support whatever kind of leveling experience you're looking to provide at this point (within reason :P). Experience sharing in clan or group has a max distance of 25f (4-5 floor lengths). Recently added options to control experience gain from units spawned via vermin nest and other unit spawners along with nerfing rift trash experience if you so desire. Can now tune how much experience higher level players receive from lower level kills as well.
- **Blood Legacies:** Provides further stat customization for legacies (blood types) similar to expertise with a different set of stats. Experience for this is gained per feed kill/execute for equipped blood type. Old blood quality enhancement option still exists for those who would like to use it, although do note if using prestige as well the quality gain will be based on legacy prestige level in that blood. Number of stat choices is configurable. Resetting choices is free by default but can be made to cost items. Can be further enhanced by activating soft OR hard synergies (classes) to encourage and/or restrict various playstyles.
- **Prestiging:** Prestige in leveling, expertise, and legacies! Each increment in leveling prestige will reduce experience rate gain for leveling by the leveling rate reducer (separate from the expertise/legacy rate reducer) and increase expertise/legacy rate gains by the rate multiplier. Prestiging in expertise/legacies will increase their max stat bonuses per level by the stat multiplier and will reduce rate gains in that individual prestige category by the rate reducer per increment. Configurable buffs will be permanently applied to the player each prestige level in experience (can skip levels by leaving prefab as 0). Prestiging is also configurably tied to unlocking extra spells for classes to use on shift (you can use any abilityGroup prefabs here but don't use normal player spells unless you like weird weapon cloning bugs when equipping jewels. also, some NPC spells don't really work as expected when used by players so do some testing before swapping these out and make sure they behave. can test spells with '.spell [1/2] [AbilityGroupPrefab]' to manually place them on unarmed)

- **Familiars:** Every kill of a valid unit is a chance to unlock that unit as a familiar, now including VBloods and summoners! VBloods can be toggled on/off and unit types can be configurably allowed (humans, demons, beasts, etc) although do note that some category bans will supercede VBlood checks (if humans are banned, Vincent will be banned as well even if VBloods are enabled); there is also a list for individual unit bans. They will level up, fight for you, and you can collect them all. Only one familiar can be summoned at a time. They can be toggled (called/dismissed) without fully unbinding them to quickly use waygates (attempting to use a waygate with a familiar out will dismiss it automatically now and using it will then summon the familiar again so it is there after teleporting) and perform other actions normally prohibited by having a follower like a charmed human. Each set can hold 10 and more sets will be created once the one being added to is full. You can choose the active set by name (default is FamiliarsList1, FamiliarsList2, etc), remove familiars from sets, move familiars between sets, and the sets can be renamed. To summon a familiar from your current set (which can be chosen with .cfs [Name]), .lf to see the units available and use .bind #; to choose a different one, use .unbind, and if it gets weird or buggy at any point .resetfams can be used to clear your active familiar data and followerBuffer. All units have the same stats as usual, however their phys/spell power starts at 10% at level 1 and will scale to 100% again when they reach max level. Base health is 500 and will scale with up to 2500 at max level. Traders, carriages, horses, crystals (looking at you dracula blood crystal thing) and werewolves are unavailable. Werewolves will be in the pool once I add handling for their transformations but the rest will likely stay banned (if you manually add one of them to your unlocks as an admin that's on you :P). Can't bind in combat or pvp combat.
- **Classes:** Soft synergies allows for the encouragement of stat choices without restricting choices while hard synergies will only allow the stats for that class to be chosen. Both soft or hard will apply the synergy multiplier to the max stat bonuses for that class for weapon expertise/blood legacies. Additionally, each class has a config spot for 4 blood buffs (the correct prefabs for this start with AB_BloodBuff_) that are granted every MaxPlayerLevel/#buffs while leveling up (with 4 buffs and 90 being the max, they'll be applied at 22, 44, 66, and 88) and apply permanently at max strength without regard to the blood quality or type the player possesses (can be skipped with 0 in the spot). They do not stack with buffs from the same blood type (if a player has the scholar free cast proc buff from class and also has 100% scholar blood, those chances will not stack) which maintains a sense of balance while allowing each class to have a persistent theme/identity that can be earned without excessive grinding. Classes also get extra spells to use on shift that are configurable, both the spell and at what level of prestige they are earned at but do keep in mind the considerations mentioned in the prestige section. Will probably be changing at least a few aspects of these new class systems in the future but am overall happy with where they're at in the sense that they allow for a lot of customization and can be made to feel as unique as you'd like. Tried to make decent thematic choices for the default blood buffs and spells per class but no promises :P. Changing classes should properly remove and apply buffs based on level. Also, spell school effects on hit unique to each class if enabled!
  
## Commands

### Blood Commands
- `.bloodlegacy getprogress [Blood]`
  - Display progress and bonuses in entered blood legacy.
  - Shortcut: *.blg get [Blood]*
- `.bloodlegacy logprogress`
  - Enables or disables blood legacy logging.
  - Shortcut: *.blg log*
- `.bloodlegacy choosestat [Blood] [Stat]`
  - Chooses stat bonus to apply to a blood type that scales with that blood legacy level.
  - Shortcut: *.blg cst [Blood] [Stat]*
- `.bloodlegacy set [Player] [Blood] [Level]` 🔒
  - Sets player blood legacy level.
  - Shorcut: *.blg set [Player] [Blood] [Level]*
- `.bloodlegacy liststats`
  - Lists blood stats available along with bonuses at max legacy.
  - Shortcut: *.blg lst*
- `.bloodlegacy resetstats`
  - Resets stat choices for currently equipped blood for configurable item cost/quanity.
  - Shortcut: *.blg rst*
- `.bloodlegacy list`
  - Lists blood legacies available.
  - Shortcut: *.blg l*

### Class Commands
- `.class choose [Class]`
  - Chooses class (see options with .classes)
  - Shortcut: *.class c [Class]*
- `.class choosespell [#]`
  - Choose class spell (see options with .class lsp)
  - Shortcut: *.class csp [#]*
- `.class change [Class]`
  - Changes to specified class.
  - Shortcut: *.class change [Class]*
- `.class list`
  - List classes.
  - Shortcut: *.class l*
- `.class syncbuffs`
  - Applies class buffs appropriately if not present
  - Shortcut: *.class sb*
- `.class listbuffs [Class]`
  - List class buffs.
  - Shortcut: *.class lb [Class]*
- `.class listspells [Class]`
  - List class spells.
  - Shortcut: *.class lsp [Class]*
- `.class liststats [Class]`
  - List class spells.
  - Shortcut: *.class lst [Class]*
 
### Leveling Commands
- `.level get`
  - Display current level progress.
  - Shortcut: *.lvl get*
- `.level log`
  - Enables or disables leveling experience logging.
  - Shortcut: *.lvl log*
- `.level set [Player] [Level]` 🔒
  - Sets player experience level.
  - Shorcut: *.lvl set [Player] [Level]*

### Prestige Commands
- `.prestige me [PrestigeType]`
  - Handles all player prestiging. Must be at max configured level for experience, expertise, or legacies to be eligible.
  - Shortcut: *.prestige me [PrestigeType]*
- `.prestige get [PrestigeType]`
  - Shows information about player's prestige status and rates for entered type of prestige.
  - Shortcut: *.prestige get [PrestigeType]*
- `.prestige reset [Name] [PrestigeType]` 🔒
  - Resets prestige type and removes buffs gained from prestiges in leveling if applicable.
  - Shortcut: *.prestige r [Name] [PrestigeType]*
- `.prestige set [Name] [PrestigeType] [Level]` 🔒
  - Sets player prestige level.
  - Shorcut: *.spr [Name] [PrestigeType] [Level]*
- `.prestige list`
  - Lists prestiges available to the player.
  - Shortcut: *.prestige l*
- `.prestige listbuffs`
  - Lists prestige buffs available.
  - Shortcut: *.prestige lb*
- `.prestige syncbuffs`
  - Applies prestige buffs appropriately if not present.
  - Shortcut: *.prestige sb*

### Party Commands
- `.party toggleinvites`
  - Toggles party invites.
  - Shortcut: *.party toginv*
- `.party add [Player]`
  - Adds player to party.
  - Shortcut: *.party a [Player]*
- `.party remove [Player]`
  - Removes player from party.
  - Shortcut: *.party r [Player]*
- `.party disband`
  - Disbands party.
  - Shortcut: *.party end*
- `.party listmembers`
  - Lists party members.
  - Shortcut: *.party lm*
- `.party leave`
  - Leaves party.
  - Shortcut: *.party drop*


### Profession Commands
- `.profession getprogress [Profession]`
  - Display progress in entered profession.
  - Shortcut: *.prof get [Profession]*
- `.profession logprogress`
  - Enables or disables profession progress logging (also controls if user is informed of bonus yields from profession levels).
  - Shortcut: *.prof log*
- `.profession setlevel [Name] [Profession] [Level]` 🔒
  - Sets player profession level.
  - Shortcut: *.prof set [Name] [Profession] [Level]*
- `.profession list`
  - Lists available professions.
  - Shortcut: *.prof l*
    
### Weapon Commands
- `.weapon getexpertise`
  - Display expertise progress for current weapon along with any bonus stats if applicable.
  - Shortcut: *.wep get*
- `.weapon logexpertise`
  - Enables or disables expertise logging.
  - Shortcut: *.wep log*
- `.weapon choosestat [Weapon] [Stat]`
  - Chooses stat bonus to apply to a weapon type that scales based on expertise level.
  - Shortcut: *.wep cst [Weapon] [Stat]*
- `.weapon setexpertise [Player] [Weapon] [Level]` 🔒
  - Sets player weapon expertise level.
  - Shorcut: *.wep set [Player] [Weapon] [Level]*
- `.weapon liststats`
  - Lists weapon stats available along with bonuses at max expertise.
  - Shortcut: *.wep lst*
- `.weapon list`
  - Lists weapon types/expertises available.
  - Shortcut: *.wep l*
- `.weapon resetstats`
  - Resets stat choices for currently equipped weapon for configurable item cost/quanity.
  - Shortcut: *.wep rst*
- `.weapon setspells [Slot] [AbilityGroupPrefab]` 🔒
  - Manually set spells in unarmed slots. Useful for testing but do be mindful, some NPC spells are fairly dangerous- and not in an OP sort of way :P
  - Shortcut: *.wep spell [Slot] [AbilityGroupPrefab]*
- `.weapon restorelevels`
  - Restores weapon levels in player inventory to what they should be if they had been modified via expertise level in earlier versions of the mod. Don't use unless needed.
  - Shortcut: *.wep restore*

- `.lockSpell`
  - Enables registering spells to use in unarmed slots if extra slots for unarmed are enabled. Toggle, move spells to slots, then toggle again and switch to unarmed.
  - Shortcut: *.locksp*
- `.lockshift`
  - Toggles set class spell on shift. Works for unarmed and weapons.
  - Shortcut: *.shift*
 
### Familiar Commands
- `.familiar bind [#]`
  - Activates specified familiar from current list.
  - Shortcut: *.fam b [#]*
- `.familiar unbind`
  - Destroys active familiar.
  - Shortcut: *.fam ub*
- `.familiar list`
  - Lists unlocked familiars from current box.
  - Shortcut: *.fam l*
- `.familiar boxes`
  - Shows the available familiar boxes.
  - Shorcut: *.fam box*
- `.familiar choosebox [Name]`
  - Choose active box of familiars to bind from.
  - Shortcut: *.fam cb [Name]*
- `.familiar renamebox [CurrentName] [NewName]`
  - Renames a box.
  - Shortcut: *.fam rb [CurrentName] [NewName]*
- `.familiar movebox [BoxName]`
  - Move active familiar to specified box.
  - Shortcut: *.fam mb [SetName]*
- `.familiar toggle`
  - Calls or dismisses familar. Wave also does this if .emotes is toggled on.
  - Shortcut: *.fam toggle*
- `.familiar add [CharPrefab]` 🔒
  - Adds familiar to active list.
  - Shortcut: *.fam a [CharPrefab]*
- `.familiar remove [#]`
  - Removes familiar from unlocks.
  - Shortcut: *.rf [#]*
- `.toggleEmotes`
  - Toggles emote commands (only 1 right now) on.
  - Shortcut: *.emotes*
- `.listEmoteActions`
  - Lists emote actions and what they do.
  - Shortcut: *.fam le*
- `.familiar getlevel`
  - Display current familiar leveling progress.
  - Shortcut: *.fam gl*
- `.familiar prestige [StatType]`
  - Prestiges familiar if at max level and valid stat is chosen.
  - Shortcut: *.fam pr [StatType]*
- `.familiar setlevel [Level]` 🔒
  - Set current familiar level. Rebind to force updating stats if that doesn't happen when this is used.
  - Shortcut: *.fam sl [Level]*
- `.familiar reset`
  - Resets (destroys) entities found in followerbuffer and clears familiar actives data.
  - Shortcut: *.fam reset*

### Misc Commands
- `.prepareForTheHunt`
  - Completes GettingReadyForTheHunt if not already completed. 
  - Shortcut: *.prepare*

 
## Configuration

### General
- **Language Localization**: `LanguageLocalization` (string, default: English)
  The language localization for prefabs displayed to users. English by default. Options: Brazilian, English, French, German, Hungarian, Italian, Japanese, Koreana, Latam, Polish, Russian, SimplifiedChinese, TraditionalChinese, Thai, Turkish, Vietnamese
  
### Leveling/Prestige Systems
- **Enable Leveling System**: `LevelingSystem` (bool, default: false)  
  Enable or disable the leveling system.
- **Enable Prestige System**: `PrestigeSystem` (bool, default: false)  
  Enable or disable the prestige system.
- **Prestige Buffs**: `PrestigeBuffs` (string, default: "1504279833,1966156848,505940050,-692773400,-1971511915,-564979747,1796711064,1486229325,1126020850,1126020850")
  The Prefabs for general prestige buffs, use 0 to skip otherwise buff applies at the prestige level based on spot in the list.
- **Prestige Levels to Unlock Class Spells:** `PrestigeLevelsToUnlockClassSpells` (string, default: "0,1,2,3")
  The prestige levels at which class spells are unlocked. This should match the number of spells per class. Can leave at 0 if you want them unlocked from the start.
- **Blood Knight Buffs:** `BloodKnightBuffs` (string, default: "1828387635,-714434113,-534491790,-1055766373")
  The PrefabGUID hashes for blood knight leveling blood buffs.
- **Blood Knight Spells:** `BloodKnightSpells` (string, default: "-433204738,-1161896955,1957691133,-7407393")
  Blood Knight shift spells, granted at levels of prestige.
- **Demon Hunter Buffs:** `DemonHunterBuffs` (string, default: "-154702686,-285745649,-1510965956,-536284884")
  The PrefabGUID hashes for demon hunter leveling blood buffs.
- **Demon Hunter Spells:** `DemonHunterSpells` (string, default: "-433204738,1611191665,-328617085,-1161896955")
  Demon Hunter shift spells, granted at levels of prestige.
- **Vampire Lord Buffs:** `VampireLordBuffs` (string, default: "-1266262267,-1413561088,1103099361,1558171501")
  The PrefabGUID hashes for vampire lord leveling blood buffs.
- **Vampire Lord Spells:** `VampireLordSpells` (string, default: "-433204738,716346677,1450902136,-254080557")
  Vampire Lord shift spells, granted at levels of prestige.
- **Shadow Blade Buffs:** `ShadowBladeBuffs` (string, default: "894725875,997154800,-1576592687,-285745649")
  The PrefabGUID hashes for shadow blade leveling blood buffs.
- **Shadow Blade Spells:** `ShadowBladeSpells` (string, default: "-433204738,94933870,642767950,1922493152")
  Shadow Blade shift spells, granted at levels of prestige.
- **Arcane Sorcerer Buffs:** `ArcaneSorcererBuffs` (string, default: "-901503997,884683323,-993492354,-1859298707")
  The PrefabGUID hashes for arcane sorcerer leveling blood buffs.
- **Arcane Sorcerer Spells:** `ArcaneSorcererSpells` (string, default: "-433204738,495259674,1217615468,-1503327574")
  Arcane Sorcerer shift spells, granted at levels of prestige.
- **Death Mage Buffs:** `DeathMageBuffs` (string, default: "1643157297,1159173627,1006510207,997154800")
  The PrefabGUID hashes for death mage leveling blood buffs.
- **Death Mage Spells:** `DeathMageSpells` (string, default: "-433204738,234226418,1619461812,1006960825")
  Demon Hunter shift spells, granted at levels of prestige.
- **Max Player Level**: `MaxLevel` (int, default: 90)  
  The maximum level a player can reach.
- **Max Leveling Prestiges**: `MaxLevelingPrestiges` (int, default: 10)  
  The maximum number of prestiges a player can reach in leveling.
- **Prestige Rates Reducer**: `PrestigeRatesReducer` (float, default: 0.10)  
  Factor by which rates are reduced in expertise/legacy/experience per increment of prestige in expertise, legacy or experience.
- **Prestige Stat Multiplier**: `PrestigeStatMultiplier` (float, default: 0.10)  
  Factor by which stats are increased in expertise/legacy bonuses per increment of prestige in expertise/legacy.
- **Prestige Rates Multiplier**: `PrestigeRatesMultiplier` (float, default: 0.10)  
  Factor by which rates are increased in expertise/legacy per increment of prestige in leveling.
- **Starting Player Level**: `StartingLevel` (int, default: 0)  
  The starting level for players. Use this to skip the first few journal quests for now until I think of a better solution.
- **Unit Leveling Multiplier**: `UnitLevelingMultiplier` (float, default: 7.5)  
  Multiplier for experience gained from units.
- **VBlood Leveling Multiplier**: `VBloodLevelingMultiplier` (float, default: 15)  
  Multiplier for experience gained from VBloods.
- **War Event Multiplier**: `WarEventMultiplier` (float, default: 0.2)  
  The multiplier for experience gained from war event trash spawns.
- **Unit Spawner Multiplier**: `UnitSpawnerMultiplier` (float, default: 0)  
  The multiplier for experience gained from unit spawners (vermin nest, graves).
- **Docile Unit Multiplier**: `UnitSpawnerMultiplier` (float, default: 0.15)  
  The multiplier for experience gained from non-aggressive units.
- **Group Leveling Multiplier**: `GroupLevelingMultiplier` (float, default: 1)  
  Multiplier for experience gained from group kills.
- **Experience Share Distance**: `ExpShareDistance` (float, default: 25)
  Default is about 5 floor tile lengths.
- **Level Scaling Multiplier**: `LevelScalingMultiplier` (float, default: 0.05)  
  Reduces experience gained from kills with a large level gap between player and unit, increase to make harsher decrease or set to 0 to remove.
- **Player Parties**: `PlayerParties` (bool, default: false)  
  Enable or disable player alliances.
- **Prevent Friendly Fire**: `PreventFriendlyFire` (bool, default: false)  
  True to prevent damage between players in same alliance, false to allow.
- **Max Party Size**: `MaxPartySize` (int, default: 5)  
  The maximum number of players that can form an alliance.
- **Change Class Item**: `ChangeClassItem` (int, default: 576389135)  
  Item PrefabGUID for changing classes.
- **Change Class Item Quantity**: `ChangeClassQuantity` (int, default: 1000)  
  Quantity of item cost required to change class.
  
### Expertise System
- **Enable Expertise System**: `ExpertiseSystem` (bool, default: false)  
  Enable or disable the expertise system.
- **Unarmed Slots**: `UnarmedSlots` (bool, default: false)  
  Enables extra spell slots for unarmed.
- **Second Slot Unlock**: `ShiftSlot` (bool, default: false)  
  Enables class spell on shift.
- **Max Expertise Level**: `MaxExpertiseLevel` (int, default: 99)  
  Maximum level in weapon expertise.
- **Max Expertise Prestiges**: `MaxExpertisePrestiges` (int, default: 10)  
  Maximum prestiges for weapon expertise.
- **Unit Expertise Multiplier**: `UnitExpertiseMultiplier` (float, default: 2)  
  Multiplier for expertise gained from units.
- **VBlood Expertise Multiplier**: `VBloodExpertiseMultiplier` (float, default: 5)  
  Multiplier for expertise gained from VBloods.
- **Expertise Stat Choices**: `ExpertiseStatChoices` (int, default: 2)  
  The maximum number of stat choices a player can choose for weapon expertise per weapon.
- **Reset Expertise Item**: `ResetExpertiseItem` (int, default: 576389135)  
  Item PrefabGUID cost for resetting expertise stats.
- **Reset Expertise Item Quantity**: `ResetExpertiseItemQuantity` (int, default: 500)  
  Quantity of item cost required for resetting expertise stats.

### Expertise Stats
- **Max Health**: `MaxHealth` (float, default: 250.0)  
  Base cap for max health.
- **Movement Speed**: `MovementSpeed` (float, default: 0.25)  
  Base cap for movement speed.
- **Primary Attack Speed**: `PrimaryAttackSpeed` (float, default: 0.10)  
  Base cap for primary attack speed.
- **Physical Lifeleech**: `PhysicalLifeLeech` (float, default: 0.10)  
  Base cap for physical lifeleech.
- **Spell Lifeleech**: `SpellLifeLeech` (float, default: 0.10)  
  Base cap for spell lifeleech.
- **Primary Lifeleech**: `PrimaryLifeleech` (float, default: 0.15)  
  Base cap for primary lifeleech.
- **Physical Power**: `PhysicalPower` (float, default: 10)  
  Base cap for physical power.
- **Spell Power**: `SpellPower` (float, default: 10)  
  Base cap for spell power.
- **Physical Crit Chance**: `PhysicalCritChance` (float, default: 0.10)  
  Base cap for physical critical strike chance.
- **Physical Crit Damage**: `PhysicalCritDamage` (float, default: 0.50)  
  Base cap for physical critical strike damage.
- **Spell Crit Chance**: `SpellCritChance` (float, default: 0.10)  
  Base cap for spell critical strike chance.
- **Spell Crit Damage**: `SpellCritDamage` (float, default: 0.50)  
  Base cap for spell critical strike damage.

### Blood System
- **Enable Blood System**: `BloodSystem` (bool, default: false)  
  Enable or disable the blood legacy system.
- **Max Blood Level**: `MaxBloodLevel` (int, default: 99)  
  Maximum level in blood legacies.
- **Max Legacy Prestiges**: `MaxLegacyPrestiges` (int, default: 10)  
  Maximum prestiges in legacies.
- **Blood Quality Bonus**: `BloodQualityBonus` (bool, default: false)  
  Enable or disable blood quality bonus (if using presige, legacy level will be used with PrestigeBloodQuality multiplier below)
- **Prestige Blood Quality**: `PrestigeBloodQuality` (float, default: 5)  
  Blood quality bonus per prestige legacy level.
- **Unit Legacy Multiplier**: `UnitLegacyMultiplier` (float, default: 5)  
  Multiplier for essence gained from units.
- **VBlood Legacy Multiplier**: `VBloodLegacyMultipler` (float, default: 15)  
  Multiplier for essence gained from VBloods.
- **Legacy Stat Choices**: `LegacyStatChoices` (int, default: 2)  
  The maximum number of stat choices a player can choose for weapon expertise per weapon.
- **Reset Legacy Item**: `ResetLegacyItem` (int, default: 576389135)  
  Item PrefabGUID cost for resetting legacy stats.
- **Reset Legacy Item Quantity**: `ResetLegacyItemQuantity` (int, default: 500)  
  Quantity of item cost required for resetting legacy stats.

### Legacy Stats
- **Healing Received**: `HealingReceived` (float, default: 0.15)  
  The base cap for healing received.
- **Damage Reduction**: `DamageReduction` (float, default: 0.05)  
  The base cap for damage reduction.
- **Physical Resistance**: `PhysicalResistance` (float, default: 0.10)  
  The base cap for physical resistance.
- **Spell Resistance**: `SpellResistance` (float, default: 0.10)  
  The base cap for spell resistance.
- **Resource Yield**: `ResourceYield` (float, default: 0.25)  
  The base cap for blood drain.
- **Crowd Control Reduction**: `CCReduction` (float, default: 0.20)  
  The base cap for crowd control reduction.
- **Spell Cooldown Recovery Rate**: `SpellCooldownRecoveryRate` (float, default: 0.10)  
  The base cap for spell cooldown recovery rate.
- **Weapon Cooldown Recovery Rate**: `WeaponCooldownRecoveryRate` (float, default: 0.10)  
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
- **Extra Recipes**: `ExtraRecipes` (bool, default: false)  
  Adds copper wires to fabricator, vampiric dust to advanced grinder, and silver ingots to basic furnace.

### Familiar System
- **Enable Familiar System**: `FamiliarSystem` (bool, default: false)  
  Enable or disable the familiar system.
- **Allow VBloods**: `AllowVBloods` (bool, default: false)  
  Allow VBloods to be unlocked as familiars (this includes shardbearers, if you want those excluded use the bannedUnits list).
- **Banned Units**: `BannedUnits` (string, default: "")  
  Prefabs for banned units go here.
- **Banned Types**: `BannedTypes` (string, default: "")  
  The types of units that cannot be used as familiars go here. (Human, Undead, Demon, Mechanical, Beast)
- **Max Familiar Level**: `MaxFamiliarLevel` (int, default: 90)  
  Maximum level familiars can reach.
- **Familiar Prestiges**: `FamiliarPrestiges` (bool, default: false)  
  Enables prestiging for familiars.
- **Max Familiar Prestiges**: `MaxFamiliarPrestiges` (int, default: 10)  
  Maximum prestiges for familiars.
- **Familiar Prestige Stat Multiplier**: `FamiliarPrestigeStatMultiplier` (float, default: 0.10)  
  Factor by which stats are increased in for prestiged familiars.
- **Unit Familiar Multiplier**: `UnitFamiliarMultiplier` (float, default: 5)  
  Multiplier for experience gained from units.
- **VBlood Familiar Multiplier**: `VBloodFamiliarMultiplier` (float, default: 15)  
  Multiplier for experience gained from VBloods.
- **VBlood Damage Multiplier**: `VBloodDamageMultiplier` (float, default: 1)  
  Controls damage to VBloods by familiars.
- **Player Damage Multiplier**: `PlayerDamageMultiplier` (float, default: 1)  
  Controls damage to players by familiars.
- **Unit Unlock Chance**: `UnitUnlockChance` (float, default: 0.05)  
  The chance for a unit to unlock a familiar when killed.
- **VBlood Unlock Chance**: `VBloodUnlockChance` (float, default: 0.01)  
  The chance for a VBlood to unlock a familiar when killed.

### Class System
- **Soft Synergies**: `SoftSynergies` (bool, default: false)
  Allow class synergies (turns on classes and does not restrict stat choices, do not use this and hard synergies at the same time).
- **Hard Synergies**: `HardSynergies` (bool, default: false)
  Allow class synergies (turns on classes and restricts stat choices, do not use this and soft synergies at the same time).
- **Stat Synergy Multiplier**: `StatSynergyMultiplier` (float, default: 2)
  Multiplier for class stat synergies to max stat bonus.
- **Blood Knight Weapons:** `BloodKnightWeapon` (string, default: "0,3,5,6")
  Blood Knight weapon synergies.
- **Blood Knight Blood:** `BloodKnightBlood` (string, default: "0,2,3,11")
  Blood Knight blood synergies.
- **Demon Hunter Weapons:** `DemonHunterWeapon` (string, default: "1,2,8,9")
  Demon Hunter weapon synergies.
- **Demon Hunter Blood:** `DemonHunterBlood` (string, default: "2,5,7,9")
  Demon Hunter blood synergies.
- **Vampire Lord Weapons:** `VampireLordWeapon` (string, default: "0,5,6,7")
  Vampire Lord weapon synergies.
- **Vampire Lord Blood:** `VampireLordBlood` (string, default: "1,4,8,11")
  Blood Knight blood synergies.
- **Shadow Blade Weapons:** `ShadowBladeWeapon` (string, default: "1,2,3,4")
  Shadow Blade weapon synergies.
- **Shadow Blade Blood:** `ShadowBladeBlood` (string, default: "3,7,8,10")
  Shadow Blade blood synergies.
- **Arcane Sorcerer Weapons:** `ArcaneSorcererWeapon` (string, default: "4,7,10,11")
  Arcane Sorcerer weapon synergies.
- **Arcane Sorcerer Blood:** `ArcaneSorcererBlood` (string, default: "0,6,8,10")
  Arcane Sorcerer blood synergies.
- **Death Mage Weapons:** `DeathMageWeapon` (string, default: "0,3,4,7")
  Death Mage weapon synergies.
- **Death Mage Blood:** `DeathMageBlood` (string, default: "2,6,9,10")
  Death Mage blood synergies.

  


