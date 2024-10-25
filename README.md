## Table of Contents

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
- **Experience Leveling:** Implements a leveling system to replace traditional gearscore and provide a greater sense of progression. Experience for this is gained per kill (should apply to all kills that can be linked back to the player) and can complete level 20 quest to track vbloods with .quickStart (.start). Still tuning this as needed but the config options should be there to support whatever kind of leveling experience you're looking to provide at this point (within reason :P). Experience sharing in clan or group has a max distance of 25f (4-5 floor lengths). Recently added options to control experience gain from units spawned via vermin nest and other unit spawners along with nerfing rift trash experience if you so desire. Can now tune how much experience higher level players receive from lower level kills as well.
- **Blood Legacies:** Provides further stat customization for legacies (blood types) similar to expertise with a different set of stats. Experience for this is gained per feed kill/execute for equipped blood type. Old blood quality enhancement option still exists for those who would like to use it, although do note if using prestige as well the quality gain will be based on legacy prestige level in that blood. Number of stat choices is configurable. Resetting choices is free by default but can be made to cost items. Can be further enhanced by activating soft OR hard synergies (classes) to encourage and/or restrict various playstyles.
- **Prestiging:** Prestige in leveling, expertise, and legacies! Each increment in leveling prestige will reduce experience rate gain for leveling by the leveling rate reducer (separate from the expertise/legacy rate reducer) and increase expertise/legacy rate gains by the rate multiplier. Prestiging in expertise/legacies will increase their max stat bonuses per level by the stat multiplier and will reduce rate gains in that individual prestige category by the rate reducer per increment. Configurable buffs will be permanently applied to the player each prestige level in experience (can skip levels by leaving prefab as 0). Prestiging is also configurably tied to unlocking extra spells for classes to use on shift (you can use any abilityGroup prefabs here but don't use normal player spells unless you like weird weapon cloning bugs when equipping jewels. also, some NPC spells don't really work as expected when used by players so do some testing before swapping these out and make sure they behave. can test spells with '.spell [1/2] [AbilityGroupPrefab]' to manually place them on unarmed). Exo prestiging is also available for those who have already maxed out normal experience prestige options and want more of a NG+ feel, increases damage taken/done by configured amount per level up to 100 times by default with configured item reward per level, trying to emulate a NG+ feel with this one.
- **Familiars:** Every kill of a valid unit is a chance to unlock that unit as a familiar, now including VBloods and summoners! VBloods can be toggled on/off and unit types can be configurably allowed (humans, demons, beasts, etc) although do note that some category bans will supercede VBlood checks (if humans are banned, Vincent will be banned as well even if VBloods are enabled); there is also a list for individual unit bans. They will level up, fight for you, and you can collect them all. Only one familiar can be summoned at a time. They can be toggled (called/dismissed) without fully unbinding them to quickly use waygates (attempting to use a waygate with a familiar out will dismiss it automatically now and using it will then summon the familiar again so it is there after teleporting) and perform other actions normally prohibited by having a follower like a charmed human. Each set can hold 10 and more sets will be created once the one being added to is full. You can choose the active set by name (default is FamiliarsList1, FamiliarsList2, etc), remove familiars from sets, move familiars between sets, and the sets can be renamed. To summon a familiar from your current set (which can be chosen with .cfs [Name]), .lf to see the units available and use .bind #; to choose a different one, use .unbind, and if it gets weird or buggy at any point .resetfams can be used to clear your active familiar data and followerBuffer. All units have the same stats as usual, however their phys/spell power starts at 10% at level 1 and will scale to 100% again when they reach max level. Base health is 500 and will scale with up to 2500 at max level. Traders, carriages, horses, crystals (looking at you dracula blood crystal thing) and werewolves are unavailable. Werewolves will be in the pool once I add handling for their transformations (ngl completely forgot about this, oops >_>) but the rest will likely stay banned (if you manually add one of them to your unlocks as an admin that's on you :P). Can't bind in combat or pvp combat. Shinies are back! 20% chance to unlock 1 of 6 visuals when first obtaining unit and 100% to unlock one on repeat unlock of same unit, one visual per unit for now. Can toggle visual being applied when familiar is bound using 'yes' if familiar emote actions are enabled. '.fam v [SpellSchool]' is good for one free shiny visual of choice and will apply to your active familiar.
- **Classes:** Soft synergies allows for the encouragement of stat choices without restricting choices while hard synergies will only allow the stats for that class to be chosen. Both soft or hard will apply the synergy multiplier to the max stat bonuses for that class for weapon expertise/blood legacies. Additionally, each class has a config spot for 4 blood buffs (the correct prefabs for this start with AB_BloodBuff_) that are granted every MaxPlayerLevel/#buffs while leveling up (with 4 buffs and 90 being the max, they'll be applied at 22, 44, 66, and 88) and apply permanently at max strength without regard to the blood quality or type the player possesses (can be skipped with 0 in the spot). They do not stack with buffs from the same blood type (if a player has the scholar free cast proc buff from class and also has 100% scholar blood, those chances will not stack) which maintains a sense of balance while allowing each class to have a persistent theme/identity that can be earned without excessive grinding. Classes also get extra spells to use on shift that are configurable, both the spell and at what level of prestige they are earned at but do keep in mind the considerations mentioned in the prestige section. Will probably be changing at least a few aspects of these new class systems in the future but am overall happy with where they're at in the sense that they allow for a lot of customization and can be made to feel as unique as you'd like. Tried to make decent thematic choices for the default blood buffs and spells per class but no promises :P. Changing classes should properly remove and apply buffs based on level. Also, spell school effects on hit unique to each class if enabled! (will be debuff of spell school for class and if debuff already present will proc respective t08 necklace effect)
- **Quests:** Daily and weekly kill quests. Configurable reward pool and amounts. Dailies give 2.5% of your total exp as reward, weeklies give 10%. Can track progress and see details with commands.

## Commands

### Blood Commands
- `.bloodlegacy get [Blood]`
  - Display progress and bonuses in entered blood legacy.
  - Shortcut: *.bl get [Blood]*
- `.bloodlegacy log`
  - Enables or disables blood legacy logging.
  - Shortcut: *.bl log*
- `.bloodlegacy choosestat [Blood] [Stat]`
  - Chooses stat bonus to apply to a blood type that scales with that blood legacy level.
  - Shortcut: *.bl cst [Blood] [Stat]*
- `.bloodlegacy set [Player] [Blood] [Level]` 🔒
  - Sets player blood legacy level.
  - Shorcut: *.bl set [Player] [Blood] [Level]*
- `.bloodlegacy liststats`
  - Lists blood stats available along with bonuses at max legacy.
  - Shortcut: *.bl lst*
- `.bloodlegacy resetstats`
  - Resets stat choices for currently equipped blood for configurable item cost/quanity.
  - Shortcut: *.bl rst*
- `.bloodlegacy list`
  - Lists blood legacies available.
  - Shortcut: *.bl l*

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
  - Shorcut: *.prestige spr [Name] [PrestigeType] [Level]*
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
  - Shortcut: *.party invites*
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
- `.profession get [Profession]`
  - Display progress in entered profession.
  - Shortcut: *.prof get [Profession]*
- `.profession log`
  - Enables or disables profession progress logging (also controls if user is informed of bonus yields from profession levels).
  - Shortcut: *.prof log*
- `.profession set [Name] [Profession] [Level]` 🔒
  - Sets player profession level.
  - Shortcut: *.prof set [Name] [Profession] [Level]*
- `.profession list`
  - Lists available professions.
  - Shortcut: *.prof l*
    
### Weapon Commands
- `.weapon get`
  - Display expertise progress for current weapon along with any bonus stats if applicable.
  - Shortcut: *.wep get*
- `.weapon log`
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
  - Calls or dismisses familar. Wave also does this if .fam e is toggled on.
  - Shortcut: *.fam toggle*
- `.familiar togglecombat`
  - Enables/disables combat. Salute also does this if .fam e is toggled on.
  - Shortcut: *.fam c*
- `.familiar add [Player] [CharPrefab]` 🔒
  - Adds familiar to last list of player named.
  - Shortcut: *.fam a [CharPrefab]*
- `.familiar remove [#]`
  - Removes familiar from unlocks.
  - Shortcut: *.fam r [#]*
- `.familiar emotes`
  - Toggles emote commands (only 1 right now) on.
  - Shortcut: *.fam e*
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
- `.familiar search [Name]`
  - Searches for familiars matching entered name in your boxes.
  - Shortcut: *.fam s [Name]*
- `.familiar search [Name]`
  - Searches for familiars matching entered name in your boxes.
  - Shortcut: *.fam s [Name]*
- `.familiar visual [SpellSchool]`
  - Assigns shiny visual to current active familiar. One freebie.
  - Shortcut: *.fam v [SpellSchool]*
- `.familiar resetvisualchoice [Name]` 🔒
  - Allows players to use '.fam v [SpellSchool]' again at admin discretion if so desired. Note that this will not remove visuals the player has chosen for a familiar with that command.
  - Shortcut: *.fam rv [Name]*
- `.familiar option [setting]`
  - Controls various familiar settings (currently shiny/vbloodemotes).
  - Shortcut: *.fam option [Setting]*

### Misc Commands
- `.starterkit`
  - Grants kit if configured.
  - Shortcut: *.kitme*
- `.prepareforthehunt`
  - Completes GettingReadyForTheHunt if not already completed. This shouldn't be needed anymore but leaving just incase.
  - Shortcut: *.prepare*
- `.lockSpell`
  - Enables registering spells to use in unarmed slots if extra slots for unarmed are enabled. Toggle, move spells to slots, then toggle again and switch to unarmed.
  - Shortcut: *.locksp*
- `.lockshift`
  - Toggles set class spell on shift. Works for unarmed and weapons.
  - Shortcut: *.shift*
- `.sct`
  - Toggle scrolling text appearing when earning profession XP.
  - Shortcut: *.sct*
- `.remindme`
  - Toggle reminders from mod for various features.
  - Shortcut: *.remindme*
- `.userstats`
  - Shows stats from credits on demand.
  - Shortcut: *.userstats*
- `.silence`
  - Gets rid of combat music that gets bugged on player if needed.
  - Shortcut: *.silence*
- `.cleanupfams`
  - Finds and destroys old familiars preventing from building in a location.
  - Shortcut: *.cleanupfams*

### Quest Commands
- `.quest log`
  - Enable/disable logging quest progress. 
  - Shortcut: *.quest log*
- `.quest daily`
  - Displays daily quest details.
  - Shortcut: *.quest d*
- `.quest weekly`
  - Displays weekly quest details.
  - Shortcut: *.quest w*
- `.quest track [Daily/Weekly]`
  - Finds closest quest target or rerolls quest if none are found.
  - Shortcut: *.quest t [Daily/Weekly]*
- `.quest reroll [Daily/Weekly]`
  - Rerolls quest for player if eligible at cost.
  - Shortcut: *.quest r*
- `.quest refresh [Name]` 🔒
  - Refreshes quests for player.
  - Shortcut: *.quest rf [Name]*
 
## Configuration

### General
- **Language Localization**: `LanguageLocalization` (string, default: English)
  The language localization for prefabs displayed to users. English by default. Options: Brazilian, English, French, German, Hungarian, Italian, Japanese, Koreana, Latam, Polish, Russian, SimplifiedChinese, TraditionalChinese, Thai, Turkish, Vietnamese
- **Starter Kit**: `StarterKit` (bool, default: false)  
  Enable or disable the starter kit.
- **KitPrefabs**: `KitPrefabs` (string, default: "862477668,-1531666018,-1593377811,1821405450")  
  PrefabGUIDs for the starter kit.
- **KitQuantities**: `KitQuantities` (string, default: "500,1000,1000,250")
  Quantities for the starter kit.
- **Potion Stacking**: `PotionStacking` (bool, default: false)  
  Enable or disable stacking T01/T02 potions.
- **Client Companion**: `ClientCompanion` (bool, default: false)  
  Enable or disable allowing clients to register with server for UI updates.

### Leveling System
- **Enable Leveling System**: `LevelingSystem` (bool, default: false)  
  Enable or disable the leveling system.
- **Enable Rested XP**: `RestedXPSystem` (bool, default: false)  
  Enable or disable rested XP (get bonus exp when killing if you log out in your coffin ala logging out in an inn from WoW).
- **Rested XP Rate**: `RestedXPRate` (float, default: 0.05)  
  Accumulation rate of rested XP per tick.
- **Max Stored Rested XP**: `RestedXPMax` (int, default: 5)  
  Maximum extra levels worth of rested XP that can be stored before capping.
- **Rested XP Tick Rate**: `RestedXPTickRate` (float, default: 120)  
  Minutes offline required per tick of earned rested XP.
- **Max Player Level**: `MaxLevel` (int, default: 90)  
  The maximum level a player can reach.
- **Starting Player Level**: `StartingLevel` (int, default: 0)  
  The starting level for players.
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

### Prestige System
- **Enable Prestige System**: `PrestigeSystem` (bool, default: false)  
  Enable or disable the prestige system.
- **Prestige Buffs**: `PrestigeBuffs` (string, default: "1504279833,475045773,1643157297,946705138,-1266262267,-773025435,-1043659405,-1583573438,-1869022798,-536284884")
  The Prefabs for general prestige buffs, use 0 to skip otherwise buff applies at the prestige level based on spot in the list.
- **Prestige Levels to Unlock Class Spells:** `PrestigeLevelsToUnlockClassSpells` (string, default: "0,1,2,3,4,5")
  The prestige levels at which class spells are unlocked. This should match the number of spells per class. Can leave at 0 if you want them unlocked from the start.
- **Max Leveling Prestiges**: `MaxLevelingPrestiges` (int, default: 10)  
  The maximum number of prestiges a player can reach in leveling.
- **Prestige Rates Reducer**: `PrestigeRatesReducer` (float, default: 0.10)  
  Factor by which rates are reduced in expertise/legacy/experience per increment of prestige in expertise, legacy or experience.
- **Prestige Stat Multiplier**: `PrestigeStatMultiplier` (float, default: 0.10)  
  Factor by which stats are increased in expertise/legacy bonuses per increment of prestige in expertise/legacy.
- **Prestige Rates Multiplier**: `PrestigeRatesMultiplier` (float, default: 0.10)  
  Factor by which rates are increased in expertise/legacy per increment of prestige in leveling.
- **Level Scaling Multiplier**: `LevelScalingMultiplier` (float, default: 0.05)  
  Reduces experience gained from kills with a large level gap between player and unit, increase to make harsher decrease or set to 0 to remove.
  
### Quest System
- **Enable Quest System**: `QuestSystem` (bool, default: false)  
  Enable or disable the quest system.
- **Infinite Dailies**: `InfiniteDailies` (bool, default: false)  
  Enable or disable infinite dailies (will grant new daily upon completion of current).
- **Quest Rewards**: `QuestRewards` (string, default: "2103989354,576389135,28358550")  
  Prefab pool for quest rewards.
- **Quest Reward Amounts**: `QuestRewardAmounts` (string, default: "250,100,50")  
  Amounts for quest rewards. Must match the number of rewards in the pool.
- **Daily Reroll Prefab**: `RerollDailyPrefab` (int, default: -949672483)  
  Prefab item required for rerolling daily.
- **Daily Reroll Cost**: `RerollDailyAmount` (int, default: 50)  
  Cost of prefab for rerolling daily.
- **Weekly Reroll Prefab**: `RerollWeeklyPrefab` (int, default: -949672483)  
  Prefab item for rerolling weekly.
- **Weekly Reroll Cost**: `RerollWeeklyAmount` (int, default: 50)  
  Cost of prefab for rerolling weekly.
 
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
- **Physical Power**: `PhysicalPower` (float, default: 20)  
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
- **Share Unlocks**: `ShareUnlocks` (bool, default: false)  
  Enable or disable sharing unlocks between players in clans or parties (uses exp share distance).
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
- **Shiny Chance**: `ShinyChance` (float, default: 0.2)  
  The chance to unlock a visual for a familiar when first unlocking unit.
- **Shiny Cost Item**: `ShinyCostItemPrefab` (int, default: -77477508)  
  Item PrefabGUID cost for choosing shiny visual if familiar already has one.
- **Shiny Cost Item Quantity**: `ShinyCostItemQuantity` (int, default: 1)  
  Quantity of item cost required shiny cost item.

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
- **Blood Knight Buffs:** `BloodKnightBuffs` (string, default: "1828387635,-534491790,-1055766373,-584203677")
  The PrefabGUID hashes for blood knight leveling blood buffs.
- **Blood Knight Spells:** `BloodKnightSpells` (string, default: "-880131926,651613264,2067760264,189403977,375131842")
  Blood Knight shift spells, granted at levels of prestige.
- **Demon Hunter Buffs:** `DemonHunterBuffs` (string, default: "-154702686,-285745649,-1510965956,-397097531")
  The PrefabGUID hashes for demon hunter leveling blood buffs.
- **Demon Hunter Spells:** `DemonHunterSpells` (string, default: "-356990326,-987810170,1071205195,1249925269,-914344112")
  Demon Hunter shift spells, granted at levels of prestige.
- **Vampire Lord Buffs:** `VampireLordBuffs` (string, default: "1558171501,997154800,-1413561088,1103099361")
  The PrefabGUID hashes for vampire lord leveling blood buffs.
- **Vampire Lord Spells:** `VampireLordSpells` (string, default: "78384915,295045820,-1000260252,91249849,1966330719")
  Vampire Lord shift spells, granted at levels of prestige.
- **Shadow Blade Buffs:** `ShadowBladeBuffs` (string, default: "894725875,-1596803256,-993492354,210193036")
  The PrefabGUID hashes for shadow blade leveling blood buffs.
- **Shadow Blade Spells:** `ShadowBladeSpells` (string, default: "1019568127,1575317901,1112116762,-358319417,1174831223")
  Shadow Blade shift spells, granted at levels of prestige.
- **Arcane Sorcerer Buffs:** `ArcaneSorcererBuffs` (string, default: "1614027598,884683323,-1576592687,-1859298707")
  The PrefabGUID hashes for arcane sorcerer leveling blood buffs.
- **Arcane Sorcerer Spells:** `ArcaneSorcererSpells` (string, default: "247896794,268059675,-242769430,-2053450457,1650878435")
  Arcane Sorcerer shift spells, granted at levels of prestige.
- **Death Mage Buffs:** `DeathMageBuffs` (string, default: "-901503997,-804597757,1934870645,1201299233")
  The PrefabGUID hashes for death mage leveling blood buffs.
- **Death Mage Spells:** `DeathMageSpells` (string, default: "-1204819086,481411985,1961570821,2138402840,-1781779733")
  Demon Hunter shift spells, granted at levels of prestige.
- **Change Class Item**: `ChangeClassItem` (int, default: 576389135)  
  Item PrefabGUID for changing classes.
- **Change Class Item Quantity**: `ChangeClassQuantity` (int, default: 1000)  
  Quantity of item cost required to change class.

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
