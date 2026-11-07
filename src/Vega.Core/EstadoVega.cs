using System.Collections.Immutable;
using Schneegans.Unattend;

namespace Vega.Core;

public enum TipoConta
{
    Local,
    Microsoft,
}

public enum ModoInstalacao
{
    /// <summary>A instalação normal do Windows, com a tela de discos dela.</summary>
    TelaNormal,

    /// <summary>O Vega particiona e aplica a imagem por um script gerado.</summary>
    Script,

    /// <summary>Um script próprio substitui toda essa etapa.</summary>
    ScriptProprio,
}

public enum AlvoDisco
{
    Perguntar,
    Criterios,
    Numero,
    ScriptProprio,
}

public enum Particionamento
{
    ApagarDisco,
    AMao,
    ScriptDiskpart,
}

public enum OrigemImagem
{
    Edicao,
    Perguntar,
    Indice,
    Nome,
}

public enum ArquivoPaginacao
{
    Automatico,
    Nenhum,
    Personalizado,
}

/// <summary>O que a pessoa escolheu na tela. Vira uma <see cref="Configuration"/> na hora de gerar.</summary>
public sealed class EstadoVega
{
    public HashSet<string> OpcoesLigadas { get; } = [];
    public HashSet<string> AppsRemovidos { get; } = [];

    public bool EscolherIdiomaNaInstalacao { get; set; }
    public string IdiomaImagem { get; set; } = "pt-BR";
    public string IdiomaRegional { get; set; } = "pt-BR";
    public string Teclado { get; set; } = "00010416";
    public string Regiao { get; set; } = "32";

    /// <summary>Vazio deixa a instalação perguntar a edição.</summary>
    public string Edicao { get; set; } = "";
    public bool IgnorarRequisitos { get; set; } = true;
    public bool UsarConfigurationSet { get; set; }

    public TipoConta Conta { get; set; } = TipoConta.Local;
    public bool MostrarTelaWifi { get; set; }

    public ExpressSettingsMode Privacidade { get; set; } = ExpressSettingsMode.DisableAll;
    public TaskbarSearchMode BuscaBarra { get; set; } = TaskbarSearchMode.Box;
    public HideModes ArquivosOcultos { get; set; } = HideModes.Hidden;

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
    public bool ApagaDisco => Modo switch
    {
        ModoInstalacao.Script => Particoes != Particionamento.AMao,
        ModoInstalacao.ScriptProprio => true,
        _ => false,
    };

    /// <summary>Verdadeiro quando o disco pode ser apagado sem nenhuma pergunta nem pausa.</summary>
    public bool ApagaSemPerguntar =>
        Modo == ModoInstalacao.Script
        && Particoes != Particionamento.AMao
        && Alvo != AlvoDisco.Perguntar
        && !PausarAntesDeFormatar;

    /// <summary>Volta o que os perfis mudam ao estado inicial, mantendo idioma e conta.</summary>
    public void Limpar()
    {
        OpcoesLigadas.Clear();
        AppsRemovidos.Clear();
        Modo = ModoInstalacao.TelaNormal;
        Particoes = Particionamento.ApagarDisco;
        Alvo = AlvoDisco.Perguntar;
        Recuperacao = RecoveryMode.Partition;
        DesligarDefender = false;
        DesligarNomes8Dot3 = false;
        PausarAntesDeFormatar = true;
        PausarAntesDeReiniciar = false;
        CompactarWindows = false;
        VerificarImagem = true;
    }

    public Configuration Montar(UnattendGenerator gerador)
    {
        var config = Configuration.Default with
        {
            LanguageSettings = EscolherIdiomaNaInstalacao
                ? new InteractiveLanguageSettings()
                : new UnattendedLanguageSettings(
                    ImageLanguage: gerador.Lookup<ImageLanguage>(IdiomaImagem),
                    LocaleAndKeyboard: new LocaleAndKeyboard(
                        gerador.Lookup<UserLocale>(IdiomaRegional),
                        gerador.Lookup<KeyboardIdentifier>(Teclado)),
                    LocaleAndKeyboard2: null,
                    LocaleAndKeyboard3: null,
                    GeoLocation: gerador.Lookup<GeoLocation>(Regiao)),
            AccountSettings = Conta == TipoConta.Local
                ? new InteractiveLocalAccountSettings()
                : new InteractiveMicrosoftAccountSettings(),
            WifiSettings = MostrarTelaWifi ? new InteractiveWifiSettings() : new SkipWifiSettings(),
            PESettings = MontarInstalacao(gerador),
            ExpressSettings = Privacidade,
            TaskbarSearch = BuscaBarra,
            HideFiles = ArquivosOcultos,
            Bloatwares = AppsRemovidos.Select(gerador.Lookup<Bloatware>).ToImmutableList(),
        };

        foreach (var opcao in Catalogo.Opcoes)
        {
            config = opcao.Aplicar(config, OpcoesLigadas.Contains(opcao.Id));
        }

        return config;
    }

    public byte[] GerarXml(UnattendGenerator gerador) =>
        UnattendGenerator.Serialize(gerador.GenerateXml(Montar(gerador)));

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
            EditionSettings: string.IsNullOrEmpty(Edicao)
                ? new InteractiveEditionSettings()
                : new UnattendedEditionSettings(gerador.Lookup<WindowsEdition>(Edicao)),
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
        OrigemImagem.Edicao when !string.IsNullOrEmpty(Edicao) =>
            new EditionInstallFromSettings(gerador.Lookup<WindowsEdition>(Edicao)),
        _ => new InteractiveInstallFromSettings(),
    };
}
