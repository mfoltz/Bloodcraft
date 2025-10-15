`1.12.17`
- removed `ConsumeBloodDebugEvent` from component registry for compatability with V Rising `v1.1.10.1-r94466-b2` update.
- let's all join hands and pray the ill-timed `Recipes.cs` refactor is fine >_>. (`#yolo`)

`1.12.16`
- minor hotfix to restore emote actions, forgot to remove some debug stuff; sorry about that!

`1.12.15`
- primal rifts are slightly more deadly (and fun!) but dreadhorns are no longer quite as instant of a death either so it might balance out
- added config option for  of primal rift events per day
- added config option for KitFamiliar, unlocks when using starter kit
- various minor bug fixes

`1.11.14`
- added config option for ElitePrimalRifts (units/gateBosses are amped similarly to EliteShardBearers with some surprises ;D WIP feature, use admin console command to spawn primal war events for now)
- added config option for DisabledProfessions, valid options: Enchanting,Alchemy,Harvesting,Blacksmithing,Tailoring,Woodcutting,Mining,Fishing
- added config option for EquipmentOnly (true for equipment slots without inventory, false for both)
- modified config option for Eclipse; will be active by default if any features that can sync with the client are enabled, true for near-realtime client updates (requires Eclipse 1.3.11 to take advantage of this, will be pushed when this is) and false for old behavior.
- PvP prevents calling/enabling familiar for the duration if dismissed during combat or not already present

`1.10.13`
- if shiny chance for familiars is 0 they will no longer be guaranteed on second unlock
- can restrict unarmed to one slot (set Duality in config to false, leave at true for two slots)
- touched up spell mod table for primal jewels (you may roll synergistic effects missing their synergy, I think this is okay given the fusion forge and how rare some modifers would otherwise be)
- realtime closest quest target tracking #SystemBase
- added Daggers to BleedingEdge (stacks will refresh even when unequipped if active)
- familiar servant inventory can now be used as a mule (appreciate ScarletCarrier helping to confirm a linked coffin is required; equipped items will still persist per familiar but the rest of the inventory contents are dropped when servant dies/is destroyed and will likely NOT persist between restarts)
- new visual effect for prestiging stuff, may stick with one for all or change it up later
- fixed autocall for bat landing

`1.9.12`
- prestige properly affects blood legacy rates again
- new spells added to primal jewels from gemcutter, should only consume one perfect gem at a time now as well

`1.9.11`
- previous fix only worked for arenas, added check for the duel buff equivalent (didn't realize there was one buff for each)

`1.9.10`
- corrected base profession experience for woodcutting/mining
- anti-griefing measures skipped for players in active duels/arenas

`1.9.9`
- small hotfix preventing initialization on server hosts, was trying out unicode characters in console oops >_>_

`1.9.8`
- better entity existence checks for quest tracking command
- familiars should retain equipment stats after leveling up
- can no longer equip ancestral weapons/legendaries on familiars as was intended
- quest target querying should be more stable, also filtering some targets again that got lost in refactoring but need to keep an eye on that
- shard bosses should again be elite after restarting without needing to be killed first
- elite drac works again without crashing at portal
- familiar equipment remembers durability (data still good with the compromise of durability starting at 0, they can still use the equipment like servants and it will save going forward)
- replacing invalid familiar equipment that snuck in when a filter wasn't working as it used to with rough equivalents that don't cause issues
- shards will need to be acquired once more for normal behaviour to return, sorry >_>

`1.9.7`
- CHAR_Treant_Corrupted PrefabGuid(1496810447), this will attach mantraps to your character I am not changing it or autobanning it at this time but you may feel free to do so if you're not a fan
- two exoforms to switch between if both unlocked, will use same duration cooldown ('.prestige sf [EvolvedVampire|CorruptedSerpent]'; these are probably unbalanced and should be considered experimental, variety of jank to smooth out after 1.1 but appear generally functional)
- fixed exoform application after taunt if unlocked and enabled and a few other parts that were unintentionally moved around while refactoring
- elixirs and coatings give alchemy experience and elixirs have increased duration from alchemy level, elixirs may also give increased stats but I am not making an effort to show that in Eclipse either way at this time (extra duration on coatings just makes them last forever so leaving that alone for now)
- inventorySlots on newly crafted bags are no longer affected by tailoring, unsure when this started happening; should eliminate any directly related weirdness
- familiar equipment saves when exiting interact mode and not just when unbinding, equipment should no longer be lost on death (that will have an appropriate consequence when I have time to implement it)
- banned gold golems as familiars internally
- tuned kill quest generation for players at max or above

`1.8.6`
- changed stats buff application, hoping will reduce/eliminate the occasional recursive group errors
- fixed some recipe/item modifications that require slightly different handling after 1.1 to work as before, added serpent shard related things to places it was missing from
- corrected name for oakveil fishing stuff (should fix drops issue)
- localization languages for prefab names working again
- fixed scaling for unarmed gearscore with expertise no leveling
- familiar stat buff applied at slight delay when first binding to make sure equipment is ready for processing

`1.8.5`
- touched up README commands/config option autogeneration, should be accurate now
- added bleeding edge details to README
- small change to data creation for new players

`1.8.4`
- minor change after hotfix
- snipped bloodkey gearscore when leveling enabled

`1.8.3`
- removed obsolete check on class changing
- removed class requirement for choosing stats
- fixed synergies reply command
- new weapons and blood should save progress
- elite shard bearers does not modify Dracula for now to avoid portal crashing issues (may need to boss lock/unlock with KindredCommands)
- profession multiplier affects all professions as intended, renaming the config option to reset the value to new default
- unarmed weapon level equivalent matches expertise when leveling is not active
- fixed credit for fishing quests, should no longer be an empty key in chat when viewing details
- familiar max level should no longer clamp to the level below max
- exp share looks much better if not fixed entirely, dare I hope? :fingerscrossed:
- changing harmony priority for more proactive Eclipse message handling
- added oakveil fishing drops table
- level ranges for different types of wood gathering quests
- ensured quest prefab pools and objective generation are functioning as expected
- continuing cleanup of old commands not relevant to current feature set
- will update descriptions and such soon now that I've secured a tiny box's worth of breathing room :p

`1.8.2`
- fixed a few issue with default config generation for class weapon blood stats types caused by mismatched string->enum and using wrong parsing method
- fixed fam unlock buff not having lifetime as it used to

`1.8.1`
- minor versioning to fix workflow snafu

`1.8.0`
- various changes to features and implementations for VRising 1.1 compatibility
- units only possess basic core stats (many stats are now 'VampireSpecificAttributes'); additional stats for prestiging familiars are not currently an option and their existing stats (except health) will continue to scale with prestiges (REWORKING)
- viable options for class and prestige buffs are significantly lessened; replacing class passive buffs with various stat equivalents or some such and reorienting player prestiges, leaving prestige buff config for now but default is empty except for shroud of the forest (REWORKING)
- most attribute bonuses from Expertise, Legacy and other sources require Eclipse to be reflected on the character sheet post 1.1
- WIP >_>

`1.7.7`
- quest progress command replies with time remaining till reset if already completed
- Simon familiar holy rain lasts for 30s again, Dracula should no longer try to return/hide
- changed buff bonus stats hitch a ride on, should prevent stats from being able to stack as was sometimes the case for various reasons
- onyx tears give alchemy profession experience
- Familiar battles under renovation, not hard-disabling incase they work well enough with new group storage for people to find enjoyable but not really expecting that to be the case atm. Needs a lot of refactoring for later plans and ease of testing which is very hard to do with current implementation
- support for storing multiple familiar battle groups, naming, adding, deleting etc.
- fixed drop tables not always being fully cleared for remaining familiars when killed/destroyed on server restarts
- added command to ignore players on prestige leaderboard ('.prestige ignore [Player]', ty Odjit c:)
- added expertise and legacy experience to available SCT options
- smartbind works for primal blood souls ('.fam sb "Primal Frostmaw"', '.fam sb "Primal Polora"', etc.)
- added prestige command with long name for changing config buffs similar to the class version recently added
- experience share without parties, distance/level range are configurable and need to be in combat for credit, can also ban people from being eligible for bad behavior if needed (.lvl ignore [Player]), see further details in config description
- primal echoes cost scaling uses unit level instead of spawn tier buff for better consistency
- default value lines in config are now the correct originals and do not show as whatever they have been changed to
- familiars ignore units with blood quality higher than 90%, prioritize vBlood targets, and somewhat factor in distance of enemy from the player
- active box name shows when using '.fam l'
- preventing damage to charmed units for PvE
- perfect gems worked into quest rewards, can control spell school of rolled primal jewels in gemcutter
- familiar equipment! Beckon (emote) to interact with familiar menu and equip gear, beckon again when finished; equipment is per familiar and will be retained when unbinding (will even retain profession quality bonus :p), stats are applied as shown on gear like they are for the player (they may benefit from magic source unique effect buffs as well, ancestral/legendary weapons are tentatively WIP and unable to be equipped on them right now)
- config option for prestige leaderboard enable/disable
- if leveling system is inactive with quests enabled players get a simulated level based on number of vBloods killed for quest goal generation instead of using gear score
- various bug fixes and optimizations

`1.6.6`
- shinies for golems and robots working again
- reverted changes to familiar collision and should no longer sometimes prevent batform/waygate usage in weird disable loop
- removed some recipes from small grinder that were not supposed to be there
- durability bonus for blacksmithing on legendaries corrected
- modified class change process for consistency in buff removal and application ('.class passives' to toggle buffs off, '.class c [Class]' to change, then '.class sb' to apply)
- added another class command with a long name, now there is one for changing configurations entirely and one for resyncing all existing buffs if needed (best used while server has password under a small maintenance period before then changing class buffs in config then rebooting, can use the global sync after rebooting but still with password or players can just use the personal '.class sb' command)
- small service to keep disabled familiars with their owners instead of where they were last left
- death mage bite stuff removed, you will need to change that buff accordingly if were making use of it
- clearing drop tables more thoroughly to prevent any drops when lingering fams are cleared on server restarts (this may still need touching up for some stickier drops)
- handling for various instances of observed stat doubling
- fixed alchemy experience from blood potions and merlots, scales with quality of prisoner
- familar and player experience scrolling text staggered to prevent overlap and improved origination

`1.6.5`
- tired, check commands list if anything doesn't do what you expect as that's autogenerated and will match whatever is in build
- reduced chances for random seeds per 20 harvesting levels to [2, 4, 6, 8, 10] from [4, 8, 12, 16, 20]
- config option for echoes cost, haven't double checked this yet #testingandtired
- added acceptable ranges for some values (schematics for familiar prestiging, vampiric dust for shinies, cost factor for echoes), details with respective config options
- bounds console spam from Adam fixed
- werewolves behaving
- can use number or class name from '.class l' for '.class s [Class]', no spaces
- battles no longer affected by lifetime error

`1.6.4`
- lockspells moved to wep group ('.wep locksp'), lockshift moved to class group ('.class shift'), choose class changed to select class ('.class s [Class]'), change class adjusted shorthand ('.class c [Class]')
- added config option to allow minion unlocks as familiars, do so at own risk
- command to enable/disable class buffs for players ('.class passives') and global class buff purge for admins when planning on changing configured class buffs which should only be used when players are offline like, launch server with password->run command, wait for autosave->make config changes and save them->boot server without password ('.class iacknowledgethiswillremoveallclassbuffsfromplayersandwantthattohappen')
- Moved exoform under prestige command group, misc commands now under 'misc' command group (moved lockspells to wep group and lockshift to class group)
- Manual aggro handling for familiars now applies for both PvE and PvP
- Added minor effects to binding and unbinding
- Added minor effect when unlocking a new familiar
- Introduced smartbind command ('.fam sb [Name]') to streamline switching and summoning familiars in general, binds to matching familiar and automatically unbinds active familiar if needed
- Familiars should be brought along with players when teleporting via BloodyPoint
- Shiny familiars apply spell school debuffs with a chance per hit, similar to class-based onhit debuffs
- Combat mode for familiars can no longer be toggled while in combat
- Added unused ability (dash) to bear form, see config option
- Reduced delay for Eclipse to begin showing information
- Familiars despawned on logout again instead of just when entering coffin
- Refactored familiar summoning and modifying for more stability and reduced complexity
- SCT added for player experience, similar to existing implementation for familiars
- SCT added for bonus yields from professions when harvesting (general bonus yields and profession specific bonus yields)
- Config option to enable immortal blood during Exo form
- Familiar experience from kills now reduced after prestiging and reduced for unit spawner kills according to the same configured values for players
- Eclipse correctly displays NPC shift spell cooldowns
- Removed free shiny familiar; players can now use vampiric dust to add or change shiny buffs (costs 25% to change)
- Holy potions now only extend duration and no longer increase resist from alchemy
- Professions that enhance durability now provide a 10% equipment stat bonus (this will not show on the tooltip, made an attempt but much more a can of worms than can handle atm)
- Recipe additions and salvage adjustments; best coordinated with Eclipse for accurate UI visuals, inputs and outputs are controlled by the server though
- Exo prestiges and max profession level no longer configurable for various design reasons
- Added werewolf handling for familiars
- NPC spell cooldowns now set more strictly
- Miscellaneous improvements to commands and responses
- Minor adjustments to existing prestige effects
- Reduced the amount of non-standard wood required for gathering quests
- Familiars now display allied player icons on the map
- Replaced CCReduction and ShieldAbsorb with MovementSpeed and CastSpeed for familiar prestige stats
- Adjusted familiar collision
- Any issues related to player bools (kits, weird familiar binding bugs, etc) should be fixed

`1.5.3`
- deprecating '.cleanupfams' and '.wep restore', latter no longer needed former less useful than it used to be and prone to causing crashes
- quest experience rewards overflow to expertise/legacy/familiar depending on which ones are maxed and active when player is at max level (expertise/legacy if both under max, expertise or legacy if one of the two is maxed, then familiar if both are maxed; gains split between expertise/legacy when awarded to both)
- when at max level for experience, expertise and legacy will no longer receive notifications for gains
- familiar battle queuing system! choose a location for the arena center (would recommend closing this off in such a way people can still view) and players can challenge each other to battle, see details in feature section for familiars (WIP)
- added '.prestige shroud' command to toggle perma shroud from prestige if applicable
- prestige buff will no longer cause players to appear on the boosted players list from KindredCommands (if they log out in their coffin, this will also unbind active familiars and recommend using rested XP to encourage this although people are usually pretty good about it already)
- no more >100% blood quality from legacy prestiging, culprit of occasional stat stacking
- modified return buff prefabs such that large amounts of SCT entities are no longer generated as was happening under certain conditions
- deathmage tier 2 passive default changed to MutantBiteBuff,spawns a short-lived fallen angel instead (can't find any direct references to the player who triggers it so had to get creative for allied angel, 100% to spawn on first bite then 0% chance next two bites before back to 100% on fourth, repeat)
- config now handles different locales correctly in regards to decimals (period vs comma)
- excluded holy pots from potion stacking as temporary bandaid for overall problem with holy resist/damage reduction stat interaction
- taking a more delicate approach to some things that were causing crashes for various reasons (familiar destruction and various buff stuff no longer happens on log in or log out)
- '.quest d' and '.quest w' are now under '.quest progress/p d/w', familiar command for choosing/changing shiny buffs is now '.fam shiny [SpellSchool]'
- going forward Bloodcraft will work with older versions of Eclipse from 1.1.2 (usually >_> WIP) onwards (will still need to update Eclipse for newer features)
- added config option to unbind familiars when entering pvp combat
- familiars receive shared experience when player should, refactored death event handling to facilitate this and improve general performance
- parties should work more consistently, previously a failed check allowed players to be in more than one party at once which the code was not designed to handle
- familiar aggression is reset when returned to player from too far away or when calling/dismissing them
- added config to set level for shard bearers if using elite option, can leave at 0 for no change
- can add empty fam boxes, delete empty fam boxes via '.fam ab [BoxName]' and '.fam db [BoxName]' respectively
- added minor visual effects when incrementing various prestiges
- non-clanned familiars tolerable on PvP servers (fam minions targeting unchanged but won't damage pvp protected players) and generally extended handling for preventing negative effects for PvE servers and PvP protected players on PvP servers (also witches can no longer pig VBloods)
- Legacy essence values gained from feed events are now multipled by x3 if consuming the same bloodType and/or then up to x2 if the player has 100% blood quality (this is much slower to level at defaults than was intended)
- Exo rework! Old effects are no longer applicable and are removed from players upon logging in if they have exo data. Instead, if Dracula has been defeated and have at least 1 exo prestige, unlock a powerful new form that will increase in duration and power as you gain exo prestiges! Use '.exoform' to enable taunting to active if requirements met
- Traders no longer provide any progression to players when killed
- Fixed bug with crafting credit for professions/crafting quests, removed crafting credit cooldown, balanced crafting quests and removed highest tiers of equipment from target pool
- Familiars should play nicer with others on PvE servers, have also added handling for some abilities that were ignoring allied status and preventing some debuffs from being applied to players when they are PvP protected
- Using a waygate with a familiar out will dismiss it without player having to use waygate again as is currently the case, after teleporting it will return (only if familiar was present/active first)
- Using batform with a familiar out will dismiss it, after landing it will return (only if familiar was present/active first)
- Removed crystal, thistle and coal from gathering quest prefab pool
- RestedXP adjusted downwards based on the player's new calculated cap instead of being reset entirely after prestiging in experience/leveling
- Can use 'vblood' as the name with familiar search command to return all boxes with at least one vblood or blood soul (ty Odjit c:)
- Profession experience and quest progress now granted to players when using clan/allied stations and not just stations placed by the player
- NPC spell cooldowns for classes again functioning as they were previously, cooldown will be 8 seconds multiplied by the order of the spell in the class spells list
- Added config option to control expertise received from vermin nests, tombs etc (UnitSpawnerExpertiseFactor, set to 0 for none gained leave at 1 for no change)
- Removed auto rerolling quest targets, was trickier to get right than it was worth now that manual rerolling is an option (please let me know of any unit prefabs that result in players being unable to complete a quest and they will be added to filter)
- Target tracking will no longer detect imprisoned units or player familiars
- Checking unit level against player level using unit prefab instead of unit entity in questTarget cache to prevent assigning quest kill targets outside intended level range (added handling for player levels higher than max mod default to prevent repeated fallback target)
- Added logging and handling when configurations for item prefabs/quantities are not as expected instead of failing to initialize entirely
- Changed playerBools data to ConcurrentDictionary instead of Dictionary (hoping this resolves .kitme and other potential playerBool-related issues, also remembered to change the inner dictionary this time <_<)

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
