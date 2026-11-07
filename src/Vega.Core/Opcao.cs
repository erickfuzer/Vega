using Schneegans.Unattend;

namespace Vega.Core;

public enum Categoria
{
    Instalacao,
    Disco,
    Conta,
    Apps,
    Privacidade,
    Desempenho,
    Seguranca,
    Interface,
    Aparencia,
    Edge,
    MaquinaVirtual,
    Avancado,
}

/// <summary>Quanto a opção pede de atenção de quem vai usar.</summary>
public enum Aviso
{
    Nenhum,
    Atencao,
    Risco,
}

/// <summary>Uma opção de ligar e desligar da <see cref="Configuration"/>.</summary>
public sealed record Opcao(
    string Id,
    Categoria Categoria,
    string Titulo,
    string Descricao,
    Func<Configuration, bool, Configuration> Aplicar,
    Aviso Aviso = Aviso.Nenhum,
    string? Custo = null);

/// <summary>Uma opção que só existe no modo script, quando o Vega aplica a imagem por conta própria.</summary>
public sealed record OpcaoScript(
    string Id,
    Categoria Categoria,
    string Titulo,
    string Descricao,
    Func<EstadoVega, bool> Ler,
    Action<EstadoVega, bool> Gravar,
    Aviso Aviso = Aviso.Nenhum,
    string? Custo = null);

/// <summary>Leva a busca até um campo que não é de ligar e desligar.</summary>
public sealed record AtalhoBusca(string Titulo, string Termos, Categoria Categoria);

/// <summary>Um item removível, com nome em português e o que se perde ao tirar.</summary>
public sealed record ItemRemovivel(
    string Id,
    string Nome,
    string Grupo,
    Aviso Aviso = Aviso.Nenhum,
    string? Custo = null);
