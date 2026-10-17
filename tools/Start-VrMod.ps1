param([Parameter(Mandatory=$true)][string]$GameDirectory)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $GameDirectory).Path
$exe = Join-Path $root 'NeuroMita.exe'
foreach ($relative in @('NeuroMita.exe','BepInEx/plugins/NeuroMitaVRmod/NeuroMitaVRmod.dll','BepInEx/plugins/NeuroMitaVRmod/NeuroMita.VR.Runtime.dll','BepInEx/plugins/NeuroMitaVRmod/neuromita-vr-assets','NeuroMita_Data/Plugins/x86_64/UnityOpenXR.dll')) {
    if (!(Test-Path -LiteralPath (Join-Path $root $relative) -PathType Leaf)) { throw "Required file missing: $relative" }
}
$probes = @(Get-ChildItem -LiteralPath (Join-Path $root 'BepInEx/plugins') -Filter 'NeuroMita.XR.Probe.dll' -Recurse)
if ($probes.Count -gt 0) { throw 'Move the diagnostic XR probe outside BepInEx/plugins before starting gameplay VR.' }
$start = New-Object System.Diagnostics.ProcessStartInfo
$start.FileName = $exe
$start.WorkingDirectory = $root
$start.UseShellExecute = $false
$start.EnvironmentVariables.Remove('NEUROMITA_XR_SESSION_PROBE')
$process = [Diagnostics.Process]::Start($start)
Write-Output ("VR player started, PID {0}." -f $process.Id)
Write-Output (Join-Path $root 'BepInEx/LogOutput.log')