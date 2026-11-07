using System.Text;
using System.Xml;
using Schneegans.Unattend;
using Vega.Core;

namespace Vega.Tests;

/// <summary>Compara o Vega com o gerador original e cobre o que foi corrigido nessa comparação.</summary>
public class ParidadeTests
{
    private static readonly UnattendGenerator Gerador = new();

    /// <summary>Todos os campos do formulário do site original, tirados da página em 2026-11-06.</summary>
    private const string CamposDoSite = """
        LanguageMode UILanguage Locale Keyboard UseKeyboard2 Locale2 Keyboard2 UseKeyboard3 Locale3 Keyboard3 GeoLocation PEMode
        WindowsEditionMode WindowsEdition ProductKey BypassRequirementsCheck UseConfigurationSet Disable8Dot3Names DisableDefender
        PauseBeforeFormatting PauseBeforeReboot CompactOs SkipIntegrityCheck InstallFromMode InstallFromEdition InstallFromName InstallFromIndex
        PartitionMode TargetDiskMode TargetDiskNoPartitions TargetDiskInterfaceType TargetDiskMediaType TargetDiskSize TargetDiskMinSize TargetDiskMaxSize
        TargetDiskIndex TargetDisk TargetDiskScript PartitionLayout SystemSize RecoveryMode RecoverySize DiskpartScript PagingFileMode InitPagingFileSize
        MaxPagingFileSize PEScript UseActivationKey ActivationKey ProcessorArchitecture BypassNetworkCheck HidePowerShellWindows KeepSensitiveFiles UseNarrator
        ComputerNameMode ComputerName ComputerNameScript TimeZoneMode TimeZone UserAccountMode AccountName0 AccountDisplayName0 AccountPassword0 AccountGroup0
        AccountName14 AccountDisplayName14 AccountPassword14 AccountGroup14 AutoLogonMode BuiltinAdministratorPassword ObscurePasswords PasswordExpirationMode
        PasswordMaxAge LockoutMode LockoutThreshold LockoutWindow LockoutDuration HideFiles ShowFileExtensions ClassicContextMenu HideInfoTip LaunchToThisPC
        ShowEndTask TaskbarSearch TaskbarIconsMode TaskbarIconsXml DisableWidgets LeftTaskbar HideTaskViewButton ShowAllTrayIcons DisableBingResults
        StartTilesMode StartTilesXml StartPinsMode StartPinsJson DisableWindowsUpdate DisableUac DisableSac DisableSmartScreen DisableFastStartup
        DisableSystemRestore EnableLongPaths EnableRemoteDesktop HardenSystemDriveAcl DeleteJunctions AllowPowerShellScripts DisableLastAccess
        PreventAutomaticReboot TurnOffSystemSounds DisableAppSuggestions PreventDeviceEncryption HideEdgeFre DisableEdgeStartupBoost MakeEdgeUninstallable
        DisablePointerPrecision DeleteWindowsOld DisableAutomaticRestartSignOn DisableWpbt PreventDeviceApps ProcessAudit ProcessAuditCommandLine EffectsMode
        ControlAnimations AnimateMinMax TaskbarAnimations DWMAeroPeekEnabled MenuAnimation TooltipAnimation SelectionFade DWMSaveThumbnailEnabled CursorShadow
        ListviewShadow ThumbnailsOrIcon ListviewAlphaSelect DragFullWindows ComboBoxAnimation FontSmoothing ListBoxSmoothScrolling DropShadow DeleteEdgeDesktopIcon
        DesktopIconsMode IconControlPanel IconDesktop IconDocuments IconDownloads IconGallery IconHome IconMusic IconNetwork IconPictures IconRecycleBin IconThisPC
        IconUserFiles IconVideos StartFoldersMode StartFolderDocuments StartFolderDownloads StartFolderFileExplorer StartFolderMusic StartFolderNetwork
        StartFolderPersonalFolder StartFolderPictures StartFolderSettings StartFolderVideos CoreIsolationMode VBoxGuestAdditions VMwareTools VirtIoGuestTools
        ParallelsTools WifiMode WifiName WifiNonBroadcast WifiAuthentication WifiPassword WifiProfileXml ExpressSettings LockKeysMode CapsLockInitial
        CapsLockBehavior NumLockInitial NumLockBehavior ScrollLockInitial ScrollLockBehavior StickyKeysMode StickyKeysHotKeyActive StickyKeysHotKeySound
        StickyKeysIndicator StickyKeysAudibleFeedback StickyKeysTriState StickyKeysTwoKeysOff ColorMode SystemColorTheme AppsColorTheme AccentColor
        AccentColorOnStart AccentColorOnBorders EnableTransparency WallpaperMode WallpaperColor WallpaperScript LockScreenMode LockScreenScript
        Remove3DViewer RemoveBingSearch RemoveCalculator RemoveCamera RemoveClipchamp RemoveClock RemoveCopilot RemoveCortana RemoveDevHome RemoveFamily
        RemoveFeedbackHub RemoveGameAssist RemoveGetHelp RemoveGetStarted RemoveHandwriting RemoveInternetExplorer RemoveMailCalendar RemoveMaps
        RemoveMathInputPanel RemoveMediaFeatures RemoveMixedReality RemoveNews RemoveNotepad RemoveOffice365 RemoveOneDrive RemoveOneNote RemoveOneSync
        RemoveOpenSSHClient RemoveOutlook RemovePaint RemovePaint3D RemovePeople RemovePhotos RemovePowerAutomate RemovePowerShell2 RemovePowerShellISE
        RemoveQuickAssist RemoveRdpClient RemoveRecall RemoveSkype RemoveSnippingTool RemoveSolitaire RemoveSpeech RemoveStepsRecorder RemoveStickyNotes
        RemoveStore RemoveTeams RemoveToDo RemoveVoiceRecorder RemoveWallet RemoveWeather RemoveWindowsHello RemoveWindowsMediaPlayer RemoveWindowsTerminal
        RemoveWordPad RemoveXboxApps RemoveYourPhone RemoveZuneMusic RemoveZuneVideo SystemScript0 SystemScriptType0 SystemScript14 SystemScriptType14
        DefaultUserScript0 DefaultUserScriptType0 FirstLogonScript0 FirstLogonScriptType0 UserOnceScript0 UserOnceScriptType0 RestartExplorer AppLockerMode
        AppLockerPolicyXml ComponentContent0 Component0 ComponentContent14 Component14 NotFilename
        """;

    private static string Gerar(EstadoVega estado) => Encoding.ASCII.GetString(estado.GerarXml(Gerador));

    /// <summary>
    /// Um XML baixado do gerador original é pessoal, então não fica no repositório. Para comparar,
    /// aponte a variável de ambiente VEGA_XML_DO_SITE para um arquivo seu. Sem ela, o teste não confere nada.
    /// </summary>
    private static string? ExemploDoSite()
    {
        var caminho = Environment.GetEnvironmentVariable("VEGA_XML_DO_SITE");
        return string.IsNullOrEmpty(caminho) ? null : File.ReadAllText(caminho, Encoding.UTF8);
    }

    /// <summary>Tira comentários e diferenças de formatação, para comparar só o conteúdo.</summary>
    private static string Normalizar(string xml)
    {
        var documento = new XmlDocument { PreserveWhitespace = false };
        documento.LoadXml(xml);
        foreach (var comentario in documento.SelectNodes("//comment()")!.Cast<XmlNode>().ToList())
        {
            comentario.ParentNode!.RemoveChild(comentario);
        }

        return documento.OuterXml;
    }

    private static EstadoVega DoEndereco(string consulta) => ImportadorXml.DoEndereco(consulta).Estado;

    // Importar XML do site

    [Fact]
    public void Abrir_o_xml_do_site_e_gerar_de_novo_da_o_mesmo_arquivo()
    {
        var original = ExemploDoSite();
        if (original is null)
        {
            return;
        }

        var aberta = ImportadorXml.Abrir(original);

        Assert.Equal(Normalizar(original), Normalizar(Gerar(aberta.Estado)));
        Assert.DoesNotContain("ignorou", aberta.Mensagem);
    }

    [Fact]
    public void Todos_os_campos_do_site_sao_conhecidos()
    {
        var campos = CamposDoSite.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        Assert.Empty(campos.Where(c => !ImportadorXml.ConheceParametro(c)));
    }

    [Fact]
    public void Campo_desconhecido_vira_aviso_em_vez_de_sumir_em_silencio()
    {
        var aberta = ImportadorXml.DoEndereco("PEMode=Default&CampoNovoDoSite=true");

        Assert.Contains("CampoNovoDoSite", aberta.Mensagem);
    }

    [Fact]
    public void Xml_sem_comentario_e_recusado_com_mensagem_clara()
    {
        var erro = Assert.Throws<FormatException>(() => Compartilhamento.Abrir("<unattend />"));

        Assert.Contains("não traz as opções", erro.Message);
    }

    [Theory]
    [InlineData("PEMode=Default&WindowsEditionMode=Interactive", "")]
    [InlineData("PEMode=Default&WindowsEditionMode=Generic&WindowsEdition=home", "home")]
    [InlineData("PEMode=Default&WindowsEditionMode=Custom&ProductKey=AAAAA-BBBBB-CCCCC-DDDDD-EEEEE", EstadoVega.EdicaoChave)]
    [InlineData("PEMode=Default&WindowsEditionMode=Firmware", EstadoVega.EdicaoFirmware)]
    public void Importa_os_modos_de_edicao(string consulta, string edicao)
    {
        var estado = DoEndereco(consulta);

        Assert.Equal(ModoInstalacao.TelaNormal, estado.Modo);
        Assert.Equal(edicao, estado.Edicao);
        Assert.Contains("<unattend", Gerar(estado));
    }

    [Fact]
    public void Importa_contas_login_e_politicas()
    {
        var estado = DoEndereco("UserAccountMode=Unattended&AccountName0=Ana&AccountDisplayName0=Ana+Maria&AccountPassword0=a%26b&AccountGroup0=Administrators"
            + "&AccountName1=Bia&AccountPassword1=x&AccountGroup1=Users&AutoLogonMode=Builtin&BuiltinAdministratorPassword=adm&ObscurePasswords=true"
            + "&PasswordExpirationMode=Custom&PasswordMaxAge=30&LockoutMode=Custom&LockoutThreshold=5&LockoutWindow=7&LockoutDuration=9");

        Assert.Equal(TipoConta.Pronta, estado.Conta);
        Assert.Equal(2, estado.Contas.Count);
        Assert.Equal("Ana Maria", estado.Contas[0].NomeExibido);
        Assert.Equal("a&b", estado.Contas[0].Senha);
        Assert.Equal(Constants.UsersGroup, estado.Contas[1].Grupo);
        Assert.Equal(LoginAutomatico.Administrador, estado.Login);
        Assert.True(estado.OcultarSenhas);
        Assert.Equal(30, estado.ExpiracaoDias);
        Assert.Equal((5, 7, 9), (estado.BloqueioTentativas, estado.BloqueioJanela, estado.BloqueioDuracao));
        Assert.Contains("<unattend", Gerar(estado));
    }

    [Theory]
    [InlineData("WifiMode=Skip", ModoWifi.Pular)]
    [InlineData("WifiMode=Interactive", ModoWifi.Perguntar)]
    [InlineData("WifiMode=Unattended&WifiName=Casa&WifiPassword=12345678&WifiAuthentication=WPA3SAE", ModoWifi.Configurar)]
    [InlineData("WifiMode=FromProfile", ModoWifi.ArquivoXml)]
    public void Importa_os_modos_de_wifi(string consulta, ModoWifi modo) =>
        Assert.Equal(modo, DoEndereco(consulta).Wifi);

    [Fact]
    public void Importa_disco_imagem_e_paginacao_do_modo_script()
    {
        var estado = DoEndereco("PEMode=Generated&PauseBeforeFormatting=true&SkipIntegrityCheck=true&InstallFromMode=Index&InstallFromIndex=3"
            + "&PartitionMode=Unattended&TargetDiskMode=Generated&TargetDiskNoPartitions=true&TargetDiskSize=true&TargetDiskMinSize=50&TargetDiskMaxSize=900"
            + "&PartitionLayout=GPT&SystemSize=500&RecoveryMode=None&PagingFileMode=Custom&InitPagingFileSize=100&MaxPagingFileSize=200");

        Assert.Equal(ModoInstalacao.Script, estado.Modo);
        Assert.True(estado.PausarAntesDeFormatar);
        Assert.False(estado.VerificarImagem);
        Assert.Equal((OrigemImagem.Indice, 3), (estado.Origem, estado.ImagemIndice));
        Assert.Equal(AlvoDisco.Criterios, estado.Alvo);
        Assert.True(estado.CriterioSemParticoes);
        Assert.False(estado.CriterioTipoInterno);
        Assert.Equal((50, 900), (estado.CriterioMinimoGiB, estado.CriterioMaximoGiB));
        Assert.Null(estado.CriterioNumero);
        Assert.Equal(PartitionLayout.GPT, estado.Layout);
        Assert.Equal(500, estado.TamanhoSistemaMiB);
        Assert.Equal(RecoveryMode.None, estado.Recuperacao);
        Assert.Equal(ArquivoPaginacao.Personalizado, estado.Paginacao);
        Assert.Contains("<unattend", Gerar(estado));
    }

    [Fact]
    public void Importa_listas_de_efeitos_aderencia_icones_pastas_e_arquiteturas()
    {
        var estado = DoEndereco("EffectsMode=Custom&FontSmoothing=true&StickyKeysMode=Custom&StickyKeysIndicator=true"
            + "&DesktopIconsMode=Custom&IconThisPC=true&StartFoldersMode=Custom&StartFolderDownloads=true"
            + "&ProcessorArchitecture=amd64&ProcessorArchitecture=arm64&CoreIsolationMode=Disabled&DisableWidgets=true&RemoveTeams=true");

        Assert.Equal([Effect.FontSmoothing], estado.EfeitosLigados);
        Assert.Equal([StickyKeys.Indicator], estado.AderenciaOpcoes);
        Assert.Equal(["ThisPC"], estado.IconesArea);
        Assert.Equal(["Downloads"], estado.PastasIniciar);
        Assert.Equal(2, estado.Arquiteturas.Count);
        Assert.Contains(nameof(Configuration.DisableCoreIsolation), estado.OpcoesLigadas);
        Assert.Contains(nameof(Configuration.DisableWidgets), estado.OpcoesLigadas);
        Assert.Contains("RemoveTeams", estado.AppsRemovidos);
    }

    [Fact]
    public void Importa_scripts_componentes_e_nome_alternativo()
    {
        var estado = DoEndereco("SystemScript0=a&SystemScriptType0=Reg&FirstLogonScript2=b&FirstLogonScriptType2=Ps1&UserOnceScript0=c&UserOnceScriptType0=Vbs"
            + "&Component0=Microsoft-Windows-Audio-AudioCore-specialize&ComponentContent0=%3CA+%2F%3E&NotFilename=true");

        Assert.Equal(3, estado.Scripts.Count);
        Assert.Equal((ScriptPhase.System, ScriptType.Reg, "a"), (estado.Scripts[0].Fase, estado.Scripts[0].Tipo, estado.Scripts[0].Conteudo));
        Assert.Equal(ScriptPhase.FirstLogon, estado.Scripts[1].Fase);
        Assert.Equal(ScriptType.Vbs, estado.Scripts[2].Tipo);
        var componente = Assert.Single(estado.Componentes);
        Assert.Equal(("Microsoft-Windows-Audio-AudioCore", Pass.specialize, "<A />"), (componente.Componente, componente.Fase, componente.Xml));
        Assert.Equal("notautounattend.xml", estado.NomeDoArquivo);
    }

    // Pontos de partida

    [Fact]
    public void Saida_minima_e_o_mesmo_xml_do_motor_sem_nenhuma_escolha()
    {
        var motor = Encoding.ASCII.GetString(UnattendGenerator.Serialize(Gerador.GenerateXml(Configuration.Default)));

        var estado = Perfis.Pontos.Single(p => p.Id == "minimo").Criar();

        Assert.Equal(Normalizar(motor), Normalizar(Gerar(estado)));
    }

    [Fact]
    public void So_um_usuario_local_pula_a_conta_microsoft_e_a_rede()
    {
        var estado = Perfis.Pontos.Single(p => p.Id == "usuario-local").Criar();

        Assert.Equal(TipoConta.Local, estado.Conta);
        Assert.Equal(ModoWifi.Pular, estado.Wifi);
        Assert.Contains("<HideOnlineAccountScreens>true", Gerar(estado));
        Assert.Contains("<HideWirelessSetupInOOBE>true", Gerar(estado));
    }

    // Link e comentário no XML

    private static EstadoVega EstadoComSegredos()
    {
        var estado = new EstadoVega
        {
            Conta = TipoConta.Pronta,
            Contas = [new ContaPronta { Nome = "Ana", Senha = "senha-da-ana" }],
            SenhaAdministrador = "senha-adm",
            Wifi = ModoWifi.Configurar,
            WifiNome = "Casa",
            WifiSenha = "senha-do-wifi",
            ChaveProduto = "AAAAA-BBBBB-CCCCC-DDDDD-EEEEE",
            UsarChaveAtivacao = true,
            ChaveAtivacao = "FFFFF-GGGGG-HHHHH-IIIII-JJJJJ",
            Energia = PlanoEnergia.DesempenhoMaximo,
            RemoverEdge = true,
        };
        estado.OpcoesLigadas.Add(nameof(Configuration.DisableWidgets));
        return estado;
    }

    [Fact]
    public void O_link_guarda_as_escolhas_e_nao_leva_segredos()
    {
        var estado = EstadoComSegredos();

        var codigo = Compartilhamento.Codificar(estado);
        var texto = Encoding.UTF8.GetString(Convert.FromBase64String(codigo));
        var aberto = Compartilhamento.Decodificar(codigo);

        foreach (var segredo in new[] { "senha-da-ana", "senha-adm", "senha-do-wifi", "AAAAA-", "FFFFF-" })
        {
            Assert.DoesNotContain(segredo, texto);
        }

        Assert.Equal(PlanoEnergia.DesempenhoMaximo, aberto.Energia);
        Assert.True(aberto.RemoverEdge);
        Assert.Equal("Ana", Assert.Single(aberto.Contas).Nome);
        Assert.Equal("", aberto.Contas[0].Senha);
        Assert.Contains(nameof(Configuration.DisableWidgets), aberto.OpcoesLigadas);
    }

    [Fact]
    public void O_link_guarda_so_o_que_difere_do_padrao() =>
        Assert.Equal("{}", Encoding.UTF8.GetString(Convert.FromBase64String(Compartilhamento.Codificar(new EstadoVega()))));

    [Fact]
    public void O_xml_do_vega_reabre_com_as_mesmas_escolhas()
    {
        var estado = new EstadoVega { Energia = PlanoEnergia.AltoDesempenho, Efeitos = ModoEfeitos.Desempenho, RemoverEdge = true, CriterioMinimoGiB = null };
        estado.AppsRemovidos.Add("RemoveTeams");

        var aberta = Compartilhamento.Abrir(Gerar(estado));

        Assert.Equal(Normalizar(Gerar(estado)), Normalizar(Gerar(aberta.Estado)));
        Assert.Null(aberta.Estado.CriterioMinimoGiB);
    }

    [Fact]
    public void O_comentario_do_xml_nao_leva_segredos()
    {
        var comentario = new XmlDocument();
        comentario.LoadXml(Gerar(EstadoComSegredos()));

        var texto = comentario.SelectSingleNode("//comment()")!.Value!;
        var conteudo = Encoding.UTF8.GetString(Convert.FromBase64String(texto[(texto.IndexOf('=') + 1)..]));

        Assert.StartsWith(Compartilhamento.EnderecoSite, texto);
        Assert.DoesNotContain("senha-da-ana", conteudo);
    }

    [Theory]
    [InlineData("###")]
    [InlineData("bm90IGpzb24=")]
    public void Link_quebrado_vira_erro_claro(string codigo)
    {
        var erro = Assert.Throws<FormatException>(() => Compartilhamento.Decodificar(codigo));

        Assert.Contains("link", erro.Message);
    }

    // Abrir JSON

    [Theory]
    [InlineData("""{"name":"meu-pacote"}""")]
    [InlineData("""[1,2,3]""")]
    [InlineData("isto não é json")]
    [InlineData("""{"OpcoesLigadas":[],"AppsRemovidos":[],"Modo":99}""")]
    [InlineData("""{"OpcoesLigadas":[],"AppsRemovidos":[],"Modo":"Inventado"}""")]
    [InlineData("""{"OpcoesLigadas":[],"AppsRemovidos":[],"Scripts":[{"Fase":77}]}""")]
    public void Json_que_nao_e_do_vega_e_recusado(string json)
    {
        var erro = Assert.Throws<FormatException>(() => Compartilhamento.Abrir(json));

        Assert.Contains("não é uma configuração do Vega", erro.Message);
    }

    [Fact]
    public void Json_com_nulos_volta_aos_padroes_sem_derrubar_a_tela()
    {
        var aberta = Compartilhamento.Abrir("""
            {"OpcoesLigadas":null,"AppsRemovidos":null,"Scripts":null,"Contas":[null,{"Nome":null}],"Edicao":null,"Componentes":[null],"CriterioMinimoGiB":null}
            """);

        var estado = aberta.Estado;
        Assert.Equal(0, estado.ContarAlteracoes(Categoria.Apps));
        Assert.Empty(estado.Scripts);
        Assert.Equal("", Assert.Single(estado.Contas).Nome);
        Assert.Equal("", estado.Edicao);
        Assert.Empty(estado.Componentes);
        Assert.Null(estado.CriterioMinimoGiB);
        Assert.Equal(0, estado.ContarAlteracoes(Categoria.Instalacao));
    }

    [Fact]
    public void Json_antigo_com_enums_em_numero_continua_abrindo()
    {
        var aberta = Compartilhamento.Abrir("""{"OpcoesLigadas":[],"AppsRemovidos":[],"Energia":2,"Efeitos":1,"Arquiteturas":[1]}""");

        Assert.Equal(PlanoEnergia.DesempenhoMaximo, aberta.Estado.Energia);
        Assert.Equal(ModoEfeitos.Desempenho, aberta.Estado.Efeitos);
    }

    [Fact]
    public void Enums_sao_salvos_com_o_nome() =>
        Assert.Contains("\"Energia\": \"DesempenhoMaximo\"", new EstadoVega { Energia = PlanoEnergia.DesempenhoMaximo }.Exportar());

    // ISO

    private const int Setor = 2048;

    private static int Pequeno32(byte[] dados, int posicao) => BitConverter.ToInt32(dados, posicao);

    [Fact]
    public void A_iso_tem_o_arquivo_na_raiz_dos_dois_sistemas_de_arquivos()
    {
        var conteudo = Encoding.ASCII.GetBytes(new string('x', 5000));

        var iso = GeradorIso.Criar("autounattend.xml", conteudo, new DateTime(2026, 11, 7, 10, 20, 30, DateTimeKind.Utc));

        Assert.Equal(0, iso.Length % Setor);
        Assert.Equal("CD001", Encoding.ASCII.GetString(iso, 16 * Setor + 1, 5));
        Assert.Equal(1, iso[16 * Setor]);
        Assert.Equal(2, iso[17 * Setor]);
        Assert.Equal("%/E", Encoding.ASCII.GetString(iso, 17 * Setor + 88, 3));
        Assert.Equal(255, iso[18 * Setor]);
        Assert.Equal(iso.Length / Setor, Pequeno32(iso, 16 * Setor + 80));

        ConferirRaiz(iso, 16, "AUTOUNATTEND.XML;1", Encoding.ASCII, conteudo);
        ConferirRaiz(iso, 17, "autounattend.xml;1", Encoding.BigEndianUnicode, conteudo);
    }

    private static void ConferirRaiz(byte[] iso, int descritor, string nome, Encoding codificacao, byte[] conteudo)
    {
        var inicio = descritor * Setor;
        var setorRaiz = Pequeno32(iso, inicio + 156 + 2);
        Assert.Equal(Setor, Pequeno32(iso, inicio + 156 + 10));

        var posicao = setorRaiz * Setor;
        var ponto = iso[posicao];
        var pontoPonto = iso[posicao + ponto];
        Assert.Equal(setorRaiz, Pequeno32(iso, posicao + 2));
        var arquivo = posicao + ponto + pontoPonto;

        var tamanhoNome = iso[arquivo + 32];
        Assert.Equal(nome, codificacao.GetString(iso, arquivo + 33, tamanhoNome));
        Assert.Equal(0, iso[arquivo + 25]);
        Assert.Equal(conteudo.Length, Pequeno32(iso, arquivo + 10));
        Assert.Equal(conteudo, iso.AsSpan(Pequeno32(iso, arquivo + 2) * Setor, conteudo.Length).ToArray());

        var tabela = Pequeno32(iso, inicio + 140);
        Assert.Equal(setorRaiz, Pequeno32(iso, tabela * Setor + 2));
    }

    // Rótulos do pe.cmd

    [Fact]
    public void Disco_e_imagem_perguntados_nao_repetem_o_rotulo_do_pe()
    {
        var estado = new EstadoVega { Modo = ModoInstalacao.Script, Alvo = AlvoDisco.Perguntar, Origem = OrigemImagem.Perguntar };

        var xml = Gerar(estado);

        // Cada troca aparece no script que roda e na cópia de consulta que o motor grava.
        Assert.Equal(1, Contar(xml, "echo::choice"));
        Assert.Equal(1, Contar(xml, "echo::imagem"));
        Assert.Equal(2, Contar(xml, "goto :choice"));
        Assert.Equal(2, Contar(xml, "goto :imagem"));
        Assert.True(xml.IndexOf("echo::choice", StringComparison.Ordinal) < xml.IndexOf("echo::imagem", StringComparison.Ordinal));
        Assert.Contains("goto :imagem", TextoDaLinha(xml, "CHOICE=Enter index of the image"));
        Assert.Contains("goto :choice", TextoDaLinha(xml, "CHOICE=Enter index of the disk"));

        var copia = new XmlDocument();
        copia.LoadXml(xml);
        var texto = copia.SelectSingleNode("//*[local-name()='PEScriptCopy']")!.InnerText;
        Assert.Equal(1, Contar(texto, "goto :choice"));
        Assert.Equal(1, Contar(texto, "goto :imagem"));
        Assert.Matches(@"(?m)^\s*:imagem\b", texto);
    }

    [Fact]
    public void Script_proprio_com_rotulos_repetidos_fica_como_foi_escrito()
    {
        var estado = new EstadoVega { Modo = ModoInstalacao.ScriptProprio, ScriptInstalacao = ":choice\r\ngoto :choice\r\n:choice\r\ngoto :choice" };

        Assert.DoesNotContain(":imagem", Gerar(estado));
    }

    [Fact]
    public void So_a_pergunta_do_disco_mantem_o_rotulo_original()
    {
        var estado = new EstadoVega { Modo = ModoInstalacao.Script, Alvo = AlvoDisco.Perguntar, Origem = OrigemImagem.Indice, ImagemIndice = 1 };

        var xml = Gerar(estado);

        Assert.Equal(1, Contar(xml, "echo::choice"));
        Assert.Equal(0, Contar(xml, "echo::imagem"));
    }

    [Fact]
    public void Trocar_o_rotulo_nao_estoura_o_limite_de_cada_linha()
    {
        var documento = new XmlDocument();
        documento.LoadXml(Gerar(new EstadoVega { Modo = ModoInstalacao.Script }));

        Assert.All(documento.GetElementsByTagName("Path").Cast<XmlNode>(), caminho => Assert.True(caminho.InnerText.Length <= 259));
    }

    private static int Contar(string texto, string trecho) => (texto.Length - texto.Replace(trecho, "").Length) / trecho.Length;

    private static string TextoDaLinha(string texto, string trecho)
    {
        var posicao = texto.IndexOf(trecho, StringComparison.Ordinal);
        Assert.True(posicao >= 0);
        return texto.Substring(posicao, 120);
    }

    // Validação

    public static TheoryData<string, Action<EstadoVega>> Invalidos => new()
    {
        { "script do WinPE", e => e.Modo = ModoInstalacao.ScriptProprio },
        { "script do diskpart", e => { e.Modo = ModoInstalacao.Script; e.Particoes = Particionamento.ScriptDiskpart; } },
        { "escolhe o disco", e => { e.Modo = ModoInstalacao.Script; e.Alvo = AlvoDisco.ScriptProprio; } },
        { "número do disco", e => { e.Modo = ModoInstalacao.Script; e.Alvo = AlvoDisco.Numero; e.DiscoNumero = -1; } },
        { "tamanho mínimo", e => { e.Modo = ModoInstalacao.Script; e.Alvo = AlvoDisco.Criterios; e.CriterioMinimoGiB = -1; } },
        { "100 MiB", e => { e.Modo = ModoInstalacao.Script; e.TamanhoSistemaMiB = 0; } },
        { "300 MiB", e => { e.Modo = ModoInstalacao.Script; e.TamanhoRecuperacaoMiB = 0; } },
        { "número da imagem", e => { e.Modo = ModoInstalacao.Script; e.Origem = OrigemImagem.Indice; e.ImagemIndice = 0; } },
        { "nome da imagem", e => { e.Modo = ModoInstalacao.Script; e.Origem = OrigemImagem.Nome; } },
        { "paginação", e => { e.Modo = ModoInstalacao.Script; e.Paginacao = ArquivoPaginacao.Personalizado; e.PaginacaoInicialMiB = 9000; e.PaginacaoMaximaMiB = 10; } },
        { "1 e 999 dias", e => { e.Expiracao = ModoExpiracao.Personalizado; e.ExpiracaoDias = 0; } },
        { "0 e 999", e => { e.Bloqueio = ModoBloqueio.Personalizado; e.BloqueioTentativas = 1000; } },
        { "tempo de bloqueio", e => { e.Bloqueio = ModoBloqueio.Personalizado; e.BloqueioDuracao = 0; } },
        { "nome do computador", e => e.NomeComputadorModo = ModoNomeComputador.Script },
        { "rede Wi-Fi", e => e.Wifi = ModoWifi.Configurar },
        { "senha do Wi-Fi", e => { e.Wifi = ModoWifi.Configurar; e.WifiNome = "Casa"; e.WifiSenha = "123"; } },
        { "cor de destaque", e => { e.PersonalizarCores = true; e.CorDestaque = "azul"; } },
        { "papel de parede", e => e.Papel = ModoPapel.Script },
        { "tela de bloqueio", e => e.PersonalizarTelaBloqueio = true },
        { "fixados", e => e.FixadosIniciar = ModoFixados.Personalizado },
        { "não existe na etapa", e => e.Componentes.Add(new ComponenteXml { Componente = "Microsoft-Windows-MediaPlayer-Core", Fase = Pass.windowsPE, Xml = "<A />" }) },
    };

    [Theory]
    [MemberData(nameof(Invalidos))]
    public void Campo_invalido_impede_gerar_o_xml(string trecho, Action<EstadoVega> ajuste)
    {
        var estado = new EstadoVega();
        ajuste(estado);

        var erro = Assert.Throws<ConfigurationException>(() => estado.GerarXml(Gerador));

        Assert.Contains(trecho, erro.Message);
    }

    [Fact]
    public void Campo_de_outro_modo_nao_atrapalha()
    {
        var estado = new EstadoVega { TamanhoSistemaMiB = 0, ImagemIndice = 0, Origem = OrigemImagem.Indice, WifiNome = "" };

        Assert.Contains("<unattend", Gerar(estado));
    }

    [Fact]
    public void Wifi_aberto_nao_exige_senha()
    {
        var estado = new EstadoVega { Wifi = ModoWifi.Configurar, WifiNome = "Casa", WifiAutenticacao = WifiAuthentications.Open };

        Assert.Contains("<unattend", Gerar(estado));
    }

    // Linhas em branco

    [Fact]
    public void Script_e_componente_em_branco_nao_mudam_o_xml()
    {
        var limpo = new EstadoVega();
        var comBrancos = new EstadoVega
        {
            Scripts = [new ScriptUsuario { Conteudo = "   " }],
            Componentes = [new ComponenteXml { Componente = "Microsoft-Windows-Deployment", Fase = Pass.specialize, Xml = "" }],
        };

        Assert.Equal(Normalizar(Gerar(limpo)), Normalizar(Gerar(comBrancos)));
        Assert.Equal(0, comBrancos.ContarAlteracoes(Categoria.Avancado));
    }

    // Componentes que o motor já usa

    [Fact]
    public void Avisa_quando_o_componente_substitui_o_que_o_motor_gerou()
    {
        var estado = new EstadoVega();
        estado.OpcoesLigadas.Add(nameof(Configuration.DisableWidgets));

        var usados = estado.ComponentesJaUsados(Gerador);

        Assert.Contains(("Microsoft-Windows-Deployment", Pass.specialize), usados);
        Assert.Contains(("Microsoft-Windows-Shell-Setup", Pass.oobeSystem), usados);
        Assert.DoesNotContain(("Microsoft-Windows-MediaPlayer-Core", Pass.specialize), usados);
    }

    // Teclados

    [Fact]
    public void Terceiro_teclado_escondido_nao_vale()
    {
        var estado = new EstadoVega { UsarTeclado2 = false, UsarTeclado3 = true };

        Assert.Contains("<InputLocale>0416:00010416</InputLocale>", Gerar(estado));
        Assert.Equal(0, estado.ContarAlteracoes(Categoria.Instalacao));
    }

    // Disco

    [Fact]
    public void Diskpart_sem_pausa_e_apagar_sem_perguntar()
    {
        var estado = new EstadoVega { Modo = ModoInstalacao.Script, Particoes = Particionamento.ScriptDiskpart, PausarAntesDeFormatar = false, Alvo = AlvoDisco.Perguntar };

        Assert.True(estado.ApagaSemPerguntar);
        estado.PausarAntesDeFormatar = true;
        Assert.False(estado.ApagaSemPerguntar);
    }

    [Theory]
    [InlineData("SELECT DISK 0\nCLEAN", true)]
    [InlineData("create partition primary\nset id=27", false)]
    [InlineData("format quick label=\"Recovery\"", false)]
    public void Avisa_quando_o_diskpart_apaga_a_recuperacao(string script, bool apaga)
    {
        var estado = new EstadoVega { Modo = ModoInstalacao.Script, Particoes = Particionamento.ScriptDiskpart, ScriptDiskpart = script };

        Assert.Equal(apaga, estado.DiskpartApagaRecuperacao);
    }

    // Padrões e perfis

    [Fact]
    public void A_senha_nao_expira_por_padrao_como_no_site() =>
        Assert.IsType<UnlimitedPasswordExpirationSettings>(new EstadoVega().Montar(Gerador).PasswordExpirationSettings);

    [Fact]
    public void Aplicar_o_padrao_em_um_estado_novo_nao_muda_nada()
    {
        var estado = new EstadoVega();

        Perfis.Todos.Single(p => p.Id == "padrao").Aplicar(estado);

        Assert.Equal(new EstadoVega().Exportar(), estado.Exportar());
    }

    [Fact]
    public void Windows_padrao_entrega_a_conta_local_sem_pedir_internet()
    {
        var estado = new EstadoVega { Conta = TipoConta.Microsoft, Wifi = ModoWifi.Perguntar };

        Perfis.Todos.Single(p => p.Id == "padrao").Aplicar(estado);

        var xml = Gerar(estado);
        Assert.Contains("<HideOnlineAccountScreens>true", xml);
        Assert.Contains("<HideWirelessSetupInOOBE>true", xml);
    }

    [Fact]
    public void Trocar_de_perfil_nao_deixa_ajustes_para_tras()
    {
        var estado = new EstadoVega
        {
            Layout = PartitionLayout.MBR,
            Origem = OrigemImagem.Indice,
            Paginacao = ArquivoPaginacao.Nenhum,
            TamanhoSistemaMiB = 100,
            UsarAppLocker = true,
            AuditarProcessos = true,
            Papel = ModoPapel.CorSolida,
            PersonalizarCores = true,
            BuscaBarra = TaskbarSearchMode.Hide,
            Bloqueio = ModoBloqueio.Desligado,
        };

        Perfis.Todos.Single(p => p.Id == "padrao").Aplicar(estado);

        Assert.Equal(new EstadoVega().Exportar(), estado.Exportar());
    }

    [Fact]
    public void Trocar_de_perfil_mantem_idioma_edicao_e_o_que_foi_digitado()
    {
        var estado = new EstadoVega
        {
            IdiomaImagem = "en-US",
            Edicao = "home",
            NomeComputador = "VEGA",
            Contas = [new ContaPronta { Nome = "Ana" }],
            Scripts = [new ScriptUsuario { Conteudo = "x" }],
            ScriptDiskpart = "y",
        };

        Perfis.Todos.Single(p => p.Id == "maximo").Aplicar(estado);

        Assert.Equal("en-US", estado.IdiomaImagem);
        Assert.Equal("home", estado.Edicao);
        Assert.Equal("VEGA", estado.NomeComputador);
        Assert.Single(estado.Contas);
        Assert.Single(estado.Scripts);
        Assert.Equal("y", estado.ScriptDiskpart);
    }

    [Fact]
    public void Perfil_maximo_nao_usa_o_que_o_autor_do_motor_desaconselha()
    {
        var estado = new EstadoVega();

        Perfis.Todos.Single(p => p.Id == "maximo").Aplicar(estado);

        Assert.DoesNotContain(nameof(Configuration.MakeEdgeUninstallable), estado.OpcoesLigadas);
        Assert.DoesNotContain("RemoveNotepadClassic", estado.AppsRemovidos);
        Assert.Equal(ModoFixados.Vazio, estado.FixadosIniciar);
    }

    [Theory]
    [InlineData("diario")]
    [InlineData("maximo")]
    public void Perfis_que_removem_apps_nao_deixam_fixados_fantasmas(string id)
    {
        var estado = new EstadoVega();

        Perfis.Todos.Single(p => p.Id == id).Aplicar(estado);

        Assert.NotEmpty(estado.AppsRemovidos);
        Assert.Equal(ModoFixados.Vazio, estado.FixadosIniciar);
    }

    [Fact]
    public void Marcar_os_sem_aviso_nao_inclui_item_com_custo()
    {
        Assert.DoesNotContain(Catalogo.AppsSemAviso, a => a.Id == "RemoveOpenSSHClient");
        Assert.DoesNotContain(Catalogo.AppsSemAviso, a => a.Id == "RemoveStore");
    }

    // Contador

    public static TheoryData<Categoria, Action<EstadoVega>> Ajustes => new()
    {
        { Categoria.Interface, e => e.BuscaBarra = TaskbarSearchMode.Hide },
        { Categoria.Interface, e => e.ArquivosOcultos = HideModes.None },
        { Categoria.Conta, e => e.Privacidade = ExpressSettingsMode.EnableAll },
        { Categoria.Conta, e => e.Expiracao = ModoExpiracao.Padrao },
        { Categoria.Instalacao, e => e.EscolherIdiomaNaInstalacao = true },
        { Categoria.Instalacao, e => e.IgnorarRequisitos = false },
        { Categoria.Disco, e => e.UsarConfigurationSet = true },
        { Categoria.Seguranca, e => e.UsarAppLocker = true },
        { Categoria.Avancado, e => e.NomeSemAutoInstalar = true },
    };

    [Theory]
    [MemberData(nameof(Ajustes))]
    public void O_contador_ve_cada_ajuste(Categoria categoria, Action<EstadoVega> ajuste)
    {
        var estado = new EstadoVega();
        ajuste(estado);

        Assert.Equal(1, estado.ContarAlteracoes(categoria));
    }
}
