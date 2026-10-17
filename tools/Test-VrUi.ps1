param([Parameter(Mandatory=$true)][string]$RunDirectory)
$ErrorActionPreference='Stop'
$run=(Resolve-Path -LiteralPath $RunDirectory).Path
function Send([string]$command){
 $path=Join-Path $run 'command.txt'
 if(Test-Path -LiteralPath $path){throw 'Previous command still pending.'}
 Set-Content -LiteralPath $path -Value $command
 $until=(Get-Date).AddSeconds(8)
 while((Test-Path -LiteralPath $path) -and (Get-Date) -lt $until){Start-Sleep -Milliseconds 100}
 if(Test-Path -LiteralPath $path){throw 'Command not consumed: '+$command}
}
function State([string]$name){
 Start-Sleep -Milliseconds 1400
 Send 'state'
 Start-Sleep -Milliseconds 400
 $line=Select-String -LiteralPath (Join-Path $run 'vr-trace.log') -Pattern 'UI state' | Select-Object -Last 1
 $script:lastState=$line.Line
 Write-Output ($name+': '+$line.Line)
}
$baselineLength=(Get-Content (Join-Path $run 'vr-trace.log') -Raw).Length
$baselineLength=(Get-Content (Join-Path $run 'vr-trace.log') -Raw).Length
Send 'crazy-house'
$until=(Get-Date).AddSeconds(25)
do{Start-Sleep -Milliseconds 250;$trace=(Get-Content (Join-Path $run 'vr-trace.log') -Raw).Substring($baselineLength)}while($trace -notmatch 'rig attach \| scene=CrazyHouse' -and (Get-Date) -lt $until)
if($trace -notmatch 'rig attach \| scene=CrazyHouse'){throw 'House rig not ready.'}
State 'house baseline'
Send 'pause-test'
State 'pause open'
if($script:lastState -notmatch 'focus=PauseMenuElement,'){throw 'Pause did not open.'}
Send 'pause-test'
State 'pause closed'
if($script:lastState -notmatch 'focus=,'){throw 'Pause did not close.'}
Send 'help-test'
State 'help toggle 1'
if($script:lastState -notmatch 'helpVisible=False'){throw 'Hints did not hide.'}
Send 'help-test'
State 'help toggle 2'
if($script:lastState -notmatch 'helpVisible=True'){throw 'Hints did not show.'}
Send 'radio-test'
State 'radio open'
if($script:lastState -notmatch 'radioOpen=True'){throw 'Radio did not open.'}
Send 'pause-test'
State 'radio back'
if($script:lastState -notmatch 'focus=,.*radioOpen=False'){throw 'Back did not close radio.'}
Send 'capture'
Get-Content (Join-Path $run 'vr-trace.log') -Tail 8
