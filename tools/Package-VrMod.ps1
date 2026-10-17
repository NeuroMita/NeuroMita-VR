param(
    [string]$UnityProject,
    [string]$OpenXRPackage,
    [string]$AssetBundle,
    [Parameter(Mandatory=$true)][string]$HostDirectory,
    [string]$GameDataName = 'NeuroMita_Data'
)
$ErrorActionPreference = 'Stop'
if ([IO.Path]::GetFileName($GameDataName) -ne $GameDataName) { throw 'GameDataName must be a directory name.' }
$repoRoot = Split-Path $PSScriptRoot -Parent
$dotnet = Join-Path $repoRoot '.tools/dotnet/dotnet.exe'
if (!(Test-Path -LiteralPath $dotnet)) { $dotnet = (Get-Command dotnet -ErrorAction Stop).Source }
if (!$AssetBundle -and $UnityProject) { $AssetBundle = Join-Path $UnityProject 'Temp/VrModAssets/neuromita-vr-assets' }
if (!$AssetBundle) { $AssetBundle = Join-Path $repoRoot 'assets/prebuilt/neuromita-vr-assets' }
if (!$AssetBundle -or !(Test-Path -LiteralPath $AssetBundle -PathType Leaf)) { throw 'Pass -AssetBundle for the prebuilt VR bundle. This script never starts Unity.' }
$bundle = (Resolve-Path -LiteralPath $AssetBundle).Path
if (!$OpenXRPackage -and $UnityProject) {
    $packages = @(Get-ChildItem -LiteralPath (Join-Path $UnityProject 'Library/PackageCache') -Directory | Where-Object Name -Like 'com.unity.xr.openxr@*')
    if ($packages.Count -ne 1) { throw 'Expected one resolved Unity OpenXR package.' }
    $OpenXRPackage = $packages[0].FullName
}
if (!$OpenXRPackage) { throw 'Pass -OpenXRPackage for Unity OpenXR 1.16.1 native files and notices.' }
$package = (Resolve-Path -LiteralPath $OpenXRPackage).Path
$version = (Get-Content -LiteralPath (Join-Path $package 'package.json') -Raw | ConvertFrom-Json).version
foreach ($project in @('NeuroMitaVRmod','NeuroMita.VR.Runtime')) {
    & $dotnet build (Join-Path $repoRoot "src/$project/$project.csproj") -c Release --nologo "-p:HostDirectory=$HostDirectory"
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
Copy-Item -LiteralPath (Join-Path $repoRoot 'docs/source-provenance.md') -Destination (Join-Path $staging 'SOURCE_PROVENANCE.md')
Copy-Item -LiteralPath (Join-Path $repoRoot 'README.md'),(Join-Path $repoRoot 'README.en.md') -Destination $staging
$packageDocs = Join-Path $staging 'docs'
New-Item -ItemType Directory -Path $packageDocs -Force | Out-Null
Copy-Item -Path (Join-Path $repoRoot 'docs/*.md') -Destination $packageDocs
$assetDocs = Join-Path $staging 'assets'
New-Item -ItemType Directory -Path $assetDocs -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $repoRoot 'assets/README.md') -Destination $assetDocs
Copy-Item -LiteralPath (Join-Path $repoRoot 'tools/Start-VrMod.ps1') -Destination $staging
Copy-Item -LiteralPath (Join-Path $repoRoot 'LICENSE'),(Join-Path $repoRoot 'THIRD_PARTY_NOTICES.md') -Destination $staging
$files = @(Get-ChildItem -LiteralPath $staging -File -Recurse | ForEach-Object {
    @{ path=$_.FullName.Substring($staging.Length+1).Replace('\','/'); sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
})
$sourceRevision = (& git -C $repoRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Cannot read mod source revision.' }
$sourceDirty = [bool](& git -C $repoRoot status --porcelain -- src tools)
@{ kind='experimental-gameplay-vr'; openXRVersion=$version; fullVrRuntime=$true; headsetAcceptance=$false; modSourceRevision=$sourceRevision; modSourceDirty=$sourceDirty; files=$files } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $staging 'package-receipt.json') -Encoding utf8
$archive = "$staging.zip"
Compress-Archive -Path (Join-Path $staging '*') -DestinationPath $archive
Write-Output $archive