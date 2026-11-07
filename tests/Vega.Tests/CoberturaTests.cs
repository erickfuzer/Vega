using System.Text;
using BindingFlags = System.Reflection.BindingFlags;
using Schneegans.Unattend;
using Vega.Core;

namespace Vega.Tests;

/// <summary>Garante que nada da configuração do motor fica de fora do site.</summary>
public class CoberturaTests
{
    [Theory]
    [InlineData("Must have at least one administrator account.", "É preciso ter pelo menos uma conta de administrador.")]
    [InlineData("Computer name 'A B' is invalid.", "O nome de computador \"A B\" é inválido. Use até 15 caracteres, sem espaços nem símbolos.")]
    [InlineData("Your diskpart script must contain a line such as ‘ASSIGN LETTER=W’ to assign the drive letter ‘W:’ to the Windows partition.", "Seu script do diskpart precisa ter uma linha ASSIGN LETTER=W.")]
    [InlineData("Mensagem desconhecida", "Mensagem desconhecida")]
    public void Traduz_os_erros_do_motor(string original, string esperado) =>
        Assert.Equal(esperado, ErrosDoMotor.Traduzir(original));

    [Fact]
    public void A_traducao_do_componente_na_etapa_pe_mantem_o_nome_da_pasta() =>
        Assert.Contains("$WinPEDriver$", ErrosDoMotor.Traduzir("Cannot create .cmd script with custom components (x) for the ‘windowsPE’ pass."));

    [Theory]
    [InlineData("Aparência", "aparencia", true)]
    [InlineData("Segurança", "SEGURANCA", true)]
    [InlineData("Plano de energia", "  energia ", true)]
    [InlineData("Plano de energia", "disco", false)]
    [InlineData("alto desempenho maximo", "maximo desempenho", true)]
    [InlineData("alto desempenho", "maximo desempenho", false)]
    public void A_busca_ignora_acentos_e_maiusculas(string texto, string termo, bool esperado) =>
        Assert.Equal(esperado, Busca.Combina(texto, termo));

    [Fact]
    public void A_busca_acha_o_modo_de_desempenho_maximo() =>
        Assert.Contains(Catalogo.Atalhos, a => a.Categoria == Categoria.Desempenho && Busca.Combina(a.Termos, "desempenho maximo"));

    [Fact]
    public void O_estado_inicial_nao_conta_nenhuma_alteracao()
    {
        var estado = new EstadoVega();
        Assert.All(Enum.GetValues<Categoria>(), c => Assert.Equal(0, estado.ContarAlteracoes(c)));
    }

    [Fact]
    public void O_contador_inclui_os_ajustes_que_nao_sao_interruptores()
    {
        var estado = new EstadoVega { Efeitos = ModoEfeitos.Desempenho, Energia = PlanoEnergia.DesempenhoMaximo, RemoverEdge = true, Wifi = ModoWifi.Perguntar };
        Assert.Equal(2, estado.ContarAlteracoes(Categoria.Desempenho));
        Assert.Equal(1, estado.ContarAlteracoes(Categoria.Edge));
        Assert.Equal(1, estado.ContarAlteracoes(Categoria.Conta));
    }

    [Fact]
    public void Componente_repetido_na_mesma_etapa_vira_erro_em_portugues()
    {
        var estado = new EstadoVega
        {
            Componentes =
            [
                new ComponenteXml { Componente = "Microsoft-Windows-Shell-Setup", Fase = Pass.specialize, Xml = "<A />" },
                new ComponenteXml { Componente = "Microsoft-Windows-Shell-Setup", Fase = Pass.specialize, Xml = "<B />" },
            ],
        };
        var erro = Assert.Throws<ConfigurationException>(() => estado.GerarXml(new UnattendGenerator()));
        Assert.Contains("mais de uma vez", erro.Message);
    }

    private static readonly UnattendGenerator Gerador = new();

    private const string AppLockerVazio = """<AppLockerPolicy Version="1"><RuleCollection Type="Exe" EnforcementMode="NotConfigured" /></AppLockerPolicy>""";
    private const string WifiXml = """
        <WLANProfile xmlns="http://www.microsoft.com/networking/WLAN/profile/v1">
          <name>Casa</name>
          <SSIDConfig><SSID><name>Casa</name></SSID><nonBroadcast>false</nonBroadcast></SSIDConfig>
          <connectionType>ESS</connectionType>
          <connectionMode>auto</connectionMode>
          <MSM><security><authEncryption><authentication>open</authentication><encryption>none</encryption><useOneX>false</useOneX></authEncryption></security></MSM>
        </WLANProfile>
        """;

    /// <summary>Um estado em que cada grupo de opções está diferente do padrão do motor.</summary>
    private static EstadoVega EstadoCompleto()
    {
        var estado = new EstadoVega
        {
            UsarTeclado2 = true,
            UsarTeclado3 = true,
            Edicao = "pro",
            UsarChaveAtivacao = true,
            ChaveAtivacao = "AAAAA-BBBBB-CCCCC-DDDDD-EEEEE",
            Arquiteturas = [ProcessorArchitecture.amd64, ProcessorArchitecture.arm64],
            Conta = TipoConta.Pronta,
            Contas = [new ContaPronta { Nome = "Teste", Senha = "senha" }],
            Login = LoginAutomatico.PrimeiraConta,
            Expiracao = ModoExpiracao.Nunca,
            Bloqueio = ModoBloqueio.Desligado,
            AuditarProcessos = true,
            AuditarLinhaDeComando = true,
            NomeComputadorModo = ModoNomeComputador.Personalizado,
            NomeComputador = "VEGA-TESTE",
            FusoHorario = "E. South America Standard Time",
            Wifi = ModoWifi.Configurar,
            WifiNome = "Casa",
            WifiSenha = "senha-do-wifi",
            UsarAppLocker = true,
            AppLockerXml = AppLockerVazio,
            Scripts = [new ScriptUsuario { Conteudo = "Write-Output 'oi'" }],
            ReiniciarExplorador = true,
            Componentes = [new ComponenteXml { Componente = "Microsoft-Windows-Shell-Setup", Fase = Pass.specialize, Xml = "<TimeZone>UTC</TimeZone>" }],
            Privacidade = ExpressSettingsMode.Interactive,
            ConfigurarTeclas = true,
            Papel = ModoPapel.CorSolida,
            PersonalizarTelaBloqueio = true,
            ScriptTelaBloqueio = "[byte[]] @(1, 2, 3)",
            PersonalizarCores = true,
            Modo = ModoInstalacao.Script,
            BuscaBarra = TaskbarSearchMode.Hide,
            ArquivosOcultos = HideModes.None,
            FixadosIniciar = ModoFixados.Vazio,
            BlocosIniciar = ModoFixados.Vazio,
            IconesBarra = ModoFixados.Vazio,
            Efeitos = ModoEfeitos.Desempenho,
            PersonalizarIconesArea = true,
            Aderencia = ModoAderencia.Desligado,
            PersonalizarPastasIniciar = true,
        };
        estado.AppsRemovidos.Add("RemoveTeams");
        return estado;
    }

    [Fact]
    public void Todo_campo_nao_booleano_do_motor_muda_a_partir_do_vega()
    {
        var montada = EstadoCompleto().Montar(Gerador);
        var padrao = Configuration.Default;
        var campos = typeof(Configuration)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType != typeof(bool) && p.GetIndexParameters().Length == 0 && p.Name != "EqualityContract");

        var iguais = campos
            .Where(p => Equals(Descrever(p.GetValue(montada)), Descrever(p.GetValue(padrao))))
            .Select(p => p.Name)
            .ToList();

        Assert.Empty(iguais);
    }

    /// <summary>Os campos são classes sem igualdade por valor, então compara o tipo e o conteúdo simples.</summary>
    private static string Descrever(object? valor) => valor switch
    {
        null => "nulo",
        string or Enum => valor.ToString()!,
        System.Collections.IEnumerable lista => string.Join(",", lista.Cast<object>().Select(i => i.ToString())),
        _ => valor.ToString()!,
    };

    [Fact]
    public void Estado_completo_gera_um_xml_valido()
    {
        Assert.Contains("<unattend", Gerar(EstadoCompleto()));
    }

    [Fact]
    public void Salvar_e_abrir_devolve_a_mesma_configuracao()
    {
        var original = EstadoCompleto();
        original.OpcoesLigadas.Add(nameof(Configuration.DisableWidgets));
        original.RemoverEdge = true;

        var aberto = EstadoVega.Importar(original.Exportar());

        Assert.Equal(Gerar(original), Gerar(aberto));
    }

    [Fact]
    public void Abrir_mantem_os_padroes_sem_duplicar_listas()
    {
        var aberto = EstadoVega.Importar(new EstadoVega().Exportar());

        Assert.Single(aberto.Arquiteturas);
        Assert.Equal(Enum.GetValues<Effect>().Length, aberto.EfeitosLigados.Count);
    }

    [Theory]
    [InlineData(ModoEfeitos.Desempenho)]
    [InlineData(ModoEfeitos.Aparencia)]
    [InlineData(ModoEfeitos.Personalizado)]
    public void Efeitos_visuais_mudam_o_xml(ModoEfeitos modo)
    {
        Assert.NotEqual(Gerar(new EstadoVega()), Gerar(new EstadoVega { Efeitos = modo }));
    }

    [Theory]
    [InlineData(PlanoEnergia.AltoDesempenho, "SCHEME_MIN")]
    [InlineData(PlanoEnergia.DesempenhoMaximo, "e9a42b02-d5df-448d-aa00-03f14749eb61")]
    public void Plano_de_energia_entra_no_xml(PlanoEnergia plano, string esperado)
    {
        Assert.Contains(esperado, Gerar(new EstadoVega { Energia = plano }));
        Assert.DoesNotContain(esperado, Gerar(new EstadoVega()));
    }

    [Fact]
    public void Remover_o_edge_roda_no_primeiro_login_e_mantem_o_webview()
    {
        var xml = new System.Xml.XmlDocument();
        xml.LoadXml(Gerar(new EstadoVega { RemoverEdge = true }));
        var espacos = new System.Xml.XmlNamespaceManager(xml.NameTable);
        espacos.AddNamespace("s", Constants.MyNamespaceUri);

        var arquivos = xml.SelectNodes("//s:File", espacos)!.Cast<System.Xml.XmlElement>().ToList();
        var primeiroLogin = arquivos.Single(a => a.GetAttribute("path").EndsWith("FirstLogon.ps1")).InnerText;
        var especializacao = arquivos.SingleOrDefault(a => a.GetAttribute("path").EndsWith("Specialize.ps1"))?.InnerText ?? "";
        var script = arquivos.Single(a => a.GetAttribute("path").EndsWith("unattend-01.ps1")).InnerText;

        Assert.Contains("unattend-", primeiroLogin);
        Assert.DoesNotContain("MicrosoftEdgeElevationService", especializacao);
        Assert.DoesNotContain("EdgeWebView", script);
        Assert.DoesNotContain("EdgeCore", script);
        Assert.Contains(@"Microsoft\Edge'", script);
    }

    [Fact]
    public void Perfil_maximo_usa_desempenho_e_remove_o_edge()
    {
        var estado = new EstadoVega();
        Perfis.Todos.Single(p => p.Id == "maximo").Aplicar(estado);

        Assert.Equal(ModoEfeitos.Desempenho, estado.Efeitos);
        Assert.True(estado.RemoverEdge);
        Assert.Contains("<unattend", Gerar(estado));
    }

    [Fact]
    public void Nomes_em_portugues_cobrem_o_motor()
    {
        Assert.Empty(Enum.GetValues<Effect>().Except(Catalogo.NomesEfeitos.Keys));
        Assert.Empty(Enum.GetValues<StickyKeys>().Except(Catalogo.NomesAderencia.Keys));
        Assert.Empty(Gerador.DesktopIcons.Keys.Except(Catalogo.NomesIconesArea.Keys));
        Assert.Empty(Gerador.StartFolders.Keys.Except(Catalogo.NomesPastasIniciar.Keys));
    }

    [Theory]
    [InlineData(EstadoVega.EdicaoFirmware)]
    [InlineData(EstadoVega.EdicaoChave)]
    [InlineData("home")]
    public void Modos_de_edicao_geram_um_xml_valido(string edicao)
    {
        var estado = new EstadoVega { Edicao = edicao, ChaveProduto = "AAAAA-BBBBB-CCCCC-DDDDD-EEEEE" };

        Assert.Contains("<unattend", Gerar(estado));
    }

    [Theory]
    [InlineData(ModoWifi.Pular)]
    [InlineData(ModoWifi.Perguntar)]
    [InlineData(ModoWifi.Configurar)]
    [InlineData(ModoWifi.ArquivoXml)]
    public void Modos_de_wifi_geram_um_xml_valido(ModoWifi modo)
    {
        var estado = new EstadoVega { Wifi = modo, WifiNome = "Casa", WifiSenha = "senha-do-wifi", WifiXml = WifiXml };

        Assert.Contains("<unattend", Gerar(estado));
    }

    [Theory]
    [InlineData(ModoNomeComputador.Aleatorio)]
    [InlineData(ModoNomeComputador.Personalizado)]
    [InlineData(ModoNomeComputador.Perguntar)]
    [InlineData(ModoNomeComputador.Script)]
    public void Modos_de_nome_geram_um_xml_valido(ModoNomeComputador modo)
    {
        var estado = new EstadoVega { NomeComputadorModo = modo, NomeComputador = "VEGA", ScriptNomeComputador = "return 'VEGA';" };

        Assert.Contains("<unattend", Gerar(estado));
    }

    [Theory]
    [InlineData(LoginAutomatico.Nenhum, true)]
    [InlineData(LoginAutomatico.PrimeiraConta, false)]
    [InlineData(LoginAutomatico.Administrador, false)]
    public void Modos_de_login_geram_um_xml_valido(LoginAutomatico login, bool manterArquivos)
    {
        var estado = new EstadoVega
        {
            Conta = TipoConta.Pronta,
            Contas = [new ContaPronta { Nome = "Teste", Senha = "senha" }],
            Login = login,
            SenhaAdministrador = "admin",
        };
        if (manterArquivos)
        {
            estado.OpcoesLigadas.Add(nameof(Configuration.KeepSensitiveFiles));
        }

        Assert.Contains("<unattend", Gerar(estado));
    }

    [Fact]
    public void Todas_as_fases_e_tipos_de_script_geram_um_xml_valido()
    {
        foreach (var fase in Enum.GetValues<ScriptPhase>())
        {
            foreach (var tipo in fase.GetAllowedTypes())
            {
                var conteudo = tipo == ScriptType.Reg ? "Windows Registry Editor Version 5.00" : "rem teste";
                var estado = new EstadoVega { Scripts = [new ScriptUsuario { Fase = fase, Tipo = tipo, Conteudo = conteudo }] };

                Assert.Contains("<unattend", Gerar(estado));
            }
        }
    }

    [Theory]
    [InlineData(ModoFixados.Vazio)]
    public void Fixados_vazios_geram_um_xml_valido(ModoFixados modo)
    {
        var estado = new EstadoVega { FixadosIniciar = modo, BlocosIniciar = modo, IconesBarra = modo };

        Assert.Contains("<unattend", Gerar(estado));
    }

    private static string Gerar(EstadoVega estado) => Encoding.ASCII.GetString(estado.GerarXml(Gerador));
}
