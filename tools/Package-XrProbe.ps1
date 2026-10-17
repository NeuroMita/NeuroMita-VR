param(
    [Parameter(Mandatory=$true)][string]$UnityProject,
    [Parameter(Mandatory=$true)][string]$HostDirectory,
    [string]$GameDataName = 'NeuroMita_Data'
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$project = Join-Path $repoRoot 'src/NeuroMita.XR.Probe/NeuroMita.XR.Probe.csproj'
$dotnet = Join-Path $repoRoot '.tools/dotnet/dotnet.exe'
if (!(Test-Path -LiteralPath $dotnet)) { throw 'Local dotnet SDK is required.' }
$packages = @(Get-ChildItem -LiteralPath (Join-Path $UnityProject 'Library/PackageCache') -Directory | Where-Object Name -Like 'com.unity.xr.openxr@*')
if ($packages.Count -ne 1) { throw 'Expected one resolved Unity OpenXR package.' }
$package = $packages[0].FullName
$version = (Get-Content -LiteralPath (Join-Path $package 'package.json') -Raw | ConvertFrom-Json).version
& $dotnet build $project -c Release --nologo "-p:HostDirectory=$HostDirectory"
if ($LASTEXITCODE -ne 0) { throw 'XR probe build failed.' }
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$staging = Join-Path $repoRoot "dist/xr-probe-$stamp"
$native = Join-Path $staging "$GameDataName/Plugins/x86_64"
$manifest = Join-Path $staging "$GameDataName/UnitySubsystems/UnityOpenXR"
$plugins = Join-Path $staging 'BepInEx/plugins/NeuroMita.XR.Probe'
$notices = Join-Path $staging 'Notices'
New-Item -ItemType Directory -Path $native,$manifest,$plugins,$notices -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $package 'Runtime/windows/x64/UnityOpenXR.dll') -Destination $native
Copy-Item -LiteralPath (Join-Path $package 'RuntimeLoaders/windows/x64/openxr_loader.dll') -Destination $native
Copy-Item -LiteralPath (Join-Path $package 'Runtime/UnitySubsystemsManifest.json') -Destination $manifest
Copy-Item -LiteralPath (Join-Path $package 'LICENSE.md'),(Join-Path $package 'Third Party Notices.md') -Destination $notices
Copy-Item -LiteralPath (Join-Path $repoRoot 'src/NeuroMita.XR.Probe/bin/Release/net6.0/NeuroMita.XR.Probe.dll') -Destination $plugins
Copy-Item -LiteralPath (Join-Path $repoRoot 'docs/xr-probe-package.md') -Destination (Join-Path $staging 'README.md')
Copy-Item -LiteralPath (Join-Path $repoRoot 'tools/Start-XrProbe.ps1') -Destination (Join-Path $staging 'Start-XrProbe.ps1')
$files = @(Get-ChildItem -LiteralPath $staging -File -Recurse | ForEach-Object {
    @{ path = $_.FullName.Substring($staging.Length+1).Replace('\','/'); sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
})
@{ kind='diagnostic-only'; openXRVersion=$version; fullVrRuntime=$false; headsetAcceptance=$false; files=$files } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $staging 'package-receipt.json') -Encoding utf8
$archive = "$staging.zip"
Compress-Archive -Path (Join-Path $staging '*') -DestinationPath $archive
Write-Output $archive
