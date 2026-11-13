# Digita na VM com teclado ABNT2, com os símbolos que o keyboardputstring erra.
param([Parameter(Mandatory)][AllowEmptyString()][string]$Texto, [switch]$Enter, [string]$Vm = 'VegaTeste')
$v = 'C:\Program Files\Oracle\VirtualBox\VBoxManage.exe'
$especiais = @{
    ':' = @('2a', '35', 'b5', 'aa')
    '\' = @('56', 'd6')
    '/' = @('73', 'f3')
    '*' = @('37', 'b7')
    '>' = @('2a', '34', 'b4', 'aa')
    '%' = @('2a', '06', '86', 'aa')
    '"' = @('2a', '29', 'a9', 'aa')
}
# Na rodada de 2026-11-08, o keyboardputstring perdeu um dígito no WinPE. Números vão por código da tecla.
$digitos = '1234567890'
for ($i = 0; $i -lt 10; $i++) { $c = 0x02 + $i; $especiais[[string]$digitos[$i]] = @(('{0:x2}' -f $c), ('{0:x2}' -f ($c + 0x80))) }
$buffer = ''
foreach ($c in $Texto.ToCharArray()) {
    $k = [string]$c
    if ($especiais.ContainsKey($k)) {
        if ($buffer) { & $v controlvm $Vm keyboardputstring $buffer; $buffer = '' }
        & $v controlvm $Vm keyboardputscancode @($especiais[$k])
    } else {
        $buffer += $k
    }
}
if ($buffer) { & $v controlvm $Vm keyboardputstring $buffer }
if ($Enter) { & $v controlvm $Vm keyboardputscancode 1c 9c }
