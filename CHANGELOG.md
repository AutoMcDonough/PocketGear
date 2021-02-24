# [1.5.2](https://github.com/AutoMcDonough/PocketGear/compare/v1.5.1...v1.5.2) (2021-02-24)


### Bug Fixes

* fix a potential crash when no mechanical connected subgrids found ([c3f7947](https://github.com/AutoMcDonough/PocketGear/commit/c3f794778863e25a0083e70043a368caf1adcdfa))
* fix deprecation warning ([896edf6](https://github.com/AutoMcDonough/PocketGear/commit/896edf67517bc1d6d3701fcd34bd40bdd3c7ec9e))
* workaround for multiplayer which would not show a new placed pad ([e585c3e](https://github.com/AutoMcDonough/PocketGear/commit/e585c3e0ba13ce16e9e94c52a55c0ba4c4e18c05))



# [1.5.1](https://github.com/AutoMcDonough/PocketGear/compare/v1.5.0...v1.5.1) (2021-02-24)


### Bug Fixes

* fix compatibility issues with `Mechanical Keybinds` ([b3c717f](https://github.com/AutoMcDonough/PocketGear/commit/b3c717f7ba638453ffc49016dcde4d18cad918f7))



# [1.5.0](https://github.com/AutoMcDonough/PocketGear/compare/v1.1.5...v1.5.0) (2019-08-22)


### Bug Fixes

* fix a potential crash when placing a pocket gear ([7f61e4b](https://github.com/AutoMcDonough/PocketGear/commit/7f61e4b))
* fix an issue that applies a deploy velocity change only after a deploy or retract ([06729e8](https://github.com/AutoMcDonough/PocketGear/commit/06729e8))
* fix an issue where pocket gear pads and landing gears have different lock states on 'p' press ([74b3142](https://github.com/AutoMcDonough/PocketGear/commit/74b3142))
* fix an issue where terminal controls would be initialized again ([078be5d](https://github.com/AutoMcDonough/PocketGear/commit/078be5d))
* fix an issue which prevent pocketgear pad's to disable when retracted ([60b7195](https://github.com/AutoMcDonough/PocketGear/commit/60b7195))
* fix an NRE on clients when trying to place a pocketgear ([bc03e76](https://github.com/AutoMcDonough/PocketGear/commit/bc03e76))
* fix terminal controls and visual states in multiplayer  ([602d018](https://github.com/AutoMcDonough/PocketGear/commit/602d018))
* fix the protobuf assembly reference again after SE update ([d395899](https://github.com/AutoMcDonough/PocketGear/commit/d395899))


### Code Refactoring

* refactor terminal control related code for more consistency across projects ([61643a2](https://github.com/AutoMcDonough/PocketGear/commit/61643a2))


### Features

* add English and German block descriptions ([49359e2](https://github.com/AutoMcDonough/PocketGear/commit/49359e2))
* enable auto lock for pocket gears ([7aa5cec](https://github.com/AutoMcDonough/PocketGear/commit/7aa5cec))


### BREAKING CHANGES

* terminal action id's changed. You need to rebind action on your toolbar



<a name="1.1.5"></a>
# [1.1.5](https://github.com/AutoMcDonough/PocketGear/compare/v1.1.4...v1.1.5) (2018-08-03)


### Bug Fixes

* fix a bug that prevents the "Place Pad" button from being activated ([fdb7c97](https://github.com/AutoMcDonough/PocketGear/commit/fdb7c97))
* hide PocketGear Parts in G menu ([6e118f6](https://github.com/AutoMcDonough/PocketGear/commit/6e118f6))
* hide special PocketGear Pads in the G menu ([1175a93](https://github.com/AutoMcDonough/PocketGear/commit/1175a93))



<a name="1.1.4"></a>
# [1.1.4](https://github.com/AutoMcDonough/PocketGear/compare/v1.1.3...v1.1.4) (2018-08-02)


### Bug Fixes

* fix some crashes caused by TerminalControls ([b9694f9](https://github.com/AutoMcDonough/PocketGear/commit/b9694f9))



<a name="1.1.3"></a>
# [1.1.3](https://github.com/AutoMcDonough/PocketGear/compare/v1.1.2...v1.1.3) (2018-08-01)


### Bug Fixes

* used translated values as ids for controls and actions ([b2d5de6](https://github.com/AutoMcDonough/PocketGear/commit/b2d5de6))


### BREAKING CHANGES

* PocketGear actions that were used on the hotbar may need a new binding because I used previously translated ids, Sorry.



<a name="1.1.2"></a>
# [1.1.2](https://github.com/AutoMcDonough/PocketGear/compare/v1.1.1...v1.1.2) (2018-08-01)


### Bug Fixes

* enable the damage handler on clients after settings are sync ([5c5a7ad](https://github.com/AutoMcDonough/PocketGear/commit/5c5a7ad))
* fix an error that prevented PocketGear from being retracted ([fb2b57f](https://github.com/AutoMcDonough/PocketGear/commit/fb2b57f))
* fix an error which sometimes happens on new created blocks ([ca7c169](https://github.com/AutoMcDonough/PocketGear/commit/ca7c169))



<a name="1.1.1"></a>
# [1.1.1](https://github.com/AutoMcDonough/PocketGear/compare/v1.1.0...v1.1.1) (2018-08-01)


### Bug Fixes

* fix an error which prevents the game to save ([9d9092a](https://github.com/AutoMcDonough/PocketGear/commit/9d9092a))



<a name="1.1.0"></a>
# [1.1.0](https://github.com/AutoMcDonough/PocketGear/compare/v1.0.0...v1.1.0) (2018-08-01)


### Bug Fixes

* reduce clang on lock/unlock PocketGears ([611f20f](https://github.com/AutoMcDonough/PocketGear/commit/611f20f))
* reduce rotor resets on grid ship/station convert ([f1da58b](https://github.com/AutoMcDonough/PocketGear/commit/f1da58b))


### Features

* add "Lock Retract" behaviors ([b326d2b](https://github.com/AutoMcDonough/PocketGear/commit/b326d2b))
* add a german translation ([7243405](https://github.com/AutoMcDonough/PocketGear/commit/7243405))
* add a Maglock block ([893d3a7](https://github.com/AutoMcDonough/PocketGear/commit/893d3a7))
* add an auto place functionality for PocketGear Pads ([a852d83](https://github.com/AutoMcDonough/PocketGear/commit/a852d83))
* add Deploy/Retract switch to PG Bases, disable pads if retracted ([17f43c9](https://github.com/AutoMcDonough/PocketGear/commit/17f43c9))
* add manual placable PocketGear Pads ([441472c](https://github.com/AutoMcDonough/PocketGear/commit/441472c))
* allow players to place new pads from the PocketGear Base controls ([b5615a9](https://github.com/AutoMcDonough/PocketGear/commit/b5615a9))
* change voxel damage immunity  to impact damage calculation ([3d1babe](https://github.com/AutoMcDonough/PocketGear/commit/3d1babe))
* enable PocketGear Pad locking on 'P' keypresses ([f3b5bb1](https://github.com/AutoMcDonough/PocketGear/commit/f3b5bb1))
* protect only deployed PocketGears ([de592a9](https://github.com/AutoMcDonough/PocketGear/commit/de592a9))


### BREAKING CHANGES

* Removed Velocity slider and actions on PocketGear Bases
* Removed Revert button and action on PocketGear Bases
* Removed RotorLock ability from PocketGears
* Removed AutoLock ability from PocketGear Pads



<a name="1.0.0"></a>
# 1.0.0 (2018-07-21)

### Release Notes

Added protection from voxel damage! In my tests this worked very nicely for most reasonable things, however if going very fast (expecting it to stop you from 30+mps) the legs have tendency to get stuck in voxels. I expect that I'll have to increase size of collision box.. so more updates in the future.

### Features

* add protection from voxel damage.



<a name="0.9.0"></a>
# 0.9.0 (2018-07-20)

### Release Notes

So, there's some pro's and con's to what I just did. The con obviously being no lock, but I will soon add a long-range magnet of some sort to park the ship itself as if it were direct landing gear, no subgrids. This is the only way to avoid clang.
The pros being no-fuss legs. Simply "Add Rotor Head" if you lose one, or "Detach Rotor Head" If it gets stuck in voxels or some other trouble. I suggest grouping them and putting these in toolbar next to the "Reverse Direction" to deploy them. This way it is easy to cut them loose or re-add them. 

### Features

* increase torque of all parts.
* increase durability of parts by a significant amount. Looking into removing damage from voxels entirely, but the big MP update threw a wrench into that (and Darkstar's entire shield mod).
* make Rotor lock visible again


### Breaking Changes

* remove locking gear pads, the pad is now part of the moving foot.
* remove "P" keybind.
* removed locking, this is now a rotor suspension only.



<a name="0.8.0"></a>
# 0.8.0 (2018-07-18)

### Release Notes

No angles to set, done automatically. (Thank you Digi)
Speed: up to you, I suggest 5rpm or so.
Max torque: Tune this to the mass of your craft, a good setting will act as shock absorber if you have velocity also set to hold the gear out. It will constantly push.

### Features

* add script by Digi to automatically set up limits, P activates.

### Bug Fixes

* fix torque, crafts lift up now!



<a name="0.7.0"></a>
# 0.7.0 (2018-07-17)


### Features

* add "L" variants that are 2x the size.