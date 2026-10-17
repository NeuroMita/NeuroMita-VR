# Debugging the external mod

Use an isolated copy of the matching IL2CPP game with BepInEx interop already generated. Start SteamVR and connect the headset first. Debugging uses the built player, not Unity Play Mode.

Build with tools/Test-Static.ps1 and install both mod DLLs and their matching PDB files beside each other in BepInEx/plugins/NeuroMitaVRmod. Do not replace DLLs while a player is running. Pass HostDirectory when building the solution; it is intentionally machine-specific and is not stored in Git.

## Visual Studio mixed debugging

Visual Studio 2022 needs native C++ and .NET debugging installed. Run these commands in 64-bit Windows PowerShell 5.1, with the same Windows user/elevation as Visual Studio and the player:

```powershell
powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File .\tools\Debug-VrVisualStudio.ps1 -Action launch -GameDirectory 'E:\YourIsolatedGame'
powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File .\tools\Debug-VrVisualStudio.ps1 -Action snapshot
powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File .\tools\Debug-VrVisualStudio.ps1 -Action continue
```

The launcher attaches the native and .NET Core engines and enables first-chance break on Win32 access violation (0xC0000005). A visible exception helper is an expected debugger stop: inspect the call stack before continuing. A native access violation can corrupt process state; restart the isolated player after capturing evidence instead of assuming it is safe to resume.

Use `-Action attach -GameDirectory ... -TargetPid ...` to attach to an existing player. `break` pauses the attached process and `detach` releases the debugger. Existing-player attachment does not add diagnostic environment variables retroactively. The utility operates on the active Visual Studio debugger; avoid using it while debugging another application in that instance.

IL2CPP host methods are native code. The exact matching GameAssembly.pdb, if available from the build backup, provides native symbols; ordinary C# source breakpoints on host scripts are not equivalent to managed Unity Editor debugging. Mod PDBs allow the CoreCLR frames to resolve. Release optimization can make locals unavailable.

## Recorded diagnostics

The launch prints a new ignored artifacts/vr-debug/<timestamp> directory. The mod writes flushed phase markers there when NEUROMITA_VR_DIAGNOSTICS is set. An optional command.txt accepts only `capture`, `views`, `state`, `house`, `crazy-house`, `menu`, `stop`, `pause-test`, `help-test`, or `radio-test`; it is consumed on the main thread. `house` uses the host menu transition to KindHouse and requires an active menu. Screenshots are saved in the same directory.

For unattended crash capture, tools/Start-VrDebug.ps1 uses a Microsoft-signed ProcDump executable supplied locally. It records first-chance access violations and termination dumps. Do not use ProcDump and Visual Studio simultaneously on one process. These tools do not register a global postmortem debugger. The launch flag -silent-crashes avoids the Unity crash dialog while leaving exceptions observable to the debugger/dump monitor.

Raw logs and dumps remain ignored: they may contain local paths, game data and process memory. Do not publish them with source code.

## Confirmed house crash

On the 46d89dce host, mixed debugging identified a crash in GCHandle::GetTarget reached from Il2CppSystem.Span<RaycastHit>'s reconstructed constructor, through Physics.BoxCastNonAlloc and VrHandProxy.Sweep. VrPhysicsQueries bypasses that constructor using a pinned blittable RaycastHit buffer and the host's injected native query. This adapter is specific to the generated Unity physics ABI and must be checked again when changing Unity/interop versions. Overlap query buffers are native IL2CPP arrays so results remain available to callers without implicit managed-array copies.

The pause-test/help-test commands inject a single diagnostic edge through the normal published host API; they do not emulate a physical controller. radio-test opens the first scene radio through its host SetUIMode entrypoint. state records the current UI focus, back availability, help visibility and radio open state. Use these only on an isolated test copy; they change gameplay UI. The early PlayerLoop callback is verified by reading the current loop back after installation, including nested IL2CPP value-type writeback.

From the main menu, tools/Test-VrUi.ps1 -RunDirectory <current diagnostic run> performs and checks the CrazyHouse pause/hints/radio smoke. It changes the active scene and UI. It tests synthetic input edges; physical Touch acceptance remains separate.

The views diagnostic command logs all camera and canvas states, active graphics, stereoEnabled and XR render-pass count. It does not reposition or toggle views. A running display with stereo enabled and render passes is pipeline evidence, not proof of the image seen by the headset user.
### Render isolation commands

`eye-state` records near/far clipping, orthographic mode, clear flags/color, culling mask, frame number, stereo view/projection matrices and eye world positions. `render-state` records camera behaviours and comfort fade/vignette values. Both are read-only.

`render-no-effects` disables the current camera's comfort presenter and PostProcessLayer for this run; restart to restore their normal lifecycle. `render-probe` saves the camera's culling mask, clear flags and background color, then requests a solid magenta clear with no geometry. `render-restore` restores those saved values on that same camera. Restore before changing scenes, or restart the player. These commands are diagnostic isolation only, never a permanent graphics fix; camera animation may overwrite values, so inspect eye-state as well as the captured image. A screenshot is not headset acceptance.

`panels` logs UIDocument attachment/layout and Mita interaction panel visibility. `capture-radio` reads the active music panel RenderTexture into a PNG without changing its position. `mita-test` asks the existing interaction service to open the first discovered Mita actor; it obeys host eligibility checks but does not reproduce natural pointer targeting, so it is a developer-only test. Existing `radio-test` likewise opens the first discovered radio and may select a distant device. Debug commands only exist when NEUROMITA_VR_DIAGNOSTICS was supplied at launch.
