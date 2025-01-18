## Table of Contents

*README under construction, putting these enums here at the top until deciding where to integrate them properly. note that when choosing these via command as stat bonuses in-game use the number shown by the respective list stats command for bloods or weapons*

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

Jairon Orellana; Odjit; Jera; Eve winters; Kokuren TCG and Gaming Shop;

## Features

- **Weapon Expertise:** Enhances gameplay by introducing expertise in different weapon types and optionally adds extra spell slots to unarmed. Expertise is gained per kill based on equipped weapon. Number of stat choices is configurable. Resetting choices is free by default but can be made to cost items. Can be further enhanced by activating soft OR hard synergies (classes) to encourage and/or restrict various playstyles.
- **Player Professions:** Adds various professions, allowing players to specialize and gain benefits from leveling the professions they like most. Proficiency is gained per resource broken, item crafted, or succesful catch. Mining/woodcutting/harvesting provide bonus yields per resource harvest, fishing provide bonus fish every 20 levels based on the area fished in, alchemy enhances consumable duration and effects, and blacksmithing/enchanting/tailoring increases durability in armors/weapons crafted. Do note you only receive experience for crafting and extra durability is only applied if you own the station/placed it if in a clan, plan on improving that and revamping these a bit in the future.
- **Experience Leveling:** Implements a leveling system to replace traditional gearscore and provide a greater sense of progression. Experience for this is gained per kill (should apply to all kills that can be linked back to the player) and can complete level 20 quest to track vbloods with .prepareforthehunt. Still tuning this as needed but the config options should be there to support whatever kind of leveling experience you're looking to provide at this point (within reason :P). Experience sharing in clan or group has a max distance of 25f (4-5 floor lengths). Recently added options to control experience gain from units spawned via vermin nest and other unit spawners along with nerfing rift trash experience if you so desire. Can now tune how much experience higher level players receive from lower level kills as well.
- **Blood Legacies:** Provides further stat customization for legacies (blood types) similar to expertise with a different set of stats. Experience for this is gained per feed kill/execute for equipped blood type. Old blood quality enhancement option still exists for those who would like to use it, although do note if using prestige as well the quality gain will be based on legacy prestige level in that blood. Number of stat choices is configurable. Resetting choices is free by default but can be made to cost items. Can be further enhanced by activating soft OR hard synergies (classes) to encourage and/or restrict various playstyles.
- **Prestiging:** Prestige in leveling, expertise, and legacies! Each increment in leveling prestige will reduce experience rate gain for leveling by the leveling rate reducer (separate from the expertise/legacy rate reducer) and increase expertise/legacy rate gains by the rate multiplier. Prestiging in expertise/legacies will increase their max stat bonuses per level by the stat multiplier and will reduce rate gains in that individual prestige category by the rate reducer per increment. Configurable buffs will be permanently applied to the player each prestige level in experience (can skip levels by leaving prefab as 0). Prestiging is also configurably tied to unlocking extra spells for classes to use on shift (you can use any abilityGroup prefabs here but don't use normal player spells unless you like weird weapon cloning bugs when equipping jewels. also, some NPC spells don't really work as expected when used by players so do some testing before swapping these out and make sure they behave. can test spells with '.spell [1/2] [AbilityGroupPrefab]' to manually place them on unarmed). Exo prestiging is also available for those who have already maxed out normal experience prestige options and want more of a NG+ feel, increases damage taken/done by configured amount per level up to 100 times by default with configured item reward per level, trying to emulate a NG+ feel with this one.
- **Familiars:** Every kill of a valid unit is a chance to unlock that unit as a familiar, now including VBloods and summoners! VBloods can be toggled on/off and unit types can be configurably allowed (humans, demons, beasts, etc) although do note that some category bans will supercede VBlood checks (if humans are banned, Vincent will be banned as well even if VBloods are enabled); there is also a list for individual unit bans. They will level up, fight for you, and you can collect them all. Only one familiar can be summoned at a time. They can be toggled (called/dismissed) without fully unbinding them to quickly use waygates (attempting to use a waygate with a familiar out will dismiss it automatically now and using it will then summon the familiar again so it is there after teleporting) and perform other actions normally prohibited by having a follower like a charmed human. Each set can hold 10 and more sets will be created once the one being added to is full. You can choose the active set by name (default is FamiliarsList1, FamiliarsList2, etc), remove familiars from sets, move familiars between sets, and the sets can be renamed. To summon a familiar from your current set (which can be chosen with .cfs [Name]), .lf to see the units available and use .bind #; to choose a different one, use .unbind, and if it gets weird or buggy at any point .resetfams can be used to clear your active familiar data and followerBuffer. All units have the same stats as usual, however their phys/spell power starts at 10% at level 1 and will scale to 100% again when they reach max level. Base health is 500 and will scale with up to 2500 at max level. Traders, carriages, horses, crystals (looking at you dracula blood crystal thing) and werewolves are unavailable. Werewolves will be in the pool once I add handling for their transformations (ngl completely forgot about this, oops >_>) but the rest will likely stay banned (if you manually add one of them to your unlocks as an admin that's on you :P). Can't bind in combat or pvp combat. Shinies are back! 20% chance to unlock 1 of 6 visuals when first obtaining unit and 100% to unlock one on repeat unlock of same unit, one visual per unit for now. Can toggle visual being applied when familiar is bound using 'yes' if familiar emote actions are enabled. '.fam v [SpellSchool]' is good for one free shiny visual of choice and will apply to your active familiar.
- **Classes:** Soft synergies allows for the encouragement of stat choices without restricting choices while hard synergies will only allow the stats for that class to be chosen. Both soft or hard will apply the synergy multiplier to the max stat bonuses for that class for weapon expertise/blood legacies. Additionally, each class has a config spot for 4 blood buffs (the correct prefabs for this start with AB_BloodBuff_) that are granted every MaxPlayerLevel/#buffs while leveling up (with 4 buffs and 90 being the max, they'll be applied at 22, 44, 66, and 88) and apply permanently at max strength without regard to the blood quality or type the player possesses (can be skipped with 0 in the spot). They do not stack with buffs from the same blood type (if a player has the scholar free cast proc buff from class and also has 100% scholar blood, those chances will not stack) which maintains a sense of balance while allowing each class to have a persistent theme/identity that can be earned without excessive grinding. Classes also get extra spells to use on shift that are configurable, both the spell and at what level of prestige they are earned at but do keep in mind the considerations mentioned in the prestige section. Will probably be changing at least a few aspects of these new class systems in the future but am overall happy with where they're at in the sense that they allow for a lot of customization and can be made to feel as unique as you'd like. Tried to make decent thematic choices for the default blood buffs and spells per class but no promises :P. Changing classes should properly remove and apply buffs based on level. Also, spell school effects on hit unique to each class if enabled! (will be debuff of spell school for class and if debuff already present will proc respective t08 necklace effect)
- **Quests:** Daily and weekly kill quests. Configurable reward pool and amounts. Dailies give 2.5% of your total exp as reward, weeklies give 10%. Can track progress and see details with commands.

### Experience Leveling

### Weapon Expertise
- Gain levels in weapon types when slaying (equipped for final blow) enemies.
  - .wep log (toggle gains logging in chat) 
  - .wep get (view weapon progression/stat details)
- Pick stats to enhance per weapon when equipped.
  - .wep lst (view weapon bonus stat options)
  - .wep cst [Weapon] [WeaponStat] (select bonus stat for entered weapon)
  - .wep rst (reset stat selection for current weapon)
- Stat bonus values scale with player weapon expertise level per weapon.
  - classes have synergies with different weapon stats (see Classes section)

### Blood Legacies
- Gain levels in blood types when feeding on (executes and completions) enemies.
  - .bl log (toggle gains logging in chat) 
  - .bl get (view blood progression/stat details)
- Pick stats to enhance per blood when using.
  - .bl lst (view blood bonus stat options)
  - .bl cst [Blood] [BloodStat] (select bonus stat for entered weapon)
  - .bl rst (reset stat selection for current weapon)
- Stat bonus values scale with player blood legacy level per blood.
  - classes have synergies with different blood stats (see Classes section)
	
### Prestige

### Classes

### Familiars

### Quests

### Professions

## Commands

### Bloodlegacy Commands
- `.bloodlegacy getlegacy.bl get [BloodType]`
  - Display current blood legacy details.
  - Shortcut: *.bl get [BloodType]*
- `.bloodlegacy loglegacies.bl log`
  - Toggles Legacy progress logging.
  - Shortcut: *.bl log*
- `.bloodlegacy choosestat [Blood] [BloodStat]`
  - Choose a bonus stat to enhance for your blood legacy.
  - Shortcut: *.bl cst [Blood] [BloodStat]*
- `.bloodlegacy resetstats`
  - Reset stats for current blood.
  - Shortcut: *.bl rst*
- `.bloodlegacy liststats`
  - Lists blood stats available.
  - Shortcut: *.bl lst*
- `.bloodlegacy setlegacy.bl set [Player] [Blood] [Level]` 🔒
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
- `.familiar listprestigestats`
  - Display options for familiar prestige stats.
  - Shortcut: *.fam lst*
- `.familiar prestige [BonusStat]`
  - Prestiges familiar if at max, raising base stats by configured multiplier and adding an extra chosen stat.
  - Shortcut: *.fam pr [BonusStat]*
- `.familiar reset`
  - Resets (destroys) entities found in followerbuffer and clears familiar actives data.
  - Shortcut: *.fam reset*
- `.familiar search [Name]`
  - Searches boxes for unit with entered name.
  - Shortcut: *.fam s [Name]*
- `.familiar smartbind [Name] [OptionalIndex]`
  - Searches and binds a familiar. If multiple matches are found, returns a list for clarification.
  - Shortcut: *.fam sb [Name] [OptionalIndex]*
- `.familiar shinybuff [SpellSchool]`
  - Chooses shiny for current active familiar, one freebie then costs configured amount to change if already unlocked.
  - Shortcut: *.fam shiny [SpellSchool]*
- `.familiar resetshiny [Name]` 🔒
  - Allows player to make another familiar shiny as if freebie were unused.
  - Shortcut: *.fam rs [Name]*
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

### Misc Commands
- `.famservant` 🔒
  - testing
  - Shortcut: *.fs*
- `.famhorse` 🔒
  - testing
  - Shortcut: *.fh*
- `.queuetest` 🔒
  - Queue testing.
  - Shortcut: *.qt [PlayerOne] [PlayerTwo]*
- `.forcechallenge` 🔒
  - Challenge testing.
  - Shortcut: *.fc [PlayerName]*
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
- `.cleanupfams` 🔒
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
- `.party reset`
  - Removes a player from all parties they are in and disbands any party they own.
  - Shortcut: *.party r*

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
- `.weapon getexpertise`
  - Displays current weapon expertise details.
  - Shortcut: *.wep get*
- `.weapon logexpertise`
  - Toggles expertise logging.
  - Shortcut: *.wep log*
- `.weapon choosestat [Weapon] [WeaponStat]`
  - Choose a weapon stat to enhance based on your expertise.
  - Shortcut: *.wep cst [Weapon] [WeaponStat]*
- `.weapon resetstats`
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
- **Potion Stacking**: `PotionStacking` (bool, default: False)
  Enable or disable potion stacking (can have t01 effects and t02 effects at the same time. also requires professions enabled).
- **Bear Form Dash**: `BearFormDash` (bool, default: False)
  Enable or disable bear form dash.

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
  Enable or disable exo prestiges (need to max normal prestiges first).
- **Exo Prestiges**: `ExoPrestiges` (int, default: 100)
  The number of exo prestiges available.
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
- **Max Profession Level**: `MaxProfessionLevel` (int, default: 100)
  The maximum level a player can reach in professions.
- **Profession Multiplier**: `ProfessionMultiplier` (float, default: 10)
  The multiplier for profession experience gained.
- **Extra Recipes**: `ExtraRecipes` (bool, default: False)
  Enable or disable extra recipes.

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
  The multiplier for stats gained from familiar prestiges.
- **Max Familiar Level**: `MaxFamiliarLevel` (int, default: 90)
  The maximum level a familiar can reach.
- **Allow V Bloods**: `AllowVBloods` (bool, default: False)
  Allow VBloods to be unlocked as familiars (this includes shardbearers, if you want those excluded use the bannedUnits list).
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
- **Shiny Chance**: `ShinyChance` (float, default: 0.2)
  The chance for a shiny when unlocking familiars (6 total, 1 per familiar). Guaranteed on second unlock of same unit, chance on damage dealt (same as configured onHitEffect chance) to apply spell school debuff.
- **Shiny Cost Item Prefab**: `ShinyCostItemPrefab` (int, default: -77477508)
  Item PrefabGUID cost for changing shiny visual if one is already unlocked (currently demon fragment by default).
- **Shiny Cost Item Quantity**: `ShinyCostItemQuantity` (int, default: 1)
  Quantity of item required for changing shiny buff.
- **Prestige Cost Item Quantity**: `PrestigeCostItemQuantity` (int, default: 2500)
  Quantity of schematics required to immediately prestige familiar (gain total levels equal to max familiar level, extra levels remaining from the amount needed to prestige will be added to familiar after prestiging).

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
- **Death Mage Buffs**: `DeathMageBuffs` (string, default: "-901503997,-491525099,1934870645,1201299233")
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
Do my best to mention/attribute where ideas and bug reports come from in the changelog and commit history, if I've missed anybody on a specific change please let me know!
