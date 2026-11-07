using Schneegans.Unattend;

namespace Vega.Core;

/// <summary>
/// As opções do gerador, em português e organizadas por categoria.
/// Os testes falham se o gerador ganhar uma opção que não está aqui.
/// </summary>
public static class Catalogo
{
    public static IReadOnlyDictionary<Categoria, string> NomesCategorias { get; } = new Dictionary<Categoria, string>
    {
        [Categoria.Instalacao] = "Instalação",
        [Categoria.Conta] = "Conta e rede",
        [Categoria.Apps] = "Apps e recursos",
        [Categoria.Privacidade] = "Privacidade",
        [Categoria.Desempenho] = "Desempenho",
        [Categoria.Seguranca] = "Segurança",
        [Categoria.Interface] = "Interface",
        [Categoria.Edge] = "Edge",
        [Categoria.MaquinaVirtual] = "Máquina virtual",
        [Categoria.Avancado] = "Avançado",
    };

    public static IReadOnlyList<Opcao> Opcoes { get; } =
    [
        // Instalação
        new(nameof(Configuration.BypassNetworkCheck), Categoria.Instalacao, "Permitir instalar sem internet",
            "Libera a opção de concluir a configuração inicial sem conexão.",
            (c, v) => c with { BypassNetworkCheck = v }),
        new(nameof(Configuration.DeleteWindowsOld), Categoria.Instalacao, "Apagar a pasta Windows.old",
            "Remove a cópia da instalação anterior ao terminar.",
            (c, v) => c with { DeleteWindowsOld = v },
            Aviso.Atencao, "Depois disso, não dá para recuperar arquivos da instalação anterior."),
        new(nameof(Configuration.UseNarrator), Categoria.Instalacao, "Usar o Narrador na instalação",
            "Liga o leitor de tela durante a instalação, para quem precisa de acessibilidade.",
            (c, v) => c with { UseNarrator = v }),

        // Conta e rede
        new(nameof(Configuration.EnableRemoteDesktop), Categoria.Conta, "Permitir conexões de Área de Trabalho Remota",
            "Libera o RDP no firewall para acessar este computador por outro. Não é preciso para o AnyDesk.",
            (c, v) => c with { EnableRemoteDesktop = v },
            Aviso.Atencao, "Abre uma porta de acesso. Use com senha forte e fora de redes públicas."),
        new(nameof(Configuration.DisableAutomaticRestartSignOn), Categoria.Conta, "Não entrar sozinho após reiniciar",
            "Impede que o Windows conclua o login por conta própria depois de atualizar.",
            (c, v) => c with { DisableAutomaticRestartSignOn = v }),

        // Privacidade
        new(nameof(Configuration.DisableAppSuggestions), Categoria.Privacidade, "Desligar sugestões de apps",
            "Reduz sugestões, anúncios e apps instalados sem pedir.",
            (c, v) => c with { DisableAppSuggestions = v }),
        new(nameof(Configuration.DisableBingResults), Categoria.Privacidade, "Tirar o Bing da busca do Iniciar",
            "A pesquisa do Iniciar deixa de buscar na web.",
            (c, v) => c with { DisableBingResults = v }),
        new(nameof(Configuration.DisableWidgets), Categoria.Privacidade, "Desligar os Widgets",
            "Tira o painel de notícias e clima da barra de tarefas.",
            (c, v) => c with { DisableWidgets = v }),
        new(nameof(Configuration.PreventDeviceApps), Categoria.Privacidade, "Bloquear apps de dispositivos",
            "Impede que periféricos baixem apps e metadados do fabricante pela rede.",
            (c, v) => c with { PreventDeviceApps = v },
            Aviso.Atencao, "Alguns dispositivos podem ficar sem o app de configuração deles."),
        new(nameof(Configuration.DisableWpbt), Categoria.Privacidade, "Bloquear instaladores embutidos na BIOS",
            "Impede o Windows de executar programas que o fabricante deixa no firmware (WPBT).",
            (c, v) => c with { DisableWpbt = v },
            Aviso.Atencao, "Ferramentas do fabricante que usam esse caminho podem não se instalar."),

        // Desempenho
        new(nameof(Configuration.DisableCoreIsolation), Categoria.Desempenho, "Desligar o isolamento de núcleo (VBS/HVCI)",
            "Costuma melhorar o desempenho, principalmente em processadores mais antigos.",
            (c, v) => c with { DisableCoreIsolation = v },
            Aviso.Risco, "Tira uma proteção do kernel contra drivers e códigos maliciosos."),
        new(nameof(Configuration.DisableFastStartup), Categoria.Desempenho, "Desligar a inicialização rápida",
            "Faz um desligamento completo. Evita problemas com drivers e dual boot.",
            (c, v) => c with { DisableFastStartup = v },
            Custo: "O boot pode ficar alguns segundos mais lento."),
        new(nameof(Configuration.DisableLastAccess), Categoria.Desempenho, "Não registrar o último acesso a arquivos",
            "Reduz escritas no disco desligando esse registro do NTFS.",
            (c, v) => c with { DisableLastAccess = v },
            Custo: "Programas que dependem da data de último acesso perdem essa informação."),
        new(nameof(Configuration.DisableSystemRestore), Categoria.Desempenho, "Desligar a Restauração do Sistema",
            "Economiza espaço em disco ao parar de criar pontos de restauração.",
            (c, v) => c with { DisableSystemRestore = v },
            Aviso.Risco, "Sem pontos de restauração, fica mais difícil desfazer um problema."),
        new(nameof(Configuration.DisablePointerPrecision), Categoria.Desempenho, "Desligar a aceleração do mouse",
            "O ponteiro acompanha o movimento da mão sem aceleração. Muito usado em jogos.",
            (c, v) => c with { DisablePointerPrecision = v }),
        new(nameof(Configuration.TurnOffSystemSounds), Categoria.Desempenho, "Desligar os sons do sistema",
            "Silencia os avisos do Windows e o som de inicialização.",
            (c, v) => c with { TurnOffSystemSounds = v }),

        // Segurança
        new(nameof(Configuration.DisableWindowsUpdate), Categoria.Seguranca, "Pausar o Windows Update",
            "Cria uma tarefa que renova a pausa de 7 dias a cada boot e todo dia, mantendo o Update parado.",
            (c, v) => c with { DisableWindowsUpdate = v },
            Aviso.Risco, "Fica sem correções de segurança e sem drivers automáticos. Em um teste do Vega, a pausa sumiu sozinha em um notebook."),
        new(nameof(Configuration.PreventAutomaticReboot), Categoria.Seguranca, "Não reiniciar sozinho após atualizar",
            "Move as horas ativas a cada 4 horas para o Windows não reiniciar enquanto você usa.",
            (c, v) => c with { PreventAutomaticReboot = v },
            Custo: "Deixa uma tarefa agendada rodando a cada 4 horas."),
        new(nameof(Configuration.DisableSmartScreen), Categoria.Seguranca, "Desligar o SmartScreen",
            "Para de verificar downloads e sites pela Microsoft.",
            (c, v) => c with { DisableSmartScreen = v },
            Aviso.Risco, "Tira um aviso importante contra arquivos e sites maliciosos."),
        new(nameof(Configuration.DisableSac), Categoria.Seguranca, "Desligar o Controle Inteligente de Aplicativos",
            "Desativa o bloqueio de apps não confiáveis (Smart App Control).",
            (c, v) => c with { DisableSac = v },
            Aviso.Atencao, "Depois de desligado, só volta reinstalando o Windows."),
        new(nameof(Configuration.DisableUac), Categoria.Seguranca, "Desligar o Controle de Conta de Usuário (UAC)",
            "Tira os pedidos de permissão de administrador.",
            (c, v) => c with { DisableUac = v },
            Aviso.Risco, "Qualquer programa pode ganhar acesso de administrador sem avisar, e alguns apps deixam de abrir."),
        new(nameof(Configuration.PreventDeviceEncryption), Categoria.Seguranca, "Impedir a criptografia automática do disco",
            "Evita que o BitLocker ligue sozinho e ganha um pouco de desempenho.",
            (c, v) => c with { PreventDeviceEncryption = v },
            Aviso.Atencao, "Em um notebook, quem pegar o disco consegue ler os arquivos."),
        new(nameof(Configuration.HardenSystemDriveAcl), Categoria.Seguranca, "Restringir a raiz do disco C:",
            "Impede usuários comuns de criar arquivos e pastas direto em C:\\.",
            (c, v) => c with { HardenSystemDriveAcl = v },
            Custo: "Programas antigos que gravam em C:\\ podem falhar."),

        // Interface
        new(nameof(Configuration.ClassicContextMenu), Categoria.Interface, "Menu de contexto clássico",
            "O botão direito volta a mostrar o menu completo, sem o \"Mostrar mais opções\".",
            (c, v) => c with { ClassicContextMenu = v }),
        new(nameof(Configuration.LeftTaskbar), Categoria.Interface, "Barra de tarefas à esquerda",
            "Alinha os ícones à esquerda, como no Windows 10.",
            (c, v) => c with { LeftTaskbar = v }),
        new(nameof(Configuration.HideTaskViewButton), Categoria.Interface, "Esconder a Visão de Tarefas",
            "Tira esse botão da barra de tarefas.",
            (c, v) => c with { HideTaskViewButton = v }),
        new(nameof(Configuration.ShowAllTrayIcons), Categoria.Interface, "Mostrar todos os ícones da bandeja",
            "Deixa os ícones sempre visíveis ao lado do relógio.",
            (c, v) => c with { ShowAllTrayIcons = v }),
        new(nameof(Configuration.ShowEndTask), Categoria.Interface, "Finalizar tarefa na barra",
            "Adiciona \"Finalizar tarefa\" ao clicar com o botão direito em um app aberto.",
            (c, v) => c with { ShowEndTask = v }),
        new(nameof(Configuration.ShowFileExtensions), Categoria.Interface, "Mostrar extensões de arquivo",
            "Exibe .exe, .txt e as outras extensões no Explorador.",
            (c, v) => c with { ShowFileExtensions = v }),
        new(nameof(Configuration.LaunchToThisPC), Categoria.Interface, "Abrir o Explorador em Este Computador",
            "O Explorador abre mostrando os discos, em vez da página inicial.",
            (c, v) => c with { LaunchToThisPC = v }),
        new(nameof(Configuration.HideInfoTip), Categoria.Interface, "Esconder dicas ao passar o mouse",
            "Tira as dicas que aparecem sobre arquivos e pastas no Explorador.",
            (c, v) => c with { HideInfoTip = v }),

        // Edge
        new(nameof(Configuration.MakeEdgeUninstallable), Categoria.Edge, "Permitir desinstalar o Edge",
            "Libera o botão de desinstalar nas Configurações. Não remove o Edge sozinho.",
            (c, v) => c with { MakeEdgeUninstallable = v }),
        new(nameof(Configuration.DeleteEdgeDesktopIcon), Categoria.Edge, "Apagar o atalho do Edge",
            "Tira o ícone do Edge da área de trabalho.",
            (c, v) => c with { DeleteEdgeDesktopIcon = v }),
        new(nameof(Configuration.HideEdgeFre), Categoria.Edge, "Pular a apresentação do Edge",
            "Esconde as telas de boas-vindas ao abrir o Edge pela primeira vez.",
            (c, v) => c with { HideEdgeFre = v }),
        new(nameof(Configuration.DisableEdgeStartupBoost), Categoria.Edge, "Desligar a inicialização do Edge com o Windows",
            "Impede o Edge de ficar carregado em segundo plano.",
            (c, v) => c with { DisableEdgeStartupBoost = v }),

        // Máquina virtual
        new(nameof(Configuration.VBoxGuestAdditions), Categoria.MaquinaVirtual, "VirtualBox Guest Additions",
            "Instala as ferramentas do VirtualBox a partir do disco delas, que precisa estar conectado.",
            (c, v) => c with { VBoxGuestAdditions = v }),
        new(nameof(Configuration.VMwareTools), Categoria.MaquinaVirtual, "VMware Tools",
            "Instala as ferramentas do VMware a partir do disco delas, que precisa estar conectado.",
            (c, v) => c with { VMwareTools = v }),
        new(nameof(Configuration.VirtIoGuestTools), Categoria.MaquinaVirtual, "VirtIO Guest Tools",
            "Instala as ferramentas do VirtIO, usadas em QEMU e KVM, a partir do disco delas.",
            (c, v) => c with { VirtIoGuestTools = v }),
        new(nameof(Configuration.ParallelsTools), Categoria.MaquinaVirtual, "Parallels Tools",
            "Instala as ferramentas do Parallels a partir do disco delas.",
            (c, v) => c with { ParallelsTools = v }),

        // Avançado
        new(nameof(Configuration.AllowPowerShellScripts), Categoria.Avancado, "Permitir scripts do PowerShell",
            "Muda a política para RemoteSigned, necessária para rodar seus próprios scripts.",
            (c, v) => c with { AllowPowerShellScripts = v }),
        new(nameof(Configuration.EnableLongPaths), Categoria.Avancado, "Ativar caminhos longos",
            "Libera caminhos com mais de 260 caracteres. Útil para programação e Node.",
            (c, v) => c with { EnableLongPaths = v }),
        new(nameof(Configuration.DeleteJunctions), Categoria.Avancado, "Apagar atalhos antigos do sistema",
            "Remove junções herdadas, como \"Documents and Settings\" e \"Arquivos de Programas\".",
            (c, v) => c with { DeleteJunctions = v },
            Aviso.Atencao, "Programas muito antigos podem depender desses caminhos."),
        new(nameof(Configuration.HidePowerShellWindows), Categoria.Avancado, "Esconder as janelas de script",
            "Os scripts da instalação rodam sem abrir janela do PowerShell.",
            (c, v) => c with { HidePowerShellWindows = v },
            Custo: "Fica mais difícil ver em que etapa a instalação está."),
        new(nameof(Configuration.KeepSensitiveFiles), Categoria.Avancado, "Manter os arquivos de instalação",
            "Não apaga o XML e os logs ao terminar. Útil para investigar problemas.",
            (c, v) => c with { KeepSensitiveFiles = v },
            Aviso.Risco, "O XML pode conter senhas e chaves, e fica guardado no disco."),
    ];

    public static IReadOnlyList<ItemRemovivel> Apps { get; } =
    [
        // IA e busca
        new("RemoveCopilot", "Copilot", "IA e busca"),
        new("RemoveRecall", "Recall", "IA e busca"),
        new("RemoveBingSearch", "Busca do Bing", "IA e busca"),
        new("RemoveCortana", "Cortana", "IA e busca"),

        // Office e comunicação
        new("RemoveOffice365", "Office 365", "Office e comunicação"),
        new("RemoveOneNote", "OneNote", "Office e comunicação"),
        new("RemoveOutlook", "Outlook para Windows", "Office e comunicação"),
        new("RemoveMailCalendar", "Email e Calendário", "Office e comunicação"),
        new("RemoveOneSync", "OneSync", "Office e comunicação", Custo: "Sincroniza email e calendário nos apps da Microsoft."),
        new("RemoveTeams", "Teams", "Office e comunicação"),
        new("RemoveSkype", "Skype", "Office e comunicação"),
        new("RemovePeople", "Pessoas", "Office e comunicação"),
        new("RemoveToDo", "To Do", "Office e comunicação"),
        new("RemoveOneDrive", "OneDrive", "Office e comunicação", Custo: "Para de sincronizar arquivos com a nuvem."),
        new("RemoveYourPhone", "Vincular ao Celular", "Office e comunicação"),

        // Jogos
        new("RemoveXboxApps", "Apps do Xbox", "Jogos", Aviso.Atencao, "Game Pass e alguns jogos da Store dependem deles."),
        new("RemoveGameAssist", "Game Assist", "Jogos"),
        new("RemoveSolitaire", "Coleção Solitaire", "Jogos"),

        // Mídia
        new("RemovePhotos", "Fotos", "Mídia", Aviso.Atencao, "Sem ele, o Windows fica sem visualizador de imagens padrão."),
        new("RemoveCamera", "Câmera", "Mídia", Custo: "A webcam continua funcionando em outros apps."),
        new("RemoveVoiceRecorder", "Gravador de Som", "Mídia"),
        new("RemoveClipchamp", "Clipchamp", "Mídia"),
        new("RemoveZuneMusic", "Media Player (novo)", "Mídia", Custo: "Sem ele, o Windows fica sem player de música padrão."),
        new("RemoveZuneVideo", "Filmes e TV", "Mídia"),
        new("RemoveWindowsMediaPlayer", "Windows Media Player (clássico)", "Mídia"),
        new("RemoveMediaFeatures", "Recursos de mídia", "Mídia", Aviso.Atencao, "Jogos e programas antigos podem depender deles."),
        new("RemovePaint", "Paint", "Mídia"),
        new("RemovePaint3D", "Paint 3D", "Mídia"),
        new("Remove3DViewer", "Visualizador 3D", "Mídia"),

        // Utilitários
        new("RemoveNotepad", "Bloco de Notas (novo)", "Utilitários", Aviso.Atencao, "Sem ele, arquivos de texto ficam sem programa padrão."),
        new("RemoveNotepadClassic", "Bloco de Notas (clássico)", "Utilitários"),
        new("RemoveWordPad", "WordPad", "Utilitários"),
        new("RemoveCalculator", "Calculadora", "Utilitários"),
        new("RemoveClock", "Relógio", "Utilitários"),
        new("RemoveSnippingTool", "Ferramenta de Captura", "Utilitários", Aviso.Atencao, "O atalho Win + Shift + S deixa de funcionar."),
        new("RemoveStickyNotes", "Notas Autoadesivas", "Utilitários"),
        new("RemoveMaps", "Mapas", "Utilitários"),
        new("RemoveNews", "Notícias", "Utilitários"),
        new("RemoveWeather", "Clima", "Utilitários"),
        new("RemoveWallet", "Carteira", "Utilitários"),
        new("RemoveFamily", "Família", "Utilitários"),
        new("RemovePowerAutomate", "Power Automate", "Utilitários"),
        new("RemoveMixedReality", "Realidade Misturada", "Utilitários"),
        new("RemoveGetHelp", "Obter Ajuda", "Utilitários"),
        new("RemoveGetStarted", "Dicas", "Utilitários"),
        new("RemoveFeedbackHub", "Hub de Comentários", "Utilitários"),
        new("RemoveStepsRecorder", "Gravador de Passos", "Utilitários"),
        new("RemoveQuickAssist", "Assistência Rápida", "Utilitários", Custo: "Serve para receber ou dar ajuda remota."),

        // Sistema
        new("RemoveStore", "Microsoft Store", "Sistema", Aviso.Risco, "Apps e codecs da Store ficam difíceis de instalar depois."),
        new("RemoveWindowsHello", "Reconhecimento facial (Windows Hello)", "Sistema", Custo: "Câmeras com reconhecimento facial deixam de desbloquear o Windows."),
        new("RemoveInternetExplorer", "Internet Explorer", "Sistema"),
        new("RemoveMathInputPanel", "Painel de Entrada de Matemática", "Sistema"),
        new("RemoveHandwriting", "Escrita à mão (todos os idiomas)", "Sistema", Custo: "Afeta quem usa caneta."),
        new("RemoveSpeech", "Fala (todos os idiomas)", "Sistema", Custo: "Afeta o ditado por voz."),
        new("RemoveRdpClient", "Conexão de Área de Trabalho Remota", "Sistema", Custo: "Não afeta o AnyDesk."),

        // Desenvolvimento
        new("RemoveDevHome", "Dev Home", "Desenvolvimento"),
        new("RemoveWindowsTerminal", "Terminal do Windows", "Desenvolvimento", Aviso.Atencao, "O PowerShell continua, mas em uma janela mais simples."),
        new("RemoveOpenSSHClient", "Cliente OpenSSH", "Desenvolvimento", Custo: "Remove o comando ssh, usado por Git e servidores."),
        new("RemovePowerShellISE", "PowerShell ISE", "Desenvolvimento"),
        new("RemovePowerShell2", "PowerShell 2.0", "Desenvolvimento"),
    ];

    public static IReadOnlyList<string> GruposApps { get; } = Apps.Select(a => a.Grupo).Distinct().ToList();
}
