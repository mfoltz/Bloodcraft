# 🌠 VCreate
*so uh this might also be a familiar mod now lol, that deserves it's own writeup which I will do *sooooon*

Collection of tools and features for V Rising. Consider this an open-beta; it's mostly stable and ready for stress-testing. If you like what you see and want to support development, my Kofi can be found here: https://ko-fi.com/zfolmt

## Commands

**Notes**:
- 🔒 Requires admin permissions

---

#### 🔒 `.equip`

Toggles equipping extra skills for unarmed on weapon-unequip. Q is required to use most mod features and E was too much fun to get rid of 🤷‍♂️

#### 🔒 `.emotes`

Toggles mode-changing for Q via emoting. Only one mode can be active at a time and changing to one will deactivate the rest (this doesn't apply to snapping, map icons, and tile immortality which will remain as toggled unless 'Wave' is used to reset all modes/toggles).

#### 🔒 `.list`

Lists the modes and toggles you can control with emotes.

#### 🔒 `.perms [Name]`

Controls moving and dismantling structures that were placed with '.twb' per player. This does not apply to objects spawned with Q in TileMode but you can achieve similar functionality by toggling tile immortality via the emote menu (prevents tiles from being destroyed when taking damage if they normally would be, works as expected for most objects but is not perfectly consistent for reasons I have yet to determine).

#### 🔒 `.rot [0/90/180/270]`

Sets rotation of objects placed in TileMode. Can also use Bow to cycle through rotations.

#### 🔒 `.snap [1/2/3]`

Sets level of snapping for objects placed in TileMode if snapping is enabled. 1 is 2.5 grid units, 2 is 5 grid units, and 3 is 7.5 grid units. 

#### 🔒 `.char [PrefabGUID]`

Sets the unit that will be spawned as charmed with CopyMode. Otherwise will be set to last unit inspected with InspectMode.

#### 🔒 `.sb [PrefabGUID]`

Sets the buff you apply to units on hover with Q when in BuffMode.

#### 🔒 `.sd [PrefabGUID]`

Sets the buff you remove from units on hover with Q when in DebuffMode.

#### 🔒 `.map [PrefabGUID]`

Sets the map icon that will be applied to objects spawned in TileMode if map icons are enabled.

#### 🔒 `.tm [PrefabGUID]`

Sets the tile model that will be spawned in TileMode. It will currently let you spawn any set tile model with TileMode but if the object/structure is something already present in the buildmenu I would recommend using '.twb' instead as this is mostly meant for decorations and other props not normally available to the player.

#### 🔒 `.undo`

Destroys last entity placed with TileMode, up to 10. 

#### 🔒 `.destroynodes`

Destroys resources in castle territories. This is intended to cleanup overgrowth that spawns on player castles when enabling '.twb'. Make sure '.twb' is disabled before using; if any undesired resources remain, add time in console to make them growup then run the command again. The server will probably hang for a few seconds while it does this.

#### 🔒 `.dtm [PrefabName] [Radius]`

Destroys objects matching prefab name in radius around the user. This is to remove anything placed with TileMode that can't otherwise be gotten rid of (immortal tiles, tiles you can't hit with your character like a waygate, etc).

#### 🔒 `.twb`

Disables building costs and building placement restrictions globally. Enabling this will cause resources in castle territories to respawn even if a player castle is present, use '.destroynodes' after disabling '.twb' to cleanup. 

#### 🔒 `.tbc`

Disables building costs (also appears to disable recipe costs in general, including castle heart upgrades) globally. Use this instead of '.twb' for building in your territory as structures placed while '.twb' is enabled will not be tied to your castle heart.

#### 🔒 `.tchc`

Disables castle heart connection requirement for structures that normally require it. Mostly for testing/debugging, wouldn't recommend using unless you have a specific reason for doing so but should be generally safe to toggle.

#### 🔒 `.deus`

Makes user invulnerable. Can be removed with DebuffMode (using '.deus' will automatically set the appropriate buff to be removed in DebuffMode, if you change the set debuff to something else while '.deus' is activated you can use the command again to set the correct buff to be removed).

#### 🔒 `.ul [Player]`

Unlocks VBloods and research for named player.

#### 🔒 `.bm [Type] [Quantity] [Quality]`

Provides a blood merlot as ordered.

#### `.!`

Displays ping.

## Modes & Toggles

**Notes**:
- 🔒 Requires admin permissions

---

#### 🔒 `TileMode` || Beckon

Spawns set tile model object at cursor with applied settings if applicable (snapping, rotation, map icon, immortal).

#### 🔒 `ConvertMode` || Clap

Converts the target to your team, it will follow and fight until death. Can't be used on vampires.

#### 🔒 `InspectMode` || Point

Shows buffs and prefab of target in chat, logs components to console, and sets inspected unit as next to spawn with CopyMode (unless inspecting a vampire).

#### 🔒 `DestroyMode` || No

Destroys the target. Can't be used on vampires.

#### 🔒 `CopyMode` || Salute

Spawns last unit inspected or set via command as charmed.

#### 🔒 `BuffMode` || Sit

Buffs target with last buff set via command (buff and debuff are set separately).

#### 🔒 `DebuffMode` || Taunt

Removes last buff set from target (buff and debuff are set separately).

#### 🔒 `ToggleRotation` || Bow

Cycles rotation for objects placed with TileMode.

#### 🔒 `ToggleSnapping` || Yes

Toggles grid snapping for objects placed with TileMode.

#### 🔒 `ToggleMapIcons` || Surrender

Toggles map icons for objects placed with TileMode if you have one set.

#### 🔒 `ToggleImmortalTiles` || Shrug

Toggles immortality for objects placed with TileMode, generally prevents them from being messed with but doesn't work for everything equally.

#### 🔒 `Reset` || Wave

Resets modes and toggles.

## Other

Borrowed the README format from LeadAHorseToWater (https://github.com/decaprime/LeadAHorseToWater). Code examples from decaprime, willis, cheesasaurus, trodi, backxtar, and other members of the V Rising modding community were all very helpful in putting this together. https://discord.com/invite/QG2FmueAG9
