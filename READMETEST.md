## Table of Contents

Commands/config up to date, better feature summaries next on the list.

- [Sponsors](#sponsors)
- [Features](#features)
- [Commands](#commands)
- [Configuration](#configuration)
- [Recommended Mods](#recommended)

## Sponsor this project

[![patreon](https://i.imgur.com/u6aAqeL.png)](https://www.patreon.com/join/4865914)  [![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/zfolmt)

## Sponsors

Jairon Orellana; Odjit; Jera; Eve winters; Kokuren TCG and Gaming Shop;

## Commands

### Bloodlegacy Commands
- `.bloodlegacy get [BloodType]`
  - Display your current blood legacy progress.
  - Shortcut: *.bl get [BloodType]*
- `.bloodlegacy log`
  - Toggles Legacy progress logging.
  - Shortcut: *.bl log*
- `.bloodlegacy choosestat [Blood] [BloodStat]`
  - Choose a blood stat to enhance based on your legacy.
  - Shortcut: *.bl cst [Blood] [BloodStat]*
- `.bloodlegacy resetstats`
  - Reset stats for current blood.
  - Shortcut: *.bl rst*
- `.bloodlegacy liststats`
  - Lists blood stats available.
  - Shortcut: *.bl lst*
- `.bloodlegacy set [Player] [Blood] [Level]` 🔒
  - Sets player Blood Legacy level.
  - Shortcut: *.bl set [Player] [Blood] [Level]*
- `.bloodlegacy list`
  - Lists blood legacies available.
  - Shortcut: *.bl l*

### Class Commands
- `.class choose [Class]`
  - Choose class.
  - Shortcut: *.class c [Class]*
- `.class choosespell [#]`
  - Sets shift spell for class if prestige level is high enough.
  - Shortcut: *.class csp [#]*
- `.class change [Class]`
  - Change classes.
  - Shortcut: *.class change [Class]*
- `.class syncbuffs`
  - Applies class buffs appropriately if not present.
  - Shortcut: *.class sb*
- `.class list`
  - Lists classes.
  - Shortcut: *.class l*
- `.class listbuffs [ClassType]`
  - Shows perks that can be gained from class.
  - Shortcut: *.class lb [ClassType]*
- `.class listspells [ClassType]`
  - Shows spells that can be gained from class.
  - Shortcut: *.class lsp [ClassType]*
- `.class liststats [Class]`
  - Shows weapon and blood stat synergies for a class.
  - Shortcut: *.class lst [Class]*

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
  - Shortcut: *.fam box*
- `.familiar choosebox [Name]`
  - Choose active box of familiars.
  - Shortcut: *.fam cb [Name]*
- `.familiar renamebox [CurrentName] [NewName]`
  - Renames a box.
  - Shortcut: *.fam rb [CurrentName] [NewName]*
- `.familiar movebox [BoxName]`
  - Moves active familiar to specified box.
  - Shortcut: *.fam mb [BoxName]*
- `.familiar deletebox [BoxName]`
  - Deletes specified box if empty.
  - Shortcut: *.fam db [BoxName]*
- `.familiar addbox [BoxName]`
  - Adds empty box with name.
  - Shortcut: *.fam ab [BoxName]*
- `.familiar add [Name] [PrefabGUID/CHAR_Unit_Name]` 🔒
  - Unit testing.
  - Shortcut: *.fam a [Name] [PrefabGUID/CHAR_Unit_Name]*
- `.familiar remove [#]`
  - Removes familiar from current set permanently.
  - Shortcut: *.fam r [#]*
- `.familiar getlevel`
  - Display current familiar leveling progress.
  - Shortcut: *.fam gl*
- `.familiar setlevel [Player] [Level]` 🔒
  - Set current familiar level.
  - Shortcut: *.fam sl [Player] [Level]*
- `.familiar prestige [BonusStat]`
  - Prestiges familiar if at max, raising base stats by configured multiplier and adding an extra chosen stat.
  - Shortcut: *.fam pr [BonusStat]*
- `.familiar reset`
  - Resets (destroys) entities found in followerbuffer and clears familiar actives data.
  - Shortcut: *.fam reset*
- `.familiar search [Name]`
  - Searches boxes for unit with entered name.
  - Shortcut: *.fam s [Name]*
- `.familiar shinybuff [SpellSchool]`
  - Chooses shiny for current active familiar, one freebie then costs configured amount to change if already unlocked.
  - Shortcut: *.fam shiny [SpellSchool]*
- `.familiar resetshiny [Name]` 🔒
  - Allows player to choose another free visual, however, does not erase any visuals they have chosen previously. Mainly for testing.
  - Shortcut: *.fam rs [Name]*
- `.familiar toggleoption [Setting]`
  - Toggles various familiar settings.
  - Shortcut: *.fam option [Setting]*

### Level Commands
- `.level log`
  - Toggles leveling progress logging.
  - Shortcut: *.lvl log*
- `.level get`
  - Display current leveling progress.
  - Shortcut: *.lvl get*
- `.level set [Player] [Level]` 🔒
  - Sets player level.
  - Shortcut: *.lvl set [Player] [Level]*

### Misc Commands
- `.reminders`
  - Toggles general reminders for various mod features.
  - Shortcut: *.remindme*
- `.sct`
  - Toggles scrolling text.
  - Shortcut: *.sct*
- `.starterkit`
  - Provides starting kit.
  - Shortcut: *.kitme*
- `.prepareforthehunt`
  - Completes GettingReadyForTheHunt if not already completed.
  - Shortcut: *.prepare*
- `.lockspells`
  - Locks in the next spells equipped to use in your unarmed slots.
  - Shortcut: *.locksp*
- `.lockshift`
  - Toggle shift spell.
  - Shortcut: *.shift*
- `.userstats`
  - Shows neat information about the player.
  - Shortcut: *.userstats*
- `.silence`
  - Resets music for player.
  - Shortcut: *.silence*
- `.exoform`
  - Toggles taunting to enter exo form.
  - Shortcut: *.exoform*
- `.cleanupfams. 🔒`
  - Removes disabled, invisible familiars on the map preventing building.
  - Shortcut: *.cleanupfams*

### Party Commands
- `.party toggleinvites`
  - Toggles being able to be invited to parties, prevents damage and share exp.
  - Shortcut: *.party inv*
- `.party add [Player]`
  - Adds player to party.
  - Shortcut: *.party a [Player]*
- `.party remove [Player]`
  - Removes player from party.
  - Shortcut: *.party r [Player]*
- `.party listmembers`
  - Lists party members of your active party.
  - Shortcut: *.party lm*
- `.party disband`
  - Disbands party.
  - Shortcut: *.party end*
- `.party leave`
  - Leaves party if in one.
  - Shortcut: *.party drop*

### Prestige Commands
- `.prestige self [PrestigeType]`
  - Handles player prestiging.
  - Shortcut: *.prestige me [PrestigeType]*
- `.prestige set [Name] [PrestigeType] [Level]` 🔒
  - Sets the specified player to a certain level of prestige in a certain type of prestige.
  - Shortcut: *.prestige set [Name] [PrestigeType] [Level]*
- `.prestige listbuffs`
  - Lists prestige buff names.
  - Shortcut: *.prestige lb*
- `.prestige reset [Name] [PrestigeType]` 🔒
  - Handles resetting prestiging.
  - Shortcut: *.prestige r [Name] [PrestigeType]*
- `.prestige syncbuffs`
  - Applies prestige buffs appropriately if not present.
  - Shortcut: *.prestige sb*
- `.prestige get [PrestigeType]`
  - Shows information about player's prestige status.
  - Shortcut: *.prestige get [PrestigeType]*
- `.prestige list`
  - Lists prestiges available.
  - Shortcut: *.prestige l*
- `.prestige leaderboard [PrestigeType]`
  - Lists prestige leaderboard for type.
  - Shortcut: *.prestige lb [PrestigeType]*
- `.prestige shroud`
  - Toggles permashroud if applicable.
  - Shortcut: *.prestige shroud*

### Profession Commands
- `.profession log`
  - Toggles profession progress logging.
  - Shortcut: *.prof log*
- `.profession get [Profession]`
  - Display your current profession progress.
  - Shortcut: *.prof get [Profession]*
- `.profession set [Name] [Profession] [Level]` 🔒
  - Sets player profession level.
  - Shortcut: *.prof set [Name] [Profession] [Level]*
- `.profession list`
  - Lists professions available.
  - Shortcut: *.prof l*

### Quest Commands
- `.quest log`
  - Toggles quest progress logging.
  - Shortcut: *.quest log*
- `.quest progress [QuestType]`
  - Display your current quest progress.
  - Shortcut: *.quest p [QuestType]*
- `.quest track [QuestType]`
  - Locate and track quest target.
  - Shortcut: *.quest t [QuestType]*
- `.quest refresh [Name]` 🔒
  - Refreshes daily and weekly quests for player.
  - Shortcut: *.quest rf [Name]*
- `.quest reroll [QuestType]`
  - Reroll quest for cost (daily only currently).
  - Shortcut: *.quest r [QuestType]*

### Weapon Commands
- `.weapon getexpertise`
  - Displays your current expertise.
  - Shortcut: *.wep get*
- `.weapon logexpertise`
  - Toggles expertise logging.
  - Shortcut: *.wep log*
- `.weapon choosestat [Weapon] [WeaponStat]`
  - Choose a weapon stat to enhance based on your expertise.
  - Shortcut: *.wep cst [Weapon] [WeaponStat]*
- `.weapon resetwepstats`
  - Reset the stats for current weapon.
  - Shortcut: *.wep rst*
- `.weapon setexpertise [Name] [Weapon] [Level]` 🔒
  - Sets player weapon expertise level.
  - Shortcut: *.wep set [Name] [Weapon] [Level]*
- `.weapon liststats`
  - Lists weapon stats available.
  - Shortcut: *.wep lst*
- `.weapon list`
  - Lists weapon expertises available.
  - Shortcut: *.wep l*
- `.weapon setspells [Name] [Slot] [PrefabGUID] [Radius]` 🔒
  - Manually sets spells for testing (if you enter a radius it will apply to players around the entered name).
  - Shortcut: *.wep spell [Name] [Slot] [PrefabGUID] [Radius]*
- `.weapon restorelevels`
  - Fixes weapon levels if they are not correct. Don't use this unless you need to.
  - Shortcut: *.wep restore*

## Configuration

### General
- **Language Localization**: `LanguageLocalization` (string, default: "English")
  The language localization for prefabs displayed to users. English by default. Options: Brazilian, English, French, German, Hungarian, Italian, Japanese, Koreana, Latam, Polish, Russian, SimplifiedChinese, Spanish, TraditionalChinese, Thai, Turkish, Vietnamese
- **Client Companion**: `ClientCompanion` (bool, default: False)
  Enable if using the client companion mod, can configure what's displayed in the client config.
- **Elite Shard Bearers**: `EliteShardBearers` (bool, default: False)
  Enable or disable elite shard bearers.
- **Shard Bearer Level**: `ShardBearerLevel` (int, default: 0)
  Sets level of shard bearers if elite shard bearers is enabled. Leave at 0 for no effect.
- **Potion Stacking**: `PotionStacking` (bool, default: True)
  Enable or disable potion stacking (can have t01 effects and t02 effects at the same time. also requires professions enabled).

### StarterKit
- **Starter Kit**: `StarterKit` (bool, default: False)
  Enable or disable the starter kit.
- **Kit Prefabs**: `KitPrefabs` (string, default: "862477668,-1531666018,-1593377811,1821405450")
  The PrefabGUID hashes for the starter kit.
- **Kit Quantities**: `KitQuantities` (string, default: "500,1000,1000,250")
  The quantity of each item in the starter kit.

### Quests
- **Quest System**: `QuestSystem` (bool, default: False)
  Enable or disable quests (currently only kill quests).
- **Infinite Dailies**: `InfiniteDailies` (bool, default: False)
  Enable or disable infinite dailies.
- **Quest Rewards**: `QuestRewards` (string, default: "28358550,576389135,-257494203")
  The PrefabGUID hashes for quest reward pool.
- **Quest Reward Amounts**: `QuestRewardAmounts` (string, default: "50,250,50")
  The amount of each reward in the pool. Will be multiplied accordingly for weeklies (*5) and vblood kill quests (*3).
- **Reroll Daily Prefab**: `RerollDailyPrefab` (int, default: -949672483)
  Prefab item for rerolling daily.
- **Reroll Daily Amount**: `RerollDailyAmount` (int, default: 50)
  Cost of prefab for rerolling daily.
- **Reroll Weekly Prefab**: `RerollWeeklyPrefab` (int, default: -949672483)
  Prefab item for rerolling weekly.
- **Reroll Weekly Amount**: `RerollWeeklyAmount` (int, default: 50)
  Cost of prefab for rerolling weekly. Won't work if already completed for the week.

### Leveling
- **Leveling System**: `LevelingSystem` (bool, default: False)
  Enable or disable the leveling system.
- **Rested XP System**: `RestedXPSystem` (bool, default: False)
  Enable or disable rested experience for players logging out inside of coffins (half for wooden, full for stone). Prestiging level will reset accumulated rested xp.
- **Rested XP Rate**: `RestedXPRate` (float, default: 0.05)
  Rate of Rested XP accumulation per tick (as a percentage of maximum allowed rested XP, if configured to one tick per hour 20 hours offline in a stone coffin will provide maximum current rested XP).
- **Rested XP Max**: `RestedXPMax` (int, default: 5)
  Maximum extra levels worth of rested XP a player can accumulate.
- **Rested XP Tick Rate**: `RestedXPTickRate` (float, default: 120)
  Minutes required to accumulate one tick of Rested XP.
- **Max Level**: `MaxLevel` (int, default: 90)
  The maximum level a player can reach.
- **Starting Level**: `StartingLevel` (int, default: 0)
  Starting level for players if no data is found.
- **Unit Leveling Multiplier**: `UnitLevelingMultiplier` (float, default: 7.5)
  The multiplier for experience gained from units.
- **V Blood Leveling Multiplier**: `VBloodLevelingMultiplier` (float, default: 15)
  The multiplier for experience gained from VBloods.
- **Docile Unit Multiplier**: `DocileUnitMultiplier` (float, default: 0.15)
  The multiplier for experience gained from docile units.
- **War Event Multiplier**: `WarEventMultiplier` (float, default: 0.2)
  The multiplier for experience gained from war event trash spawns.
- **Unit Spawner Multiplier**: `UnitSpawnerMultiplier` (float, default: 0)
  The multiplier for experience gained from unit spawners (vermin nests, tombs).
- **Group Leveling Multiplier**: `GroupLevelingMultiplier` (float, default: 1)
  The multiplier for experience gained from group kills.
- **Level Scaling Multiplier**: `LevelScalingMultiplier` (float, default: 0.05)
  Reduces experience gained from kills with a large level gap between player and unit, increase to make harsher decrease or set to 0 to remove.
- **Player Parties**: `PlayerParties` (bool, default: False)
  Enable or disable the ability to group with players not in your clan for experience/familiar unlock sharing.
- **Max Party Size**: `MaxPartySize` (int, default: 5)
  The maximum number of players that can share experience in a group.
- **Exp Share Distance**: `ExpShareDistance` (float, default: 25)
  Default is ~5 floor tile lengths.

### Prestige
- **Prestige System**: `PrestigeSystem` (bool, default: False)
  Enable or disable the prestige system (requires leveling to be enabled)
- **Prestige Buffs**: `PrestigeBuffs` (string, default: "1504279833,475045773,1643157297,946705138,-1266262267,-773025435,-1043659405,-1583573438,-1869022798,-536284884")
  The PrefabGUID hashes for general prestige buffs, use 0 to skip otherwise buff applies at the prestige level
- **Prestige Levels To Unlock Class Spells**: `PrestigeLevelsToUnlockClassSpells` (string, default: "0,1,2,3,4,5")
  The prestige levels at which class spells are unlocked. This should match the number of spells per class +1 to account for the default class spell. Can leave at 0 if you want them unlocked from the start.
- **Max Leveling Prestiges**: `MaxLevelingPrestiges` (int, default: 10)
  The maximum number of prestiges a player can reach in leveling
- **Leveling Prestige Reducer**: `LevelingPrestigeReducer` (float, default: 0.05)
  Flat factor by which experience is reduced per increment of prestige in leveling
- **Prestige Rates Reducer**: `PrestigeRatesReducer` (float, default: 0.1)
  Flat factor by which rates are reduced in expertise/legacy per increment of prestige in expertise/legacy
- **Prestige Stat Multiplier**: `PrestigeStatMultiplier` (float, default: 0.1)
  Flat factor by which stats are increased in expertise/legacy bonuses per increment of prestige in expertise/legacy
- **Prestige Rate Multiplier**: `PrestigeRateMultiplier` (float, default: 0.1)
  Flat factor by which rates are increased in expertise/legacy per increment of prestige in leveling
- **Exo Prestiging**: `ExoPrestiging` (bool, default: False)
  Enable or disable exo prestiges (need to max normal prestiges first).
- **Exo Prestiges**: `ExoPrestiges` (int, default: 100)
  The number of exo prestiges available
- **Exo Prestige Reward**: `ExoPrestigeReward` (int, default: 28358550)
  The reward for exo prestiging (tier 3 nether shards by default).
- **Exo Prestige Reward Quantity**: `ExoPrestigeRewardQuantity` (int, default: 500)
  The quantity of the reward for exo prestiging.

### Expertise
- **Expertise System**: `ExpertiseSystem` (bool, default: False)
  Enable or disable the expertise system
- **Max Expertise Prestiges**: `MaxExpertisePrestiges` (int, default: 10)
  The maximum number of prestiges a player can reach in expertise
- **Unarmed Slots**: `UnarmedSlots` (bool, default: False)
  Enable or disable the ability to use extra unarmed spell slots
- **Shift Slot**: `ShiftSlot` (bool, default: False)
  Enable or disable using class spell on shift
- **Max Expertise Level**: `MaxExpertiseLevel` (int, default: 100)
  The maximum level a player can reach in weapon expertise
- **Unit Expertise Multiplier**: `UnitExpertiseMultiplier` (float, default: 2)
  The multiplier for expertise gained from units
- **V Blood Expertise Multiplier**: `VBloodExpertiseMultiplier` (float, default: 5)
  The multiplier for expertise gained from VBloods
- **Unit Spawner Expertise Factor**: `UnitSpawnerExpertiseFactor` (float, default: 1)
  The multiplier for experience gained from unit spawners (vermin nests, tombs).
- **Expertise Stat Choices**: `ExpertiseStatChoices` (int, default: 2)
  The maximum number of stat choices a player can pick for a weapon expertise. Max of 3 will be sent to client UI for display.
- **Reset Expertise Item**: `ResetExpertiseItem` (int, default: 576389135)
  Item PrefabGUID cost for resetting weapon stats
- **Reset Expertise Item Quantity**: `ResetExpertiseItemQuantity` (int, default: 500)
  Quantity of item required for resetting stats
- **Max Health**: `MaxHealth` (float, default: 250)
  The base cap for maximum health
- **Movement Speed**: `MovementSpeed` (float, default: 0.25)
  The base cap for movement speed
- **Primary Attack Speed**: `PrimaryAttackSpeed` (float, default: 0.1)
  The base cap for primary attack speed
- **Physical Life Leech**: `PhysicalLifeLeech` (float, default: 0.1)
  The base cap for physical life leech
- **Spell Life Leech**: `SpellLifeLeech` (float, default: 0.1)
  The base cap for spell life leech
- **Primary Life Leech**: `PrimaryLifeLeech` (float, default: 0.15)
  The base cap for primary life leech
- **Physical Power**: `PhysicalPower` (float, default: 20)
  The base cap for physical power
- **Spell Power**: `SpellPower` (float, default: 10)
  The base cap for spell power
- **Physical Crit Chance**: `PhysicalCritChance` (float, default: 0.1)
  The base cap for physical critical strike chance
- **Physical Crit Damage**: `PhysicalCritDamage` (float, default: 0.5)
  The base cap for physical critical strike damage
- **Spell Crit Chance**: `SpellCritChance` (float, default: 0.1)
  The base cap for spell critical strike chance
- **Spell Crit Damage**: `SpellCritDamage` (float, default: 0.5)
  The base cap for spell critical strike damage

### Legacies
- **Blood System**: `BloodSystem` (bool, default: False)
  Enable or disable the blood legacy system
- **Max Legacy Prestiges**: `MaxLegacyPrestiges` (int, default: 10)
  The maximum number of prestiges a player can reach in blood legacies
- **Max Blood Level**: `MaxBloodLevel` (int, default: 100)
  The maximum level a player can reach in blood legacies
- **Unit Legacy Multiplier**: `UnitLegacyMultiplier` (float, default: 1)
  The multiplier for lineage gained from units
- **V Blood Legacy Multiplier**: `VBloodLegacyMultiplier` (float, default: 5)
  The multiplier for lineage gained from VBloods
- **Legacy Stat Choices**: `LegacyStatChoices` (int, default: 2)
  The maximum number of stat choices a player can pick for a blood legacy. Max of 3 will be sent to client UI for display.
- **Reset Legacy Item**: `ResetLegacyItem` (int, default: 576389135)
  Item PrefabGUID cost for resetting blood stats
- **Reset Legacy Item Quantity**: `ResetLegacyItemQuantity` (int, default: 500)
  Quantity of item required for resetting blood stats
- **Healing Received**: `HealingReceived` (float, default: 0.15)
  The base cap for healing received
- **Damage Reduction**: `DamageReduction` (float, default: 0.05)
  The base cap for damage reduction
- **Physical Resistance**: `PhysicalResistance` (float, default: 0.1)
  The base cap for physical resistance
- **Spell Resistance**: `SpellResistance` (float, default: 0.1)
  The base cap for spell resistance
- **Resource Yield**: `ResourceYield` (float, default: 0.25)
  The base cap for resource yield
- **CC Reduction**: `CCReduction` (float, default: 0.2)
  The base cap for crowd control reduction
- **Spell Cooldown Recovery Rate**: `SpellCooldownRecoveryRate` (float, default: 0.1)
  The base cap for spell cooldown recovery rate
- **Weapon Cooldown Recovery Rate**: `WeaponCooldownRecoveryRate` (float, default: 0.1)
  The base cap for weapon cooldown recovery rate
- **Ultimate Cooldown Recovery Rate**: `UltimateCooldownRecoveryRate` (float, default: 0.2)
  The base cap for ultimate cooldown recovery rate
- **Minion Damage**: `MinionDamage` (float, default: 0.25)
  The base cap for minion damage
- **Shield Absorb**: `ShieldAbsorb` (float, default: 0.5)
  The base cap for shield absorb
- **Blood Efficiency**: `BloodEfficiency` (float, default: 0.1)
  The base cap for blood efficiency

### Professions
- **Profession System**: `ProfessionSystem` (bool, default: False)
  Enable or disable the profession system
- **Max Profession Level**: `MaxProfessionLevel` (int, default: 100)
  The maximum level a player can reach in professions
- **Profession Multiplier**: `ProfessionMultiplier` (float, default: 10)
  The multiplier for profession experience gained
- **Extra Recipes**: `ExtraRecipes` (bool, default: False)
  Enable or disable extra recipes

### Familiars
- **Familiar System**: `FamiliarSystem` (bool, default: False)
  Enable or disable the familiar system
- **Share Unlocks**: `ShareUnlocks` (bool, default: False)
  Enable or disable sharing unlocks between players in clans or parties (uses exp share distance)
- **Familiar Combat**: `FamiliarCombat` (bool, default: True)
  Enable or disable combat for familiars.
- **Familiar Pv P**: `FamiliarPvP` (bool, default: True)
  Enable or disable PvP participation for familiars. (if set to false, familiars will be unbound when entering PvP combat)
- **Familiar Prestige**: `FamiliarPrestige` (bool, default: False)
  Enable or disable the prestige system for familiars
- **Max Familiar Prestiges**: `MaxFamiliarPrestiges` (int, default: 10)
  The maximum number of prestiges a familiar can reach
- **Familiar Prestige Stat Multiplier**: `FamiliarPrestigeStatMultiplier` (float, default: 0.1)
  The multiplier for stats gained from familiar prestiges
- **Max Familiar Level**: `MaxFamiliarLevel` (int, default: 90)
  The maximum level a familiar can reach
- **Allow V Bloods**: `AllowVBloods` (bool, default: False)
  Allow VBloods to be unlocked as familiars (this includes shardbearers, if you want those excluded use the bannedUnits list)
- **Banned Units**: `BannedUnits` (string, default: "")
  The PrefabGUID hashes for units that cannot be used as familiars. Same structure as the buff lists except unit prefabs
- **Banned Types**: `BannedTypes` (string, default: "")
  The types of units that cannot be used as familiars go here (Human, Undead, Demon, Mechanical, Beast)
- **V Blood Damage Multiplier**: `VBloodDamageMultiplier` (float, default: 1)
  Leave at 1 for no change (controls damage familiars do to VBloods)
- **Unit Familiar Multiplier**: `UnitFamiliarMultiplier` (float, default: 7.5)
  The multiplier for experience gained from units
- **V Blood Familiar Multiplier**: `VBloodFamiliarMultiplier` (float, default: 15)
  The multiplier for experience gained from VBloods
- **Unit Unlock Chance**: `UnitUnlockChance` (float, default: 0.05)
  The chance for a unit to unlock a familiar
- **V Blood Unlock Chance**: `VBloodUnlockChance` (float, default: 0.01)
  The chance for a VBlood to unlock a familiar
- **Shiny Chance**: `ShinyChance` (float, default: 0.2)
  The chance for a visual when unlocking a familiar
- **Shiny Cost Item Prefab**: `ShinyCostItemPrefab` (int, default: -77477508)
  Item PrefabGUID cost for changing shiny visual if one is already unlocked (currently demon fragment by default)
- **Shiny Cost Item Quantity**: `ShinyCostItemQuantity` (int, default: 1)
  Quantity of item required for changing shiny visual

### Classes
- **Soft Synergies**: `SoftSynergies` (bool, default: False)
  Allow class synergies (turns on classes and does not restrict stat choices, do not use this and hard syergies at the same time)
- **Hard Synergies**: `HardSynergies` (bool, default: False)
  Enforce class synergies (turns on classes and restricts stat choices, do not use this and soft syergies at the same time)
- **Change Class Item**: `ChangeClassItem` (int, default: 576389135)
  Item PrefabGUID cost for changing class.
- **Change Class Quantity**: `ChangeClassQuantity` (int, default: 750)
  Quantity of item required for changing class.
- **Class Spell School On Hit Effects**: `ClassSpellSchoolOnHitEffects` (bool, default: False)
  Enable or disable class spell school on hit effects (respective debuff from spell school, leech chill condemn etc).
- **On Hit Proc Chance**: `OnHitProcChance` (float, default: 0.075)
  The chance for a class effect to proc on hit.
- **Stat Synergy Multiplier**: `StatSynergyMultiplier` (float, default: 1.5)
  Multiplier for class stat synergies to base stat cap
- **Blood Knight Weapon**: `BloodKnightWeapon` (string, default: "0,3,5,6")
  Blood Knight weapon synergies
- **Blood Knight Blood**: `BloodKnightBlood` (string, default: "1,5,7,10")
  Blood Knight blood synergies
- **Demon Hunter Weapon**: `DemonHunterWeapon` (string, default: "1,2,8,9")
  Demon Hunter weapon synergies
- **Demon Hunter Blood**: `DemonHunterBlood` (string, default: "2,5,7,9")
  Demon Hunter blood synergies
- **Vampire Lord Weapon**: `VampireLordWeapon` (string, default: "0,4,6,7")
  Vampire Lord weapon synergies
- **Vampire Lord Blood**: `VampireLordBlood` (string, default: "1,3,8,11")
  Vampire Lord blood synergies
- **Shadow Blade Weapon**: `ShadowBladeWeapon` (string, default: "1,2,6,9")
  Shadow Blade weapon synergies
- **Shadow Blade Blood**: `ShadowBladeBlood` (string, default: "3,5,7,10")
  Shadow Blade blood synergies
- **Arcane Sorcerer Weapon**: `ArcaneSorcererWeapon` (string, default: "4,7,10,11")
  Arcane Sorcerer weapon synergies
- **Arcane Sorcerer Blood**: `ArcaneSorcererBlood` (string, default: "0,6,8,10")
  Arcane Sorcerer blood synergies
- **Death Mage Weapon**: `DeathMageWeapon` (string, default: "0,4,7,11")
  Death Mage weapon synergies
- **Death Mage Blood**: `DeathMageBlood` (string, default: "2,3,6,9")
  Death Mage blood synergies
- **Default Class Spell**: `DefaultClassSpell` (int, default: -433204738)
  Default spell (veil of shadow) available to all classes.
- **Blood Knight Buffs**: `BloodKnightBuffs` (string, default: "1828387635,-534491790,-1055766373,-584203677")
  The PrefabGUID hashes for blood knight leveling blood buffs. Granted every MaxLevel/(# of blood buffs)
- **Blood Knight Spells**: `BloodKnightSpells` (string, default: "-880131926,651613264,2067760264,189403977,375131842")
  Blood Knight shift spells, granted at levels of prestige
- **Demon Hunter Buffs**: `DemonHunterBuffs` (string, default: "-154702686,-285745649,-1510965956,-397097531")
  The PrefabGUID hashes for demon hunter leveling blood buffs
- **Demon Hunter Spells**: `DemonHunterSpells` (string, default: "-356990326,-987810170,1071205195,1249925269,-914344112")
  Demon Hunter shift spells, granted at levels of prestige
- **Vampire Lord Buffs**: `VampireLordBuffs` (string, default: "1558171501,997154800,-1413561088,1103099361")
  The PrefabGUID hashes for vampire lord leveling blood buffs
- **Vampire Lord Spells**: `VampireLordSpells` (string, default: "78384915,295045820,-1000260252,91249849,1966330719")
  Vampire Lord shift spells, granted at levels of prestige
- **Shadow Blade Buffs**: `ShadowBladeBuffs` (string, default: "894725875,-1596803256,-993492354,210193036")
  The PrefabGUID hashes for shadow blade leveling blood buffs
- **Shadow Blade Spells**: `ShadowBladeSpells` (string, default: "1019568127,1575317901,1112116762,-358319417,1174831223")
  Shadow Blade shift spells, granted at levels of prestige
- **Arcane Sorcerer Buffs**: `ArcaneSorcererBuffs` (string, default: "1614027598,884683323,-1576592687,-1859298707")
  The PrefabGUID hashes for arcane leveling blood buffs
- **Arcane Sorcerer Spells**: `ArcaneSorcererSpells` (string, default: "247896794,268059675,-242769430,-2053450457,1650878435")
  Arcane Sorcerer shift spells, granted at levels of prestige
- **Death Mage Buffs**: `DeathMageBuffs` (string, default: "-901503997,-651661301,1934870645,1201299233")
  The PrefabGUID hashes for death mage leveling blood buffs
- **Death Mage Spells**: `DeathMageSpells` (string, default: "-1204819086,481411985,1961570821,2138402840,-1781779733")
  Death Mage shift spells, granted at levels of prestige

## Recommended
- [KindredCommands](https://thunderstore.io/c/v-rising/p/odjit/KindredCommands/) 
  Highly recommend getting this if you plan on using Bloodcraft or any other mods for V Rising in general. Invaluable set of tools and options that will greatly improve your modding experience.
- [KindredLogistics](https://thunderstore.io/c/v-rising/p/Kindred/KindredLogistics/) 
  If you mourn QuickStash, this is for you! It comes with much more that will drastically improve your inventory-managing experience in V Rising, only requires server installation.
- [KindredArenas](https://thunderstore.io/c/v-rising/p/odjit/KindredArenas/)
  Allows for controlling the areas players can engage each other in on PvP servers.
- [KindredSchematics](https://thunderstore.io/c/v-rising/p/odjit/KindredSchematics/) 
  Closest thing to creative mode we'll likely ever get. Copy/paste castles, build wherever with whatever you want!
- [KindredPortals](https://thunderstore.io/c/v-rising/p/odjit/KindredPortals/)
  Create custom portals and waygates for your server! Pairs great with KindredSchematics for making new areas.
- [Sanguis](https://thunderstore.io/c/v-rising/p/zfolmt/Sanguis/)
  Simple login reward/reward players for being online mod.
- [BloodyNotify](https://thunderstore.io/c/v-rising/p/Trodi/Notify/)
  Notifications for players coming online or going offline, VBlood kills, and more.
- [BloodyMerchants](https://thunderstore.io/c/v-rising/p/Trodi/BloodyMerchant/)
  Custom merchants! Great for letting players buy items they normally can't or providing a use for otherwise unused prefabs.
- [XPRising](https://thunderstore.io/c/v-rising/p/XPRising/XPRising/)
  If you like the idea of a mod with RPG features but Bloodcraft doesn't float your boat maybe this will!
