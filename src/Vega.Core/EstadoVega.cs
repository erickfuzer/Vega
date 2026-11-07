using System.Collections.Immutable;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Schneegans.Unattend;
using BindingFlags = System.Reflection.BindingFlags;
using Formatting = Newtonsoft.Json.Formatting;

namespace Vega.Core;

public enum TipoConta { Local, Microsoft, Pronta }
public enum ModoInstalacao
{
    /// <summary>A instalação normal do Windows, com a tela de discos dela.</summary>
    TelaNormal,

    /// <summary>O Vega particiona e aplica a imagem por um script gerado.</summary>
    Script,

    /// <summary>Um script próprio substitui toda essa etapa.</summary>
    ScriptProprio,
}
public enum AlvoDisco { Perguntar, Criterios, Numero, ScriptProprio }
public enum Particionamento { ApagarDisco, AMao, ScriptDiskpart }
public enum OrigemImagem { Edicao, Perguntar, Indice, Nome }
public enum ArquivoPaginacao { Automatico, Nenhum, Personalizado }
public enum ModoWifi { Pular, Perguntar, Configurar, ArquivoXml }
public enum ModoNomeComputador { Aleatorio, Personalizado, Perguntar, Script }
public enum LoginAutomatico { Nenhum, PrimeiraConta, Administrador }
public enum ModoExpiracao { Padrao, Nunca, Personalizado }
public enum ModoBloqueio { Padrao, Desligado, Personalizado }
public enum ModoAderencia { Padrao, Desligado, Personalizado }
public enum ModoEfeitos { Padrao, Desempenho, Aparencia, Personalizado }
public enum ModoFixados { Padrao, Vazio, Personalizado }
public enum ModoPapel { Padrao, CorSolida, Script }
public enum PlanoEnergia { Padrao, AltoDesempenho, DesempenhoMaximo }

public sealed class ContaPronta
{
    public string Nome { get; set; } = "";
    public string NomeExibido { get; set; } = "";
    public string Senha { get; set; } = "";
    public string Grupo { get; set; } = Constants.AdministratorsGroup;
}

public sealed class ScriptUsuario
{
    public ScriptPhase Fase { get; set; } = ScriptPhase.FirstLogon;
    public ScriptType Tipo { get; set; } = ScriptType.Ps1;
    public string Conteudo { get; set; } = "";
}

public sealed class ComponenteXml
{
    public string Componente { get; set; } = "";
    public Pass Fase { get; set; } = Pass.specialize;
    public string Xml { get; set; } = "";
}

/// <summary>O que a pessoa escolheu na tela. Vira uma <see cref="Configuration"/> na hora de gerar.</summary>
public sealed class EstadoVega
{
    /// <summary>Valores especiais de <see cref="Edicao"/>, além dos ids das edições e de vazio para perguntar.</summary>
    public const string EdicaoFirmware = "@firmware";
    public const string EdicaoChave = "@chave";

    public HashSet<string> OpcoesLigadas { get; set; } = [];
    public HashSet<string> AppsRemovidos { get; set; } = [];

    // Idioma e teclado
    public bool EscolherIdiomaNaInstalacao { get; set; }
    public string IdiomaImagem { get; set; } = "pt-BR";
    public string IdiomaRegional { get; set; } = "pt-BR";
    public string Teclado { get; set; } = "00010416";
    public string Regiao { get; set; } = "32";
    public bool UsarTeclado2 { get; set; }
    public string IdiomaRegional2 { get; set; } = "en-US";
    public string Teclado2 { get; set; } = "00000409";
    public bool UsarTeclado3 { get; set; }
    public string IdiomaRegional3 { get; set; } = "en-GB";
    public string Teclado3 { get; set; } = "00000809";

    // Edição e arquitetura
    public string Edicao { get; set; } = "";
    public string ChaveProduto { get; set; } = "";
    public bool UsarChaveAtivacao { get; set; }
    public string ChaveAtivacao { get; set; } = "";
    public HashSet<ProcessorArchitecture> Arquiteturas { get; set; } = [ProcessorArchitecture.amd64];
    public bool IgnorarRequisitos { get; set; } = true;
    public bool UsarConfigurationSet { get; set; }

    // Conta e rede
    public TipoConta Conta { get; set; } = TipoConta.Local;
    public List<ContaPronta> Contas { get; set; } = [];
    public LoginAutomatico Login { get; set; } = LoginAutomatico.PrimeiraConta;
    public string SenhaAdministrador { get; set; } = "";
    public bool OcultarSenhas { get; set; } = true;
    public ModoExpiracao Expiracao { get; set; } = ModoExpiracao.Nunca;
    public int ExpiracaoDias { get; set; } = DefaultPasswordExpirationSettings.MaxAge;
    public ModoBloqueio Bloqueio { get; set; } = ModoBloqueio.Padrao;
    public int BloqueioTentativas { get; set; } = 10;
    public int BloqueioDuracao { get; set; } = 10;
    public int BloqueioJanela { get; set; } = 10;
    public ModoNomeComputador NomeComputadorModo { get; set; } = ModoNomeComputador.Aleatorio;
    public string NomeComputador { get; set; } = "";
    public string ScriptNomeComputador { get; set; } = "";
    public string FusoHorario { get; set; } = "";
    public ModoWifi Wifi { get; set; } = ModoWifi.Pular;
    public string WifiNome { get; set; } = "";
    public string WifiSenha { get; set; } = "";
    public WifiAuthentications WifiAutenticacao { get; set; } = WifiAuthentications.WPA2PSK;
    public bool WifiConectarSozinho { get; set; } = true;
    public bool WifiOculta { get; set; }
    public string WifiXml { get; set; } = "";
    public ExpressSettingsMode Privacidade { get; set; } = ExpressSettingsMode.DisableAll;

    // Interface
    public TaskbarSearchMode BuscaBarra { get; set; } = TaskbarSearchMode.Box;
    public HideModes ArquivosOcultos { get; set; } = HideModes.Hidden;

    // Teclado
    public bool ConfigurarTeclas { get; set; }
    public LockKeyInitial CapsLockInicial { get; set; } = LockKeyInitial.Off;
    public LockKeyBehavior CapsLockComportamento { get; set; } = LockKeyBehavior.Toggle;
    public LockKeyInitial NumLockInicial { get; set; } = LockKeyInitial.On;
    public LockKeyBehavior NumLockComportamento { get; set; } = LockKeyBehavior.Toggle;
    public LockKeyInitial ScrollLockInicial { get; set; } = LockKeyInitial.Off;
    public LockKeyBehavior ScrollLockComportamento { get; set; } = LockKeyBehavior.Toggle;
    public ModoAderencia Aderencia { get; set; } = ModoAderencia.Padrao;
    public HashSet<StickyKeys> AderenciaOpcoes { get; set; } =
        [StickyKeys.HotKeyActive, StickyKeys.Indicator, StickyKeys.AudibleFeedback, StickyKeys.HotKeySound];

    // Desempenho
    public ModoEfeitos Efeitos { get; set; } = ModoEfeitos.Padrao;
    public HashSet<Effect> EfeitosLigados { get; set; } = [.. Enum.GetValues<Effect>()];
    public PlanoEnergia Energia { get; set; } = PlanoEnergia.Padrao;

    // Aparência
    public bool PersonalizarCores { get; set; }
    public ColorTheme TemaSistema { get; set; } = ColorTheme.Dark;
    public ColorTheme TemaApps { get; set; } = ColorTheme.Dark;
    public bool Transparencia { get; set; } = true;
    public bool CorNoIniciar { get; set; }
    public bool CorNasBordas { get; set; }
    public string CorDestaque { get; set; } = "#0078D4";
    public ModoPapel Papel { get; set; } = ModoPapel.Padrao;
    public string CorPapel { get; set; } = "#000000";
    public string ScriptPapel { get; set; } = "";
    public bool PersonalizarTelaBloqueio { get; set; }
    public string ScriptTelaBloqueio { get; set; } = "";
    public ModoFixados FixadosIniciar { get; set; } = ModoFixados.Padrao;
    public string FixadosIniciarJson { get; set; } = "";
    public ModoFixados BlocosIniciar { get; set; } = ModoFixados.Padrao;
    public string BlocosIniciarXml { get; set; } = "";
    public ModoFixados IconesBarra { get; set; } = ModoFixados.Padrao;
    public string IconesBarraXml { get; set; } = "";
    public bool PersonalizarIconesArea { get; set; }
    public HashSet<string> IconesArea { get; set; } = ["RecycleBin"];
    public bool PersonalizarPastasIniciar { get; set; }
    public HashSet<string> PastasIniciar { get; set; } = [];

    // Edge
    public bool RemoverEdge { get; set; }

    // Avançado
    public bool AuditarProcessos { get; set; }
    public bool AuditarLinhaDeComando { get; set; }
    public bool UsarAppLocker { get; set; }
    public string AppLockerXml { get; set; } = "";
    public List<ScriptUsuario> Scripts { get; set; } = [];
    public bool ReiniciarExplorador { get; set; }
    public List<ComponenteXml> Componentes { get; set; } = [];

    /// <summary>Com esse nome, o Windows não acha o arquivo sozinho e o pendrive não inicia a instalação.</summary>
    public bool NomeSemAutoInstalar { get; set; }

    // Disco e imagem
    public ModoInstalacao Modo { get; set; } = ModoInstalacao.TelaNormal;
    public string ScriptInstalacao { get; set; } = "";
    public Particionamento Particoes { get; set; } = Particionamento.ApagarDisco;
    public string ScriptDiskpart { get; set; } = "";
    public AlvoDisco Alvo { get; set; } = AlvoDisco.Perguntar;
    public int DiscoNumero { get; set; }
    public string ScriptAlvoDisco { get; set; } = "";
    public int? CriterioMinimoGiB { get; set; } = Constants.TargetDiskMinSizeGiB;
    public int? CriterioMaximoGiB { get; set; } = Constants.TargetDiskMaxSizeGiB;
    public int? CriterioNumero { get; set; }
    public bool CriterioSemParticoes { get; set; } = true;
    public bool CriterioTipoInterno { get; set; } = true;
    public bool CriterioMidiaFixa { get; set; } = true;
    public PartitionLayout Layout { get; set; } = PartitionLayout.Automatic;
    public RecoveryMode Recuperacao { get; set; } = RecoveryMode.Partition;
    public int TamanhoSistemaMiB { get; set; } = Constants.SystemPartitionSize;
    public int TamanhoRecuperacaoMiB { get; set; } = Constants.RecoveryPartitionSize;
    public OrigemImagem Origem { get; set; } = OrigemImagem.Edicao;
    public int ImagemIndice { get; set; } = 1;
    public string ImagemNome { get; set; } = "";
    public ArquivoPaginacao Paginacao { get; set; } = ArquivoPaginacao.Automatico;
    public int PaginacaoInicialMiB { get; set; } = 1024;
    public int PaginacaoMaximaMiB { get; set; } = 4096;
    public bool DesligarDefender { get; set; }
    public bool DesligarNomes8Dot3 { get; set; }
    public bool PausarAntesDeFormatar { get; set; } = true;
    public bool PausarAntesDeReiniciar { get; set; }
    public bool CompactarWindows { get; set; }
    public bool VerificarImagem { get; set; } = true;

    /// <summary>Verdadeiro quando o XML pode apagar um disco sem passar pela tela de discos do Windows.</summary>
    [JsonIgnore]
    public bool ApagaDisco => Modo switch
    {
        ModoInstalacao.Script => Particoes != Particionamento.AMao,
        ModoInstalacao.ScriptProprio => true,
        _ => false,
    };

    /// <summary>Verdadeiro quando o disco pode ser apagado sem nenhuma pergunta nem pausa.</summary>
    [JsonIgnore]
    public bool ApagaSemPerguntar =>
        Modo == ModoInstalacao.Script
        && !PausarAntesDeFormatar
        && (Particoes == Particionamento.ScriptDiskpart
            || (Particoes == Particionamento.ApagarDisco && Alvo != AlvoDisco.Perguntar));

    /// <summary>Palavras que o motor procura no script do diskpart para saber se ele cria a recuperação.</summary>
    private static readonly string[] PistasDeRecuperacao = ["SET ID=27", "LABEL=\"Recovery\"", "SET ID=\"de94bba4-06d1-4d40-a16a-bfd50179d6ac\""];

    /// <summary>Verdadeiro quando o motor vai apagar o WinRE porque o script do diskpart não cria a partição dele.</summary>
    [JsonIgnore]
    public bool DiskpartApagaRecuperacao =>
        Modo == ModoInstalacao.Script
        && Particoes == Particionamento.ScriptDiskpart
        && !PistasDeRecuperacao.Any(pista => ScriptDiskpart.Contains(pista, StringComparison.OrdinalIgnoreCase));

    [JsonIgnore]
    public string NomeDoArquivo => NomeSemAutoInstalar ? "notautounattend.xml" : "autounattend.xml";

    /// <summary>O que não muda ao trocar de perfil: idioma, edição, identidade da máquina e todo texto digitado.</summary>
    private static readonly HashSet<string> MantidosPelosPerfis =
    [
        nameof(EscolherIdiomaNaInstalacao), nameof(IdiomaImagem), nameof(IdiomaRegional), nameof(Teclado), nameof(Regiao),
        nameof(UsarTeclado2), nameof(IdiomaRegional2), nameof(Teclado2), nameof(UsarTeclado3), nameof(IdiomaRegional3), nameof(Teclado3),
        nameof(Edicao), nameof(ChaveProduto), nameof(UsarChaveAtivacao), nameof(ChaveAtivacao), nameof(Arquiteturas),
        nameof(Contas), nameof(Login), nameof(SenhaAdministrador), nameof(OcultarSenhas),
        nameof(NomeComputadorModo), nameof(NomeComputador), nameof(ScriptNomeComputador), nameof(FusoHorario),
        nameof(WifiNome), nameof(WifiSenha), nameof(WifiAutenticacao), nameof(WifiConectarSozinho), nameof(WifiOculta), nameof(WifiXml),
        nameof(ScriptInstalacao), nameof(ScriptDiskpart), nameof(ScriptAlvoDisco), nameof(ScriptPapel), nameof(ScriptTelaBloqueio),
        nameof(FixadosIniciarJson), nameof(BlocosIniciarXml), nameof(IconesBarraXml), nameof(AppLockerXml),
        nameof(Scripts), nameof(Componentes), nameof(NomeSemAutoInstalar),
    ];

    private static IEnumerable<System.Reflection.PropertyInfo> CamposGravaveis =>
        typeof(EstadoVega).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite);

    /// <summary>Volta tudo ao padrão, menos o que <see cref="MantidosPelosPerfis"/> lista.</summary>
    public void Limpar()
    {
        var padrao = new EstadoVega();
        foreach (var campo in CamposGravaveis.Where(p => !MantidosPelosPerfis.Contains(p.Name)))
        {
            campo.SetValue(this, campo.GetValue(padrao));
        }
    }

    /// <summary>Quantos ajustes fogem do padrão, somando todas as categorias.</summary>
    public int ContarAlteracoes() => Enum.GetValues<Categoria>().Sum(ContarAlteracoes);

    /// <summary>Quantos ajustes fogem do padrão em uma categoria, para o contador da tela.</summary>
    public int ContarAlteracoes(Categoria categoria)
    {
        if (categoria == Categoria.Apps)
        {
            return AppsRemovidos.Count;
        }

        var opcoes = Catalogo.Opcoes.Count(o => o.Categoria == categoria && OpcoesLigadas.Contains(o.Id));
        return opcoes + categoria switch
        {
            Categoria.Instalacao => Contar(
                EscolherIdiomaNaInstalacao,
                UsarTeclado2 && !EscolherIdiomaNaInstalacao,
                UsarTeclado2 && UsarTeclado3 && !EscolherIdiomaNaInstalacao,
                !string.IsNullOrEmpty(Edicao),
                UsarChaveAtivacao,
                !IgnorarRequisitos && Modo == ModoInstalacao.TelaNormal,
                !Arquiteturas.SetEquals([ProcessorArchitecture.amd64])),
            Categoria.Disco => Contar(Modo != ModoInstalacao.TelaNormal, UsarConfigurationSet && Modo == ModoInstalacao.TelaNormal),
            Categoria.Seguranca => Contar(
                Modo == ModoInstalacao.Script && DesligarDefender,
                AuditarProcessos,
                UsarAppLocker),
            Categoria.Conta => Contar(
                Conta != TipoConta.Local,
                Expiracao != ModoExpiracao.Nunca,
                Bloqueio != ModoBloqueio.Padrao,
                NomeComputadorModo != ModoNomeComputador.Aleatorio,
                !string.IsNullOrEmpty(FusoHorario),
                Wifi != ModoWifi.Pular,
                Privacidade != ExpressSettingsMode.DisableAll),
            Categoria.Desempenho => Contar(Efeitos != ModoEfeitos.Padrao, Energia != PlanoEnergia.Padrao),
            Categoria.Interface => Contar(
                ConfigurarTeclas,
                Aderencia != ModoAderencia.Padrao,
                BuscaBarra != TaskbarSearchMode.Box,
                ArquivosOcultos != HideModes.Hidden),
            Categoria.Aparencia => Contar(
                PersonalizarCores,
                Papel != ModoPapel.Padrao,
                PersonalizarTelaBloqueio,
                FixadosIniciar != ModoFixados.Padrao,
                BlocosIniciar != ModoFixados.Padrao,
                IconesBarra != ModoFixados.Padrao,
                PersonalizarIconesArea,
                PersonalizarPastasIniciar),
            Categoria.Edge => Contar(RemoverEdge),
            Categoria.Avancado =>
                Scripts.Count(s => !string.IsNullOrWhiteSpace(s.Conteudo))
                + Componentes.Count(c => !string.IsNullOrWhiteSpace(c.Xml))
                + Contar(ReiniciarExplorador, NomeSemAutoInstalar),
            _ => 0,
        };
    }

    private static int Contar(params bool[] ajustes) => ajustes.Count(a => a);

    private static readonly JsonSerializerSettings ConfiguracaoJson = new()
    {
        ObjectCreationHandling = ObjectCreationHandling.Replace,
        Converters = [new StringEnumConverter()],
    };

    /// <summary>A configuração em JSON. Pode conter senhas e chaves digitadas.</summary>
    public string Exportar() => JsonConvert.SerializeObject(this, Formatting.Indented, ConfiguracaoJson);

    internal JObject ParaJson() => JObject.FromObject(this, JsonSerializer.Create(ConfiguracaoJson));

    /// <summary>Abre um arquivo salvo por <see cref="Exportar"/>. Recusa um JSON que não seja do Vega.</summary>
    public static EstadoVega Importar(string json)
    {
        var objeto = JObject.Parse(json);
        if (!objeto.ContainsKey(nameof(OpcoesLigadas)) || !objeto.ContainsKey(nameof(AppsRemovidos)))
        {
            throw new JsonException("Esse JSON não é uma configuração do Vega.");
        }

        return DeJson(objeto);
    }

    /// <summary>Monta um estado a partir do padrão e do que veio no JSON, que pode ter sido editado à mão.</summary>
    internal static EstadoVega DeJson(JObject objeto)
    {
        var estado = new EstadoVega();
        JsonConvert.PopulateObject(objeto.ToString(Formatting.None), estado, ConfiguracaoJson);
        estado.Sanear();
        return estado;
    }

    /// <summary>A tela não espera nulos nem valores fora dos enums, e um JSON editado à mão pode trazer os dois.</summary>
    private void Sanear()
    {
        var padrao = new EstadoVega();
        foreach (var campo in CamposGravaveis)
        {
            var valor = campo.GetValue(this);
            if (valor is null && Nullable.GetUnderlyingType(campo.PropertyType) is null)
            {
                campo.SetValue(this, campo.GetValue(padrao));
            }
            else if (campo.PropertyType.IsEnum && !Enum.IsDefined(campo.PropertyType, valor!))
            {
                throw new JsonException($"O valor de {campo.Name} é inválido.");
            }
        }

        OpcoesLigadas.RemoveWhere(item => item is null);
        AppsRemovidos.RemoveWhere(item => item is null);
        IconesArea.RemoveWhere(item => item is null);
        PastasIniciar.RemoveWhere(item => item is null);

        Contas.RemoveAll(item => item is null);
        foreach (var conta in Contas)
        {
            conta.Nome ??= "";
            conta.NomeExibido ??= "";
            conta.Senha ??= "";
            conta.Grupo ??= Constants.UsersGroup;
        }

        Scripts.RemoveAll(item => item is null);
        foreach (var script in Scripts)
        {
            script.Conteudo ??= "";
            if (!Enum.IsDefined(script.Fase) || !Enum.IsDefined(script.Tipo))
            {
                throw new JsonException("Um script tem etapa ou tipo inválido.");
            }
        }

        Componentes.RemoveAll(item => item is null);
        foreach (var componente in Componentes)
        {
            componente.Componente ??= "";
            componente.Xml ??= "";
            if (!Enum.IsDefined(componente.Fase))
            {
                throw new JsonException("Um componente tem etapa inválida.");
            }
        }
    }

    /// <summary>
    /// Componentes e etapas que o motor já preencheu. Um XML próprio neles substitui o que foi gerado,
    /// o que pode tirar scripts, contas ou o nome do computador. Vazio se a configuração ainda não gera.
    /// </summary>
    public IReadOnlySet<(string Componente, Pass Fase)> ComponentesJaUsados(UnattendGenerator gerador)
    {
        var usados = new HashSet<(string, Pass)>();
        try
        {
            var documento = gerador.GenerateXml(Montar(gerador) with { Components = ImmutableDictionary<ComponentAndPass, string>.Empty });
            var espacos = new XmlNamespaceManager(documento.NameTable);
            espacos.AddNamespace("u", "urn:schemas-microsoft-com:unattend");
            foreach (var etapa in documento.SelectNodes("/u:unattend/u:settings", espacos)!.Cast<XmlElement>())
            {
                if (!Enum.TryParse<Pass>(etapa.GetAttribute("pass"), out var fase))
                {
                    continue;
                }

                foreach (var componente in etapa.SelectNodes("u:component", espacos)!.Cast<XmlElement>().Where(c => c.HasChildNodes))
                {
                    usados.Add((componente.GetAttribute("name"), fase));
                }
            }
        }
        catch (Exception)
        {
            // Este resultado só alimenta um aviso. O erro de verdade aparece ao gerar o XML.
            usados.Clear();
        }

        return usados;
    }

    public Configuration Montar(UnattendGenerator gerador)
    {
        var config = Configuration.Default with
        {
            LanguageSettings = MontarIdioma(gerador),
            AccountSettings = MontarConta(),
            LockoutSettings = Bloqueio switch
            {
                ModoBloqueio.Desligado => new DisableLockoutSettings(),
                ModoBloqueio.Personalizado => new CustomLockoutSettings(BloqueioTentativas, BloqueioDuracao, BloqueioJanela),
                _ => new DefaultLockoutSettings(),
            },
            PasswordExpirationSettings = Expiracao switch
            {
                ModoExpiracao.Nunca => new UnlimitedPasswordExpirationSettings(),
                ModoExpiracao.Personalizado => new CustomPasswordExpirationSettings(ExpiracaoDias),
                _ => new DefaultPasswordExpirationSettings(),
            },
            ProcessAuditSettings = AuditarProcessos
                ? new EnabledProcessAuditSettings(AuditarLinhaDeComando)
                : new DisabledProcessAuditSettings(),
            ComputerNameSettings = NomeComputadorModo switch
            {
                ModoNomeComputador.Personalizado => new CustomComputerNameSettings(NomeComputador),
                ModoNomeComputador.Perguntar => new PromptComputerNameSettings(),
                ModoNomeComputador.Script => new ScriptComputerNameSettings(ScriptNomeComputador),
                _ => new RandomComputerNameSettings(),
            },
            TimeZoneSettings = string.IsNullOrEmpty(FusoHorario)
                ? new ImplicitTimeZoneSettings()
                : gerador.CreateExplicitTimeZoneSettings(FusoHorario),
            WifiSettings = Wifi switch
            {
                ModoWifi.Perguntar => new InteractiveWifiSettings(),
                ModoWifi.Configurar => new ParameterizedWifiSettings(WifiNome, WifiSenha, WifiConectarSozinho, WifiAutenticacao, WifiOculta),
                ModoWifi.ArquivoXml => new XmlWifiSettings(WifiXml),
                _ => new SkipWifiSettings(),
            },
            AppLockerSettings = UsarAppLocker ? new ConfigureAppLockerSettings(AppLockerXml) : new SkipAppLockerSettings(),
            ProcessorArchitectures = [.. Arquiteturas],
            Components = Componentes
                .Where(c => !string.IsNullOrWhiteSpace(c.Xml))
                .ToImmutableDictionary(c => new ComponentAndPass(c.Componente, c.Fase), c => c.Xml),
            Bloatwares = AppsRemovidos.Select(gerador.Lookup<Bloatware>).ToImmutableList(),
            ExpressSettings = Privacidade,
            ScriptSettings = new ScriptSettings(MontarScripts(), ReiniciarExplorador),
            LockKeySettings = ConfigurarTeclas
                ? new ConfigureLockKeySettings(
                    new LockKeySetting(CapsLockInicial, CapsLockComportamento),
                    new LockKeySetting(NumLockInicial, NumLockComportamento),
                    new LockKeySetting(ScrollLockInicial, ScrollLockComportamento))
                : new SkipLockKeySettings(),
            WallpaperSettings = Papel switch
            {
                ModoPapel.CorSolida => new SolidWallpaperSettings(ColorTranslator.FromHtml(CorPapel)),
                ModoPapel.Script => new ScriptWallpaperSettings(ScriptPapel),
                _ => new DefaultWallpaperSettings(),
            },
            LockScreenSettings = PersonalizarTelaBloqueio
                ? new ScriptLockScreenSettings(ScriptTelaBloqueio)
                : new DefaultLockScreenSettings(),
            ColorSettings = PersonalizarCores
                ? new CustomColorSettings(TemaSistema, TemaApps, Transparencia, CorNoIniciar, CorNasBordas, ColorTranslator.FromHtml(CorDestaque))
                : new DefaultColorSettings(),
            PESettings = MontarInstalacao(gerador),
            ActivationKey = UsarChaveAtivacao ? new ProductKey(ChaveAtivacao) : null,
            TaskbarSearch = BuscaBarra,
            StartPinsSettings = FixadosIniciar switch
            {
                ModoFixados.Vazio => new EmptyStartPinsSettings(),
                ModoFixados.Personalizado => new CustomStartPinsSettings(FixadosIniciarJson),
                _ => new DefaultStartPinsSettings(),
            },
            StartTilesSettings = BlocosIniciar switch
            {
                ModoFixados.Vazio => new EmptyStartTilesSettings(),
                ModoFixados.Personalizado => new CustomStartTilesSettings(BlocosIniciarXml),
                _ => new DefaultStartTilesSettings(),
            },
            TaskbarIcons = IconesBarra switch
            {
                ModoFixados.Vazio => new EmptyTaskbarIcons(),
                ModoFixados.Personalizado => new CustomTaskbarIcons(IconesBarraXml),
                _ => new DefaultTaskbarIcons(),
            },
            Effects = Efeitos switch
            {
                ModoEfeitos.Desempenho => new BestPerformanceEffects(),
                ModoEfeitos.Aparencia => new BestAppearanceEffects(),
                ModoEfeitos.Personalizado => new CustomEffects(
                    Enum.GetValues<Effect>().ToImmutableDictionary(e => e, EfeitosLigados.Contains)),
                _ => new DefaultEffects(),
            },
            DesktopIcons = PersonalizarIconesArea
                ? new CustomDesktopIconSettings(gerador.DesktopIcons.Values.ToDictionary(i => i, i => IconesArea.Contains(i.Id)))
                : new DefaultDesktopIconSettings(),
            StickyKeysSettings = Aderencia switch
            {
                ModoAderencia.Desligado => new DisabledStickyKeysSettings(),
                ModoAderencia.Personalizado => new CustomStickyKeysSettings(AderenciaOpcoes),
                _ => new DefaultStickyKeysSettings(),
            },
            StartFolderSettings = PersonalizarPastasIniciar
                ? new CustomStartFolderSettings(gerador.StartFolders.Values.ToDictionary(p => p, p => PastasIniciar.Contains(p.Id)))
                : new DefaultStartFolderSettings(),
            HideFiles = ArquivosOcultos,
        };

        foreach (var opcao in Catalogo.Opcoes)
        {
            config = opcao.Aplicar(config, OpcoesLigadas.Contains(opcao.Id));
        }

        return config;
    }

    public byte[] GerarXml(UnattendGenerator gerador)
    {
        var problemas = Validacao.Problemas(this, gerador);
        if (problemas.Count > 0)
        {
            throw new ConfigurationException(string.Join(" ", problemas));
        }

        var documento = gerador.GenerateXml(Montar(gerador));
        CorrigirRotulosDoPe(documento);
        documento.DocumentElement!.PrependChild(documento.CreateComment(Compartilhamento.Link(this)));
        return UnattendGenerator.Serialize(documento);
    }

    /// <summary>
    /// Contorna um bug do motor: quando o disco e a imagem são perguntados na hora, o pe.cmd ganha
    /// dois rótulos :choice. Enter em branco na imagem volta para a pergunta do disco e o diskpart
    /// roda de novo, e Enter em branco no disco pula o particionamento. O novo nome tem o mesmo
    /// tamanho, para não estourar o limite de caracteres de cada linha.
    /// </summary>
    private void CorrigirRotulosDoPe(XmlDocument documento)
    {
        // Só o script gerado pelo motor. Um script escrito pela pessoa fica como ela escreveu.
        if (Modo != ModoInstalacao.Script)
        {
            return;
        }

        var espacos = new XmlNamespaceManager(documento.NameTable);
        espacos.AddNamespace("u", "urn:schemas-microsoft-com:unattend");
        Trocar(documento.SelectNodes("/u:unattend/u:settings[@pass='windowsPE']//u:Path", espacos)!.Cast<XmlNode>());

        // O motor também grava uma cópia do script, só para consulta, que precisa continuar igual.
        if (documento.SelectSingleNode("//*[local-name()='PEScriptCopy']") is { } copia)
        {
            Trocar([copia]);
        }

        static void Trocar(IEnumerable<XmlNode> nos)
        {
            var rotulos = 0;
            var saltos = 0;
            foreach (var no in nos.ToList())
            {
                var novo = Regex.Replace(no.InnerText, @"(?m)(?:echo:|^[ \t]*):choice\b|goto :choice\b", achado =>
                {
                    var ordem = achado.Value.StartsWith("goto") ? ++saltos : ++rotulos;
                    return ordem < 2 ? achado.Value : achado.Value.Replace(":choice", ":imagem");
                });
                if (novo != no.InnerText)
                {
                    no.InnerText = novo;
                }
            }
        }
    }

    private ILanguageSettings MontarIdioma(UnattendGenerator gerador)
    {
        if (EscolherIdiomaNaInstalacao)
        {
            return new InteractiveLanguageSettings();
        }

        LocaleAndKeyboard Par(string idioma, string teclado) => new(
            gerador.Lookup<UserLocale>(idioma),
            gerador.Lookup<KeyboardIdentifier>(teclado));

        return new UnattendedLanguageSettings(
            ImageLanguage: gerador.Lookup<ImageLanguage>(IdiomaImagem),
            LocaleAndKeyboard: Par(IdiomaRegional, Teclado),
            LocaleAndKeyboard2: UsarTeclado2 ? Par(IdiomaRegional2, Teclado2) : null,
            LocaleAndKeyboard3: UsarTeclado2 && UsarTeclado3 ? Par(IdiomaRegional3, Teclado3) : null,
            GeoLocation: gerador.Lookup<GeoLocation>(Regiao));
    }

    private IAccountSettings MontarConta() => Conta switch
    {
        TipoConta.Microsoft => new InteractiveMicrosoftAccountSettings(),
        TipoConta.Pronta => new UnattendedAccountSettings(
            Contas.Select(c => new Account(c.Nome, string.IsNullOrEmpty(c.NomeExibido) ? c.Nome : c.NomeExibido, c.Senha, c.Grupo)).ToImmutableList(),
            Login switch
            {
                LoginAutomatico.PrimeiraConta => new OwnAutoLogonSettings(),
                LoginAutomatico.Administrador => new BuiltinAutoLogonSettings(SenhaAdministrador),
                _ => new NoneAutoLogonSettings(),
            },
            OcultarSenhas),
        _ => new InteractiveLocalAccountSettings(),
    };

    private List<Script> MontarScripts()
    {
        var lista = Scripts
            .Where(s => !string.IsNullOrWhiteSpace(s.Conteudo))
            .Select(s => new Script(s.Conteudo, s.Fase, s.Tipo))
            .ToList();
        if (RemoverEdge)
        {
            lista.Add(new Script(ScriptsVega.RemoverEdge, ScriptPhase.FirstLogon, ScriptType.Ps1));
        }
        if (Energia != PlanoEnergia.Padrao)
        {
            lista.Add(new Script(ScriptsVega.PlanoDeEnergia(Energia), ScriptPhase.FirstLogon, ScriptType.Ps1));
        }
        return lista;
    }

    private IEditionSettings MontarEdicao(UnattendGenerator gerador) => Edicao switch
    {
        "" => new InteractiveEditionSettings(),
        EdicaoFirmware => new FirmwareEditionSettings(),
        EdicaoChave => new CustomEditionSettings(new ProductKey(ChaveProduto)),
        _ => new UnattendedEditionSettings(gerador.Lookup<WindowsEdition>(Edicao)),
    };

    private IPESettings MontarInstalacao(UnattendGenerator gerador) => Modo switch
    {
        ModoInstalacao.Script => new GeneratePESettings(
            PartitionSettings: MontarParticoes(),
            InstallFromSettings: MontarOrigem(gerador),
            PagingFileSettings: Paginacao switch
            {
                ArquivoPaginacao.Nenhum => new NoPagingFileSettings(),
                ArquivoPaginacao.Personalizado => new CustomPagingFileSettings(PaginacaoInicialMiB, PaginacaoMaximaMiB),
                _ => new AutomaticPagingFileSettings(),
            },
            DisableDefender: DesligarDefender,
            Disable8Dot3Names: DesligarNomes8Dot3,
            PauseBeforeFormatting: PausarAntesDeFormatar,
            PauseBeforeReboot: PausarAntesDeReiniciar,
            CompactOs: CompactarWindows,
            SkipIntegrityCheck: !VerificarImagem),
        ModoInstalacao.ScriptProprio => new ScriptPESetttings(ScriptInstalacao),
        _ => new DefaultPESettings(
            EditionSettings: MontarEdicao(gerador),
            BypassRequirementsCheck: IgnorarRequisitos,
            UseConfigurationSet: UsarConfigurationSet),
    };

    private IPartitionSettings MontarParticoes() => Particoes switch
    {
        Particionamento.AMao => new InteractivePartitionSettings(),
        Particionamento.ScriptDiskpart => new CustomPartitionSettings(ScriptDiskpart),
        _ => new UnattendedPartitionSettings(
            TargetDiskSettings: Alvo switch
            {
                AlvoDisco.Numero => new FixedTargetDiskSettings(DiscoNumero),
                AlvoDisco.ScriptProprio => new ScriptTargetDiskSettings(ScriptAlvoDisco),
                AlvoDisco.Criterios => new GeneratedTargetDiskSettings(
                    minSizeGiB: CriterioMinimoGiB,
                    maxSizeGiB: CriterioMaximoGiB,
                    index: CriterioNumero,
                    assertNoPartitions: CriterioSemParticoes,
                    assertInterfaceType: CriterioTipoInterno,
                    assertMediaType: CriterioMidiaFixa),
                _ => new InteractiveTargetDiskSettings(),
            },
            PartitionLayout: Layout,
            RecoveryMode: Recuperacao,
            SystemSize: TamanhoSistemaMiB,
            RecoverySize: TamanhoRecuperacaoMiB),
    };

    private IInstallFromSettings MontarOrigem(UnattendGenerator gerador) => Origem switch
    {
        OrigemImagem.Indice => new IndexInstallFromSettings(ImagemIndice),
        OrigemImagem.Nome => new NameInstallFromSettings(ImagemNome),
        OrigemImagem.Edicao when gerador.WindowsEditions.ContainsKey(Edicao) =>
            new EditionInstallFromSettings(gerador.Lookup<WindowsEdition>(Edicao)),
        _ => new InteractiveInstallFromSettings(),
    };
}
