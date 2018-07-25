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