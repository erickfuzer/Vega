using System.Text;
using Schneegans.Unattend;
using Vega.Core;

namespace Vega.Tests;

public class CatalogoTests
{
    private static readonly UnattendGenerator Gerador = new();

    public static IEnumerable<object[]> TodasOpcoes => Catalogo.Opcoes.Select(o => new object[] { o.Id });

    public static IEnumerable<object[]> TodosPerfis => Perfis.Todos.Select(p => new object[] { p.Id });

    [Fact]
    public void Toda_opcao_booleana_do_gerador_esta_no_catalogo()
    {
        var doGerador = typeof(Configuration).GetProperties()
            .Where(p => p.PropertyType == typeof(bool) && p.GetSetMethod() is not null)
            .Select(p => p.Name)
            .ToHashSet();

        Assert.Empty(doGerador.Except(Catalogo.Opcoes.Select(o => o.Id)));
        Assert.Empty(Catalogo.Opcoes.Select(o => o.Id).Except(doGerador));
    }

    [Fact]
    public void Todo_item_removivel_do_gerador_esta_no_catalogo()
    {
        var doGerador = Gerador.Bloatwares.Keys.ToHashSet();

        Assert.Empty(doGerador.Except(Catalogo.Apps.Select(a => a.Id)));
        Assert.Empty(Catalogo.Apps.Select(a => a.Id).Except(doGerador));
    }

    [Fact]
    public void Ids_do_catalogo_nao_se_repetem()
    {
        Assert.Equal(Catalogo.Opcoes.Count, Catalogo.Opcoes.Select(o => o.Id).Distinct().Count());
        Assert.Equal(Catalogo.Apps.Count, Catalogo.Apps.Select(a => a.Id).Distinct().Count());
    }

    [Fact]
    public void Toda_categoria_tem_nome()
    {
        foreach (var categoria in Enum.GetValues<Categoria>())
        {
            Assert.True(Catalogo.NomesCategorias.ContainsKey(categoria), categoria.ToString());
        }
    }

    [Theory]
    [MemberData(nameof(TodasOpcoes))]
    public void Cada_opcao_ligada_sozinha_gera_um_xml_valido(string id)
    {
        var padrao = Gerar(new EstadoVega());
        var estado = new EstadoVega();
        estado.OpcoesLigadas.Add(id);

        var gerado = Gerar(estado);

        Assert.Contains("<unattend", gerado);
        if (id is not nameof(Configuration.KeepSensitiveFiles))
        {
            Assert.NotEqual(padrao, gerado);
        }
    }

    [Fact]
    public void Todas_as_opcoes_e_remocoes_juntas_geram_um_xml_valido()
    {
        var estado = new EstadoVega();
        estado.OpcoesLigadas.UnionWith(Catalogo.Opcoes.Select(o => o.Id));
        estado.AppsRemovidos.UnionWith(Catalogo.Apps.Select(a => a.Id));

        Assert.Contains("<unattend", Gerar(estado));
    }

    [Theory]
    [MemberData(nameof(TodosPerfis))]
    public void Cada_perfil_gera_um_xml_valido(string id)
    {
        var estado = new EstadoVega();
        Perfis.Todos.Single(p => p.Id == id).Aplicar(estado);

        Assert.Contains("<unattend", Gerar(estado));
    }

    [Fact]
    public void Perfil_de_uso_diario_mantem_protecao_e_recuperacao()
    {
        var estado = new EstadoVega();
        Perfis.Todos.Single(p => p.Id == "diario").Aplicar(estado);

        Assert.DoesNotContain(nameof(Configuration.DisableSmartScreen), estado.OpcoesLigadas);
        Assert.DoesNotContain(nameof(Configuration.DisableWindowsUpdate), estado.OpcoesLigadas);
        Assert.DoesNotContain(nameof(Configuration.DisableSystemRestore), estado.OpcoesLigadas);
        Assert.DoesNotContain("RemoveStore", estado.AppsRemovidos);
    }

    [Fact]
    public void Conta_local_sem_tela_de_rede_pula_as_duas_telas()
    {
        var gerado = Gerar(new EstadoVega { Conta = TipoConta.Local, MostrarTelaWifi = false });

        Assert.Contains("<HideOnlineAccountScreens>true</HideOnlineAccountScreens>", gerado);
        Assert.Contains("<HideWirelessSetupInOOBE>true</HideWirelessSetupInOOBE>", gerado);
    }

    [Fact]
    public void Idioma_padrao_fica_em_portugues_do_brasil()
    {
        Assert.Contains("<UILanguage>pt-BR</UILanguage>", Gerar(new EstadoVega()));
    }

    private static string Gerar(EstadoVega estado) => Encoding.ASCII.GetString(estado.GerarXml(Gerador));
}
