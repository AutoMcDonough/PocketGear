<a name="1.1.0"></a>
# [1.10](https://github.com/AutoMcDonough/PocketGear/compare/v1.0.0...v1.1.0) (2018-08-01)


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