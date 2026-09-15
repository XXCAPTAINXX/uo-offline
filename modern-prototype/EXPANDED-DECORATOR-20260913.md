# Expanded Interior Decorator

Existing tools now show rectangular controls for Turn, Up, Down, North, South,
East, West, Lock down, Secure, Release and Get color. Reopening refreshes the
menu, and selecting an action retains native repeated targeting.

Cardinal moves shift individual locked-down or secured furnishings one tile,
preserving their item identity, contents and security. The destination must
remain inside the same house, have a supporting floor within the normal
15-unit decorator height allowance, and pass collision checks. Doors and
addon components cannot be shifted piecemeal. Structural scenery is not
made movable. House co-owner permissions are checked before each new action.
Lock down, Secure and Release invoke native house methods and storage limits.

The existing library's house and floor were detected correctly in its saved
location (4201,2872,7). Its separately published Flipable attribute enables
native Turn. The extended test also verified all four cardinal directions,
content/lockdown preservation, secure and lockdown release, and stranger denial.
Native Raise/Lower worked and would not lower the library through the floor.

Native change is patches/0039-expanded-interior-decorator.patch, paired with
source/HavenDecoratorActions.cs. The patch passed git apply --check against
live before replacement. Isolated and production builds passed. Test saves
were never copied into live; a save, shutdown and backup preceded deployment.
No live client visual review was performed.
