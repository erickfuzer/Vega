# Tira um print por minuto até a tela ficar parada por 3 prints seguidos, depois de um tempo mínimo.
# Uso: .\vm-esperar.ps1 -Prefixo m1-espera -MinimoMin 6 -MaximoMin 40
param([Parameter(Mandatory)][string]$Prefixo, [int]$MinimoMin = 5, [int]$MaximoMin = 40, [string]$Vm = 'VegaTeste')
$v = 'C:\Program Files\Oracle\VirtualBox\VBoxManage.exe'
$pasta = Join-Path $PSScriptRoot 'prints'
New-Item -ItemType Directory -Force -Path $pasta | Out-Null
$inicio = Get-Date; $anterior = $null; $iguais = 0; $n = 0
while (((Get-Date) - $inicio).TotalMinutes -lt $MaximoMin) {
    Start-Sleep -Seconds 60
    $n++
    $estado = (& $v showvminfo $Vm --machinereadable 2>$null | Select-String '^VMState=').Line
    if ($estado -notmatch 'running') { "VM parou: $estado"; break }
    $arquivo = Join-Path $pasta ('{0}-{1:00}.png' -f $Prefixo, $n)
    & $v controlvm $Vm screenshotpng $arquivo 2>$null
    $hash = (Get-FileHash -LiteralPath $arquivo -Algorithm MD5).Hash
    if ($hash -eq $anterior) { $iguais++ } else { $iguais = 0 }
    $anterior = $hash
    if ($iguais -ge 2 -and ((Get-Date) - $inicio).TotalMinutes -ge $MinimoMin) { break }
}
'Tela parada: {0} | {1:N1} min | último print: {2}' -f ($iguais -ge 2), ((Get-Date) - $inicio).TotalMinutes, $arquivo
