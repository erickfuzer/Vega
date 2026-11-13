# Salva um print da tela da VM em prints\<nome>.png e devolve o caminho.
param([Parameter(Mandatory)][string]$Nome, [string]$Vm = 'VegaTeste')
$v = 'C:\Program Files\Oracle\VirtualBox\VBoxManage.exe'
$pasta = Join-Path $PSScriptRoot 'prints'
New-Item -ItemType Directory -Force -Path $pasta | Out-Null
$arquivo = Join-Path $pasta "$Nome.png"
& $v controlvm $Vm screenshotpng $arquivo 2>$null
$arquivo
