# NeuroMita VR mod

External VR module for NeuroMita. Development and headset testing continue on
`codex/neuromita-vr` in the Unity repository. Portable API commits are applied
from that history to the host later; no main/release integration is done now.

The current game backend is Unity 6000.3.12f1 IL2CPP. The production plugin must
use BepInEx 6 IL2CPP and generated interop assemblies from the matching executable.
Editor Assembly-CSharp.dll is not an IL2CPP interop assembly.

Runtime C# is shared from ../VinerX_NeuroMita-Unity/Assets/Scripts/NeuroMitaScripts/VR.
Host-owned control sessions expose input, camera, locomotion, pointer, presentation
and speech. No Harmony patches of core player behavior are intended.

Status: extraction/build integration is in progress. This repository is not yet
an installable, headset-validated VR release.
