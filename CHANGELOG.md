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