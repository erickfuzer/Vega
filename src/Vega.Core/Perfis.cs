using Schneegans.Unattend;

namespace Vega.Core;

/// <summary>Um ponto de partida. Depois de aplicar, a pessoa pode mudar qualquer opção.</summary>
public sealed record Perfil(string Id, string Nome, string Descricao, Action<EstadoVega> Aplicar);

/// <summary>Um atalho do gerador original: começa do zero, sem manter nada do que estava marcado.</summary>
public sealed record PontoDePartida(string Id, string Nome, string Descricao, Func<EstadoVega> Criar);

public static class Perfis
{
    private static readonly string[] OpcoesDiaADia =
    [
        nameof(Configuration.BypassNetworkCheck),
        nameof(Configuration.DisableAppSuggestions),
        nameof(Configuration.DisableBingResults),
        nameof(Configuration.DisableWidgets),
        nameof(Configuration.DisableFastStartup),
        nameof(Configuration.DisableAutomaticRestartSignOn),
        nameof(Configuration.PreventDeviceApps),
        nameof(Configuration.ClassicContextMenu),
        nameof(Configuration.ShowEndTask),
        nameof(Configuration.ShowFileExtensions),
        nameof(Configuration.EnableLongPaths),
        nameof(Configuration.AllowPowerShellScripts),
        nameof(Configuration.HideEdgeFre),
        nameof(Configuration.DisableEdgeStartupBoost),
        nameof(Configuration.DeleteEdgeDesktopIcon),
    ];

    private static readonly string[] OpcoesExtrasMaximo =
    [
        nameof(Configuration.DisableCoreIsolation),
        nameof(Configuration.DisableSmartScreen),
        nameof(Configuration.DisableSac),
        nameof(Configuration.DisableSystemRestore),
        nameof(Configuration.DisableWindowsUpdate),
        nameof(Configuration.PreventDeviceEncryption),
        nameof(Configuration.DisableLastAccess),
        nameof(Configuration.DisableWpbt),
        nameof(Configuration.TurnOffSystemSounds),
        nameof(Configuration.DeleteJunctions),
        nameof(Configuration.HideInfoTip),
        nameof(Configuration.HideTaskViewButton),
    ];

    private static readonly string[] AppsDiaADia =
    [
        "RemoveCopilot", "RemoveRecall", "RemoveBingSearch", "RemoveCortana", "RemoveOffice365", "RemoveOneNote",
        "RemoveOutlook", "RemoveMailCalendar", "RemoveTeams", "RemoveSkype", "RemovePeople", "RemoveToDo",
        "RemoveYourPhone", "RemoveGameAssist", "RemoveSolitaire", "RemoveClipchamp", "RemoveZuneVideo",
        "RemovePaint3D", "Remove3DViewer", "RemoveMaps", "RemoveNews", "RemoveWeather", "RemoveWallet", "RemoveFamily",
        "RemovePowerAutomate", "RemoveMixedReality", "RemoveGetHelp", "RemoveGetStarted", "RemoveFeedbackHub",
        "RemoveDevHome", "RemoveInternetExplorer", "RemoveWordPad", "RemoveStepsRecorder", "RemoveMathInputPanel",
        "RemovePowerShell2", "RemovePowerShellISE",
    ];

    /// <summary>Continuam mesmo no perfil máximo, porque quase todo mundo sente falta deles.</summary>
    private static readonly HashSet<string> AppsMantidosNoMaximo =
    [
        "RemoveNotepad", "RemoveNotepadClassic", "RemoveCalculator", "RemoveSnippingTool", "RemovePhotos", "RemoveWindowsTerminal",
    ];

    /// <summary>O XML que o motor gera sem nenhuma escolha, em que a instalação pergunta quase tudo.</summary>
    private static EstadoVega Minimo() => new()
    {
        EscolherIdiomaNaInstalacao = true,
        Conta = TipoConta.Microsoft,
        Wifi = ModoWifi.Perguntar,
        Expiracao = ModoExpiracao.Padrao,
    };

    public static IReadOnlyList<PontoDePartida> Pontos { get; } =
    [
        new("usuario-local", "Só um usuário local",
            "A instalação pergunta idioma, edição e usuário. O XML só pula a conta Microsoft e a tela de rede.",
            () =>
            {
                var estado = Minimo();
                estado.Conta = TipoConta.Local;
                estado.Wifi = ModoWifi.Pular;
                return estado;
            }),
        new("minimo", "Saída mínima",
            "O menor XML possível: a instalação pergunta quase tudo, como no Windows sem nenhum ajuste.",
            Minimo),
    ];

    public static IReadOnlyList<Perfil> Todos { get; } =
    [
        new("padrao", "Windows padrão",
            "Conta local e sem pedir internet, mas sem remover nada. Bom para começar do zero.",
            estado => estado.Limpar()),
        new("diario", "Uso diário",
            "Tira o que costuma incomodar e mantém proteção, atualização e recuperação.",
            estado =>
            {
                estado.Limpar();
                estado.OpcoesLigadas.UnionWith(OpcoesDiaADia);
                estado.AppsRemovidos.UnionWith(AppsDiaADia);
                estado.FixadosIniciar = ModoFixados.Vazio;
                estado.BlocosIniciar = ModoFixados.Vazio;
            }),
        new("maximo", "Remoção máxima",
            "Para máquinas de teste: desliga o Defender, remove quase tudo e apaga o disco que você escolher. Leia os avisos antes de usar.",
            estado =>
            {
                estado.Limpar();
                estado.OpcoesLigadas.UnionWith(OpcoesDiaADia);
                estado.OpcoesLigadas.UnionWith(OpcoesExtrasMaximo);
                estado.AppsRemovidos.UnionWith(Catalogo.Apps.Select(a => a.Id).Where(id => !AppsMantidosNoMaximo.Contains(id)));
                estado.Modo = ModoInstalacao.Script;
                estado.Recuperacao = RecoveryMode.None;
                estado.DesligarDefender = true;
                estado.CompactarWindows = true;
                estado.Efeitos = ModoEfeitos.Desempenho;
                estado.Energia = PlanoEnergia.AltoDesempenho;
                estado.RemoverEdge = true;
                estado.FixadosIniciar = ModoFixados.Vazio;
                estado.BlocosIniciar = ModoFixados.Vazio;
            }),
    ];
}
