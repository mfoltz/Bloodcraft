## Table of Contents

- [Eclipse](https://new.thunderstore.io/c/v-rising/p/zfolmt/Eclipse/) <--- **RECOMMENDED**
- [Sponsors](#sponsors)
- [Features](#features)
- [Commands](#commands)
- [Recipes](#recipes)
- [BleedingEdge](#bleedingedge)
- [Configuration](#configuration)
- [Recommended Mods](#recommended-mods)
- [Development Setup](#development-setup)
- [Codex Workflow](#codex-workflow)
- [Workflow Source](#workflow-source)
- [Credits](#credits)

## Sponsor this project

[![patreon](https://i.imgur.com/u6aAqeL.png)](https://www.patreon.com/join/4865914)  [![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/zfolmt)

## Sponsors

Jairon O.; Odjit; Jera; Kokuren TCG and Gaming Shop; Rexxn; Eduardo G.; DirtyMike; Imperivm Draconis; Geoffrey D.; SirSaia; Robin C.; Colin F.; Jade K.; Jorge L.; Adrian L.;

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
  - `.wep rst` (reset stat selection for current weapon; first reset per weapon is free)
- Stat bonus values scale with player weapon expertise level per weapon.
  - classes have synergies with different weapon stats (see Classes)

### Blood Legacies
- Gain levels in blood types when feeding on (executes and completions) enemies.
  - `.bl log` (toggle gains logging in chat) 
  - `.bl get` (view blood progression/stat details)
- Pick stats to enhance per blood when using.
  - `.bl lst` (view blood bonus stat options)
  - `.bl cst` [Blood] [BloodStat] (select bonus stat for entered weapon)
  - `.bl rst` (reset stat selection for current blood; first reset per blood is free)
- Stat bonus values scale with player blood legacy level per blood.
  - classes have synergies with different blood stats (see Classes)

### Classes
- Select a class for free when starting and change later for a cost.
  - `.class l` (list available classes)
  - `.class c [Class]` (select a class)
  - `.class change [Class]` (change class, costs configured items)
- Classes provide:
  - Weapon & Blood Stat Synergies which increase the effectiveness for specific stats from expertise and legacies
  - On-Hit Effect debuffs applied at configured chance when the player deals damage (ignite, weaken, chill, etc. will apply secondary effect that either buffs you or debuffs enemy further if first effect is already present on target)
  - Extra Spells from the themed spell school of the class, one of which unlocks every leveling prestige by default; these can be used on Shift key

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
- Resets player level, grants a permanent buff (REWORKING), reduces experience gained from units, and increases rate gains for expertise/legacies per prestige.
  - `.prestige me [Type]` (experience and/or various weapons and bloods, the latter variety will see increased bonus stat caps and reduced rate gains per prestige)
  - `.prestige get [Type]` (view prestige progress and details)

## Commands

### Bloodlegacy Commands
- `.bloodlegacy choosestat [BloodOrStat] [BloodStat]`
  - Choose a bonus stat to enhance for your blood legacy.
  - Shortcut: *.bl cst [BloodOrStat] [BloodStat]*
- `.bloodlegacy get [BloodType]`
  - Display current blood legacy details.
  - Shortcut: *.bl get [BloodType]*
- `.bloodlegacy list`
  - Lists blood legacies available.
  - Shortcut: *.bl l*
- `.bloodlegacy liststats`
  - Lists blood stats available.
  - Shortcut: *.bl lst*
- `.bloodlegacy log`
  - Toggles Legacy progress logging.
  - Shortcut: *.bl log*
- `.bloodlegacy resetstats`
  - Reset stats for current blood.
  - Shortcut: *.bl rst*
- `.bloodlegacy set [Player] [Blood] [Level]` 🔒
  - Sets player blood legacy level.
  - Shortcut: *.bl set [Player] [Blood] [Level]*

### Class Commands
- `.class change [Class]`
  - Change classes.
  - Shortcut: *.class c [Class]*
- `.class choosespell [#]`
  - Sets shift spell for class if prestige level is high enough.
  - Shortcut: *.class csp [#]*
- `.class list`
  - List available classes.
  - Shortcut: *.class l*
- `.class listspells [Class]`
  - Shows spells that can be gained from class.
  - Shortcut: *.class lsp [Class]*
- `.class liststats [Class]`
  - List weapon and blood stat synergies for a class.
  - Shortcut: *.class lst [Class]*
- `.class lockshift`
  - Toggle shift spell.
  - Shortcut: *.class shift*
- `.class select [Class]`
  - Select class.
  - Shortcut: *.class s [Class]*

### Familiar Commands
- `.familiar add [PlayerName] [PrefabGuid/CHAR_Unit_Name]` 🔒
  - Unit testing.
  - Shortcut: *.fam a [PlayerName] [PrefabGuid/CHAR_Unit_Name]*
- `.familiar addbattlegroup [BattleGroup]`
  - Creates new battle group.
  - Shortcut: *.fam abg [BattleGroup]*
- `.familiar addbox [BoxName]`
  - Adds empty box with name.
  - Shortcut: *.fam ab [BoxName]*
- `.familiar bind [#]`
  - Activates specified familiar from current list.
  - Shortcut: *.fam b [#]*
- `.familiar challenge [PlayerName]`
  - Challenges a player to battle or displays queue details.
  - Shortcut: *.fam challenge [PlayerName]*
- `.familiar choosebattlegroup [BattleGroup]`
  - Sets active battle group.
  - Shortcut: *.fam cbg [BattleGroup]*
- `.familiar choosebox [Name]`
  - Choose active box of familiars.
  - Shortcut: *.fam cb [Name]*
- `.familiar deletebattlegroup [BattleGroup]`
  - Deletes a battle group.
  - Shortcut: *.fam dbg [BattleGroup]*
- `.familiar deletebox [BoxName]`
  - Deletes specified box if empty.
  - Shortcut: *.fam db [BoxName]*
- `.familiar echoes [VBloodName]`
  - VBlood purchasing for exo reward with quantity scaling to unit tier.
  - Shortcut: *.fam echoes [VBloodName]*
- `.familiar emoteactions`
  - Shows available emote actions.
  - Shortcut: *.fam actions*
- `.familiar emotes`
  - Toggle emote actions.
  - Shortcut: *.fam e*
- `.familiar getlevel`
  - Display current familiar leveling progress.
  - Shortcut: *.fam gl*
- `.familiar list`
  - Lists unlocked familiars from current box.
  - Shortcut: *.fam l*
- `.familiar listbattlegroup [BattleGroup]`
  - Displays details of the specified battle group, or the active one if none is given.
  - Shortcut: *.fam bg [BattleGroup]*
- `.familiar listbattlegroups`
  - Lists available battle groups.
  - Shortcut: *.fam bgs*
- `.familiar listboxes`
  - Shows the available familiar boxes.
  - Shortcut: *.fam boxes*
- `.familiar movebox [BoxName]`
  - Moves active familiar to specified box.
  - Shortcut: *.fam mb [BoxName]*
- `.familiar prestige`
  - Prestiges familiar if conditions are met, raising base stats by configured multiplier.
  - Shortcut: *.fam pr*
- `.familiar remove [#]`
  - Removes familiar from current set permanently.
  - Shortcut: *.fam r [#]*
- `.familiar renamebox [CurrentName] [NewName]`
  - Renames a box.
  - Shortcut: *.fam rb [CurrentName] [NewName]*
- `.familiar reset`
  - Resets (destroys) entities found in followerbuffer and clears familiar actives data.
  - Shortcut: *.fam reset*
- `.familiar search [Name]`
  - Searches boxes for familiar(s) with matching name.
  - Shortcut: *.fam s [Name]*
- `.familiar setbattlearena` 🔒
  - Set current position as the center for the familiar battle arena.
  - Shortcut: *.fam sba*
- `.familiar setlevel [Player] [Level]` 🔒
  - Set current familiar level.
  - Shortcut: *.fam sl [Player] [Level]*
- `.familiar shinybuff [SpellSchool]`
  - Chooses shiny for current active familiar, one freebie then costs configured amount to change if already unlocked.
  - Shortcut: *.fam shiny [SpellSchool]*
- `.familiar slotbattlegroup [BattleGroupOrSlot] [Slot]`
  - Assigns active familiar to a battle group slot. If no battle group is specified, assigns to active group.
  - Shortcut: *.fam sbg [BattleGroupOrSlot] [Slot]*
- `.familiar smartbind [Name]`
  - Searches and binds a familiar. If multiple matches are found, returns a list for clarification.
  - Shortcut: *.fam sb [Name]*
- `.familiar toggle`
  - Calls or dismisses familar.
  - Shortcut: *.fam t*
- `.familiar togglecombat`
  - Enables or disables combat for familiar.
  - Shortcut: *.fam c*
- `.familiar toggleoption [Setting]`
  - Toggles various familiar settings.
  - Shortcut: *.fam option [Setting]*
- `.familiar unbind`
  - Destroys active familiar.
  - Shortcut: *.fam ub*

### Level Commands
- `.level get`
  - Display current leveling progress.
  - Shortcut: *.lvl get*
- `.level ignoresharedexperience [Player]` 🔒
  - Adds (or removes) player to list of those who are not eligible to receive shared experience.
  - Shortcut: *.lvl ignore [Player]*
- `.level log`
  - Toggles leveling progress logging.
  - Shortcut: *.lvl log*
- `.level set [Player] [Level]` 🔒
  - Sets player level.
  - Shortcut: *.lvl set [Player] [Level]*

### Miscellaneous Commands
- `.miscellaneous prepareforthehunt`
  - Completes GettingReadyForTheHunt if not already completed.
  - Shortcut: *.misc prepare*
- `.miscellaneous reminders`
  - Toggles general reminders for various mod features.
  - Shortcut: *.misc remindme*
- `.miscellaneous sct [Type]`
  - Toggles various scrolling text elements.
  - Shortcut: *.misc sct [Type]*
- `.miscellaneous silence`
  - Resets stuck combat music if needed.
  - Shortcut: *.misc silence*
- `.miscellaneous starterkit`
  - Provides starting kit.
  - Shortcut: *.misc kitme*
- `.miscellaneous userstats`
  - Shows neat information about the player.
  - Shortcut: *.misc userstats*

### Prestige Commands
- `.prestige exoform`
  - Toggles taunting to enter exoform.
  - Shortcut: *.prestige exoform*
- `.prestige get [PrestigeType]`
  - Shows information about player's prestige status.
  - Shortcut: *.prestige get [PrestigeType]*
- `.prestige iacknowledgethiswillremoveallprestigebuffsfromplayersandwantthattohappen` 🔒
  - Globally removes prestige buffs from players to facilitate changing prestige buffs in config.
  - Shortcut: *.prestige iacknowledgethiswillremoveallprestigebuffsfromplayersandwantthattohappen*
- `.prestige ignoreleaderboard [Player]` 🔒
  - Adds (or removes) player to list of those who will not appear on prestige leaderboards. Intended for admin-duties only accounts.
  - Shortcut: *.prestige ignore [Player]*
- `.prestige leaderboard [PrestigeType]`
  - Lists prestige leaderboard for type.
  - Shortcut: *.prestige lb [PrestigeType]*
- `.prestige list`
  - Lists prestiges available.
  - Shortcut: *.prestige l*
- `.prestige permashroud`
  - Toggles permashroud if applicable.
  - Shortcut: *.prestige shroud*
- `.prestige reset [Player] [PrestigeType]` 🔒
  - Handles resetting prestiging.
  - Shortcut: *.prestige r [Player] [PrestigeType]*
- `.prestige selectform [EvolvedVampire|CorruptedSerpent]`
  - Select active exoform shapeshift.
  - Shortcut: *.prestige sf [EvolvedVampire|CorruptedSerpent]*
- `.prestige self [PrestigeType]`
  - Handles player prestiging.
  - Shortcut: *.prestige me [PrestigeType]*
- `.prestige set [Player] [PrestigeType] [Level]` 🔒
  - Sets the specified player to a certain level of prestige in a certain type of prestige.
  - Shortcut: *.prestige set [Player] [PrestigeType] [Level]*
- `.prestige syncbuffs`
  - Applies prestige buffs appropriately if not present.
  - Shortcut: *.prestige sb*

### Profession Commands
- `.profession get [Profession]`
  - Display your current profession progress.
  - Shortcut: *.prof get [Profession]*
- `.profession list`
  - Lists professions available.
  - Shortcut: *.prof l*
- `.profession log`
  - Toggles profession progress logging.
  - Shortcut: *.prof log*
- `.profession set [Name] [Profession] [Level]` 🔒
  - Sets player profession level.
  - Shortcut: *.prof set [Name] [Profession] [Level]*

### Quest Commands
- `.quest complete [Name] [QuestType]` 🔒
  - Forcibly completes a specified quest for a player.
  - Shortcut: *.quest c [Name] [QuestType]*
- `.quest log`
  - Toggles quest progress logging.
  - Shortcut: *.quest log*
- `.quest progress [QuestType]`
  - Display your current quest progress.
  - Shortcut: *.quest p [QuestType]*
- `.quest refresh [Name]` 🔒
  - Refreshes daily and weekly quests for player.
  - Shortcut: *.quest rf [Name]*
- `.quest reroll [QuestType]`
  - Reroll quest for cost (daily only currently).
  - Shortcut: *.quest r [QuestType]*
- `.quest track [QuestType]`
  - Locate and track quest target.
  - Shortcut: *.quest t [QuestType]*

### Weapon Commands
- `.weapon choosestat [WeaponOrStat] [WeaponStat]`
  - Choose a weapon stat to enhance based on your expertise.
  - Shortcut: *.wep cst [WeaponOrStat] [WeaponStat]*
- `.weapon get`
  - Displays current weapon expertise details.
  - Shortcut: *.wep get*
- `.weapon list`
  - Lists weapon expertises available.
  - Shortcut: *.wep l*
- `.weapon liststats`
  - Lists weapon stats available.
  - Shortcut: *.wep lst*
- `.weapon lockspells`
  - Locks in the next spells equipped to use in your unarmed slots.
  - Shortcut: *.wep locksp*
- `.weapon log`
  - Toggles expertise logging.
  - Shortcut: *.wep log*
- `.weapon resetstats`
  - Reset the stats for current weapon.
  - Shortcut: *.wep rst*
- `.weapon set [Name] [Weapon] [Level]` 🔒
  - Sets player weapon expertise level.
  - Shortcut: *.wep set [Name] [Weapon] [Level]*
- `.weapon setspells [Name] [Slot] [PrefabGuid] [Radius]` 🔒
  - Manually sets spells for testing (if you enter a radius it will apply to players around the entered name).
  - Shortcut: *.wep spell [Name] [Slot] [PrefabGuid] [Radius]*


## Configuration

### General
- **Language Localization**: `LanguageLocalization` (string, default: "English")
  The language localization for prefabs displayed to users. English by default. Options: Brazilian, English, French, German, Hungarian, Italian, Japanese, Koreana, Latam, Polish, Russian, SimplifiedChinese, Spanish, TraditionalChinese, Thai, Turkish, Vietnamese
- **Plugin Language**: `PluginLanguage` (string, default: "English")
  Language for plugin messages.
- **Eclipse**: `Eclipse` (bool, default: True)
  Enables the server sending player mod data to clients using Eclipse.
- **Elite Shard Bearers**: `EliteShardBearers` (bool, default: False)
  Enable or disable elite shard bearers.
- **Shard Bearer Level**: `ShardBearerLevel` (int, default: 0)
  Sets level of shard bearers if elite shard bearers is enabled. Leave at 0 for no effect.
- **Potion Stacking**: `PotionStacking` (bool, default: False)
  Enable or disable potion stacking (can have t01/t02 effects at the same time).
- **Bear Form Dash**: `BearFormDash` (bool, default: False)
  Enable or disable bear form dash.
- **Bleeding Edge**: `BleedingEdge` (string, default: "")
  Enable various weapon-specific changes; some are more experimental than others, see README for details. (Slashers, Crossbow, Pistols, TwinBlades, Daggers)
- **Twilight Arsenal**: `TwilightArsenal` (bool, default: False)
  Enable or disable experimental ability replacements on shadow weapons (currently just axes but like cosplaying as Thor with two mjolnirs).
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
- **Daily Perfect Chance**: `DailyPerfectChance` (float, default: 0.1)
  Chance to receive a random perfect gem (can be used to control spell school for primal jewels in gemcutter) when completing daily quests.
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
- **Exp Share**: `ExpShare` (bool, default: True)
  Enable or disable sharing experience with nearby players (ExpShareDistance) in combat that are within level range (ExpShareLevelRange, this does not apply to players that have prestiged at least once on PvE servers or clan members of the player that does the final blow) along with familiar unlock sharing if enabled (on PvP servers will only apply to clan members).
- **Exp Share Level Range**: `ExpShareLevelRange` (int, default: 10)
  Maximum level difference between players allowed for ExpShare, players who have prestiged at least once are exempt from this. Use 0 for no level diff restrictions.
- **Exp Share Distance**: `ExpShareDistance` (float, default: 25)
  Default is ~5 floor tile lengths.

### Prestige
- **Prestige System**: `PrestigeSystem` (bool, default: False)
  Enable or disable the prestige system (requires leveling to be enabled as well).
- **Prestige Buffs**: `PrestigeBuffs` (string, default: "1504279833,0,0,0,0,0,0,0,0,0")
  The PrefabGUID hashes for general prestige buffs, use 0 to skip otherwise buff applies at the prestige level (only shroud for first default while reworked).
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
- **Leaderboard**: `Leaderboard` (bool, default: True)
  Enable or disable the various prestige leaderboard rankings.

### Expertise
- **Expertise System**: `ExpertiseSystem` (bool, default: False)
  Enable or disable the expertise system.
- **Max Expertise Prestiges**: `MaxExpertisePrestiges` (int, default: 10)
  The maximum number of prestiges a player can reach in expertise.
- **Unarmed Slots**: `UnarmedSlots` (bool, default: False)
  Enable or disable the ability to use extra unarmed spell slots.
- **Duality**: `Duality` (bool, default: True)
  True for both unarmed slots, false for one unarmed slot. Does nothing without UnarmedSlots enabled.
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
- **Expertise Stat Choices**: `ExpertiseStatChoices` (int, default: 3)
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
- **Legacy System**: `LegacySystem` (bool, default: False)
  Enable or disable the blood legacy system.
- **Max Legacy Prestiges**: `MaxLegacyPrestiges` (int, default: 10)
  The maximum number of prestiges a player can reach in blood legacies.
- **Max Blood Level**: `MaxBloodLevel` (int, default: 100)
  The maximum level a player can reach in blood legacies.
- **Unit Legacy Multiplier**: `UnitLegacyMultiplier` (float, default: 1)
  The multiplier for lineage gained from units.
- **V Blood Legacy Multiplier**: `VBloodLegacyMultiplier` (float, default: 5)
  The multiplier for lineage gained from VBloods.
- **Legacy Stat Choices**: `LegacyStatChoices` (int, default: 3)
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
- **Reduced Blood Drain**: `ReducedBloodDrain` (float, default: 0.5)
  The base cap for reduced blood drain.
- **Spell Cooldown Recovery Rate**: `SpellCooldownRecoveryRate` (float, default: 0.1)
  The base cap for spell cooldown recovery rate.
- **Weapon Cooldown Recovery Rate**: `WeaponCooldownRecoveryRate` (float, default: 0.1)
  The base cap for weapon cooldown recovery rate.
- **Ultimate Cooldown Recovery Rate**: `UltimateCooldownRecoveryRate` (float, default: 0.2)
  The base cap for ultimate cooldown recovery rate.
- **Minion Damage**: `MinionDamage` (float, default: 0.25)
  The base cap for minion damage.
- **Ability Attack Speed**: `AbilityAttackSpeed` (float, default: 0.1)
  The base cap for ability attack speed.
- **Corruption Damage Reduction**: `CorruptionDamageReduction` (float, default: 0.1)
  The base cap for corruption damage reduction.

### Professions
- **Profession System**: `ProfessionSystem` (bool, default: False)
  Enable or disable the profession system.
- **Profession Factor**: `ProfessionFactor` (float, default: 1)
  The multiplier for profession experience.
- **Extra Recipes**: `ExtraRecipes` (bool, default: False)
  Enable or disable extra recipes. Players will not be able to add/change shiny buffs for familiars without this unless other means of obtaining vampiric dust are provided, salvage additions are controlled by this setting as well. See 'Recipes' section in README for complete list of changes.

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
  Enable or disable familiar battle system (most likely not working atm after 1.1, use at own risk for now).
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
  Increase to multiply costs for vBlood purchases. Valid integers are between 1-4, if values are outside that range they will be clamped.
- **Shiny Chance**: `ShinyChance` (float, default: 0.2)
  The chance for a shiny when unlocking familiars (6 total buffs, 1 buff per familiar). Guaranteed on second unlock of same unit, chance on damage dealt (same as configured onHitEffect chance) to apply spell school debuff.
- **Shiny Cost Item Quantity**: `ShinyCostItemQuantity` (int, default: 100)
  Quantity of vampiric dust required to make a familiar shiny. May also be spent to change shiny familiar's shiny buff at 25% cost. Enable ExtraRecipes to allow player refinement of this item from Advanced Grinders. Valid values are between 50-200, if outside that range in either direction it will be clamped.
- **Prestige Cost Item Quantity**: `PrestigeCostItemQuantity` (int, default: 1000)
  Quantity of schematics required to immediately prestige familiar (gain total levels equal to max familiar level, extra levels remaining from the amount needed to prestige will be added to familiar after prestiging). Valid values are between 500-2000, if outside that range in either direction it will be clamped.

### Classes
- **Class System**: `ClassSystem` (bool, default: False)
  Enable classes without synergy restrictions.
- **Change Class Item**: `ChangeClassItem` (int, default: 576389135)
  Item PrefabGUID cost for changing class.
- **Change Class Quantity**: `ChangeClassQuantity` (int, default: 750)
  Quantity of item required for changing class.
- **Class On Hit Effects**: `ClassOnHitEffects` (bool, default: True)
  Enable or disable class spell school on hit effects (chance to proc respective debuff from spell school when dealing damage (leech, chill, condemn etc), second tier effect will proc if first is already present on target.
- **On Hit Proc Chance**: `OnHitProcChance` (float, default: 0.075)
  The chance for a class effect to proc on hit.
- **Synergy Multiplier**: `SynergyMultiplier` (float, default: 1.5)
  Multiplier for class stat synergies to base stat cap.
- **Blood Knight Weapon Synergies**: `BloodKnightWeaponSynergies` (string, default: "MaxHealth,PrimaryAttackSpeed,PrimaryLifeLeech,PhysicalPower")
  Blood Knight weapon synergies.
- **Blood Knight Blood Synergies**: `BloodKnightBloodSynergies` (string, default: "DamageReduction,ReducedBloodDrain,WeaponCooldownRecoveryRate,AbilityAttackSpeed")
  Blood Knight blood synergies.
- **Demon Hunter Weapon Synergies**: `DemonHunterWeaponSynergies` (string, default: "MovementSpeed,PrimaryAttackSpeed,PhysicalCritChance,PhysicalCritDamage")
  Demon Hunter weapon synergies.
- **Demon Hunter Blood Synergies**: `DemonHunterBloodSynergies` (string, default: "PhysicalResistance,ReducedBloodDrain,WeaponCooldownRecoveryRate,MinionDamage")
  Demon Hunter blood synergies
- **Vampire Lord Weapon Synergies**: `VampireLordWeaponSynergies` (string, default: "MaxHealth,SpellLifeLeech,PhysicalPower,SpellPower")
  Vampire Lord weapon synergies.
- **Vampire Lord Blood Synergies**: `VampireLordBloodSynergies` (string, default: "DamageReduction,SpellResistance,UltimateCooldownRecoveryRate,CorruptionDamageReduction")
  Vampire Lord blood synergies.
- **Shadow Blade Weapon Synergies**: `ShadowBladeWeaponSynergies` (string, default: "MovementSpeed,PrimaryAttackSpeed,PhysicalPower,PhysicalCritDamage")
  Shadow Blade weapon synergies.
- **Shadow Blade Blood Synergies**: `ShadowBladeBloodSynergies` (string, default: "SpellResistance,ReducedBloodDrain,WeaponCooldownRecoveryRate,AbilityAttackSpeed")
  Shadow Blade blood synergies.
- **Arcane Sorcerer Weapon Synergies**: `ArcaneSorcererWeaponSynergies` (string, default: "SpellLifeLeech,SpellPower,SpellCritChance,SpellCritDamage")
  Arcane Sorcerer weapon synergies.
- **Arcane Sorcerer Blood Synergies**: `ArcaneSorcererBloodSynergies` (string, default: "HealingReceived,SpellCooldownRecoveryRate,UltimateCooldownRecoveryRate,AbilityAttackSpeed")
  Arcane Sorcerer blood synergies.
- **Death Mage Weapon Synergies**: `DeathMageWeaponSynergies` (string, default: "MaxHealth,SpellLifeLeech,SpellPower,SpellCritDamage")
  Death Mage weapon synergies.
- **Death Mage Blood Synergies**: `DeathMageBloodSynergies` (string, default: "PhysicalResistance,SpellResistance,SpellCooldownRecoveryRate,MinionDamage")
  Death Mage blood synergies.
- **Default Class Spell**: `DefaultClassSpell` (int, default: -433204738)
  Default spell (veil of shadow) available to all classes.
- **Blood Knight Spells**: `BloodKnightSpells` (string, default: "-880131926,651613264,2067760264,189403977,375131842")
  Blood Knight shift spells, granted at levels of prestige.
- **Demon Hunter Spells**: `DemonHunterSpells` (string, default: "-356990326,-987810170,1071205195,1249925269,-914344112")
  Demon Hunter shift spells, granted at levels of prestige.
- **Vampire Lord Spells**: `VampireLordSpells` (string, default: "78384915,295045820,-1000260252,91249849,1966330719")
  Vampire Lord shift spells, granted at levels of prestige.
- **Shadow Blade Spells**: `ShadowBladeSpells` (string, default: "1019568127,1575317901,1112116762,-358319417,1174831223")
  Shadow Blade shift spells, granted at levels of prestige.
- **Arcane Sorcerer Spells**: `ArcaneSorcererSpells` (string, default: "247896794,268059675,-242769430,-2053450457,1650878435")
  Arcane Sorcerer shift spells, granted at levels of prestige.
- **Death Mage Spells**: `DeathMageSpells` (string, default: "-1204819086,481411985,1961570821,2138402840,-1781779733")
  Death Mage shift spells, granted at levels of prestige.

## Recipes

### Salvageable
- **EMPs** (20s)
	- x2 Depleted Battery  
	- x15 Tech Scrap
- **Bat Hide** (15s)
	- x3 Lesser Stygians
	- x5 Blood Essence
- **Copper Wires** (15s)
	- x1 Electricity
- **Primal Blood Essence** (10s)
	- x5 Electricity
- **Gold Ore** (10s)
	- x2 Gold Jewelry
- **Radiant Fiber** (10s)
	- x8 Gem Dust
	- x16 Plant Fiber
	- x24 Pollen

### Refinable
- **Primal Jewel** (T04s with additional modifiers, random however spell school can be influenced via accompanying respective perfect gem which are awarded randomly from dailies/weeklies)
	- Inputs: x1 Demon Fragment (default, configurable)
	- Outputs: x1 Primal Jewel
	- Station: Gem Cutting Table
- **Primal Stygian Shard** (default currency for '.fam echoes', see config option for details)
	- Inputs: x8 Greater Stygian Shards
	- Outputs: x1 Primal Stygian Shard
	- Station: Gem Cutting Table
- **Charged Battery**
	- Inputs: x1 Depleted Battery, x1 Electricity
	- Outputs: x1 Charged Battery
	- Station: Fabricator
- **Blood Crystal**
	- Inputs: x100 Crystals, 1x Greater Blood Essence
	- Outputs: x100 Blood Crystal
	- Station: Advanced Blood Press
- **Copper Wires**
	- Inputs: x3 Copper Ingots
	- Outputs: x1 Copper Wires
	- Station: Fabricator
- **Vampiric Dust** (used to apply/change shiny buffs for familiars)
	- Inputs: x8 Bleeding Hearts, x40 Blood Crystals
	- Outputs: x1 Vampiric Dust
	- Station: Advanced Grinder

## BleedingEdge

### Slashers
- Every 3rd primary attack (must chain to the last hit) will apply bleed, stacking up to 3 times.

### Crossbow
- Primary projectile is significantly faster.

### TwinBlades
- 2nd weapon skill (E) will apply shield from new frost spell when used on vBlood enemies.

### Pistols
- 25% increased primary attack projectile range.

### Daggers
- Stacks refresh even after unequipping daggers.

## Recommended Mods

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
- [XPRising](https://thunderstore.io/c/v-rising/p/XPRising/XPRising/)
  If you like the idea of a mod with RPG features but Bloodcraft doesn't float your boat maybe this will!

## Development Setup

Run `.codex/install.sh` once to install the .NET SDK and Argos Translate. The script adds
`~/.local/bin` to your `PATH` so the `argos-translate` CLI is available in future sessions.

## Localization (WIP)

Messages shown by the plugin are stored under `Resources/Localization/Messages`.
To update `English.json` with the latest hashes run:

```bash
dotnet run --project Bloodcraft.csproj -p:RunGenerateREADME=false -- generate-messages .
```

### Translation Workflow

Use `Tools/translate.py` to generate missing strings. The script protects `<...>` tags and `{...}` variables by replacing them with `[[TOKEN_n]]`. Lines made entirely of tokens receive a `TRANSLATE` suffix so Argos does not skip them. Pass `--verbose` to display each entry as it is processed and `--log-file` to keep a record. After translating, run `check-translations --show-text` to pinpoint any skipped or untranslated strings. See `AGENTS.md` for the full workflow.
`translate.py` accepts `--batch-size`, `--max-retries`, and `--timeout` options. It splits work across batches and retries each batch up to the specified limit, waiting at most `--timeout` seconds for Argos to respond. Re-run it on a clean copy of `Spanish.json` to restart translations from scratch.

### Protecting Tags During Translation

When translating strings manually, use `LocalizationHelpers.Protect` to temporarily replace rich-text tags and placeholders with numbered markers. After translating, call `LocalizationHelpers.Unprotect` to restore the original tokens.

```csharp
var (safe, tokens) = LocalizationHelpers.Protect(original);
// send `safe` to a translation service
string translated = Translate(safe);
string final = LocalizationHelpers.Unprotect(translated, tokens);
```

## Workflow Source

- Repo: https://github.com/knavillus1/codex_bootstrap/tree/dev_chat_with_tasks
- Overview: https://community.openai.com/t/repo-file-based-task-management-in-codex-example-solution/1278584

Very appreciative of the author for making this available! All portions derived or used directly are licensed under [Apache 2.0](https://www.apache.org/licenses/LICENSE-2.0).

## Credits
Do my best to mention/attribute where ideas and bug reports come from in the changelog and commit history, if I've missed something or someone on a specific change please let me know!
