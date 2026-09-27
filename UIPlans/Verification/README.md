# Forest UI verification

> Superseded by [the final native UI implementation](../native-ui-final.md). The automatic installer and play verifier described below have been removed; do not follow their old execution instructions.

The latest runtime and editor scripts compile against Unity 6000.6.0f1.

The isolated Unity project under `PreviewProject/` ran `ForestUIInstaller.BuildAssets` successfully on 2026-09-27. Its reports confirm:

- Master volume endpoints and midpoint, neutral brightness.
- Cancel restores the applied settings; Apply retains values when reopening.
- Start, Settings and Exit hover changes fill and border, pointer exit restores both, and disabled buttons do not highlight.
- Main menu, hovered menu and settings render at 1920×1080 and 1920×1200. The generated images were visually inspected.

Actual project integration is a separate check. Refresh Unity to consume the pending one-shot install/test requests. The installer updates the prefabs and MainMenu, then the play verifier opens MainMenu, checks Core initialization, settings callbacks, hover events and Start's first-map transition, and exercises the Exit callback. It restores the original saved scene setup afterward. See `play-tests.txt` for the result; absence of that file means the run has not started.

Physical resolution/fullscreen changes and standalone process termination cannot be confirmed by an Editor Play test. These still require checking a player build. Preview tests deliberately do not modify the user's PlayerPrefs or display mode.

InputReader is unchanged. Gameplay ESC continues using the existing settings panel; the new modal belongs to MainMenu.
