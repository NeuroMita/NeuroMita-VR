param([Parameter(Mandatory=$true)][string]$HostDirectory)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$dotnet = Join-Path $repoRoot '.tools/dotnet/dotnet.exe'
if (!(Test-Path -LiteralPath $dotnet)) { $dotnet = (Get-Command dotnet -ErrorAction Stop).Source }
foreach ($project in @('NeuroMitaVRmod', 'NeuroMita.VR.Runtime', 'NeuroMita.XR.Probe')) {
    & $dotnet build (Join-Path $repoRoot "src/$project/$project.csproj") -c Release --nologo "-p:HostDirectory=$HostDirectory"
    if ($LASTEXITCODE -ne 0) { throw "Build failed: $project" }
}
$audit = Join-Path $repoRoot 'tools/InteropAudit/InteropAudit.csproj'
& $dotnet build $audit -c Release --nologo "-p:HostDirectory=$HostDirectory"
if ($LASTEXITCODE -ne 0) { throw 'Interop audit build failed.' }
foreach ($project in @('NeuroMitaVRmod', 'NeuroMita.VR.Runtime')) {
    $assembly = Join-Path $repoRoot "src/$project/bin/Release/net6.0/$project.dll"
    & $dotnet (Join-Path $repoRoot 'tools/InteropAudit/bin/Release/net8.0/InteropAudit.dll') $assembly $HostDirectory
    if ($LASTEXITCODE -ne 0) { throw "Static interop audit failed: $project" }
}
& $dotnet run --project (Join-Path $repoRoot 'tools/InputPolicyTests/InputPolicyTests.csproj') -c Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'Input policy checks failed.' }
Write-Output 'Static checks passed. Unity, the game and XR were not started. Headset acceptance remains pending.'