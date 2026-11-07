using System.Text;

namespace Vega.Core;

/// <summary>
/// Cria uma ISO com um único arquivo na raiz, para conectar como CD em máquinas virtuais.
/// Usa ISO 9660 nível 2 e Joliet, que mantém o nome em minúsculas no Windows.
/// </summary>
public static class GeradorIso
{
    private const int TamanhoSetor = 2048;
    private const int SetorPrimario = 16;
    private const int SetorJoliet = 17;
    private const int SetorFim = 18;
    private const int TabelaLPrimaria = 19;
    private const int TabelaMPrimaria = 20;
    private const int TabelaLJoliet = 21;
    private const int TabelaMJoliet = 22;
    private const int RaizPrimaria = 23;
    private const int RaizJoliet = 24;
    private const int SetorArquivo = 25;
    private const int TamanhoTabelaCaminhos = 10;
    private const string NomeVolume = "VEGA";

    public static byte[] Criar(string nomeArquivo, byte[] conteudo, DateTime? data = null)
    {
        var quando = data ?? DateTime.UtcNow;
        var setoresArquivo = Math.Max(1, (conteudo.Length + TamanhoSetor - 1) / TamanhoSetor);
        var totalSetores = SetorArquivo + setoresArquivo;
        var iso = new byte[totalSetores * TamanhoSetor];

        EscreverDescritor(iso, SetorPrimario, joliet: false, totalSetores, quando);
        EscreverDescritor(iso, SetorJoliet, joliet: true, totalSetores, quando);

        var fim = Deslocamento(SetorFim);
        iso[fim] = 255;
        Encoding.ASCII.GetBytes("CD001").CopyTo(iso, fim + 1);
        iso[fim + 6] = 1;

        EscreverTabelaCaminhos(iso, TabelaLPrimaria, RaizPrimaria, bigEndian: false);
        EscreverTabelaCaminhos(iso, TabelaMPrimaria, RaizPrimaria, bigEndian: true);
        EscreverTabelaCaminhos(iso, TabelaLJoliet, RaizJoliet, bigEndian: false);
        EscreverTabelaCaminhos(iso, TabelaMJoliet, RaizJoliet, bigEndian: true);

        var nomePrimario = Encoding.ASCII.GetBytes(nomeArquivo.ToUpperInvariant() + ";1");
        var nomeJoliet = Encoding.BigEndianUnicode.GetBytes(nomeArquivo + ";1");
        EscreverRaiz(iso, RaizPrimaria, nomePrimario, conteudo.Length, quando);
        EscreverRaiz(iso, RaizJoliet, nomeJoliet, conteudo.Length, quando);

        conteudo.CopyTo(iso, Deslocamento(SetorArquivo));
        return iso;
    }

    private static int Deslocamento(int setor) => setor * TamanhoSetor;

    private static void EscreverDescritor(byte[] iso, int setor, bool joliet, int totalSetores, DateTime quando)
    {
        var inicio = Deslocamento(setor);

        void Texto(int posicao, int tamanho, string valor)
        {
            if (joliet)
            {
                var bytes = Encoding.BigEndianUnicode.GetBytes(valor.PadRight(tamanho / 2));
                bytes.CopyTo(iso, inicio + posicao);
            }
            else
            {
                Encoding.ASCII.GetBytes(valor.PadRight(tamanho)).CopyTo(iso, inicio + posicao);
            }
        }

        iso[inicio] = (byte)(joliet ? 2 : 1);
        Encoding.ASCII.GetBytes("CD001").CopyTo(iso, inicio + 1);
        iso[inicio + 6] = 1;
        Texto(8, 32, "");
        Texto(40, 32, NomeVolume);
        Ambos32(iso, inicio + 80, totalSetores);
        if (joliet)
        {
            // Sequência de escape que marca UCS-2 nível 3.
            Encoding.ASCII.GetBytes("%/E").CopyTo(iso, inicio + 88);
        }

        Ambos16(iso, inicio + 120, 1);
        Ambos16(iso, inicio + 124, 1);
        Ambos16(iso, inicio + 128, TamanhoSetor);
        Ambos32(iso, inicio + 132, TamanhoTabelaCaminhos);
        Pequeno32(iso, inicio + 140, joliet ? TabelaLJoliet : TabelaLPrimaria);
        Grande32(iso, inicio + 148, joliet ? TabelaMJoliet : TabelaMPrimaria);

        var raiz = RegistroDiretorio([0], joliet ? RaizJoliet : RaizPrimaria, TamanhoSetor, ehDiretorio: true, quando);
        raiz.CopyTo(iso, inicio + 156);

        Texto(190, 128, "");
        Texto(318, 128, "");
        Texto(446, 128, "");
        Texto(574, 128, "VEGA");
        Texto(702, 36, "");
        Texto(739, 36, "");
        Texto(776, 36, "");

        var dataIso = Encoding.ASCII.GetBytes(quando.ToString("yyyyMMddHHmmss") + "00");
        var semData = Encoding.ASCII.GetBytes(new string('0', 16));
        dataIso.CopyTo(iso, inicio + 813);
        dataIso.CopyTo(iso, inicio + 830);
        semData.CopyTo(iso, inicio + 847);
        semData.CopyTo(iso, inicio + 864);
        iso[inicio + 881] = 1;
    }

    private static void EscreverTabelaCaminhos(byte[] iso, int setor, int raiz, bool bigEndian)
    {
        var inicio = Deslocamento(setor);
        iso[inicio] = 1;
        if (bigEndian)
        {
            Grande32(iso, inicio + 2, raiz);
            iso[inicio + 6] = 0;
            iso[inicio + 7] = 1;
        }
        else
        {
            Pequeno32(iso, inicio + 2, raiz);
            iso[inicio + 6] = 1;
            iso[inicio + 7] = 0;
        }
    }

    private static void EscreverRaiz(byte[] iso, int setor, byte[] nomeArquivo, int tamanhoArquivo, DateTime quando)
    {
        var posicao = Deslocamento(setor);
        byte[][] registros =
        [
            RegistroDiretorio([0], setor, TamanhoSetor, ehDiretorio: true, quando),
            RegistroDiretorio([1], setor, TamanhoSetor, ehDiretorio: true, quando),
            RegistroDiretorio(nomeArquivo, SetorArquivo, tamanhoArquivo, ehDiretorio: false, quando),
        ];
        foreach (var registro in registros)
        {
            registro.CopyTo(iso, posicao);
            posicao += registro.Length;
        }
    }

    private static byte[] RegistroDiretorio(byte[] nome, int setor, int tamanho, bool ehDiretorio, DateTime quando)
    {
        var comprimento = 33 + nome.Length;
        if (comprimento % 2 != 0)
        {
            comprimento++;
        }

        var registro = new byte[comprimento];
        registro[0] = (byte)comprimento;
        Ambos32(registro, 2, setor);
        Ambos32(registro, 10, tamanho);
        registro[18] = (byte)(quando.Year - 1900);
        registro[19] = (byte)quando.Month;
        registro[20] = (byte)quando.Day;
        registro[21] = (byte)quando.Hour;
        registro[22] = (byte)quando.Minute;
        registro[23] = (byte)quando.Second;
        registro[25] = (byte)(ehDiretorio ? 2 : 0);
        Ambos16(registro, 28, 1);
        registro[32] = (byte)nome.Length;
        nome.CopyTo(registro, 33);
        return registro;
    }

    private static void Pequeno32(byte[] destino, int posicao, int valor)
    {
        destino[posicao] = (byte)valor;
        destino[posicao + 1] = (byte)(valor >> 8);
        destino[posicao + 2] = (byte)(valor >> 16);
        destino[posicao + 3] = (byte)(valor >> 24);
    }

    private static void Grande32(byte[] destino, int posicao, int valor)
    {
        destino[posicao] = (byte)(valor >> 24);
        destino[posicao + 1] = (byte)(valor >> 16);
        destino[posicao + 2] = (byte)(valor >> 8);
        destino[posicao + 3] = (byte)valor;
    }

    private static void Ambos32(byte[] destino, int posicao, int valor)
    {
        Pequeno32(destino, posicao, valor);
        Grande32(destino, posicao + 4, valor);
    }

    private static void Ambos16(byte[] destino, int posicao, int valor)
    {
        destino[posicao] = (byte)valor;
        destino[posicao + 1] = (byte)(valor >> 8);
        destino[posicao + 2] = (byte)(valor >> 8);
        destino[posicao + 3] = (byte)valor;
    }
}
