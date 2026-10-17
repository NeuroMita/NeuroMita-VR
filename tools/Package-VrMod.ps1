param(
    [Parameter(Mandatory=$true)][string]$UnityProject,
    [Parameter(Mandatory=$true)][string]$HostDirectory,
    [string]$GameDataName = 'NeuroMita_Data'
)
$ErrorActionPreference = 'Stop'
if ([IO.Path]::GetFileName($GameDataName) -ne $GameDataName) { throw 'GameDataName must be a directory name.' }
$repoRoot = Split-Path $PSScriptRoot -Parent
$dotnet = Join-Path $repoRoot '.tools/dotnet/dotnet.exe'
if (!(Test-Path -LiteralPath $dotnet)) { $dotnet = (Get-Command dotnet -ErrorAction Stop).Source }
$bundle = Join-Path $UnityProject 'Temp/VrModAssets/neuromita-vr-assets'
if (!(Test-Path -LiteralPath $bundle -PathType Leaf)) { throw 'Build the VR asset bundle through Unity MCP first; see docs/development.md.' }
$packages = @(Get-ChildItem -LiteralPath (Join-Path $UnityProject 'Library/PackageCache') -Directory | Where-Object Name -Like 'com.unity.xr.openxr@*')
if ($packages.Count -ne 1) { throw 'Expected one resolved Unity OpenXR package.' }
$package = $packages[0].FullName
$version = (Get-Content -LiteralPath (Join-Path $package 'package.json') -Raw | ConvertFrom-Json).version
foreach ($project in @('NeuroMitaVRmod','NeuroMita.VR.Runtime')) {
    & $dotnet build (Join-Path $repoRoot "src/$project/$project.csproj") -c Release --nologo "-p:HostDirectory=$HostDirectory" "-p:UnityProjectDir=$UnityProject"
    if ($LASTEXITCODE -ne 0) { throw "Build failed: $project" }
}
$staging = Join-Path $repoRoot ('dist/vr-mod-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
$native = Join-Path $staging "$GameDataName/Plugins/x86_64"
$manifest = Join-Path $staging "$GameDataName/UnitySubsystems/UnityOpenXR"
$plugins = Join-Path $staging 'BepInEx/plugins/NeuroMitaVRmod'
$notices = Join-Path $staging 'Notices/UnityOpenXR'
New-Item -ItemType Directory -Path $native,$manifest,$plugins,$notices -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $package 'Runtime/windows/x64/UnityOpenXR.dll') -Destination $native
Copy-Item -LiteralPath (Join-Path $package 'RuntimeLoaders/windows/x64/openxr_loader.dll') -Destination $native
Copy-Item -LiteralPath (Join-Path $package 'Runtime/UnitySubsystemsManifest.json') -Destination $manifest
Copy-Item -LiteralPath (Join-Path $package 'LICENSE.md'),(Join-Path $package 'Third Party Notices.md') -Destination $notices
foreach ($project in @('NeuroMitaVRmod','NeuroMita.VR.Runtime')) {
    Copy-Item -LiteralPath (Join-Path $repoRoot "src/$project/bin/Release/net6.0/$project.dll") -Destination $plugins
}
Copy-Item -LiteralPath $bundle -Destination $plugins
Copy-Item -LiteralPath (Join-Path $repoRoot 'docs/gameplay-package.md') -Destination (Join-Path $staging 'README.md')
Copy-Item -LiteralPath (Join-Path $repoRoot 'tools/Start-VrMod.ps1') -Destination $staging
Copy-Item -LiteralPath (Join-Path $repoRoot 'LICENSE'),(Join-Path $repoRoot 'THIRD_PARTY_NOTICES.md') -Destination $staging
$files = @(Get-ChildItem -LiteralPath $staging -File -Recurse | ForEach-Object {
    @{ path=$_.FullName.Substring($staging.Length+1).Replace('\','/'); sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
})
$sourceRevision = (& git -C $UnityProject rev-parse HEAD).Trim()
$sourceDirty = [bool](& git -C $UnityProject status --porcelain -- Assets/Scripts/NeuroMitaScripts/VR)
@{ kind='experimental-gameplay-vr'; openXRVersion=$version; fullVrRuntime=$true; headsetAcceptance=$false; unitySourceRevision=$sourceRevision; unitySourceDirty=$sourceDirty; files=$files } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $staging 'package-receipt.json') -Encoding utf8
$archive = "$staging.zip"
Compress-Archive -Path (Join-Path $staging '*') -DestinationPath $archive
Write-Output $archive