`1.4.3`
- Legacy essence values gained from feed events are now multipled by x5 if consuming the same bloodType and/or then up to x2 if the player has 100% blood quality
- Exo prestige is hard-disabled regardless of config and any current effects should be undone when players log in, will be reenabled in a future update after reworking
- Traders no longer provide any progression to players when killed
- Blood stats are now applied when chosen and update on legacy level up without needing to consume new blood of that type
- Fixed bug with crafting credit for professions/crafting quests, removed crafting credit cooldown, balanced crafting quests and removed highest tiers of equipment from target pool
- Familiars should play nicer with others on PvE servers, have also added handling for some abilities that were ignoring allied status and preventing some debuffs from being applied to players when they are PvP protected
- Using a waygate with a familiar out will dismiss it without player having to use waygate again as is currently the case, after teleporting it will return (only if familiar was present/active first)
- Using batform with a familiar out will dismiss it, after landing it will return (only if familiar was present/active first)
- Removed crystal, thistle and coal from gathering quest prefab pool
- RestedXP adjusted downwards based on the player's new calculated cap instead of being reset entirely after prestiging in experience/leveling
- Can use 'vblood' as the name with familiar search command to return all boxes with at least one vblood or blood soul (ty Odjit c:)
- Profession experience now granted to players when using clan/allied stations and not just stations placed by the player
- NPC spell cooldowns for classes again functioning as they were previously, cooldown will be 8 seconds multiplied by the order of the spell in the class spells list
- Added config option to control expertise received from vermin nests, tombs etc (UnitSpawnerExpertiseFactor, set to 0 for none gained leave at 1 for no change)
- Removed auto rerolling quest targets, was trickier to get right than it was worth now that manual rerolling is an option (please let me know of any unit prefabs that result in players being unable to complete a quest and they will be added to filter)
- Target tracking will no longer detect imprisoned units or player familiars
- Checking unit level against player level using unit prefab instead of unit entity in questTarget cache to prevent assigning quest kill targets outside intended level range (added handling for player levels higher than max mod default to prevent repeated fallback target)
- Added logging and handling when configurations for item prefabs/quantities are not as expected instead of failing to initialize entirely
- Changed playerBools data to ConcurrentDictionary instead of Dictionary (hoping this resolves .kitme and other potential player bool-related issues)

`1.4.2`
- fixed gathering/crafting quests unintentionally checking for professions/class on hit effects being enabled
- added check for full inventory before letting players enable or swap class spells to facilitate safe handling of jewels
- filtered out t01 bone weapons for crafting quests since can't be crafted at bench
- fixed erroneous temporary exp gain for legacies when blood system is disabled

`1.4.1`
- config entries are now in sections (please save a backup of your config just incase but seems to be handling migration well now)
- .cleanupfams does a bit more cleaning but will take a few seconds at least, tried having that run on restarts but game wasn't a fan so leaving it on the command for now
- normal spells on shift by default again (should leave existing configs untouched if you'd rather use NPC spells), fixed bug with legendary weapons + equipping jewels sometimes cloning the weapon and eating the jewel 
- added default class spell config and moved veil of shadows there (all classes can use this spell via '.class csp 0')
- profession experience can optionally be shown as scrolling text! use '.sct' to enable/disable, will be exploring other uses for this in the future
- changing class spells no longer requires switching weapons to take effect (this applies to blood legacy bonus stats as well but not expertise yet), redid shift toggle to maintain functionality
- crafting/gathering objectives added to quest pool, can also reroll quests with '.quest r [QuestType]' for configurable cost/amount if uncompleted or if infinite dailies are enabled for dailies (admin quest refresh moved to '.quest rf [Name]')
- required for Eclipse 1.0.0
- annnnd anything else since 1.2.4, it's been a minute. would recommend reviewing but if you read even this far I'm surprised :p

`1.3.3`
- slight changes to communication with Eclipse that will need version 0.2.0 of that to work correctly
- more filtering for quest targets
- cleaning up from experimenting with a variety of things that I really need to stop messing with if I ever want to publish this again >_>

`1.3.2`
- legacy stat bonuses will update when applied and when leveling up
- prestige buff automatically reapplied if one of them is found being destroyed
- skeletons from graves are now left untouched
- fixed total experience being displayed as rested experience for '.lvl get'
- fixed spell cooldowns for classes
- fixed null reference in craftingPatch that was interfering with profession experience

`1.3.1`
- changed UI updates to work via coroutine, more reliable and less prone to error so far
- fixed bug where wrong BloodType enum was sent to client when frailed
- no restedXP message if at max level when logging in while in coffin and restedXP is enabled
- added max level for player, legacy, and expertise to config data sent to client when registering to properly make bars full at max levels

`1.3.0`
- added config option for potion stacking
- added configurable item cost for choosing familiar visual if already shiny via '.fam v [SpellSchool]', will override current visual (freebie still works as before, cost will take precedence if familiar already has a visual unlocked)
- rested XP (solid idea from Odjit), if enabled earn bonus experience as configured for logging out in your coffin!
- familiar options command ('.fam option [Setting]' where familiar settings now live that don't warrant use of an emote action, currently shiny/vbloodemote toggles)
- familiars now seen as minions of the player by the game, for PvE prevents targetting other players and for PvP prevents them from dealing damage to PvP protected targets
- added config option to enable sending updates to clientside mod for UI (needs to be enabled on the server or client mod won't do anything by itself)
- added reminders for various mod features that will be sent to players on occasion for classes/expertise/legacies, can be toggled on and off via command ('.remindme')
- fixed familiar aggro bug when using call/dismiss even if familiar combat is disabled via config
- added command to clean up old disabled familiars ('.cleanupfams', should fix building spot bug)
- config options are in sections now! should carry over existing values but make a backup of your config just in case
- added command to show userStats ('.userstats', usually only seen after credits)

`1.2.4`
- special thanks to Odjit for tracking down the party bug and discovering the reroll issue <3
- fixed bug where parties would prevent players from taking silver burn damage and garlic stacks being applied
- added check to prevent quests from being rerolled if target was previously found in the world but all are dead and waiting on respawns
- fixed bug where exo prestiging was possible before reaching max player level
- added shinies back for familiars (see README for details)
- amped divine angel and added holy mortar buff from brutal Simon to Solarus after his holy bubble activates for eliteShardBearers
- removed divine and fallen angels from quest target pool
- fixed bug granting double quest credit on feed executes

`1.2.3`
- note: changed a key in the player prestige file from Sanguimancy to UnarmedExpertise and UnarmedExpertise will no longer be stored in the player_sanguimancy file, tested handling old data to new formats and seems to be fine but please make a backup of these files just incase_
- added command to search for familiars in boxes (.fam s [Name])
- added command to find nearest quest target, if none are found will reroll the quest
- reduced number of kills required for vbloods for dailies and weeklies to be less of a vibe-check
- can stack t01/t02 potion effects, if anyone finds this particularly bothersome can add a config option
- added command to reset commbat music for player (.silence) if it gets stuck (think I handled what was causing that but if not this will fix it and can be used by non-admins)
- added toggle to disable vblood emotes (no commands, only toggleable via taunt emote if familiar emote actions are enabled. will be considering removing any familiar commands that have an emote equivalent to cut down on bloat)
- further improved quest distribution, should be significantly more consistent and less error-prone (will still take time to rollover from old quests, would recommend deleting quest file to force initial refreshes)
- added option for infinite dailies
- added basic starter kit, configurable
- player prestiges updated when connecting to add any missing prestige types if data already exists (Exo, FishingPoleExpertise)
- more filtering to prevent non-ideal quest targets, fixed bug on generating quests for new players on fresh servers (new players receieve same starting daily/weekly quests to circumvent this)
- ancestral forge works for all shattered again and not just blue legendaries
- fixed bug where destroying disabled familiars rendered space unuseable for building
- fixed leaderboard for prestige not displaying correctly
- eliteShardBearers added as config option (experimental, WIP. significantly increases health, damage, attack speed, cast speed, and movement speed of shardbearers but removes scaling for multiple players fighting them at once and gives them minor visual effects)

`1.0.0`
- command groups added for organizational purposes (thanks Odjit <3)
- familiar prestige limit should be working as intended now
- familiar CC effects and other debuffs seem to be neutered effectively for PvE and other scenarios where they should be
- added ShareUnlocks config option for familiars when killing with clanmates or party members, uses share exp distance for range, gives all players involved a roll
- quests! kill only for now, daily and weekly, details in readme (BETA) note that it will take a small but non-trivial amount of time to distribute quests to all players when first enabled, service loop runs every 5 minutes and has a 10 sec cooldown between generating quests for players to prevent lag from doing so
- nerfed plant bonus
- fixed alchemy bonus
- reworked craft tracking for professions
- exo prestiging! need to be maxed in normal experience prestiges first, (no exp reduction for exo prestiging) each level gives configured item reward and increases your damage done slightly and your damage taken moderately based on configured values up to 100 times
- basic prestige leaderboards of top 10, use .prestige lb [PrestigeType] to view
- can now use clap to summon your most recently summoned familiar instead of having to do .fam bind # again
- mod is more or less feature complete now, future updates will focus on refining what's already here

`0.9.16`
- familiar prestiging added (familiar prestiges, max familiar prestiges, familiar prestige stat multiplier added to config), choose extra stat per prestige (use .prfam and it will show stats if none have been entered and familiar is at max level)
- familiars that use holy damage no longer hurt players when relevant
- imprisoning a familiar will destroy the familiar
- added extra recipe toggle under professions; technically no relation to professions yet and no uses atm unless configured as costs in BloodyMerchants or something similar
- ancestral forge crafts now receive extra durability from blacksmithing

`0.9.15`
- changed initialization patch, single player didn't like it (credit to Odjit for the hook~)
- class onHit debuff effects should behave more consistently now (chain lightning from static won't hit the player, skeleton spawned from condemn-afflicted kills now allied, etc)
- classBuffs useable via .scb if gearscore is sufficient and leveling is turned off
- .gbl can be used without entering a blood type and will default to equipped blood type

`0.9.14`
- fixed bug with applying buffs when changing classes
- corrected harvesting bonus for plants
- added class spell school effects on hit, can enable with new config option named as such and accompanying % to proc config. 10% chance per hit to apply class spell school debuff by default, if debuff already present and this is proc'd will do the T08 magic source amulet buff instead (leech -> lesser blood rage for BloodKnight, for reference)
- BloodKnight leech, DemonHunter static, Shadowblade ignite, VampireLord chill, ArcaneSorcerer weaken, DeathMage condemn and the corresponding T08 amulet effects if debuff is already proc'd on target and roll on hit is succesful (except for deathmage, made that an the undead guardian absorb buff since the amulet effect from that one is a spawn instead of a buff and didn't really fit in the code here)
- note that these will not proc on other players for pvp

`0.9.13`
- movement speed correctly applies as a flat stat to all weapons now instead of multiplying the stat if it already existed on the weapon
- RaidMonitor removed and made into standalone mod, alliances are now parties. options/commands have changed accordingly and will need to be configured again. same function of exp share/preventing damage.
- familiar health scaling modified to be more viable in harder difficulty settings (health boost for brutal, will expand on in future updates)
- extended familiar get level command to show stats as well (maxhealth, physical power, spell power)
- buffed resource bonus for wood & minerals from professions

`0.9.12`
- removed .tbc
- list prestige buffs command takes shorter text batches and should no longer get cutoff in chat log
- stat bonuses in .get e correctly formatted based on bonus
- minor bug fixes/optimizations

`0.9.11`
- (hotfix) can view class specific spells/buffs without being the class by entering the class name in command

`0.9.10`
- added command to list stats from classes for synergies
- familiar summon handling improved for more consistent yeeting with death timer
- dominating presence no longer summons familiar when form entered and only dismiss if familiar is present
- can view class specific spells/buffs without being the class by entering the class name in command
- fixed initialization errors on new worlds

`0.9.9`
- added damage config for damage dealt to players by familiars
- familiar summons now properly being yeeted after combat
- various issues related to initialization should be tempered if not resolved
- added set player prestige level/type command for admins
- added set player spell slots command for admins
- added sync prestige buffs command for players, applies missing prestige buffs if applicable
- added config for familiar combat being on (combat mode can be toggled by player) or off (cannot be toggled by player, permanently off)
- added config multiplier for experience from non-hostile units (does not apply to Beatrice the Tailor and Ben the Old Wanderer)
- alliances no longer make player immune to various unintended damage types
- gatebosses give rate gains

`0.9.8`
- trimmed message size for .lcb to prevent exceeding chat limit when translating replies
- added option to control damage familiars deal to VBloods (leave at 1 for no change, set to 0.2 for 20% damage, etc)
- adjusted default values to be somewhat more balanced, added default costs for resetting class/expertise/legacy, won't change existing config settings if you already have them set
- unit category bans for familiars should be more consistently applying if they were not before
- removed synthetic/isolating languages from word replacement translation (Japanese, Koreana, SChinese, TChinese, Thai, Vietnamese)

`0.9.7`
- familiars can't be bound or called when dominating presence is active and will be automatically dismissed if present when activated
- added PreventFriendlyFire option for alliances, enable to prevent friendly fire between alliance members (only affects direct damage, does not prevent debuffs or enable buffs)
- familiars will not do damage to players on PvE servers and will not do damage to alliance members on PvP servers if PreventFriendlyFire is enabled

`0.9.6`
- if leveling is not active, unarmed weapon level will match the highest weapon level you've equipped (cache only, will need to equip a weapon at least once after restarts)
- added handling for removing lingering weapon level on player gearscore if leveling was on and then turned off
- scaffolding for clan-based alliances in and seems to be working but locking that to 'false' in config until I have a bit more time to test it
- PlayerCache now clears before updating, should fix any errors that previously popped up in console
- duration for raid debuff increased to 10 seconds

`0.9.5`
- teleporting now correctly summons familiar again upon second attempt of waygate use so it is with you after traveling
- familiar actives data cleared on server start to prevent issues with being unable to bind/unbind again until using .resetfams

`0.9.4`
- applied localization to all messages in mod as was meant to be done in 0.9.3
- added config option for exp share distance
- groups rebranded to alliances, exp sharing unchanged if leveling is active, for raid monitor the raid instigator's alliance members will be allowed in the territory as will the alliance members of the castle owner (clans of both still included by default)
- fmailiar minion summons should be allied with players now although I haven't quite picked up on when they decide to do this or not which is making it difficult to verify
- added cooldown for VBloodConsumed processing that should take care of multiple gains if more than 1 player is participating in the feed
- Shadow VBloods obtainable

`0.9.3`
- changed handling of VBloodConsumed events to prevent feeds with multiple players providing gains equal to the number of players per player (this one is difficult to test alone, fingers crossed :P)
- messages to players now localized if the base game supports the language (thanks ChatGPT <3 if any of the translations are wrong or offensive it is not intentional, probably missed a word or two here and there as well but pretty happy with this for first pass)
- added command to list prestige buff names
- Anti-raid interference debuff should block healing now to prevent countering with heal pots. If that seems spotty I'll just light people on fire instead~

`0.9.2`
- made sure familiars won't get exp if not in combat or not out and active
- 'binding' key will get added to player_bools file without needing to regenerate_

`0.9.1`
- fixed harvesting bonus
- fixed class spells not all having cooldowns
- vblood unlocks moved from death event hook to vblood hook, should give all players involved in feed a chance at unlocking and prevent random unlocks from VBlood familiars
- added RaidMonitor, see readme for details. Need guinea pigs for this one since it's hard to test raids alone :P

`0.9.0`
- Started keeping changelog
- removed weapon visual expertise tracking thing, was proving too difficult to manage and avoid buggy behavior. added command for players to restore weapon levels to what they should be
- VBloods, summoners and units that transform (not werewolves quite yet but Terah seemed okay) added to unlock pool and should mostly be behaving (including Cassius and his dumb sword). added configurable ban list
- using a waygate with familiar out will dismiss it, still have to use waygate again to teleport until I figure out how to do that in one go
- classes have configurable spells and blood buffs, tried to make okay defaults but C# is turning my brain into mush and decisions are hard so no promises. use .help liberally and read over the new config options as needed until I get the chance to update the readme
- class blood buffs always provide max benefits and are applied at MaxPlayerLevel/#buffs (level 22 if max level is 90 and there are 4 blood buffs for the first, then 44, and so on), can be mixed and matched as you like to create different feels for the classes. The effects from these don't appear to stack with the actual blood buff effects which keeps things somewhat balanced while allowing each class to have a persistent theme.
- class spells are unlocked at configured prestige levels, all classes have veil of shadows by default and the rest are unique to them. configurable but do note that many spells are kinda broken if casted by a player (dracula's blood ring lasts forever, as one example) so you'll want to test before adding/changing. 
