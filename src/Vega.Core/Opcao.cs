using Schneegans.Unattend;

namespace Vega.Core;

public enum Categoria
{
    Instalacao,
    Conta,
    Apps,
    Privacidade,
    Desempenho,
    Seguranca,
    Interface,
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

/// <summary>Um item removível, com nome em português e o que se perde ao tirar.</summary>
public sealed record ItemRemovivel(
    string Id,
    string Nome,
    string Grupo,
    Aviso Aviso = Aviso.Nenhum,
    string? Custo = null);
