# Junta um relatório e os logs da instalação num zip.
# Na VM: envia para o receber-logs.js do PC que roda a VM. Num computador de verdade: -Destino com a letra de um pendrive, por exemplo -Destino E:\
param([string]$Destino, [string]$Servidor = 'http://10.0.2.2:5299/')
$ErrorActionPreference = 'Continue'
$pasta = Join-Path $env:TEMP "coleta-$env:COMPUTERNAME"
Remove-Item -LiteralPath $pasta -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $pasta | Out-Null
function Sec([string]$t) { "`r`n===== $t =====" }

& {
    Sec 'SISTEMA'
    Get-CimInstance Win32_OperatingSystem | ForEach-Object { '{0} build {1}' -f $_.Caption, $_.BuildNumber; 'RAM em uso: {0:N0} MB de {1:N0} MB' -f (($_.TotalVisibleMemorySize - $_.FreePhysicalMemory) / 1KB), ($_.TotalVisibleMemorySize / 1KB) }
    Get-CimInstance Win32_ComputerSystem | ForEach-Object { 'Máquina: {0} {1} | nome: {2}' -f $_.Manufacturer, $_.Model, $_.Name }
    'Administrador: ' + ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    'Idioma: ' + (Get-UICulture).Name + ' | Cultura: ' + (Get-Culture).Name + ' | Fuso: ' + (Get-TimeZone).Id
    Get-Volume -DriveLetter C | ForEach-Object { 'Disco C: usado {0:N1} GB de {1:N1} GB' -f (($_.Size - $_.SizeRemaining) / 1GB), ($_.Size / 1GB) }
    'Processos: ' + (Get-Process).Count + ' | Serviços rodando: ' + (Get-Service | Where-Object Status -eq 'Running').Count
    Sec 'DISCO E PARTIÇÕES'
    Get-Partition | ForEach-Object { 'Disco {0} partição {1}: {2} {3:N1} GB letra {4}' -f $_.DiskNumber, $_.PartitionNumber, $_.Type, ($_.Size / 1GB), $_.DriveLetter }
    & reagentc.exe /info
    Sec 'ENERGIA'
    & powercfg.exe /getactivescheme
    & powercfg.exe /list
    & powercfg.exe /a
    Sec 'COMPACTAÇÃO'
    & compact.exe /compactos:query
    Sec 'DEFENDER E VBS'
    Get-MpComputerStatus -ErrorAction SilentlyContinue | Select-Object AMServiceEnabled, AntivirusEnabled, RealTimeProtectionEnabled, IsTamperProtected | Format-List | Out-String
    Get-CimInstance -Namespace root\Microsoft\Windows\DeviceGuard -ClassName Win32_DeviceGuard -ErrorAction SilentlyContinue | ForEach-Object { 'VBS: ' + $_.VirtualizationBasedSecurityStatus + ' | serviços: ' + ($_.SecurityServicesRunning -join ',') }
    Sec 'EDGE'
    foreach ($p in 'C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe', 'C:\Program Files (x86)\Microsoft\EdgeWebView', 'C:\Program Files (x86)\Microsoft\EdgeUpdate', 'C:\Program Files (x86)\Microsoft\EdgeCore') { '{0}: {1}' -f $p, (Test-Path -LiteralPath $p) }
    Get-ChildItem 'C:\Users\Public\Desktop', "$env:USERPROFILE\Desktop", "$env:APPDATA\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar" -Force -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName
    Sec 'SERVIÇOS QUE O PERFIL PODE DESLIGAR'
    foreach ($n in 'DiagTrack', 'dmwappushservice', 'WerSvc', 'SysMain', 'WSearch', 'wuauserv', 'UsoSvc', 'WaaSMedicSvc', 'WinDefend', 'MDCoreSvc', 'SecurityHealthService', 'wscsvc', 'webthreatdefsvc', 'Sense', 'MicrosoftEdgeElevationService', 'edgeupdate', 'edgeupdatem') {
        $s = Get-Service -Name $n -ErrorAction SilentlyContinue
        if ($s) { '{0,-32} {1,-10} {2}' -f $n, $s.Status, $s.StartType } else { '{0,-32} ausente' -f $n }
    }
    Sec 'WINDOWS UPDATE'
    Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings' -ErrorAction SilentlyContinue | Select-Object PauseUpdatesExpiryTime, PauseFeatureUpdatesEndTime, PauseQualityUpdatesEndTime | Format-List | Out-String
    Sec 'PROCESSOS POR MEMÓRIA'
    Get-Process | Sort-Object WorkingSet64 -Descending | Select-Object -First 25 | ForEach-Object { '{0,-32} {1,7:N0} MB' -f $_.ProcessName, ($_.WorkingSet64 / 1MB) }
    Sec 'APPS DO USUÁRIO'
    Get-AppxPackage | Select-Object -ExpandProperty Name | Sort-Object
    Sec 'RECURSOS OPCIONAIS'
    Get-WindowsCapability -Online -ErrorAction SilentlyContinue | Where-Object State -eq 'Installed' | Select-Object -ExpandProperty Name
    Sec 'TAREFAS ATIVAS'
    Get-ScheduledTask | Where-Object State -ne 'Disabled' | ForEach-Object { $_.TaskPath + $_.TaskName } | Sort-Object
    Sec 'SERVIÇOS RODANDO'
    Get-Service | Where-Object Status -eq 'Running' | Select-Object -ExpandProperty Name | Sort-Object
} *>&1 | Out-String -Width 220 | Out-File -LiteralPath (Join-Path $pasta 'relatorio.txt') -Encoding utf8

# Logs: o que não puder ser lido sem administrador fica listado em vez de sumir em silêncio.
$faltou = @()
$logs = Join-Path $pasta 'logs'
New-Item -ItemType Directory -Path $logs | Out-Null
foreach ($origem in 'C:\Windows\Setup\Scripts\*.log', "$env:TEMP\UserOnce.log", 'C:\Windows\Panther\setupact.log', 'C:\Windows\Panther\setuperr.log', 'C:\Windows\Panther\UnattendGC\setupact.log', 'C:\Windows\Panther\UnattendGC\setuperr.log') {
    try { Copy-Item -Path $origem -Destination $logs -ErrorAction Stop } catch { $faltou += "$origem : $($_.Exception.Message)" }
}
foreach ($canal in 'Microsoft-Windows-Shell-Core/Operational', 'Application', 'System') {
    $saida = Join-Path $logs (($canal -replace '[/ ]', '_') + '.evtx')
    & wevtutil.exe epl $canal $saida 2>$null
    if (-not (Test-Path -LiteralPath $saida)) { $faltou += "evento $canal" }
}
$faltou | Out-File -LiteralPath (Join-Path $pasta 'nao-coletado.txt') -Encoding utf8

$zip = Join-Path $env:TEMP "coleta-$env:COMPUTERNAME.zip"
Remove-Item -LiteralPath $zip -Force -ErrorAction SilentlyContinue
Compress-Archive -Path (Join-Path $pasta '*') -DestinationPath $zip
if ($Destino) {
    $final = Join-Path $Destino ("coleta-{0}-{1:yyyyMMdd-HHmm}.zip" -f $env:COMPUTERNAME, (Get-Date))
    Copy-Item -LiteralPath $zip -Destination $final
    "Coleta salva em $final"
} else {
    Invoke-WebRequest -Uri $Servidor -Method Post -InFile $zip -UseBasicParsing | Select-Object -ExpandProperty Content
}
