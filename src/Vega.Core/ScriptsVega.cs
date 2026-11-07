namespace Vega.Core;

/// <summary>
/// Scripts que o motor original não tem. Rodam no primeiro login, depois da configuração
/// inicial, porque mudanças arriscadas antes dela já quebraram a instalação em testes.
/// </summary>
public static class ScriptsVega
{
    /// <summary>
    /// O instalador do Edge recusa desinstalar pela linha de comando (erro 532). Quando isso
    /// acontece, apaga a pasta e o registro dele e mantém EdgeCore, EdgeUpdate e EdgeWebView,
    /// de que o WebView2 depende. Testado no Windows 11 26H2, em VM e em um notebook.
    /// </summary>
    public const string RemoverEdge = """
        reg.exe add "HKLM\SOFTWARE\Microsoft\EdgeUpdate" /v DoNotUpdateToEdgeWithChromium /t REG_DWORD /d 1 /f;
        reg.exe add "HKLM\SOFTWARE\Policies\Microsoft\EdgeUpdate" /v "Install{56EB18F8-B008-4CBD-B6D2-8C97FE7E9062}" /t REG_DWORD /d 0 /f;
        Get-AppxProvisionedPackage -Online | Where-Object -Property 'DisplayName' -EQ -Value 'Microsoft.MicrosoftEdge.Stable' | Remove-AppxProvisionedPackage -AllUsers -Online -ErrorAction 'Continue';
        $edge = 'C:\Program Files (x86)\Microsoft\Edge';
        $setup = Get-ChildItem -Path "$edge\Application\*\Installer\setup.exe" -ErrorAction 'SilentlyContinue' | Sort-Object -Property 'LastWriteTime' -Descending | Select-Object -First 1;
        if( $setup ) {
          try {
            $processo = Start-Process -FilePath $setup.FullName -ArgumentList '--uninstall', '--system-level', '--verbose-logging', '--force-uninstall' -PassThru;
            if( -not $processo.WaitForExit( 300000 ) ) {
              $processo | Stop-Process -Force;
            }
          } catch {
            "Falha ao rodar o instalador do Edge: $_";
          }
        }
        if( Test-Path -LiteralPath "$edge\Application\msedge.exe" ) {
          Get-Process -Name 'msedge' -ErrorAction 'SilentlyContinue' | Stop-Process -Force -ErrorAction 'SilentlyContinue';
          sc.exe stop MicrosoftEdgeElevationService;
          sc.exe delete MicrosoftEdgeElevationService;
          Remove-Item -LiteralPath $edge -Recurse -Force -ErrorAction 'Continue';
          @(
            'Registry::HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Microsoft Edge';
            'Registry::HKLM\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{56EB18F8-B008-4CBD-B6D2-8C97FE7E9062}';
            'Registry::HKLM\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\ClientState\{56EB18F8-B008-4CBD-B6D2-8C97FE7E9062}';
            'Registry::HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\msedge.exe';
            'Registry::HKLM\SOFTWARE\Microsoft\Active Setup\Installed Components\{9459C573-B17A-45AE-9F64-1857B5D58CEE}';
            'Registry::HKLM\SOFTWARE\WOW6432Node\Microsoft\Active Setup\Installed Components\{9459C573-B17A-45AE-9F64-1857B5D58CEE}';
          ) | Where-Object -FilterScript { Test-Path -LiteralPath $_; } | Remove-Item -Recurse -Force -ErrorAction 'Continue';
        }
        @(
          'C:\Users\Public\Desktop\Microsoft Edge.lnk';
          'C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Microsoft Edge.lnk';
        ) | Where-Object -FilterScript { [System.IO.File]::Exists( $_ ); } | Remove-Item -Force -ErrorAction 'SilentlyContinue';
        "Edge presente: $( Test-Path -LiteralPath "$edge\Application\msedge.exe" )";
        """;

    public static string PlanoDeEnergia(PlanoEnergia plano) => plano switch
    {
        PlanoEnergia.AltoDesempenho => "powercfg.exe /setactive SCHEME_MIN;",
        PlanoEnergia.DesempenhoMaximo => """
            $saida = powercfg.exe /duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61;
            $guid = [regex]::Match( "$saida", '[0-9a-fA-F]{8}(-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}' ).Value;
            if( $guid ) {
              powercfg.exe /setactive $guid;
            } else {
              powercfg.exe /setactive SCHEME_MIN;
            }
            """,
        _ => "",
    };
}
