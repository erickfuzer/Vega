# Cria a VM de teste do Vega, com as configurações das rodadas que funcionaram.
# Não precisa de administrador. A placa de rede começa desconectada, para a OOBE não pedir internet.
param([string]$Vm = 'VegaTeste')
$ErrorActionPreference = 'Continue'
$v = 'C:\Program Files\Oracle\VirtualBox\VBoxManage.exe'
if (-not (Test-Path -LiteralPath $v)) { 'VirtualBox não instalado.'; return }
& $v createvm --name $Vm --ostype Windows11_64 --register
& $v modifyvm $Vm --memory 8192 --cpus 6 --firmware efi --tpm-type 2.0 --graphicscontroller vboxsvga --vram 128 --ioapic on --nic1 nat --cableconnected1 off --audio-driver none --usb-xhci off --boot1 dvd --boot2 disk --boot3 none --boot4 none --rtc-use-utc on
& $v storagectl $Vm --name SATA --add sata --controller IntelAhci --portcount 4 --bootable on
& $v showvminfo $Vm --machinereadable | Select-String '^(name|CfgFile|memory|firmware)='
