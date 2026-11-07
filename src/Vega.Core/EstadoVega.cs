using System.Collections.Immutable;
using Schneegans.Unattend;

namespace Vega.Core;

public enum TipoConta
{
    Local,
    Microsoft,
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

    public TipoConta Conta { get; set; } = TipoConta.Local;
    public bool MostrarTelaWifi { get; set; }

    public ExpressSettingsMode Privacidade { get; set; } = ExpressSettingsMode.DisableAll;
    public TaskbarSearchMode BuscaBarra { get; set; } = TaskbarSearchMode.Box;
    public HideModes ArquivosOcultos { get; set; } = HideModes.Hidden;

    public void Limpar()
    {
        OpcoesLigadas.Clear();
        AppsRemovidos.Clear();
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
            PESettings = new DefaultPESettings(
                EditionSettings: string.IsNullOrEmpty(Edicao)
                    ? new InteractiveEditionSettings()
                    : new UnattendedEditionSettings(gerador.Lookup<WindowsEdition>(Edicao)),
                BypassRequirementsCheck: IgnorarRequisitos,
                UseConfigurationSet: false),
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
}
