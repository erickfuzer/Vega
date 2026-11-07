using System.Net;
using System.Text.RegularExpressions;
using Schneegans.Unattend;

namespace Vega.Core;

/// <summary>
/// Reabre um autounattend.xml. O gerador original grava no comentário do arquivo o endereço com
/// todas as opções usadas, e o Vega grava a própria configuração do mesmo jeito.
/// </summary>
public static partial class ImportadorXml
{
    /// <summary>Parâmetros do formulário original fora os que vêm do catálogo. O teste confere contra a lista do site.</summary>
    private static readonly HashSet<string> Conhecidos =
    [
        "LanguageMode", "UILanguage", "Locale", "Keyboard", "GeoLocation", "UseKeyboard2", "Locale2", "Keyboard2", "UseKeyboard3", "Locale3", "Keyboard3",
        "PEMode", "WindowsEditionMode", "WindowsEdition", "ProductKey", "BypassRequirementsCheck", "UseConfigurationSet",
        "Disable8Dot3Names", "DisableDefender", "PauseBeforeFormatting", "PauseBeforeReboot", "CompactOs", "SkipIntegrityCheck",
        "InstallFromMode", "InstallFromEdition", "InstallFromName", "InstallFromIndex",
        "PartitionMode", "TargetDiskMode", "TargetDiskNoPartitions", "TargetDiskInterfaceType", "TargetDiskMediaType", "TargetDiskSize",
        "TargetDiskMinSize", "TargetDiskMaxSize", "TargetDiskIndex", "TargetDisk", "TargetDiskScript", "PartitionLayout", "SystemSize",
        "RecoveryMode", "RecoverySize", "DiskpartScript", "PagingFileMode", "InitPagingFileSize", "MaxPagingFileSize", "PEScript",
        "UseActivationKey", "ActivationKey", "ProcessorArchitecture", "ComputerNameMode", "ComputerName", "ComputerNameScript",
        "TimeZoneMode", "TimeZone", "UserAccountMode", "AutoLogonMode", "BuiltinAdministratorPassword", "ObscurePasswords",
        "PasswordExpirationMode", "PasswordMaxAge", "LockoutMode", "LockoutThreshold", "LockoutWindow", "LockoutDuration",
        "HideFiles", "TaskbarSearch", "TaskbarIconsMode", "TaskbarIconsXml", "StartTilesMode", "StartTilesXml", "StartPinsMode", "StartPinsJson",
        "ProcessAudit", "ProcessAuditCommandLine", "EffectsMode", "DesktopIconsMode", "StartFoldersMode", "CoreIsolationMode",
        "WifiMode", "WifiName", "WifiNonBroadcast", "WifiAuthentication", "WifiPassword", "WifiProfileXml", "ExpressSettings",
        "LockKeysMode", "CapsLockInitial", "CapsLockBehavior", "NumLockInitial", "NumLockBehavior", "ScrollLockInitial", "ScrollLockBehavior", "StickyKeysMode",
        "ColorMode", "SystemColorTheme", "AppsColorTheme", "AccentColor", "AccentColorOnStart", "AccentColorOnBorders", "EnableTransparency",
        "WallpaperMode", "WallpaperColor", "WallpaperScript", "LockScreenMode", "LockScreenScript", "RestartExplorer",
        "AppLockerMode", "AppLockerPolicyXml", "NotFilename",
    ];

    private static readonly (string Prefixo, ScriptPhase Fase)[] FasesDeScript =
    [
        ("System", ScriptPhase.System),
        ("DefaultUser", ScriptPhase.DefaultUser),
        ("FirstLogon", ScriptPhase.FirstLogon),
        ("UserOnce", ScriptPhase.UserOnce),
    ];

    private const int LimiteDeLinhas = 15;

    public static ConfiguracaoAberta Abrir(string xml)
    {
        var doVega = ComentarioVega().Match(xml);
        if (doVega.Success)
        {
            return new ConfiguracaoAberta(Compartilhamento.Decodificar(doVega.Groups[1].Value), "XML do Vega aberto. " + Compartilhamento.AvisoSegredos);
        }

        var doOriginal = ComentarioOriginal().Match(xml);
        if (doOriginal.Success)
        {
            return DoEndereco(doOriginal.Groups[1].Value);
        }

        throw new FormatException("Esse XML não traz as opções usadas. Só dá para abrir arquivos gerados pelo Vega ou pelo gerador original.");
    }

    /// <summary>Verdadeiro se o Vega sabe o que fazer com esse parâmetro do formulário original.</summary>
    public static bool ConheceParametro(string nome) =>
        Conhecidos.Contains(nome)
        || Indexados().IsMatch(nome)
        || Catalogo.Opcoes.Any(o => o.Id == nome)
        || Catalogo.Apps.Any(a => a.Id == nome)
        || Enum.GetNames<Effect>().Contains(nome)
        || nome.StartsWith("StickyKeys") && Enum.GetNames<StickyKeys>().Contains(nome["StickyKeys".Length..])
        || nome.StartsWith("Icon") && Catalogo.NomesIconesArea.ContainsKey(nome["Icon".Length..])
        || nome.StartsWith("StartFolder") && Catalogo.NomesPastasIniciar.ContainsKey(nome["StartFolder".Length..]);

    public static ConfiguracaoAberta DoEndereco(string consulta)
    {
        var q = new Parametros(consulta);
        var estado = new EstadoVega();

        LerIdioma(q, estado);
        LerInstalacao(q, estado);
        LerConta(q, estado);
        LerSistema(q, estado);
        LerAparencia(q, estado);
        LerAvancado(q, estado);

        var ignorados = q.Nomes.Where(n => !ConheceParametro(n)).ToList();
        var mensagem = ignorados.Count == 0
            ? "XML do gerador original aberto."
            : $"XML do gerador original aberto. O Vega não conhece e ignorou: {string.Join(", ", ignorados)}.";
        return new ConfiguracaoAberta(estado, mensagem);
    }

    private static void LerIdioma(Parametros q, EstadoVega e)
    {
        e.EscolherIdiomaNaInstalacao = q.Texto("LanguageMode") == "Interactive";
        e.IdiomaImagem = q.Texto("UILanguage", e.IdiomaImagem);
        e.IdiomaRegional = q.Texto("Locale", e.IdiomaRegional);
        e.Teclado = q.Texto("Keyboard", e.Teclado);
        e.Regiao = q.Texto("GeoLocation", e.Regiao);
        e.UsarTeclado2 = q.Sim("UseKeyboard2");
        e.IdiomaRegional2 = q.Texto("Locale2", e.IdiomaRegional2);
        e.Teclado2 = q.Texto("Keyboard2", e.Teclado2);
        e.UsarTeclado3 = q.Sim("UseKeyboard3");
        e.IdiomaRegional3 = q.Texto("Locale3", e.IdiomaRegional3);
        e.Teclado3 = q.Texto("Keyboard3", e.Teclado3);
    }

    private static void LerInstalacao(Parametros q, EstadoVega e)
    {
        e.UsarChaveAtivacao = q.Sim("UseActivationKey");
        e.ChaveAtivacao = q.Texto("ActivationKey");
        var arquiteturas = q.Todos("ProcessorArchitecture")
            .Select(a => Enum.TryParse<ProcessorArchitecture>(a, out var valor) ? valor : (ProcessorArchitecture?)null)
            .OfType<ProcessorArchitecture>()
            .ToHashSet();
        if (arquiteturas.Count > 0)
        {
            e.Arquiteturas = arquiteturas;
        }

        switch (q.Texto("PEMode"))
        {
            case "Generated":
                LerModoScript(q, e);
                break;
            case "Script":
                e.Modo = ModoInstalacao.ScriptProprio;
                e.ScriptInstalacao = q.Texto("PEScript");
                break;
            default:
                e.Modo = ModoInstalacao.TelaNormal;
                e.IgnorarRequisitos = q.Sim("BypassRequirementsCheck");
                e.UsarConfigurationSet = q.Sim("UseConfigurationSet");
                e.ChaveProduto = q.Texto("ProductKey");
                e.Edicao = q.Texto("WindowsEditionMode") switch
                {
                    "Generic" => q.Texto("WindowsEdition"),
                    "Custom" => EstadoVega.EdicaoChave,
                    "Firmware" => EstadoVega.EdicaoFirmware,
                    _ => "",
                };
                break;
        }
    }

    private static void LerModoScript(Parametros q, EstadoVega e)
    {
        e.Modo = ModoInstalacao.Script;
        e.DesligarNomes8Dot3 = q.Sim("Disable8Dot3Names");
        e.DesligarDefender = q.Sim("DisableDefender");
        e.PausarAntesDeFormatar = q.Sim("PauseBeforeFormatting");
        e.PausarAntesDeReiniciar = q.Sim("PauseBeforeReboot");
        e.CompactarWindows = q.Sim("CompactOs");
        e.VerificarImagem = !q.Sim("SkipIntegrityCheck");

        switch (q.Texto("InstallFromMode"))
        {
            case "Edition":
                e.Origem = OrigemImagem.Edicao;
                e.Edicao = q.Texto("InstallFromEdition");
                break;
            case "Name":
                e.Origem = OrigemImagem.Nome;
                e.ImagemNome = q.Texto("InstallFromName");
                break;
            case "Index":
                e.Origem = OrigemImagem.Indice;
                e.ImagemIndice = q.Numero("InstallFromIndex", e.ImagemIndice);
                break;
            default:
                e.Origem = OrigemImagem.Perguntar;
                break;
        }

        switch (q.Texto("PartitionMode"))
        {
            case "Custom":
                e.Particoes = Particionamento.ScriptDiskpart;
                e.ScriptDiskpart = q.Texto("DiskpartScript");
                break;
            case "Interactive":
                e.Particoes = Particionamento.AMao;
                break;
            default:
                e.Particoes = Particionamento.ApagarDisco;
                LerParticoes(q, e);
                break;
        }

        switch (q.Texto("PagingFileMode"))
        {
            case "Custom":
                e.Paginacao = ArquivoPaginacao.Personalizado;
                e.PaginacaoInicialMiB = q.Numero("InitPagingFileSize", e.PaginacaoInicialMiB);
                e.PaginacaoMaximaMiB = q.Numero("MaxPagingFileSize", e.PaginacaoMaximaMiB);
                break;
            case "None":
                e.Paginacao = ArquivoPaginacao.Nenhum;
                break;
            default:
                e.Paginacao = ArquivoPaginacao.Automatico;
                break;
        }
    }

    private static void LerParticoes(Parametros q, EstadoVega e)
    {
        e.Layout = q.Modo("PartitionLayout", e.Layout);
        e.TamanhoSistemaMiB = q.Numero("SystemSize", e.TamanhoSistemaMiB);
        e.Recuperacao = q.Modo("RecoveryMode", e.Recuperacao);
        e.TamanhoRecuperacaoMiB = q.Numero("RecoverySize", e.TamanhoRecuperacaoMiB);

        switch (q.Texto("TargetDiskMode"))
        {
            case "Generated":
                e.Alvo = AlvoDisco.Criterios;
                e.CriterioSemParticoes = q.Sim("TargetDiskNoPartitions");
                e.CriterioTipoInterno = q.Sim("TargetDiskInterfaceType");
                e.CriterioMidiaFixa = q.Sim("TargetDiskMediaType");
                e.CriterioMinimoGiB = q.Sim("TargetDiskSize") ? q.Numero("TargetDiskMinSize", 0) : null;
                e.CriterioMaximoGiB = q.Sim("TargetDiskSize") ? q.Numero("TargetDiskMaxSize", 0) : null;
                e.CriterioNumero = q.Sim("TargetDiskIndex") ? q.Numero("TargetDisk", 0) : null;
                break;
            case "Script":
                e.Alvo = AlvoDisco.ScriptProprio;
                e.ScriptAlvoDisco = q.Texto("TargetDiskScript");
                break;
            default:
                e.Alvo = AlvoDisco.Perguntar;
                break;
        }
    }

    private static void LerConta(Parametros q, EstadoVega e)
    {
        e.NomeComputadorModo = q.Texto("ComputerNameMode") switch
        {
            "Custom" => ModoNomeComputador.Personalizado,
            "Prompt" => ModoNomeComputador.Perguntar,
            "Script" => ModoNomeComputador.Script,
            _ => ModoNomeComputador.Aleatorio,
        };
        e.NomeComputador = q.Texto("ComputerName");
        e.ScriptNomeComputador = q.Texto("ComputerNameScript");
        e.FusoHorario = q.Texto("TimeZoneMode") == "Explicit" ? q.Texto("TimeZone") : "";

        e.Conta = q.Texto("UserAccountMode") switch
        {
            "Unattended" => TipoConta.Pronta,
            "Interactive" => TipoConta.Microsoft,
            _ => TipoConta.Local,
        };
        for (var i = 0; i < LimiteDeLinhas; i++)
        {
            var nome = q.Texto($"AccountName{i}");
            if (nome.Length > 0)
            {
                e.Contas.Add(new ContaPronta
                {
                    Nome = nome,
                    NomeExibido = q.Texto($"AccountDisplayName{i}"),
                    Senha = q.Texto($"AccountPassword{i}"),
                    Grupo = q.Texto($"AccountGroup{i}", Constants.UsersGroup),
                });
            }
        }

        e.Login = q.Texto("AutoLogonMode") switch
        {
            "Builtin" => LoginAutomatico.Administrador,
            "None" => LoginAutomatico.Nenhum,
            _ => LoginAutomatico.PrimeiraConta,
        };
        e.SenhaAdministrador = q.Texto("BuiltinAdministratorPassword");
        e.OcultarSenhas = q.Sim("ObscurePasswords");

        e.Expiracao = q.Texto("PasswordExpirationMode") switch
        {
            "Default" => ModoExpiracao.Padrao,
            "Custom" => ModoExpiracao.Personalizado,
            _ => ModoExpiracao.Nunca,
        };
        e.ExpiracaoDias = q.Numero("PasswordMaxAge", e.ExpiracaoDias);

        e.Bloqueio = q.Texto("LockoutMode") switch
        {
            "Disabled" => ModoBloqueio.Desligado,
            "Custom" => ModoBloqueio.Personalizado,
            _ => ModoBloqueio.Padrao,
        };
        e.BloqueioTentativas = q.Numero("LockoutThreshold", e.BloqueioTentativas);
        e.BloqueioJanela = q.Numero("LockoutWindow", e.BloqueioJanela);
        e.BloqueioDuracao = q.Numero("LockoutDuration", e.BloqueioDuracao);

        e.Wifi = q.Texto("WifiMode") switch
        {
            "Interactive" => ModoWifi.Perguntar,
            "Unattended" => ModoWifi.Configurar,
            "FromProfile" => ModoWifi.ArquivoXml,
            _ => ModoWifi.Pular,
        };
        e.WifiNome = q.Texto("WifiName");
        e.WifiOculta = q.Sim("WifiNonBroadcast");
        e.WifiAutenticacao = q.Modo("WifiAuthentication", e.WifiAutenticacao);
        e.WifiSenha = q.Texto("WifiPassword");
        e.WifiXml = q.Texto("WifiProfileXml");
        e.Privacidade = q.Modo("ExpressSettings", e.Privacidade);
    }

    private static void LerSistema(Parametros q, EstadoVega e)
    {
        e.OpcoesLigadas = Catalogo.Opcoes.Where(o => q.Sim(o.Id)).Select(o => o.Id).ToHashSet();
        if (q.Texto("CoreIsolationMode") == "Disabled")
        {
            e.OpcoesLigadas.Add(nameof(Configuration.DisableCoreIsolation));
        }

        e.AppsRemovidos = Catalogo.Apps.Where(a => q.Sim(a.Id)).Select(a => a.Id).ToHashSet();
        e.AuditarProcessos = q.Sim("ProcessAudit");
        e.AuditarLinhaDeComando = q.Sim("ProcessAuditCommandLine");

        e.Efeitos = q.Texto("EffectsMode") switch
        {
            "Performance" => ModoEfeitos.Desempenho,
            "Appearance" => ModoEfeitos.Aparencia,
            "Custom" => ModoEfeitos.Personalizado,
            _ => ModoEfeitos.Padrao,
        };
        if (e.Efeitos == ModoEfeitos.Personalizado)
        {
            e.EfeitosLigados = Enum.GetValues<Effect>().Where(f => q.Sim(f.ToString())).ToHashSet();
        }

        e.ConfigurarTeclas = q.Texto("LockKeysMode") == "Configure";
        e.CapsLockInicial = q.Modo("CapsLockInitial", e.CapsLockInicial);
        e.CapsLockComportamento = q.Modo("CapsLockBehavior", e.CapsLockComportamento);
        e.NumLockInicial = q.Modo("NumLockInitial", e.NumLockInicial);
        e.NumLockComportamento = q.Modo("NumLockBehavior", e.NumLockComportamento);
        e.ScrollLockInicial = q.Modo("ScrollLockInitial", e.ScrollLockInicial);
        e.ScrollLockComportamento = q.Modo("ScrollLockBehavior", e.ScrollLockComportamento);

        e.Aderencia = q.Texto("StickyKeysMode") switch
        {
            "Disabled" => ModoAderencia.Desligado,
            "Custom" => ModoAderencia.Personalizado,
            _ => ModoAderencia.Padrao,
        };
        if (e.Aderencia == ModoAderencia.Personalizado)
        {
            e.AderenciaOpcoes = Enum.GetValues<StickyKeys>().Where(s => q.Sim($"StickyKeys{s}")).ToHashSet();
        }
    }

    private static void LerAparencia(Parametros q, EstadoVega e)
    {
        e.ArquivosOcultos = q.Modo("HideFiles", e.ArquivosOcultos);
        e.BuscaBarra = q.Modo("TaskbarSearch", e.BuscaBarra);

        static ModoFixados Fixados(string modo) => modo switch
        {
            "Empty" => ModoFixados.Vazio,
            "Custom" => ModoFixados.Personalizado,
            _ => ModoFixados.Padrao,
        };

        e.IconesBarra = Fixados(q.Texto("TaskbarIconsMode"));
        e.IconesBarraXml = q.Texto("TaskbarIconsXml");
        e.BlocosIniciar = Fixados(q.Texto("StartTilesMode"));
        e.BlocosIniciarXml = q.Texto("StartTilesXml");
        e.FixadosIniciar = Fixados(q.Texto("StartPinsMode"));
        e.FixadosIniciarJson = q.Texto("StartPinsJson");

        e.PersonalizarIconesArea = q.Texto("DesktopIconsMode") == "Custom";
        if (e.PersonalizarIconesArea)
        {
            e.IconesArea = Catalogo.NomesIconesArea.Keys.Where(i => q.Sim($"Icon{i}")).ToHashSet();
        }

        e.PersonalizarPastasIniciar = q.Texto("StartFoldersMode") == "Custom";
        if (e.PersonalizarPastasIniciar)
        {
            e.PastasIniciar = Catalogo.NomesPastasIniciar.Keys.Where(p => q.Sim($"StartFolder{p}")).ToHashSet();
        }

        e.PersonalizarCores = q.Texto("ColorMode") == "Custom";
        e.TemaSistema = q.Modo("SystemColorTheme", e.TemaSistema);
        e.TemaApps = q.Modo("AppsColorTheme", e.TemaApps);
        e.CorDestaque = q.Texto("AccentColor", e.CorDestaque);
        e.CorNoIniciar = q.Sim("AccentColorOnStart");
        e.CorNasBordas = q.Sim("AccentColorOnBorders");
        e.Transparencia = q.Sim("EnableTransparency");

        e.Papel = q.Texto("WallpaperMode") switch
        {
            "Solid" => ModoPapel.CorSolida,
            "Script" => ModoPapel.Script,
            _ => ModoPapel.Padrao,
        };
        e.CorPapel = q.Texto("WallpaperColor", e.CorPapel);
        e.ScriptPapel = q.Texto("WallpaperScript");
        e.PersonalizarTelaBloqueio = q.Texto("LockScreenMode") == "Script";
        e.ScriptTelaBloqueio = q.Texto("LockScreenScript");
    }

    private static void LerAvancado(Parametros q, EstadoVega e)
    {
        foreach (var (prefixo, fase) in FasesDeScript)
        {
            for (var i = 0; i < LimiteDeLinhas; i++)
            {
                var conteudo = q.Texto($"{prefixo}Script{i}");
                if (conteudo.Length > 0)
                {
                    e.Scripts.Add(new ScriptUsuario { Fase = fase, Tipo = q.Modo($"{prefixo}ScriptType{i}", ScriptType.Cmd), Conteudo = conteudo });
                }
            }
        }

        e.ReiniciarExplorador = q.Sim("RestartExplorer");
        e.UsarAppLocker = q.Texto("AppLockerMode") == "Configure";
        e.AppLockerXml = q.Texto("AppLockerPolicyXml");
        e.NomeSemAutoInstalar = q.Sim("NotFilename");

        for (var i = 0; i < LimiteDeLinhas; i++)
        {
            var escolha = q.Texto($"Component{i}");
            var divisao = escolha.LastIndexOf('-');
            if (divisao > 0 && Enum.TryParse<Pass>(escolha[(divisao + 1)..], out var etapa))
            {
                e.Componentes.Add(new ComponenteXml { Componente = escolha[..divisao], Fase = etapa, Xml = q.Texto($"ComponentContent{i}") });
            }
        }
    }

    [GeneratedRegex(@"<!--\s*\S*#config=([A-Za-z0-9+/=%]+)\s*-->")]
    private static partial Regex ComentarioVega();

    [GeneratedRegex(@"<!--\s*https://schneegans\.de/windows/unattend-generator/\?(.*?)\s*-->", RegexOptions.Singleline)]
    private static partial Regex ComentarioOriginal();

    [GeneratedRegex(@"^(Account(Name|DisplayName|Password|Group)|(System|DefaultUser|FirstLogon|UserOnce)Script(Type)?|Component(Content)?)\d+$")]
    private static partial Regex Indexados();

    /// <summary>A parte depois do "?" no endereço do gerador original.</summary>
    private sealed class Parametros(string consulta)
    {
        private readonly ILookup<string, string> valores = consulta
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(par => par.Split('=', 2))
            .ToLookup(par => WebUtility.UrlDecode(par[0]), par => par.Length > 1 ? WebUtility.UrlDecode(par[1]) : "");

        public IEnumerable<string> Nomes => valores.Select(g => g.Key);

        public IEnumerable<string> Todos(string nome) => valores[nome];

        public string Texto(string nome, string padrao = "") => valores[nome].FirstOrDefault(padrao);

        public bool Sim(string nome) => Texto(nome) == "true";

        public int Numero(string nome, int padrao) => int.TryParse(Texto(nome), out var valor) ? valor : padrao;

        public T Modo<T>(string nome, T padrao) where T : struct, Enum => Enum.TryParse<T>(Texto(nome), out var valor) ? valor : padrao;
    }
}
