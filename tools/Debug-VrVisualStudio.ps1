param(
 [ValidateSet('launch','attach','snapshot','break','continue','detach')][string]$Action='snapshot',
 [string]$GameDirectory,
 [int]$TargetPid=0
)
$ErrorActionPreference='Stop'
if($PSVersionTable.PSEdition -ne 'Desktop' -or ![Environment]::Is64BitProcess){throw 'Run with 64-bit Windows PowerShell 5.1 using -STA.'}
$vswhere=Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
$vs= & $vswhere -latest -products '*' -property installationPath
if(!$vs){throw 'Visual Studio with native and .NET debugging is required.'}
$interop=Join-Path $vs 'Common7/IDE/PublicAssemblies/Microsoft.VisualStudio.Interop.dll'
Add-Type -Path $interop
Add-Type -Path (Join-Path $PSScriptRoot 'VisualStudioControl.cs') -ReferencedAssemblies $interop
$repo=Split-Path $PSScriptRoot -Parent
if($Action -eq 'launch' -or $Action -eq 'attach'){
 if(!$GameDirectory){throw 'Specify the isolated modded game directory.'}
 $root=(Resolve-Path -LiteralPath $GameDirectory).Path
 $exe=Join-Path $root 'NeuroMita.exe'
 if(!(Test-Path -LiteralPath $exe)){throw 'NeuroMita.exe missing.'}
 try{$dte=[Runtime.InteropServices.Marshal]::GetActiveObject('VisualStudio.DTE.17.0')}
 catch{$dte=New-Object -ComObject VisualStudio.DTE.17.0;$dte.UserControl=$true;$dte.MainWindow.Visible=$true}
 if($Action -eq 'launch'){
  if(Get-Process NeuroMita -ErrorAction SilentlyContinue){throw 'Close the current player before launching another debug session.'}
  $run=Join-Path $repo ('artifacts/vr-debug/'+(Get-Date -Format yyyyMMdd-HHmmss))
  New-Item -ItemType Directory -Path $run | Out-Null
  $previous=[Environment]::GetEnvironmentVariable('NEUROMITA_VR_DIAGNOSTICS','Process')
  try{
   [Environment]::SetEnvironmentVariable('NEUROMITA_VR_DIAGNOSTICS',$run,'Process')
   $log=Join-Path $run 'player.log'
   $player=Start-Process -FilePath $exe -WorkingDirectory $root -ArgumentList ('-logFile "'+$log+'" -silent-crashes -screen-fullscreen 0') -PassThru
   $TargetPid=$player.Id
  }finally{[Environment]::SetEnvironmentVariable('NEUROMITA_VR_DIAGNOSTICS',$previous,'Process')}
  Write-Output ('Player '+$TargetPid+'; diagnostics: '+$run)
 }else{
  if(!$TargetPid){throw 'Specify TargetPid for attach.'}
  $player=Get-Process -Id $TargetPid
  if($player.Path -ne $exe){throw 'TargetPid does not belong to the requested game directory.'}
 }
 [VrVsControl]::Run('attach',$TargetPid,(Join-Path $repo 'NeuroMitaVRmod.sln'))
}else{[VrVsControl]::Run($Action,0,$null)}
