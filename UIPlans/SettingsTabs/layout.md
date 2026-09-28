# Settings tabs layout
Target: runtime UGUI, Unity 6000.6.0f1 (ProjectSettings/ProjectVersion.txt).
Selection: selected_object unknown; active_ui_root resolved to ForestSettings.prefab in CoreScene/UIManager.
Mode: existing prefab restructuring. Preserve root/script file IDs and saved native UGUI objects.
No Unity MCP is available; inspect serialized owners and verify via isolated Unity editor renders.
Reference: supplied forest settings image. Composition only; reuse existing panel, leaf, button and slider sprites.
Structure: full-screen input blocker + centered 1100x680 panel, header, three tabs,
one active content page, persistent Cancel/Apply footer.
Colors/fonts: existing forest palette, pale green selection underline, existing TMP fonts.
Rows: audio Master/BGM/SFX; display Brightness/Resolution/Fullscreen.
Controls: A/D movement, Left Shift sprint, C crouch, Space jump, W/S climb, E interact,
A/D+Space wall jump, W+Space wall dash, S+Space detach; Escape settings.
Input evidence: Control.inputactions and ClimbState.HandleJumpInput.
No invented inventory, attack, quality or UI mixer controls.
Reference panel normalization: header top 0-.20, tabs .20-.30, body .30-.82, footer .82-1.
Tab buttons: equal widths, full native Button hit rects; active underlines separate Images.
Content fit: fixed design coords within centered shell, CanvasScaler 1920x1080 match .5.
Verify: 1920x1080 and 1920x1200, all tabs, draft survives tabs, Cancel/Apply, missing references.
