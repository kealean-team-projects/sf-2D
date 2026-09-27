# Forest Menu Implementation Plan

Goal: Implement the approved full forest menu and settings design using existing UGUI scenes and game flow.

Architecture: A shared UGUI factory builds menu/settings hierarchies; editor tooling exports reusable Resources prefabs. Runtime uses those prefabs when available, with a generated fallback. A settings controller owns preview/commit/cancel and persistence. Existing UIManager and MainMenuButtons connect gameplay without replacing scene files.

Tech stack: Unity 6000.6, UGUI, TMP, Input System, AudioMixer, PlayerPrefs.

- [x] Generate forest backdrop and prepare font assets.
- [x] Add vector UI decoration and responsive menu/settings factory.
- [x] Add settings preview, cancel, apply, display modes and persistence.
- [x] Connect CoreScene UIManager and MainMenuButtons; keep gameplay ESC UI separate and InputReader unchanged.
- [x] Add editor prefab export and focused settings validation.
- [x] Compile against installed Unity assemblies; render verification at two aspect ratios.
- [x] Add visible hover background/border, pressed feedback and disabled-state checks.
- [ ] Run actual MainMenu Play integration checks and inspect the report.
- [ ] Document actual verification and user entry point.

Do not overwrite unrelated existing scene or render pipeline changes. Brightness is a screen color overlay, with 50% neutral. Audio preview uses Master dB conversion. Back/close/ESC discards uncommitted settings. Apply saves and closes. Resolution changes occur only on Apply.
