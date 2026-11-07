using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Vega.Core;

/// <summary>O resultado de abrir um arquivo, com o que a pessoa precisa saber sobre ele.</summary>
public sealed record ConfiguracaoAberta(EstadoVega Estado, string Mensagem);

/// <summary>
/// O link da seleção e o comentário que vai dentro do XML. Guardam só o que difere do padrão e
/// nunca levam senhas nem chaves, porque um link costuma ir parar em mensagens e favoritos.
/// </summary>
public static class Compartilhamento
{
    public const string MarcaLink = "#config=";
    public const string EnderecoSite = "https://erickfuzer.github.io/Vega/";
    public const string AvisoSegredos = "Senhas, chaves do produto e a senha do Wi-Fi não vêm junto. Digite de novo se precisar.";

    private static readonly string[] Segredos =
    [
        nameof(EstadoVega.WifiSenha),
        nameof(EstadoVega.SenhaAdministrador),
        nameof(EstadoVega.ChaveProduto),
        nameof(EstadoVega.ChaveAtivacao),
    ];

    /// <summary>Base64 comum: o alfabeto com hífen poderia formar "--", que é proibido em comentários XML.</summary>
    public static string Codificar(EstadoVega estado)
    {
        var atual = estado.ParaJson();
        foreach (var campo in new EstadoVega().ParaJson().Properties())
        {
            if (JToken.DeepEquals(atual[campo.Name], campo.Value))
            {
                atual.Remove(campo.Name);
            }
        }

        foreach (var segredo in Segredos)
        {
            atual.Remove(segredo);
        }

        if (atual[nameof(EstadoVega.Contas)] is JArray contas)
        {
            foreach (var conta in contas.OfType<JObject>())
            {
                conta.Remove(nameof(ContaPronta.Senha));
            }
        }

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(atual.ToString(Formatting.None)));
    }

    public static string Link(EstadoVega estado) => EnderecoSite + MarcaLink + Codificar(estado);

    public static EstadoVega Decodificar(string codigo)
    {
        try
        {
            var texto = Encoding.UTF8.GetString(Convert.FromBase64String(Uri.UnescapeDataString(codigo)));
            return EstadoVega.DeJson(JObject.Parse(texto));
        }
        catch (Exception falha) when (falha is JsonException or FormatException or ArgumentException)
        {
            throw new FormatException("Esse link não traz uma configuração válida do Vega.", falha);
        }
    }

    /// <summary>Abre um JSON salvo pelo Vega, um XML do Vega ou um XML do gerador original.</summary>
    public static ConfiguracaoAberta Abrir(string conteudo)
    {
        var texto = conteudo.TrimStart('﻿', ' ', '\r', '\n', '\t');
        if (texto.StartsWith('<'))
        {
            return ImportadorXml.Abrir(texto);
        }

        try
        {
            return new ConfiguracaoAberta(EstadoVega.Importar(texto), "Configuração aberta.");
        }
        catch (JsonException falha)
        {
            throw new FormatException("Esse arquivo não é uma configuração do Vega.", falha);
        }
    }
}
