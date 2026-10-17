param([Parameter(Mandatory=$true)][string]$GameDirectory,[string]$ProcDump,[string]$RunDirectory)
$ErrorActionPreference='Stop'
$root=(Resolve-Path -LiteralPath $GameDirectory).Path
$repo=Split-Path $PSScriptRoot -Parent
if(!$ProcDump){$ProcDump=Join-Path $repo '.tools/procdump/procdump64.exe'}
$sig=Get-AuthenticodeSignature -LiteralPath $ProcDump
if($sig.Status -ne 'Valid' -or $sig.SignerCertificate.Subject -notmatch 'O=Microsoft Corporation'){throw 'A valid Microsoft ProcDump executable is required.'}
$exe=Join-Path $root 'NeuroMita.exe'
if(!(Test-Path -LiteralPath $exe)){throw 'Game executable missing.'}
if(Get-Process NeuroMita -ErrorAction SilentlyContinue){throw 'Close the current player before debugging.'}
if(Get-ChildItem -LiteralPath (Join-Path $root 'BepInEx/plugins') -Filter NeuroMita.XR.Probe.dll -Recurse){throw 'Disable the diagnostic-only XR probe first.'}
if(!$RunDirectory){$RunDirectory=Join-Path $repo ('artifacts/vr-debug/'+(Get-Date -Format yyyyMMdd-HHmmss))}
if(Test-Path -LiteralPath $RunDirectory){throw 'RunDirectory must be new.'}
New-Item -ItemType Directory -Path $RunDirectory -Force | Out-Null
$run=(Resolve-Path -LiteralPath $RunDirectory).Path
$dumps=Join-Path $run 'dumps'
$crashes=Join-Path $run 'unity-crashes'
New-Item -ItemType Directory -Path $dumps,$crashes | Out-Null
$bep=Join-Path $root 'BepInEx/LogOutput.log'
if(Test-Path -LiteralPath $bep){Copy-Item -LiteralPath $bep -Destination (Join-Path $run 'previous-bepinex.log')}
$old=Join-Path $env:USERPROFILE 'AppData/LocalLow/NeuroMita Devs/NeuroMita/Player.log'
if(Test-Path -LiteralPath $old){Copy-Item -LiteralPath $old -Destination (Join-Path $run 'previous-player.log')}
$session=@{host=$root;startedUtc=[DateTime]::UtcNow.ToString('O');modRevision=(& git -C $repo rev-parse HEAD).Trim();state='launching';run=$run}
$previousTrace=[Environment]::GetEnvironmentVariable('NEUROMITA_VR_DIAGNOSTICS','Process')
$previousProbe=[Environment]::GetEnvironmentVariable('NEUROMITA_XR_SESSION_PROBE','Process')
try{
 [Environment]::SetEnvironmentVariable('NEUROMITA_VR_DIAGNOSTICS',$run,'Process')
 [Environment]::SetEnvironmentVariable('NEUROMITA_XR_SESSION_PROBE',$null,'Process')
 function Quote-Argument([string]$value){if($value.Contains('"')){throw 'Invalid quote in argument.'};'"'+$value+'"'}
 $arguments=@('-accepteula','-mm','-e','1','-f','C0000005','-t','-n','2','-x',(Quote-Argument $dumps),(Quote-Argument $exe),'-logFile',(Quote-Argument (Join-Path $run 'player.log')),'-silent-crashes','-crash-report-folder',(Quote-Argument $crashes),'-screen-fullscreen','0','-screen-width','1280','-screen-height','720')
 $monitor=Start-Process -FilePath $ProcDump -ArgumentList $arguments -WorkingDirectory $root -WindowStyle Hidden -RedirectStandardOutput (Join-Path $run 'procdump.log') -RedirectStandardError (Join-Path $run 'procdump-error.log') -PassThru
 $session.monitorPid=$monitor.Id
 $session.state='monitoring'
 $session | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $run 'session.json') -Encoding utf8
 Write-Output ('Debug session: '+$run+'; ProcDump PID '+$monitor.Id)
 $monitor.WaitForExit()
 $session.monitorExitCode=$monitor.ExitCode
}finally{
 [Environment]::SetEnvironmentVariable('NEUROMITA_VR_DIAGNOSTICS',$previousTrace,'Process')
 [Environment]::SetEnvironmentVariable('NEUROMITA_XR_SESSION_PROBE',$previousProbe,'Process')
 if(Test-Path -LiteralPath $bep){Copy-Item -LiteralPath $bep -Destination (Join-Path $run 'bepinex.log')}
 $session.finishedUtc=[DateTime]::UtcNow.ToString('O')
 $session.state='finished'
 $session | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $run 'session.json') -Encoding utf8
}