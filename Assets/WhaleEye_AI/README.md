# Whale Eye timed hazard

`Prefabs/WhaleEye.prefab` stays closed and idle until the player enters its child `ActivationTrigger` volume. On the first entry, that trigger disables itself and calls `WhaleEyeAttackCycle.Activate()` once. After `Initial Delay`, the eye opens and begins its repeating blink cycle.

## Setup

1. Place `Prefabs/WhaleEye.prefab` in the scene.
2. Resize and position the child `ActivationTrigger` BoxCollider2D to choose where the player wakes the hazard.
3. On the root `WhaleEyeAttackCycle`, set `Initial Delay` and `Interval Between Blinks`.
4. Adjust `Overlap Offset`, `Overlap Size`, and `Target Layers` for the lethal range. The selected root draws the overlap box in the Scene view. The default target mask is project layer `Player` (layer index 7).

The lethal overlap check runs only while the eye is open. `WhaleEye_Closed` calls `OnEyeClosed` and `WhaleEye_Opening` calls `OnEyeOpened` through animation events. A player with the existing `_02._Script._01_Players.Components.DamageCompo.DamageModule` is killed through `TakeDamage()`, at most once per open phase.

The runtime code is isolated in `WhaleEyeBlink.Runtime` and locates the project's DamageModule by its full type name at runtime, so the new assembly has no compile-time dependency on `Assembly-CSharp`.
