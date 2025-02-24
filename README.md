## Table of Contents

*README under construction, putting these enums here for now. note that when selecting via command in-game should use the number shown by the respective 'lst' command for bloods & weapons*

### BloodStatType
HealingReceived, // 0
DamageReduction, // 1
PhysicalResistance, // 2
SpellResistance, // 3
ResourceYield, // 4
BloodDrain, // 5
SpellCooldownRecoveryRate, // 6
WeaponCooldownRecoveryRate, // 7
UltimateCooldownRecoveryRate, // 8
MinionDamage, // 9
ShieldAbsorb, // 10
BloodEfficiency // 11

### WeaponStatType
MaxHealth, // 0
MovementSpeed, // 1
PrimaryAttackSpeed, // 2
PhysicalLifeLeech, // 3
SpellLifeLeech, // 4
PrimaryLifeLeech, // 5
PhysicalPower, // 6
SpellPower, // 7
PhysicalCritChance, // 8
PhysicalCritDamage, // 9
SpellCritChance, // 10
SpellCritDamage // 11

- [Sponsors](#sponsors)
- [Features](#features)
- [Commands](#commands)
- [Configuration](#configuration)
- [Recommended Mods](#recommended)

## Sponsor this project

[![patreon](https://i.imgur.com/u6aAqeL.png)](https://www.patreon.com/join/4865914)  [![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/zfolmt)

## Sponsors

Jairon O.; Odjit; Jera; Eve W.; Kokuren TCG and Gaming Shop; Rexxn; Eduardo G.; DirtyMike; Imperivm Draconis; Geoffrey D.; SirSaia; Robin C.; Jason R.;

## Features

### Experience Leveling
- Gain experience and level up, primarily from slaying enemies.
  - `.lvl log` (toggle experience gain logging in chat)
  - `.lvl get` (view current level and experience progress)
- Rested XP accumulates while logged out in a coffin.
  - Stone coffins provide full rested gains, wooden coffins provide half

### Weapon Expertise
- Gain levels in weapon types when slaying (equipped for final blow) enemies.
  - `.wep log` (toggle gains logging in chat) 
  - `.wep get` (view weapon progression/stat details)
- Pick stats to enhance per weapon when equipped.
  - `.wep lst` (view weapon bonus stat options)
  - `.wep cst` [Weapon] [WeaponStat] (select bonus stat for entered weapon)
  - `.wep rst` (reset stat selection for current weapon)
- Stat bonus values scale with player weapon expertise level per weapon.
  - classes have synergies with different weapon stats (see Classes)

### Blood Legacies
- Gain levels in blood types when feeding on (executes and completions) enemies.
  - `.bl log` (toggle gains logging in chat) 
  - `.bl get` (view blood progression/stat details)
- Pick stats to enhance per blood when using.
  - `.bl lst` (view blood bonus stat options)
  - `.bl cst` [Blood] [BloodStat] (select bonus stat for entered weapon)
  - `.bl rst` (reset stat selection for current weapon)
- Stat bonus values scale with player blood legacy level per blood.
  - classes have synergies with different blood stats (see Classes)

### Classes
- Select a class for free when starting and change later for a cost.
  - `.class l` (list available classes)
  - `.class c [Class]` (select a class)
  - `.class change [Class]` (change class, costs configured items)
- Classes provide:
  - Permanent Buffs unlocked at specific levels based on class
  - Weapon & Blood Stat Synergies which increase the effectiveness for specific stats from expertise and legacies
  - On-Hit Effect debuffs applied at configured chance when the player deals damage (ignite, weaken, chill, etc. and the respective t08 necklace effect if debuff already present on target)
  - Extra Spells, one of which unlocks every leveling prestige by default, that can be used on Shift

### Familiars
- Unlock and summon defeated enemies as your familiar.
  - `.fam l` (list familiars in current box)
  - `.fam b [#]` (summon an unlocked familiar)
  - `.fam ub` (unbind current familiar)
- Familiars level up with experience like the player, stats scale with level and familiar prestiges.
  - `.fam gl` (view current familiar stats and level)
  - `.fam pr [Stat]` (prestige a familiar to gain additional stat bonuses at max or for schematics)
- Familiar Battles with queue system (must designate an arena, one allowed currently).
  - `.fam challenge [Player]` (challenge another player to battle, accept by emoting yes and decline by emoting no)
  - `.fam sba` (set battle arena location, best to enclose so players cannot interfere and view only)
- Shinies:
  - 20% chance for random shiny effect on first unit unlock, 100% chance on repeated unit unlock
  - Chance when dealing damage to apply respective spell school debuff, same proc chance as configured for class onhit effects
  - `.fam shiny [SpellSchool]` (apply or change shiny, requires vampiric dust)

### Quests
- Players can complete daily and weekly quests for XP and rewards.
  - `.quest log` (toggle quest tracking messages)
  - `.quest d` (display current daily quest)
  - `.quest w` (display current weekly quest)
  - `.quest t [daily/weekly]` (track nearest quest target)
  - `.quest r [daily/weekly]` (reroll quests for configured cost)

### Professions
- Gain profession levels by gathering resources and crafting equipment.
  - Mining, Woodcutting, Harvesting → bonus resources per broken resource object, scales with profession level (additional resources of what was broken and profession specific bonus drops; gold ore (salvage to jewelry), random saplings, and random seeds respectively)
  - Fishing → bonus fish every 20 levels
  - Alchemy → increases potion effectiveness & duration (x2 at max, holy pots do not benefit from increased effectiveness but will have longer duration)
  - Blacksmithing, Enchanting, Tailoring → increase base stats (10% at max) and durability (x2 at max) for respective types of crafted gear
  - `.prof log` (toggle profession XP gain logging)
  - `.prof get [Profession]` (view profession level)

### Prestige
- Resets player level, grants a permanent buff, reduces experience gained from units, and increases rate gains for expertise/legacies per prestige.
  - `.prestige me [Type]` (experience and/or various weapons and bloods, the latter variety will see increased bonus stat caps and reduced rate gains per prestige)
  - `.prestige get [Type]` (view prestige progress and details)

## Commands

### Bloodlegacy Commands
- `.bloodlegacy get [BloodType]`
  - Display current blood legacy details.
  - Shortcut: *.bl get [BloodType]*
- `.bloodlegacy log`
  - Toggles Legacy progress logging.
  - Shortcut: *.bl log*
- `.bloodlegacy choosestat [BloodOrStat] [BloodStat]`
  - Choose a bonus stat to enhance for your blood legacy.
  - Shortcut: *.bl cst [BloodOrStat] [BloodStat]*
- `.bloodlegacy resetstats`
  - Reset stats for current blood.
  - Shortcut: *.bl rst*
- `.bloodlegacy liststats`
  - Lists blood stats available.
  - Shortcut: *.bl lst*
- `.bloodlegacy set [Player] [Blood] [Level]` 🔒
  - Sets player blood legacy level.
  - Shortcut: *.bl set [Player] [Blood] [Level]*
- `.bloodlegacy list`
  - Lists blood legacies available.
  - Shortcut: *.bl l*

### Class Commands
- `.class select [Class]`
  - Select class.
  - Shortcut: *.class s [Class]*
- `.class choosespell [#]`
  - Sets shift spell for class if prestige level is high enough.
  - Shortcut: *.class csp [#]*
- `.class change [Class]`
  - Change classes.
  - Shortcut: *.class c [Class]*
- `.class syncbuffs`
  - Applies class buffs appropriately if not present.
  - Shortcut: *.class sb*
- `.class list`
  - Lists classes.
  - Shortcut: *.class l*
- `.class listbuffs [Class]`
  - Shows perks that can be gained from class.
  - Shortcut: *.class lb [Class]*
- `.class listspells [Class]`
  - Shows spells that can be gained from class.
  - Shortcut: *.class lsp [Class]*
- `.class liststats [Class]`
  - Shows weapon and blood stat synergies for a class.
  - Shortcut: *.class lst [Class]*
- `.class lockshift`
  - Toggle shift spell.
  - Shortcut: *.class shift*
- `.class passivebuffs`
  - Toggles class passives (buffs only, other class effects remain active).
  - Shortcut: *.class passives*
- `.class iacknowledgethiswillgloballyremovethensyncallclassbuffsonplayersandwantthattohappen` 🔒
  - Globally syncs class buffs (removes all then applies from current class) for players if needed.
  - Shortcut: *.class iacknowledgethiswillgloballyremovethensyncallclassbuffsonplayersandwantthattohappen*
- `.class iacknowledgethiswillremoveallclassbuffsfromplayersandwantthattohappen` 🔒
  - Globally removes class buffs from players to then facilitate changing class buffs in config.
  - Shortcut: *.class iacknowledgethiswillremoveallclassbuffsfromplayersandwantthattohappen*

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
- `.familiar listboxes`
  - Shows the available familiar boxes.
  - Shortcut: *.fam boxes*
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
- `.familiar add [PlayerName] [PrefabGUID/CHAR_Unit_Name]` 🔒
  - Unit testing.
  - Shortcut: *.fam a [PlayerName] [PrefabGUID/CHAR_Unit_Name]*
- `.familiar echoes [VBloodName]`
  - VBlood purchasing for exo reward with quantity scaling to unit tier.
  - Shortcut: *.fam echoes [VBloodName]*
- `.familiar remove [#]`
  - Removes familiar from current set permanently.
  - Shortcut: *.fam r [#]*
- `.familiar getlevel`
  - Display current familiar leveling progress.
  - Shortcut: *.fam gl*
- `.familiar setlevel [Player] [Level]` 🔒
  - Set current familiar level.
  - Shortcut: *.fam sl [Player] [Level]*
- `.familiar listprestigestats`
  - Display options for familiar prestige stats.
  - Shortcut: *.fam lst*
- `.familiar prestige [Stat]`
  - Prestiges familiar if at max, raising base stats by configured multiplier and adding an extra chosen stat.
  - Shortcut: *.fam pr [Stat]*
- `.familiar reset`
  - Resets (destroys) entities found in followerbuffer and clears familiar actives data.
  - Shortcut: *.fam reset*
- `.familiar search [Name]`
  - Searches boxes for familiar(s) with matching name.
  - Shortcut: *.fam s [Name]*
- `.familiar smartbind [Name]`
  - Searches and binds a familiar. If multiple matches are found, returns a list for clarification.
  - Shortcut: *.fam sb [Name]*
- `.familiar shinybuff [SpellSchool]`
  - Chooses shiny for current active familiar, one freebie then costs configured amount to change if already unlocked.
  - Shortcut: *.fam shiny [SpellSchool]*
- `.familiar toggleoption [Setting]`
  - Toggles various familiar settings.
  - Shortcut: *.fam option [Setting]*
- `.familiar battlegroup.bg [1/2/3]`
  - Set active familiar to battle group slot or list group if no slot entered.
  - Shortcut: *.bg [1/2/3]*
- `.familiar challenge [PlayerName/cancel]`
  - Challenges player if found, use cancel to exit queue after entering if needed.
  - Shortcut: *.fam challenge [PlayerName/cancel]*
- `.familiar setbattlearena` 🔒
  - Set current position as the center for the familiar battle arena.
  - Shortcut: *.fam sba*

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

### Miscellaneous Commands
- `.miscellaneous reminders`
  - Toggles general reminders for various mod features.
  - Shortcut: *.misc remindme*
- `.miscellaneous sct [Type]`
  - Toggles various scrolling text elements.
  - Shortcut: *.misc sct [Type]*
- `.miscellaneous starterkit`
  - Provides starting kit.
  - Shortcut: *.misc kitme*
- `.miscellaneous prepareforthehunt`
  - Completes GettingReadyForTheHunt if not already completed.
  - Shortcut: *.misc prepare*
- `.miscellaneous userstats`
  - Shows neat information about the player.
  - Shortcut: *.misc userstats*
- `.miscellaneous silence`
  - Resets stuck combat music if needed.
  - Shortcut: *.misc silence*

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
- `.party reset`
  - Removes a player from all parties they are in and disbands any party they own.
  - Shortcut: *.party r*

### Prestige Commands
- `.prestige self [PrestigeType]`
  - Handles player prestiging.
  - Shortcut: *.prestige me [PrestigeType]*
- `.prestige get [PrestigeType]`
  - Shows information about player's prestige status.
  - Shortcut: *.prestige get [PrestigeType]*
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
- `.prestige list`
  - Lists prestiges available.
  - Shortcut: *.prestige l*
- `.prestige leaderboard [PrestigeType]`
  - Lists prestige leaderboard for type.
  - Shortcut: *.prestige lb [PrestigeType]*
- `.prestige exoform`
  - Toggles taunting to enter exo form.
  - Shortcut: *.prestige exoform*
- `.prestige permashroud`
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
- `.quest complete [Name] [QuestType]` 🔒
  - Forcibly completes a specified quest for a player.
  - Shortcut: *.quest c [Name] [QuestType]*

### Weapon Commands
- `.weapon get`
  - Displays current weapon expertise details.
  - Shortcut: *.wep get*
- `.weapon log`
  - Toggles expertise logging.
  - Shortcut: *.wep log*
- `.weapon choosestat [WeaponOrStat] [WeaponStat]`
  - Choose a weapon stat to enhance based on your expertise.
  - Shortcut: *.wep cst [WeaponOrStat] [WeaponStat]*
- `.weapon resetstats`
  - Reset the stats for current weapon.
  - Shortcut: *.wep rst*
- `.weapon set [Name] [Weapon] [Level]` 🔒
  - Sets player weapon expertise level.
  - Shortcut: *.wep set [Name] [Weapon] [Level]*
- `.weapon liststats`
  - Lists weapon stats available.
  - Shortcut: *.wep lst*
- `.weapon list`
  - Lists weapon expertises available.
  - Shortcut: *.wep l*
- `.weapon setspells [Name] [Slot] [PrefabGuid] [Radius]` 🔒
  - Manually sets spells for testing (if you enter a radius it will apply to players around the entered name).
  - Shortcut: *.wep spell [Name] [Slot] [PrefabGuid] [Radius]*
- `.weapon lockspells`
  - Locks in the next spells equipped to use in your unarmed slots.
  - Shortcut: *.wep locksp*

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
- **Potion Stacking**: `PotionStacking` (bool, default: False)
  Enable or disable potion stacking (can have t01 effects and t02 effects at the same time. also requires professions enabled).
- **Bear Form Dash**: `BearFormDash` (bool, default: False)
  Enable or disable bear form dash.
- **Primal Jewel Cost**: `PrimalJewelCost` (int, default: -77477508)
  If extra recipes is enabled with a valid item prefab here (default demon fragments), it can be refined via gemcutter for random enhanced tier 4 jewels (better rolls, more modifiers).

### StarterKit
- **Starter Kit**: `StarterKit` (bool, default: False)
  Enable or disable the starter kit.
- **Kit Prefabs**: `KitPrefabs` (string, default: "862477668,-1531666018,-1593377811,1821405450")
  Item prefabGuids for starting kit.
- **Kit Quantities**: `KitQuantities` (string, default: "500,1000,1000,250")
  The quantity of each item in the starter kit.

### Quests
- **Quest System**: `QuestSystem` (bool, default: False)
  Enable or disable quests (kill, gather, and crafting).
- **Infinite Dailies**: `InfiniteDailies` (bool, default: False)
  Enable or disable infinite dailies.
- **Quest Rewards**: `QuestRewards` (string, default: "28358550,576389135,-257494203")
  Item prefabs for quest reward pool.
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
- **Starting Level**: `StartingLevel` (int, default: 10)
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
  The multiplier for experience gained from unit spawners (vermin nests, tombs). Applies to familiar experience as well.
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
  Enable or disable the prestige system (requires leveling to be enabled as well).
- **Prestige Buffs**: `PrestigeBuffs` (string, default: "1504279833,475045773,1643157297,946705138,-1266262267,-773025435,-1043659405,-1583573438,-1869022798,-536284884")
  The PrefabGUID hashes for general prestige buffs, use 0 to skip otherwise buff applies at the prestige level.
- **Prestige Levels To Unlock Class Spells**: `PrestigeLevelsToUnlockClassSpells` (string, default: "0,1,2,3,4,5")
  The prestige levels at which class spells are unlocked. This should match the number of spells per class +1 to account for the default class spell. Can leave at 0 each if you want them unlocked from the start.
- **Max Leveling Prestiges**: `MaxLevelingPrestiges` (int, default: 10)
  The maximum number of prestiges a player can reach in leveling.
- **Leveling Prestige Reducer**: `LevelingPrestigeReducer` (float, default: 0.05)
  Flat factor by which experience is reduced per increment of prestige in leveling.
- **Prestige Rates Reducer**: `PrestigeRatesReducer` (float, default: 0.1)
  Flat factor by which rates are reduced in expertise/legacy per increment of prestige in expertise/legacy.
- **Prestige Stat Multiplier**: `PrestigeStatMultiplier` (float, default: 0.1)
  Flat factor by which stats are increased in expertise/legacy bonuses per increment of prestige in expertise/legacy.
- **Prestige Rate Multiplier**: `PrestigeRateMultiplier` (float, default: 0.1)
  Flat factor by which rates are increased in expertise/legacy per increment of prestige in leveling.
- **Exo Prestiging**: `ExoPrestiging` (bool, default: False)
  Enable or disable exo prestiges (need to max normal prestiges first, 100 exo prestiges currently available).
- **Exo Prestige Reward**: `ExoPrestigeReward` (int, default: 28358550)
  The reward for exo prestiging (tier 3 nether shards by default).
- **Exo Prestige Reward Quantity**: `ExoPrestigeRewardQuantity` (int, default: 500)
  The quantity of the reward for exo prestiging.
- **True Immortal**: `TrueImmortal` (bool, default: False)
  Enable or disable Immortal blood for the duration of exoform.

### Expertise
- **Expertise System**: `ExpertiseSystem` (bool, default: False)
  Enable or disable the expertise system.
- **Max Expertise Prestiges**: `MaxExpertisePrestiges` (int, default: 10)
  The maximum number of prestiges a player can reach in expertise.
- **Unarmed Slots**: `UnarmedSlots` (bool, default: False)
  Enable or disable the ability to use extra unarmed spell slots.
- **Shift Slot**: `ShiftSlot` (bool, default: False)
  Enable or disable using class spell on shift.
- **Max Expertise Level**: `MaxExpertiseLevel` (int, default: 100)
  The maximum level a player can reach in weapon expertise.
- **Unit Expertise Multiplier**: `UnitExpertiseMultiplier` (float, default: 2)
  The multiplier for expertise gained from units.
- **V Blood Expertise Multiplier**: `VBloodExpertiseMultiplier` (float, default: 5)
  The multiplier for expertise gained from VBloods.
- **Unit Spawner Expertise Factor**: `UnitSpawnerExpertiseFactor` (float, default: 1)
  The multiplier for experience gained from unit spawners (vermin nests, tombs).
- **Expertise Stat Choices**: `ExpertiseStatChoices` (int, default: 2)
  The maximum number of stat choices a player can pick for a weapon expertise. Max of 3 will be sent to client UI for display.
- **Reset Expertise Item**: `ResetExpertiseItem` (int, default: 576389135)
  Item PrefabGUID cost for resetting weapon stats.
- **Reset Expertise Item Quantity**: `ResetExpertiseItemQuantity` (int, default: 500)
  Quantity of item required for resetting stats.
- **Max Health**: `MaxHealth` (float, default: 250)
  The base cap for maximum health.
- **Movement Speed**: `MovementSpeed` (float, default: 0.25)
  The base cap for movement speed.
- **Primary Attack Speed**: `PrimaryAttackSpeed` (float, default: 0.1)
  The base cap for primary attack speed.
- **Physical Life Leech**: `PhysicalLifeLeech` (float, default: 0.1)
  The base cap for physical life leech.
- **Spell Life Leech**: `SpellLifeLeech` (float, default: 0.1)
  The base cap for spell life leech.
- **Primary Life Leech**: `PrimaryLifeLeech` (float, default: 0.15)
  The base cap for primary life leech.
- **Physical Power**: `PhysicalPower` (float, default: 20)
  The base cap for physical power.
- **Spell Power**: `SpellPower` (float, default: 10)
  The base cap for spell power.
- **Physical Crit Chance**: `PhysicalCritChance` (float, default: 0.1)
  The base cap for physical critical strike chance.
- **Physical Crit Damage**: `PhysicalCritDamage` (float, default: 0.5)
  The base cap for physical critical strike damage.
- **Spell Crit Chance**: `SpellCritChance` (float, default: 0.1)
  The base cap for spell critical strike chance.
- **Spell Crit Damage**: `SpellCritDamage` (float, default: 0.5)
  The base cap for spell critical strike damage.

### Legacies
- **Blood System**: `BloodSystem` (bool, default: False)
  Enable or disable the blood legacy system.
- **Max Legacy Prestiges**: `MaxLegacyPrestiges` (int, default: 10)
  The maximum number of prestiges a player can reach in blood legacies.
- **Max Blood Level**: `MaxBloodLevel` (int, default: 100)
  The maximum level a player can reach in blood legacies.
- **Unit Legacy Multiplier**: `UnitLegacyMultiplier` (float, default: 1)
  The multiplier for lineage gained from units.
- **V Blood Legacy Multiplier**: `VBloodLegacyMultiplier` (float, default: 5)
  The multiplier for lineage gained from VBloods.
- **Legacy Stat Choices**: `LegacyStatChoices` (int, default: 2)
  The maximum number of stat choices a player can pick for a blood legacy. Max of 3 will be sent to client UI for display.
- **Reset Legacy Item**: `ResetLegacyItem` (int, default: 576389135)
  Item PrefabGUID cost for resetting blood stats.
- **Reset Legacy Item Quantity**: `ResetLegacyItemQuantity` (int, default: 500)
  Quantity of item required for resetting blood stats.
- **Healing Received**: `HealingReceived` (float, default: 0.15)
  The base cap for healing received.
- **Damage Reduction**: `DamageReduction` (float, default: 0.05)
  The base cap for damage reduction.
- **Physical Resistance**: `PhysicalResistance` (float, default: 0.1)
  The base cap for physical resistance.
- **Spell Resistance**: `SpellResistance` (float, default: 0.1)
  The base cap for spell resistance.
- **Resource Yield**: `ResourceYield` (float, default: 0.25)
  The base cap for resource yield.
- **Blood Drain**: `BloodDrain` (float, default: 0.5)
  The base cap for blood drain reduction.
- **Spell Cooldown Recovery Rate**: `SpellCooldownRecoveryRate` (float, default: 0.1)
  The base cap for spell cooldown recovery rate.
- **Weapon Cooldown Recovery Rate**: `WeaponCooldownRecoveryRate` (float, default: 0.1)
  The base cap for weapon cooldown recovery rate.
- **Ultimate Cooldown Recovery Rate**: `UltimateCooldownRecoveryRate` (float, default: 0.2)
  The base cap for ultimate cooldown recovery rate.
- **Minion Damage**: `MinionDamage` (float, default: 0.25)
  The base cap for minion damage.
- **Shield Absorb**: `ShieldAbsorb` (float, default: 0.5)
  The base cap for shield absorb.
- **Blood Efficiency**: `BloodEfficiency` (float, default: 0.1)
  The base cap for blood efficiency.

### Professions
- **Profession System**: `ProfessionSystem` (bool, default: False)
  Enable or disable the profession system.
- **Profession Multiplier**: `ProfessionMultiplier` (float, default: 10)
  The multiplier for profession experience gained.
- **Extra Recipes**: `ExtraRecipes` (bool, default: False)
  Enable or disable extra recipes. Players will not be able to add/change shiny buffs for familiars without this unless other means of obtaining vampiric dust are provided, salvage additions are controlled by this setting as well.

### Familiars
- **Familiar System**: `FamiliarSystem` (bool, default: False)
  Enable or disable the familiar system.
- **Share Unlocks**: `ShareUnlocks` (bool, default: False)
  Enable or disable sharing unlocks between players in clans or parties (uses exp share distance).
- **Familiar Combat**: `FamiliarCombat` (bool, default: True)
  Enable or disable combat for familiars.
- **Familiar Pv P**: `FamiliarPvP` (bool, default: True)
  Enable or disable PvP participation for familiars. (if set to false, familiars will be unbound when entering PvP combat).
- **Familiar Battles**: `FamiliarBattles` (bool, default: False)
  Enable or disable familiar battle system.
- **Familiar Prestige**: `FamiliarPrestige` (bool, default: False)
  Enable or disable the prestige system for familiars.
- **Max Familiar Prestiges**: `MaxFamiliarPrestiges` (int, default: 10)
  The maximum number of prestiges a familiar can reach.
- **Familiar Prestige Stat Multiplier**: `FamiliarPrestigeStatMultiplier` (float, default: 0.1)
  The multiplier for applicable stats gained per familiar prestige.
- **Max Familiar Level**: `MaxFamiliarLevel` (int, default: 90)
  The maximum level a familiar can reach.
- **Allow V Bloods**: `AllowVBloods` (bool, default: False)
  Allow VBloods to be unlocked as familiars (this includes shardbearers, if you want those excluded use the bannedUnits list).
- **Allow Minions**: `AllowMinions` (bool, default: False)
  Allow Minions to be unlocked as familiars (leaving these excluded by default since some have undesirable behaviour and I am not sifting through them all to correct that, enable at own risk).
- **Banned Units**: `BannedUnits` (string, default: "")
  The PrefabGUID hashes for units that cannot be used as familiars. Same structure as the buff lists except unit prefabs.
- **Banned Types**: `BannedTypes` (string, default: "")
  The types of units that cannot be used as familiars go here (Human, Undead, Demon, Mechanical, Beast).
- **V Blood Damage Multiplier**: `VBloodDamageMultiplier` (float, default: 1)
  Leave at 1 for no change (controls damage familiars do to VBloods).
- **Unit Familiar Multiplier**: `UnitFamiliarMultiplier` (float, default: 7.5)
  The multiplier for experience gained from units.
- **V Blood Familiar Multiplier**: `VBloodFamiliarMultiplier` (float, default: 15)
  The multiplier for experience gained from VBloods.
- **Unit Unlock Chance**: `UnitUnlockChance` (float, default: 0.05)
  The chance for a unit unlock as a familiar.
- **V Blood Unlock Chance**: `VBloodUnlockChance` (float, default: 0.01)
  The chance for a VBlood unlock as a familiar.
- **Primal Echoes**: `PrimalEchoes` (bool, default: False)
  Enable or disable acquiring vBloods with configured item reward from exo prestiging (default primal shards) at cost scaling to unit tier using exo reward quantity as the base (highest tier are shard bearers which cost exo reward quantity times 25, or in other words after 25 exo prestiges a player would be able to purchase a shard bearer). Must enable exo prestiging (and therefore normal prestiging), checks for banned vBloods before allowing if applicable.
- **Echoes Factor**: `EchoesFactor` (int, default: 1)
  Increase to multiply costs for vBlood purchases. Valid values are integers between 1-4, if outside that range in either direction it will be clamped.
- **Shiny Chance**: `ShinyChance` (float, default: 0.2)
  The chance for a shiny when unlocking familiars (6 total, 1 per familiar). Guaranteed on second unlock of same unit, chance on damage dealt (same as configured onHitEffect chance) to apply spell school debuff.
- **Shiny Cost Item Quantity**: `ShinyCostItemQuantity` (int, default: 250)
  Quantity of vampiric dust required to make a familiar shiny. May also be spent to change shiny familiar's shiny buff at 25% cost. Enable ExtraRecipes to allow player refinement of this item from Advanced Grinders. Valid values are between 100-400, if outside that range in either direction it will be clamped.
- **Prestige Cost Item Quantity**: `PrestigeCostItemQuantity` (int, default: 1000)
  Quantity of schematics required to immediately prestige familiar (gain total levels equal to max familiar level, extra levels remaining from the amount needed to prestige will be added to familiar after prestiging). Valid values are between 500-2000, if outside that range in either direction it will be clamped.

### Classes
- **Soft Synergies**: `SoftSynergies` (bool, default: False)
  Allow class synergies (turns on classes and does not restrict stat choices, do not use this and hard syergies at the same time).
- **Hard Synergies**: `HardSynergies` (bool, default: False)
  Enforce class synergies (turns on classes and restricts stat choices, do not use this and soft syergies at the same time).
- **Change Class Item**: `ChangeClassItem` (int, default: 576389135)
  Item PrefabGUID cost for changing class.
- **Change Class Quantity**: `ChangeClassQuantity` (int, default: 750)
  Quantity of item required for changing class.
- **Class Spell School On Hit Effects**: `ClassSpellSchoolOnHitEffects` (bool, default: False)
  Enable or disable class spell school on hit effects (respective debuff from spell school, leech chill condemn etc).
- **On Hit Proc Chance**: `OnHitProcChance` (float, default: 0.075)
  The chance for a class effect to proc on hit.
- **Stat Synergy Multiplier**: `StatSynergyMultiplier` (float, default: 1.5)
  Multiplier for class stat synergies to base stat cap.
- **Blood Knight Weapon**: `BloodKnightWeapon` (string, default: "0,3,5,6")
  Blood Knight weapon synergies.
- **Blood Knight Blood**: `BloodKnightBlood` (string, default: "1,5,7,10")
  Blood Knight blood synergies.
- **Demon Hunter Weapon**: `DemonHunterWeapon` (string, default: "1,2,8,9")
  Demon Hunter weapon synergies.
- **Demon Hunter Blood**: `DemonHunterBlood` (string, default: "2,5,7,9")
  Demon Hunter blood synergies
- **Vampire Lord Weapon**: `VampireLordWeapon` (string, default: "0,4,6,7")
  Vampire Lord weapon synergies.
- **Vampire Lord Blood**: `VampireLordBlood` (string, default: "1,3,8,11")
  Vampire Lord blood synergies.
- **Shadow Blade Weapon**: `ShadowBladeWeapon` (string, default: "1,2,6,9")
  Shadow Blade weapon synergies.
- **Shadow Blade Blood**: `ShadowBladeBlood` (string, default: "3,5,7,10")
  Shadow Blade blood synergies.
- **Arcane Sorcerer Weapon**: `ArcaneSorcererWeapon` (string, default: "4,7,10,11")
  Arcane Sorcerer weapon synergies.
- **Arcane Sorcerer Blood**: `ArcaneSorcererBlood` (string, default: "0,6,8,10")
  Arcane Sorcerer blood synergies.
- **Death Mage Weapon**: `DeathMageWeapon` (string, default: "0,4,7,11")
  Death Mage weapon synergies.
- **Death Mage Blood**: `DeathMageBlood` (string, default: "2,3,6,9")
  Death Mage blood synergies.
- **Default Class Spell**: `DefaultClassSpell` (int, default: -433204738)
  Default spell (veil of shadow) available to all classes.
- **Blood Knight Buffs**: `BloodKnightBuffs` (string, default: "1828387635,-534491790,-1055766373,-584203677")
  The PrefabGUID hashes for blood knight leveling blood buffs. Granted every MaxLevel/(# of blood buffs).
- **Blood Knight Spells**: `BloodKnightSpells` (string, default: "-880131926,651613264,2067760264,189403977,375131842")
  Blood Knight shift spells, granted at levels of prestige.
- **Demon Hunter Buffs**: `DemonHunterBuffs` (string, default: "-154702686,-285745649,-1510965956,-397097531")
  The PrefabGUID hashes for demon hunter leveling blood buffs.
- **Demon Hunter Spells**: `DemonHunterSpells` (string, default: "-356990326,-987810170,1071205195,1249925269,-914344112")
  Demon Hunter shift spells, granted at levels of prestige.
- **Vampire Lord Buffs**: `VampireLordBuffs` (string, default: "1558171501,997154800,-1413561088,1103099361")
  The PrefabGUID hashes for vampire lord leveling blood buffs.
- **Vampire Lord Spells**: `VampireLordSpells` (string, default: "78384915,295045820,-1000260252,91249849,1966330719")
  Vampire Lord shift spells, granted at levels of prestige.
- **Shadow Blade Buffs**: `ShadowBladeBuffs` (string, default: "894725875,-1596803256,-993492354,210193036")
  The PrefabGUID hashes for shadow blade leveling blood buffs.
- **Shadow Blade Spells**: `ShadowBladeSpells` (string, default: "1019568127,1575317901,1112116762,-358319417,1174831223")
  Shadow Blade shift spells, granted at levels of prestige.
- **Arcane Sorcerer Buffs**: `ArcaneSorcererBuffs` (string, default: "1614027598,884683323,-1576592687,-1859298707")
  The PrefabGUID hashes for arcane leveling blood buffs.
- **Arcane Sorcerer Spells**: `ArcaneSorcererSpells` (string, default: "247896794,268059675,-242769430,-2053450457,1650878435")
  Arcane Sorcerer shift spells, granted at levels of prestige.
- **Death Mage Buffs**: `DeathMageBuffs` (string, default: "-901503997,-804597757,1934870645,1201299233")
  The PrefabGUID hashes for death mage leveling blood buffs.
- **Death Mage Spells**: `DeathMageSpells` (string, default: "-1204819086,481411985,1961570821,2138402840,-1781779733")
  Death Mage shift spells, granted at levels of prestige.

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

## Credits
Do my best to mention/attribute where ideas and bug reports come from in the changelog and commit history, if I've missed something or someone on a specific change please let me know!
