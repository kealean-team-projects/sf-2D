# Native Unity UI — final structure

This supersedes the earlier factory-based implementation plan and verification instructions.

MainMenu contains a saved ForestMainMenu prefab instance. The three Button.onClick events reference the existing MainMenuButtons component in the Inspector. Its menuGroup references the prefab's Menu Foreground CanvasGroup.

CoreScene contains a saved ForestSettings prefab below UIManager. UIManager references that instance directly. There is no runtime UI generation or prefab instantiation. Gameplay ESC still uses the original settingsPanel.

All visual elements use built-in Image, TMP text, Button, Slider, Toggle, TMP_Dropdown, CanvasGroup and layout components. Borders/icons are sprite assets under Assets/Resources/ForestUI/Sprites. Buttons use Sprite Swap for normal, hovered/selected and pressed appearances. Start shares the normal style of Settings and Exit.

Removed runtime scripts: ForestUIFactory, ForestGraphic, ForestButtonFeedback, ForestMenuView. Removed editor scripts: ForestUIInstaller, ForestUIAssets, ForestUIPlayVerification. No automatic install or Play-mode test runs remain in Assets.

ForestSettings handles setting values, preview/apply/cancel and modal behavior. ForestSettingsStore handles PlayerPrefs persistence. These functional scripts remain; visuals can be edited directly in the prefab Inspector without regenerating the hierarchy.

Verification:

- Current runtime code compiles against Unity 6000.6 assemblies.
- Both native prefabs load without missing scripts; Button targets are Images with SpriteSwap sprites.
- Settings preview Cancel and Apply callbacks pass isolated Unity checks.
- MainMenu and Settings rendered at 1920×1080 and 1920×1200; images were visually inspected.
- The saved MainMenu scene was loaded in an isolated project: all three persistent callbacks and the foreground reference resolved. Gameplay dependencies in this scene-deserialization check were test doubles; map transitions were not executed.
- Physical display mode changes and standalone application exit still require a player-build check.

Historical reports and the ignored PreviewProject are verification artifacts, not runtime UI infrastructure.
