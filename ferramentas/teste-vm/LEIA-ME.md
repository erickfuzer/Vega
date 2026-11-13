# Teste do Vega sem instalar e em VM

Kit para conferir os XMLs do Vega antes de instalar e para instalar de verdade numa VM do VirtualBox, no Windows. Os resultados das rodadas feitas estão no [AGENTS.md](../../AGENTS.md).

Precisa do .NET 10 SDK, do Node.js e, para a VM, do VirtualBox 7.2 e de uma ISO oficial do Windows 11. Rode tudo dentro desta pasta. `xml/`, `prints/` e `recebidos/` ficam fora do Git.

## Conferir sem instalar

```powershell
dotnet run gerar.cs -- xml
.\conferir.ps1 -Arquivos (Get-ChildItem xml\*.xml).FullName
```

O `gerar.cs` gera os perfis padrão, uso diário e remoção máxima pelo mesmo código do site, cada um em `.xml` e `.iso`. Também gera o `bug-choice`, que mostra o bug do motor, e o `coleta.iso`.

O `conferir.ps1` acusa os erros que já custaram uma instalação:

- sintaxe dos scripts PowerShell embutidos;
- comandos do `RunSynchronous` acima de 259 caracteres;
- `findstr` no WinPE e a desativação do `CreateObjectTask`;
- no modo script, rótulos repetidos, `goto` sem destino e se as linhas de `echo` remontam o `pe.cmd` igual à cópia guardada no XML. Ele não executa nenhuma linha.

## Arquivos

| Arquivo | O que faz |
|---|---|
| `gerar.cs` | Gera os XMLs, as ISOs e o `coleta.iso` em `xml\` |
| `conferir.ps1` | A conferência acima |
| `vm-criar.ps1` | Cria a VM `VegaTeste`: 8 GB, 6 núcleos, EFI, TPM 2.0 e rede desconectada |
| `vm-rodada.ps1` | Disco novo, ISO do Windows e ISO do XML, liga a VM e dá boot pelo DVD |
| `vm-esperar.ps1` | Tira um print por minuto até a tela parar de mudar |
| `vm-print.ps1` | Print da tela da VM em `prints\` |
| `vm-teclas.ps1` | Digita na VM com teclado ABNT2. Números vão por código de tecla |
| `vm-coleta.ps1` | Depois do primeiro login: liga a rede, coloca o `coleta.iso` e roda a coleta |
| `coleta.ps1` | Relatório e logs num zip. Na VM, envia pela rede. Num computador de verdade, grava num pendrive |
| `receber-logs.js` | Recebe o zip da VM em `127.0.0.1:5299` |

## Roteiro na VM

Nenhum passo pede administrador, só a instalação do VirtualBox. A VM enxerga o `127.0.0.1` do PC no endereço `10.0.2.2`, pela rede NAT, e é por ali que a coleta chega.

1. `.\vm-criar.ps1`
2. Deixe rodando: `node receber-logs.js`
3. `.\vm-rodada.ps1 -Perfil maximo -IsoWindows <caminho da ISO>`
4. Acompanhe com `.\vm-esperar.ps1 -Prefixo rodada` e `.\vm-print.ps1 -Nome agora`, e responda com `vm-teclas.ps1` ou `VBoxManage controlvm VegaTeste keyboardputscancode`.
5. Depois do primeiro login: `.\vm-coleta.ps1`. O zip chega em `recebidos\`.
6. Repita com `-Perfil diario`.

Cuidados:

- O VirtualBox não clica por linha de comando. Nas telas gráficas da instalação, use Tab, setas e Enter. Confira a seleção num print antes de apertar Enter.
- O `keyboardputstring` perdeu um dígito no WinPE. Mande números por código de tecla, como o `vm-teclas.ps1` já faz.
- Os espaços que o `vm-rodada.ps1` manda no boot podem cair num link da instalação e abrir uma janela de aviso. Feche com Enter.
- Sem administrador dentro da VM, os logs de `Panther\UnattendGC` não são lidos. A coleta lista o que faltou em `nao-coletado.txt`.
- O `bug-choice` apaga o disco escolhido. Use só na VM: digite o disco e, na pergunta da imagem, aperte Enter em branco para ver se volta para a pergunta do disco.

## Num computador de verdade

Copie o `coleta.ps1` para um pendrive e, depois do primeiro login, rode:

```powershell
powershell -ExecutionPolicy Bypass -File E:\coleta.ps1 -Destino E:\
```

Troque `E` pela letra do pendrive. Confira o que a VM não mostra: plano de energia, tampa, Wi-Fi e touchpad.
