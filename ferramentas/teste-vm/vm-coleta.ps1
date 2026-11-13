# Depois do primeiro login na VM: liga a rede, coloca o coleta.iso no lugar do XML e roda a coleta pelo Executar.
# Antes, deixe rodando: node receber-logs.js
param([string]$Vm = 'VegaTeste', [string]$Letra = 'E')
$v = 'C:\Program Files\Oracle\VirtualBox\VBoxManage.exe'
& $v controlvm $Vm setlinkstate1 on
& $v storageattach $Vm --storagectl SATA --port 2 --device 0 --type dvddrive --medium (Join-Path $PSScriptRoot 'xml\coleta.iso') --forceunmount
Start-Sleep -Seconds 15
& $v controlvm $Vm keyboardputscancode e0 5b 13 93 e0 db   # Win+R
Start-Sleep -Seconds 3
& (Join-Path $PSScriptRoot 'vm-teclas.ps1') -Vm $Vm -Texto "powershell -ExecutionPolicy Bypass -File ${Letra}:\coleta.ps1" -Enter
'Coleta disparada. O zip chega na pasta do receber-logs.js.'
