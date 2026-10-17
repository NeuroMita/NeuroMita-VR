# NeuroMita XR deployment probe — diagnostic only

This archive is NOT a playable VR mod. It does not start stereo rendering, gameplay tracking or controller actions.

Use a separate compatible Windows x64 IL2CPP game copy. The previously tested baseline is host source 46d89dce1, Unity 6000.3.12f1 and OpenXR package 1.16.1; compatibility with other releases is not established.

1. Install BepInEx 6 IL2CPP Windows x64 separately (tested build 6.0.0-be.788+5b766a3): https://docs.bepinex.dev/master/articles/user_guide/installation/unity_il2cpp.html
2. Start the game once, wait for BepInEx interop generation, then close it.
3. Verify package-receipt.json says diagnostic-only and the *_Data folder matches your game.
4. Extract this archive beside the game EXE. Use a clean test copy; do not replace an existing OpenXR installation of unknown version.
5. Start the game normally and inspect BepInEx/LogOutput.log for XR_DESCRIPTOR_PROBE_PASS. This proves subsystem discovery only.

For the optional native session attempt, connect the PCVR headset and confirm SteamVR actually sees it. Select SteamVR as the current OpenXR runtime when testing through SteamVR. From PowerShell in the game directory, run:

    .\Start-XrProbe.ps1 -GameDirectory . -SessionProbe

Pass -GameExecutable 'OtherName.exe' if necessary. The launcher sets NEUROMITA_XR_SESSION_PROBE=1 only for that process; it does not change the system environment. This script launches the installed game, not Unity Editor.

Look for XR_NATIVE_INITIALIZE_RESULT and XR_NATIVE_CREATE_SESSION_RESULT. The probe immediately cleans up its session; even True is not gameplay/headset rendering acceptance. Without the environment variable, only descriptor discovery is checked.

Previously, session creation returned False with XR_ERROR_RUNTIME_FAILURE. The correlated SteamVR client log gave VRInitError_Driver_WirelessHmdNotConnectedAndNoSteam: the wireless headset was disconnected even though SteamVR was running. Other failures require their own logs.

Included: UnityOpenXR.dll, openxr_loader.dll, UnitySubsystemsManifest.json, NeuroMita.XR.Probe.dll, a diagnostic launcher, dependency notices and a SHA256 file receipt.
Not included: the game, BepInEx, generated interop, bootstrap or the full VR runtime.

Close the game before removal. Remove BepInEx/plugins/NeuroMita.XR.Probe and only native files added by this package, using package-receipt.json. Do not remove shared BepInEx or other mods.

Full source and development documentation are separate from this diagnostic archive. Raw local logs are intentionally not distributed.
