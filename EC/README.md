# EC Examiner Camera

Prototype for an ETS2 1.60 Convoy examiner camera.

## Flow

1. ETS2LA's existing `TrafficProvider` supplies vehicle snapshots.
2. `ExaminerCameraController.Select(id)` selects a Convoy vehicle by its existing vehicle id.
3. `Update()` computes a camera position behind/above the vehicle and a look-at rotation.
4. A native bridge will consume the resulting `CameraTarget` and apply it to ETS2's camera placement.
5. The eventual UI belongs in the existing Convoy page, not in a separate EC window.

## Important boundary

The controller intentionally does not write ETS2 memory. The native plugin bridge must be implemented against the exact ETS2LA/plugin ABI and frame lifecycle before enabling writes.

## Planned UI

The Convoy player list should expose an `Observer` action for each detected player. Starting observation selects that player's vehicle id. Stopping observation clears the target and returns camera control to the game.
