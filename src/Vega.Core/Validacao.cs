using System.Text.RegularExpressions;
using Schneegans.Unattend;

namespace Vega.Core;

/// <summary>
/// Confere o que o formulário do site original confere no navegador. O motor aceita boa parte
/// desses valores, e alguns só falhariam na instalação, depois de o disco ser apagado.
/// Só olha os campos que estão valendo no modo escolhido.
/// </summary>
public static partial class Validacao
{
    public static IReadOnlyList<string> Problemas(EstadoVega e, UnattendGenerator gerador)
    {
        var problemas = new List<string>();
        void Exige(bool certo, string mensagem)
        {
            if (!certo)
            {
                problemas.Add(mensagem);
            }
        }

        static bool Preenchido(string? texto) => !string.IsNullOrWhiteSpace(texto);

        if (e.Modo == ModoInstalacao.ScriptProprio)
        {
            Exige(Preenchido(e.ScriptInstalacao), "Escreva o script do WinPE em Disco e imagem.");
        }

        if (e.Modo == ModoInstalacao.Script)
        {
            if (e.Particoes == Particionamento.ScriptDiskpart)
            {
                Exige(Preenchido(e.ScriptDiskpart), "Escreva o script do diskpart em Disco e imagem.");
            }

            if (e.Particoes == Particionamento.ApagarDisco)
            {
                switch (e.Alvo)
                {
                    case AlvoDisco.Numero:
                        Exige(e.DiscoNumero >= 0, "O número do disco não pode ser negativo.");
                        break;
                    case AlvoDisco.ScriptProprio:
                        Exige(Preenchido(e.ScriptAlvoDisco), "Escreva o script que escolhe o disco em Disco e imagem.");
                        break;
                    case AlvoDisco.Criterios:
                        Exige(e.CriterioMinimoGiB is null or >= 0, "O tamanho mínimo do disco não pode ser negativo.");
                        Exige(e.CriterioMaximoGiB is null or >= 0, "O tamanho máximo do disco não pode ser negativo.");
                        Exige(e.CriterioNumero is null or >= 0, "O número do disco não pode ser negativo.");
                        break;
                }

                Exige(e.TamanhoSistemaMiB >= 100, "A partição do sistema precisa ter pelo menos 100 MiB.");
                if (e.Recuperacao == RecoveryMode.Partition)
                {
                    Exige(e.TamanhoRecuperacaoMiB >= 300, "A partição de recuperação precisa ter pelo menos 300 MiB.");
                }
            }

            if (e.Origem == OrigemImagem.Indice)
            {
                Exige(e.ImagemIndice >= 1, "O número da imagem começa em 1.");
            }
            else if (e.Origem == OrigemImagem.Nome)
            {
                Exige(Preenchido(e.ImagemNome), "Escreva o nome da imagem do Windows.");
            }

            if (e.Paginacao == ArquivoPaginacao.Personalizado)
            {
                Exige(e.PaginacaoInicialMiB >= 0 && e.PaginacaoInicialMiB <= e.PaginacaoMaximaMiB,
                    "O tamanho inicial do arquivo de paginação não pode passar do máximo.");
            }
        }

        if (e.Conta == TipoConta.Pronta)
        {
            Exige(e.Contas.All(c => (c.NomeExibido?.Length ?? 0) <= 256), "O nome exibido de uma conta passa de 256 caracteres.");
        }

        if (e.Expiracao == ModoExpiracao.Personalizado)
        {
            Exige(e.ExpiracaoDias is >= 1 and <= 999, "A expiração de senha precisa ficar entre 1 e 999 dias.");
        }

        if (e.Bloqueio == ModoBloqueio.Personalizado)
        {
            Exige(e.BloqueioTentativas is >= 0 and <= 999, "As tentativas antes de bloquear precisam ficar entre 0 e 999.");
            Exige(e.BloqueioDuracao is >= 1 and <= 99999, "O tempo de bloqueio precisa ficar entre 1 e 99999 minutos.");
            Exige(e.BloqueioJanela is >= 1 and <= 99999, "O tempo para zerar a contagem precisa ficar entre 1 e 99999 minutos.");
        }

        if (e.NomeComputadorModo == ModoNomeComputador.Script)
        {
            Exige(Preenchido(e.ScriptNomeComputador), "Escreva o script que devolve o nome do computador.");
        }

        if (e.Wifi == ModoWifi.Configurar)
        {
            Exige(Preenchido(e.WifiNome) && e.WifiNome.Length <= 32, "O nome da rede Wi-Fi precisa ter de 1 a 32 caracteres.");
            if (e.WifiAutenticacao != WifiAuthentications.Open)
            {
                Exige(e.WifiSenha.Length is >= 8 and <= 63, "A senha do Wi-Fi precisa ter de 8 a 63 caracteres.");
            }
        }

        if (e.PersonalizarCores)
        {
            Exige(CorHtml().IsMatch(e.CorDestaque), "A cor de destaque é inválida.");
        }

        if (e.Papel == ModoPapel.CorSolida)
        {
            Exige(CorHtml().IsMatch(e.CorPapel), "A cor do papel de parede é inválida.");
        }
        else if (e.Papel == ModoPapel.Script)
        {
            Exige(Preenchido(e.ScriptPapel), "Escreva o script que devolve o papel de parede.");
        }

        if (e.PersonalizarTelaBloqueio)
        {
            Exige(Preenchido(e.ScriptTelaBloqueio), "Escreva o script que devolve a imagem da tela de bloqueio.");
        }

        if (e.FixadosIniciar == ModoFixados.Personalizado)
        {
            Exige(Preenchido(e.FixadosIniciarJson), "Preencha a lista de fixados do Menu Iniciar.");
        }

        var componentes = e.Componentes.Where(c => Preenchido(c.Xml)).ToList();
        foreach (var componente in componentes)
        {
            var existe = gerador.Components.TryGetValue(componente.Componente, out var item) && item.Passes.Contains(componente.Fase);
            Exige(existe, $"O componente {componente.Componente} não existe na etapa {componente.Fase}.");
        }

        var repetido = componentes.GroupBy(c => (c.Componente, c.Fase)).FirstOrDefault(g => g.Count() > 1);
        if (repetido is not null)
        {
            problemas.Add($"O componente {repetido.Key.Componente} aparece mais de uma vez na etapa {repetido.Key.Fase}. Junte o XML em um só.");
        }

        return problemas;
    }

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex CorHtml();
}
