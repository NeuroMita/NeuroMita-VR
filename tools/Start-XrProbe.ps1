param(
    [Parameter(Mandatory=$true)][string]$GameDirectory,
    [string]$GameExecutable = 'NeuroMita.exe',
    [switch]$SessionProbe
)
$ErrorActionPreference = 'Stop'
if ([IO.Path]::GetFileName($GameExecutable) -ne $GameExecutable -or
    [IO.Path]::GetExtension($GameExecutable) -ne '.exe') {
    throw 'GameExecutable must be a file name ending in .exe, without a directory.'
}
$gameRoot = (Resolve-Path -LiteralPath $GameDirectory).Path
$executable = Join-Path $gameRoot $GameExecutable
if (!(Test-Path -LiteralPath $executable -PathType Leaf)) { throw 'Game executable not found.' }
$plugin = Join-Path $gameRoot 'BepInEx/plugins/NeuroMita.XR.Probe/NeuroMita.XR.Probe.dll'
$legacyPlugin = Join-Path $gameRoot 'BepInEx/plugins/NeuroMita.XR.Probe.dll'
if (!(Test-Path -LiteralPath $plugin -PathType Leaf) -and !(Test-Path -LiteralPath $legacyPlugin -PathType Leaf)) {
    throw 'Install the diagnostic package and BepInEx first.'
}
$start = New-Object System.Diagnostics.ProcessStartInfo
$start.FileName = $executable
$start.WorkingDirectory = $gameRoot
$start.UseShellExecute = $false
if ($SessionProbe) {
    $start.EnvironmentVariables['NEUROMITA_XR_SESSION_PROBE'] = '1'
} else {
    $start.EnvironmentVariables.Remove('NEUROMITA_XR_SESSION_PROBE')
}
$process = [Diagnostics.Process]::Start($start)
Write-Output ("Diagnostic player started, PID {0}. Session probe: {1}" -f $process.Id, $SessionProbe.IsPresent)
Write-Output (Join-Path $gameRoot 'BepInEx/LogOutput.log')
Write-Output 'This diagnostic does not enable stereo rendering or gameplay VR.'
