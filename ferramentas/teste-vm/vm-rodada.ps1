# Começa uma rodada: disco vazio, ISO do Windows, ISO do XML, e liga a VM apertando tecla para dar boot pelo DVD.
# Uso: .\vm-rodada.ps1 -Perfil maximo -IsoWindows D:\isos\Win11.iso   (usa xml\maximo.iso, gerado pelo gerar.cs)
# Sem -IsoWindows, usa a variável de ambiente VEGA_ISO_WINDOWS.
param([Parameter(Mandatory)][string]$Perfil, [string]$IsoWindows = $env:VEGA_ISO_WINDOWS, [string]$Vm = 'VegaTeste')
$ErrorActionPreference = 'Continue'
$v = 'C:\Program Files\Oracle\VirtualBox\VBoxManage.exe'
if (-not $IsoWindows -or -not (Test-Path -LiteralPath $IsoWindows)) { 'Informe a ISO do Windows com -IsoWindows ou VEGA_ISO_WINDOWS.'; return }
$windows = $IsoWindows
$xml = Join-Path $PSScriptRoot "xml\$Perfil.iso"
if (-not (Test-Path -LiteralPath $xml)) { "Falta $xml. Rode antes: dotnet run gerar.cs -- xml"; return }

$cfg = ((& $v showvminfo $Vm --machinereadable | Select-String '^CfgFile=').Line -replace '^CfgFile="|"$', '') -replace '\\\\', '\'
$vdi = Join-Path (Split-Path $cfg) "$Vm.vdi"
& $v storageattach $Vm --storagectl SATA --port 0 --device 0 --medium none 2>$null
if (Test-Path -LiteralPath $vdi) { & $v closemedium disk $vdi --delete }
& $v createmedium disk --filename $vdi --size 80000 --format VDI
& $v storageattach $Vm --storagectl SATA --port 0 --device 0 --type hdd --medium $vdi --nonrotational on
& $v storageattach $Vm --storagectl SATA --port 1 --device 0 --type dvddrive --medium $windows
& $v storageattach $Vm --storagectl SATA --port 2 --device 0 --type dvddrive --medium $xml
& $v modifyvm $Vm --cableconnected1 off
& $v startvm $Vm --type headless
# "Pressione qualquer tecla para iniciar do CD": aperta espaço por 10 segundos.
for ($i = 0; $i -lt 10; $i++) { & $v controlvm $Vm keyboardputscancode 39 b9 2>$null | Out-Null; Start-Sleep -Seconds 1 }
"Rodada '$Perfil' ligada. Use vm-print.ps1 para ver a tela e vm-teclas.ps1 para responder."
