# NeuroMita VR

[Русский](README.md) · [English](README.en.md)

An experimental external VR mod for [NeuroMita](https://github.com/VinerX/NeuroMita), using Windows x64, Unity IL2CPP, BepInEx 6 and OpenXR. It runs on top of a built game; playing does not require a separate VR game build or Unity Editor.

**This mod is under active development. It is not a complete VR edition with full gameplay support.** You can enter the main menu and an apartment, but several interactions and interfaces still need adaptation. Contributions and further development are welcome.

## Current state

Checked on October 17, 2026:

| Feature | Status |
|---|---|
| Unmodified public NeuroMita v2026.10.15 host + external mod | Main menu and apartment display and update in the headset; confirmed by the user |
| OpenXR and controllers | Session and tracking start; movement and item pickup were previously confirmed on a separate validation host |
| Pause menu and VR Settings | Implemented; panels were captured on the validation host. Full settings acceptance is still pending |
| Mita action panel and HUD | Fixes for UI layers, IL2CPP geometry and head anchoring are being validated |
| Music player | The complete interface renders into a texture; VR placement, visibility and interaction are still being improved |
| TV / YouTube | Full browser projection and VR text entry are implemented; headset acceptance of search, settings and exit is pending |
| QTEs, Mita grabs, resting, bat and other mechanics | Incomplete VR support; some scenarios may still need a keyboard or be awkward/unavailable |

Working menu and apartment rendering does not mean the entire game is playable with controllers. See the [interaction audit and acceptance checklist](docs/vr-interaction-audit.md) and [technical checkpoint history](docs/checkpoint.md); these detailed documents currently use Russian and/or English.

October 18 UI batch: Mita action buttons are anchored to her body, with stats above her head during interaction. Panel obstacle checks, projected easel tools and Russian VR settings labels are implemented but still await headset acceptance. A follow-up batch implements larger easel tools, ray-based drawing and the native console command catalog; these still await headset acceptance. Integration into the standard settings page remains unfinished. Full TV browser projection and Toolkit text entry are also awaiting acceptance.

## Compatibility

- Windows x64, IL2CPP, Unity **6000.3.12f1**.
- Tested public host: internal game version **v2026.10.15**. The download directory name may differ from the game's internal version.
- Tested BepInEx: **6.0.0-be.788**, Unity IL2CPP x64; Il2CppInterop **1.5.3**.
- Native Unity OpenXR **1.16.1** files and SteamVR as the active OpenXR runtime.
- Current controller profile: **Oculus Touch**. Full support for other controllers has not been confirmed.

## Install and launch

1. Use a separate copy of a compatible public game release.
2. Install BepInEx 6 **Unity IL2CPP x64**. Launch once to generate `BepInEx/interop`, then close the game. Do not copy interop from a different game build.
3. Extract the **gameplay** `vr-mod-*.zip` package into the directory containing `NeuroMita.exe`, preserving its folder structure. If you do not have a package, build one using the [development instructions](docs/development.md). A source ZIP of this repository is not an installable mod package.
4. Remove `NeuroMita.XR.Probe.dll` from `BepInEx/plugins` if previously installed. The diagnostic probe must not run alongside the gameplay VR mod.
5. Check `NeuroMita_Data/boot.config`: the tested public host needs `xrsdk-pre-init-library=UnityOpenXR`. If absent, back up the file and add that line once; preserve all other settings. The current packaging script does not add the line automatically.
6. Connect your headset to the PC, start SteamVR and confirm that the headset and controllers are available. Select SteamVR as the OpenXR runtime.
7. Launch `NeuroMita.exe`. Alternatively, run `Start-VrMod.ps1 -GameDirectory 'path to game'` from the gameplay package. VR is enabled by default; runtime startup waits for the menu or player to be ready.

The game, BepInEx and generated interop are not included in the mod package. File layout and further details: [gameplay package guide](docs/gameplay-package.md). The [XR probe](docs/installation.md) is separate diagnostics, not a gameplay mod.

## Touch controls

| Control | Action |
|---|---|
| Left stick | Move |
| Right stick | Turn |
| Right trigger | Ray pointer: click / drag UI |
| Right grip | Interact |
| Right A | Jump / submit chat in the appropriate context |
| Right B | Action menu |
| Left X | Tap: chat on release; hold 3 s: console command catalog |
| Left Y | Pause / back; exit TV minigames |
| Left trigger | Inventory |
| Left grip | Push-to-talk |
| Right stick click | In game: hints; in main menu: recenter on Mita and the menu |
| Hold both stick clicks | Recenter |

Open **VR Settings** in the pause menu to adjust preferences; settings are saved in `BepInEx/config`. This is the current mapping, not a claim that every action and context has passed headset acceptance. Some scenarios still show keyboard prompts.

## Disable and troubleshoot

- Launch `NeuroMita.exe --no-vr` for desktop mode, or set `General / Enabled = false` in the plugin configuration while the game is closed.
- To remove the gameplay plugin, close the game and remove `BepInEx/plugins/NeuroMitaVRmod`. Other mods may use shared BepInEx/OpenXR files; restore those from your backup and the package file list as appropriate.
- For a bug report, include game/mod versions, headset, PCVR connection method and reproduction steps. Logs: `BepInEx/LogOutput.log` and Unity `Player.log`. Review logs for personal data and local paths before posting.
- An `OpenXR active` log message proves a startup stage, not correct headset output or gameplay controls. See [debugging tools](docs/debugging.md).

## Development and contributions

The gameplay runtime lives here in `src/NeuroMita.VR.Runtime`; the bootstrap is in `src/NeuroMitaVRmod`. DLLs build without Unity Editor, against the matching host interop. Development and testing target the external mod. The [Unity VR branch](https://github.com/VinerX/NeuroMita-Unity/tree/codex/neuromita-vr) remains a reference implementation, not a required DLL build dependency.

- [Build, IL2CPP audit and packaging](docs/development.md).
- [Resource sources and included AssetBundle](assets/README.md). Editor is needed when changing/rebuilding resources.
- [Source provenance](docs/source-provenance.md) and [third-party notices](THIRD_PARTY_NOTICES.md).

Small contributions focused on one scenario are useful. State the host version, static checks, actual headset checks and remaining limitations. Do not commit game DLLs, interop, caches, local configuration, logs or dumps.

## License

The [NeuroMita license](LICENSE) applies, retaining the copyright notice and link to the [canonical project](https://github.com/VinerX/NeuroMita). It is not MIT. Third-party components and assets retain their own terms; the code license does not grant rights to MiSide materials.
