# Vega: guia para continuar o projeto

Responda em português do Brasil. Este arquivo é para quem continuar o Vega, pessoa ou agente: o que é o projeto, como ele funciona, o que já foi decidido e o que as instalações ensinaram.

## O que é

Site estático no GitHub Pages que gera `autounattend.xml` para instalações limpas do Windows. Ele resolve a falta de organização do gerador de Christoph Schneegans: o Vega é organizado por categoria e mais bonito, e mantém **todas as opções** do gerador original à escolha de quem usa.

- Site: https://erickfuzer.github.io/Vega/
- Visual e README seguem a família da Sirius, outro projeto do mesmo autor: tema grafite, azul de destaque e a pílula de ações embaixo.

## Arquitetura

- `vendor/unattend-generator`: motor original em submódulo, licença MIT. Não edite aqui, para continuar recebendo atualizações. Correções do Vega sobre o XML do motor ficam em `Vega.Core`.
- `src/Vega.Core`: catálogo em português, perfis, `EstadoVega`, `Validacao` (o que o formulário do site original confere), `ImportadorXml`, `Compartilhamento` (link e comentário do XML, sem segredos) e `GeradorIso`. Não depende de Blazor. Toda opção nova entra aqui.
- `src/Vega.Web`: interface em Blazor WebAssembly. `Pages/Home.razor` tem a página, a busca e as seções antigas. `Secoes/` tem as seções novas, e `Componentes/` tem `Campo`, `Interruptor` e `AbrirArquivo`.
- `tests/Vega.Tests`: falha se o motor ganhar opção booleana ou item removível fora do catálogo, e gera XMLs de cada opção e perfil.
- `ferramentas/teste-vm`: kit para conferir XMLs sem instalar e para instalar de verdade numa VM do VirtualBox. Comece pelo `LEIA-ME.md` de lá.

## Comandos

```bash
git clone --recurse-submodules https://github.com/erickfuzer/Vega.git
dotnet run --project src/Vega.Web
dotnet test tests/Vega.Tests
dotnet publish src/Vega.Web/Vega.Web.csproj -c Release -o artifacts/publish
```

O teste que vale é o publicado em Release, porque a redução de tamanho só age nele. Apague a pasta de saída antes de publicar, ou os arquivos antigos entram na medição. Para ver o publicado localmente, troque `<base href="/">` por `/Vega/` no `index.html` da saída e sirva a pasta com o prefixo `/Vega`.

Para comparar com o site original, rode os testes com `VEGA_XML_DO_SITE` apontando para um XML baixado de lá. Sem a variável, esse teste não confere nada. Esse XML é de quem baixou e não entra no repositório.

## Regras do Blazor no navegador

- `UnattendGenerator`, `Vega.Core`, `Newtonsoft.Json` e `System.Collections.Immutable` precisam de `TrimmerRootAssembly`. Sem isso, o gerador e o salvar/abrir configuração quebram só em Release, com `JsonSerializationException`.
- `InvariantGlobalization` está ligado e tira os dados de idioma do .NET.
- O workflow troca `<base href="/">` por `/Vega/` e cria `404.html` e `.nojekyll`.
- O GitHub Pages guarda o `index.html` em cache por até 10 minutos. Depois de publicar, recarregue com Ctrl+F5.
- O `Microsoft.JSInterop` guarda a referência de cada função JS. Trocar `vega.baixar` duas vezes na mesma página, em testes, não funciona: abra outra página.
- No PowerShell 5.1, aspas dentro de `git commit -m` quebram a mensagem. Passe a mensagem por arquivo, com `-F`.

## Estado

Todas as opções do motor e todos os recursos do site original existem no Vega, conferidos campo a campo. "Saída mínima" é o XML do motor sem nenhuma escolha. "Só um usuário local" é uma interpretação, porque os botões do site original enviam um formulário e não foram clicados.

Abrindo no Vega um XML do site original e gerando de novo, o arquivo sai igual, tirando o comentário do topo. Isso foi conferido com uma combinação de opções.

### Teste em máquina virtual

Feito em novembro de 2026, com o kit de `ferramentas/teste-vm`, no VirtualBox 7.2.22 (8 GB, 6 núcleos, EFI, TPM 2.0, rede desconectada até o primeiro login), com o Windows 11 26H2 build 26300.

- **Remoção máxima, modo script:** o `pe.cmd` perguntou o disco, pausou antes de apagar e perguntou a imagem. Enter em branco na pergunta da imagem repetiu essa pergunta, o que confirma `CorrigirRotulosDoPe`. Foi direto para a conta local, sem tela de rede, em cerca de 8 minutos. Resultado: Pro compactado, Alto desempenho, Defender desligado, sem recuperação, apps removidos, Edge removido e WebView2 mantido. Os logs não tiveram erro real.
- **Uso diário, modo normal:** tela de chave (a edição padrão é escolher na hora), lista de edições, tela de discos do Windows e conta local. Resultado: Defender ligado, partição de recuperação, Equilibrado, Update sem pausa, Edge mantido e apps da lista removidos.
- **Achados:**
  - O atalho do Edge fixado na barra fica com ícone genérico (`User Pinned\TaskBar\Microsoft Edge.lnk`). A correção seria apagar esse atalho na remoção e conferir numa nova rodada da Remoção máxima. O mantenedor preferiu deixar assim por enquanto.
  - A pausa do Update só aparece depois do segundo boot, porque a tarefa do motor roda no boot.
  - No modo script, `MDCoreSvc`, `SecurityHealthService` e `wscsvc` continuam rodando com o Defender desligado.
  - "Fixados vazios" ainda deixa Configurações no Iniciar.
  - No modo normal, a instalação do Windows compactou o sistema sozinha, sem pedido no XML.

### O que falta

- Uma instalação num computador de verdade: plano de energia, tampa, Wi-Fi e touchpad, que a VM não mostra.
- Se o mantenedor quiser, rodar o `bug-choice` do kit na VM para ver o bug do motor (abaixo).

### Bug do motor ainda não relatado

O motor repete o rótulo `:choice` no `pe.cmd` quando o disco e a imagem são perguntados na hora (`vendor/unattend-generator/modifier/Disk.cs:490` e `:625`). `EstadoVega.CorrigirRotulosDoPe` contorna isso só no XML do Vega.

**Não abra issue no projeto original sem o OK do mantenedor**, que quer conferir antes como o bug se comporta.

- Como ver: no site original, use `PEMode=Generated`, `TargetDiskMode=Interactive` e `InstallFromMode=Interactive`. O XML sai com `:choice` duas vezes. O `gerar.cs` do kit gera esse XML como `bug-choice`.
- Confirmado com um `.cmd` de teste, sem comandos de disco: Enter em branco na pergunta da imagem volta para a pergunta do disco.
- Só pela leitura do código: depois disso, o `diskpart` rodaria de novo no disco digitado, e Enter em branco na pergunta do disco pularia o particionamento.

### Ideia para depois

Um campo para instalar programas junto com o Windows, como um navegador. Não faça sem o mantenedor pedir.

- Hoje já dá, com um script próprio no primeiro login, por exemplo com `winget`. A ideia é deixar mais fácil.
- O XML não carrega arquivo binário. Ou o programa é baixado no primeiro login, o que precisa de internet, ou o instalador fica no pendrive, ao lado do XML, numa pasta `$OEM$`.

## Decisões

- A escolha de disco segue o padrão do gerador original: sem digitar `APAGAR` e sem recusar disco USB.
- Contra o motor, só corrigir erros vistos em instalações reais, em vez de reescrever o comportamento dele.
- Perfis prontos são pontos de partida. "Uso diário" mantém proteção, atualização e recuperação. "Remoção máxima" é para máquina de teste.
- Não faça commit nem push sem o mantenedor pedir.

## Aprendizados de instalações reais

Windows 11 26H2, build 26300, em VM e num notebook com Ryzen 5 5500U, a partir de XMLs do gerador original e do Vega.

- Rode o que é arriscado no primeiro login, depois da OOBE. Isso vale para tarefas, remoção do Edge, serviços extras e ajustes por usuário.
- Nunca desative `\Microsoft\Windows\CloudExperienceHost\CreateObjectTask`: isso quebra a OOBE.
- O WinPE da instalação não tem `findstr`. Use comandos internos do `cmd`.
- Cada `Path` de `RunSynchronous` aceita até 259 caracteres.
- Edge: `setup.exe --uninstall --system-level --force-uninstall` retorna 532 pela linha de comando. A alternativa é apagar `Program Files (x86)\Microsoft\Edge` e o registro dele, mantendo `EdgeCore`, `EdgeUpdate` e `EdgeWebView`. O atalho fixado na barra não desafixa, mas num notebook sumiu sozinho depois de um tempo.
- A tela de rede da OOBE ignorou teclado e mouse sintéticos em vários testes. Conta local e `SkipWifiSettings` pulam essa tela.
- `SecurityHealthService` e `wscsvc` recusam mudança de `Start` no primeiro login.
- A pausa do Update pelo registro apareceu na VM, mas sumiu num notebook real. Não prometa que ela dura.
- `@( @('a';'b'); @('c';'d') )` achata as listas no PowerShell. Validação de sintaxe não pega isso.
- DISM 10.0.26100 falha com erro 87 em `/Image:` numa imagem 26300.
- Os `desktop.ini` podem aparecer na área de trabalho quando os arquivos do sistema ficam visíveis.
- Wi-Fi: uma senha errada logo depois da instalação deixou o ícone "conectando" sem mostrar erro.
- Ventoy:
  - Uma ISO remontada com IMAPI2, sem El Torito, falha no modo normal e funciona em "Boot in wimboot mode".
  - Com Secure Boot ligado, o primeiro boot em cada máquina pede "Enroll key" ou "Enroll hash".
  - Depois do primeiro reboot da instalação, não escolha a ISO de novo no menu.
- Teste em VM:
  - Use um disco vazio a cada rodada, ou a VM ignora o DVD.
  - Não troque a mídia antes de o WinPE copiar o XML.
  - Logs úteis: `C:\Windows\Setup\Scripts\*.log`, `C:\Windows\Panther\` e `Microsoft-Windows-Shell-Core/Operational`.
