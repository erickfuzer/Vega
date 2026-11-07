using System.Text;

namespace Vega.Core;

/// <summary>Busca sem diferenciar maiúsculas nem acentos, para "aparencia" achar "Aparência".</summary>
public static class Busca
{
    /// <summary>Verdadeiro quando todas as palavras do termo aparecem no texto, em qualquer ordem.</summary>
    public static bool Combina(string texto, string termo)
    {
        var simples = Simplificar(texto);
        return Simplificar(termo)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .All(palavra => simples.Contains(palavra, StringComparison.Ordinal));
    }

    private static string Simplificar(string texto)
    {
        var saida = new StringBuilder(texto.Length);
        foreach (var letra in texto.ToLowerInvariant())
        {
            saida.Append(letra switch
            {
                'á' or 'à' or 'â' or 'ã' => 'a',
                'é' or 'ê' => 'e',
                'í' => 'i',
                'ó' or 'ô' or 'õ' => 'o',
                'ú' or 'ü' => 'u',
                'ç' => 'c',
                _ => letra,
            });
        }
        return saida.ToString();
    }
}
