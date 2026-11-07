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
    public void Modo_normal_nao_apaga_disco_nem_mexe_no_defender()
    {
        var estado = new EstadoVega();
        var gerado = Gerar(estado);

        Assert.False(estado.ApagaDisco);
        Assert.DoesNotContain("CLEAN", gerado);
        Assert.DoesNotContain("WinDefend", gerado);
    }

    [Fact]
    public void Defender_so_e_desligado_no_modo_script()
    {
        Assert.DoesNotContain("WinDefend", Gerar(new EstadoVega { DesligarDefender = true }));
        Assert.Contains("WinDefend", Gerar(new EstadoVega { Modo = ModoInstalacao.Script, DesligarDefender = true }));
    }

    [Fact]
    public void Modo_script_padrao_pergunta_o_disco_e_pausa_antes_de_apagar()
    {
        var estado = new EstadoVega { Modo = ModoInstalacao.Script };
        var script = LinhasPe(estado);

        Assert.True(estado.ApagaDisco);
        Assert.False(estado.ApagaSemPerguntar);
        var pergunta = script.FindIndex(l => l.Contains("Enter index of the disk"));
        var pausa = script.FindIndex(l => l.Trim() == "pause");
        var diskpart = script.FindIndex(l => l.Contains("diskpart.exe /s"));
        Assert.InRange(pergunta, 0, int.MaxValue);
        Assert.True(pergunta < pausa, "A pergunta do disco deve vir antes da pausa.");
        Assert.True(pausa < diskpart, "A pausa deve vir antes de apagar o disco.");
    }

    [Theory]
    [InlineData(AlvoDisco.Numero)]
    [InlineData(AlvoDisco.Criterios)]
    public void Apagar_sem_perguntar_e_sinalizado(AlvoDisco alvo)
    {
        var estado = new EstadoVega { Modo = ModoInstalacao.Script, Alvo = alvo, PausarAntesDeFormatar = false };

        Assert.True(estado.ApagaSemPerguntar);
        Assert.Contains("<unattend", Gerar(estado));
    }

    [Fact]
    public void Particionar_a_mao_nao_apaga_o_disco_sozinho()
    {
        var estado = new EstadoVega { Modo = ModoInstalacao.Script, Particoes = Particionamento.AMao };
        var gerado = Gerar(estado);

        Assert.False(estado.ApagaDisco);
        Assert.DoesNotContain("CLEAN", gerado);
    }

    [Fact]
    public void Recuperacao_decide_se_o_winre_e_apagado()
    {
        Assert.Contains("winre.wim", Gerar(new EstadoVega { Modo = ModoInstalacao.Script, Recuperacao = RecoveryMode.None }));
        Assert.DoesNotContain("winre.wim", Gerar(new EstadoVega { Modo = ModoInstalacao.Script, Recuperacao = RecoveryMode.Partition }));
    }

    [Theory]
    [InlineData(OrigemImagem.Edicao, "pro", "/Name:")]
    [InlineData(OrigemImagem.Perguntar, "", "Enter index of the image")]
    [InlineData(OrigemImagem.Indice, "", "/Index:4")]
    [InlineData(OrigemImagem.Nome, "", "/Name:")]
    public void Origem_da_imagem_aparece_no_script(OrigemImagem origem, string edicao, string esperado)
    {
        var estado = new EstadoVega
        {
            Modo = ModoInstalacao.Script,
            Origem = origem,
            Edicao = edicao,
            ImagemIndice = 4,
            ImagemNome = "Windows 11 Pro",
        };

        Assert.Contains(LinhasPe(estado), l => l.Contains(esperado));
    }

    [Theory]
    [InlineData(ArquivoPaginacao.Automatico)]
    [InlineData(ArquivoPaginacao.Nenhum)]
    [InlineData(ArquivoPaginacao.Personalizado)]
    public void Paginacao_gera_um_xml_valido(ArquivoPaginacao paginacao)
    {
        Assert.Contains("<unattend", Gerar(new EstadoVega { Modo = ModoInstalacao.Script, Paginacao = paginacao }));
    }

    [Theory]
    [InlineData(AlvoDisco.Perguntar)]
    [InlineData(AlvoDisco.Criterios)]
    [InlineData(AlvoDisco.Numero)]
    public void Alvos_de_disco_e_layouts_geram_um_xml_valido(AlvoDisco alvo)
    {
        foreach (var layout in Enum.GetValues<PartitionLayout>())
        {
            Assert.Contains("<unattend", Gerar(new EstadoVega { Modo = ModoInstalacao.Script, Alvo = alvo, Layout = layout }));
        }
    }

    [Fact]
    public void Scripts_proprios_geram_um_xml_valido()
    {
        const string Diskpart = "SELECT DISK=0\r\nCLEAN\r\nCREATE PARTITION PRIMARY\r\nASSIGN LETTER=S\r\nASSIGN LETTER=W";

        Assert.Contains("<unattend", Gerar(new EstadoVega { Modo = ModoInstalacao.Script, Particoes = Particionamento.ScriptDiskpart, ScriptDiskpart = Diskpart }));
        Assert.Contains("<unattend", Gerar(new EstadoVega { Modo = ModoInstalacao.Script, Alvo = AlvoDisco.ScriptProprio, ScriptAlvoDisco = "WScript.Echo 0" }));
        Assert.Contains("<unattend", Gerar(new EstadoVega { Modo = ModoInstalacao.ScriptProprio, ScriptInstalacao = "echo teste" }));
    }

    [Fact]
    public void Script_diskpart_sem_as_letras_e_recusado_com_mensagem()
    {
        var estado = new EstadoVega { Modo = ModoInstalacao.Script, Particoes = Particionamento.ScriptDiskpart, ScriptDiskpart = "LIST DISK" };

        Assert.Throws<ConfigurationException>(() => Gerar(estado));
    }

    [Fact]
    public void Perfil_maximo_desliga_o_defender_e_pergunta_o_disco()
    {
        var estado = new EstadoVega();
        Perfis.Todos.Single(p => p.Id == "maximo").Aplicar(estado);
        var script = string.Join('\n', LinhasPe(estado));

        Assert.Contains("WinDefend", script);
        Assert.Contains("/Compact", script);
        Assert.Contains("Enter index of the disk", script);
        Assert.True(estado.PausarAntesDeFormatar);
    }

    [Theory]
    [InlineData("padrao")]
    [InlineData("diario")]
    public void Perfis_comuns_nao_apagam_disco_mesmo_depois_do_maximo(string id)
    {
        var estado = new EstadoVega();
        Perfis.Todos.Single(p => p.Id == "maximo").Aplicar(estado);
        Perfis.Todos.Single(p => p.Id == id).Aplicar(estado);

        Assert.False(estado.ApagaDisco);
        Assert.False(estado.DesligarDefender);
        Assert.DoesNotContain("CLEAN", Gerar(estado));
    }

    [Fact]
    public void Cada_opcao_de_script_muda_o_estado()
    {
        foreach (var opcao in Catalogo.OpcoesScript)
        {
            var estado = new EstadoVega { Modo = ModoInstalacao.Script };
            var original = opcao.Ler(estado);
            opcao.Gravar(estado, !original);

            Assert.Equal(!original, opcao.Ler(estado));
            Assert.Contains("<unattend", Gerar(estado));
        }
    }

    [Fact]
    public void Idioma_padrao_fica_em_portugues_do_brasil()
    {
        Assert.Contains("<UILanguage>pt-BR</UILanguage>", Gerar(new EstadoVega()));
    }

    private static string Gerar(EstadoVega estado) => Encoding.ASCII.GetString(estado.GerarXml(Gerador));

    /// <summary>A cópia legível do script do WinPE, que fica no próprio XML.</summary>
    private static List<string> LinhasPe(EstadoVega estado)
    {
        var xml = new System.Xml.XmlDocument();
        xml.LoadXml(Gerar(estado));
        var espacos = new System.Xml.XmlNamespaceManager(xml.NameTable);
        espacos.AddNamespace("s", Constants.MyNamespaceUri);
        var copia = xml.SelectSingleNode("//s:PEScriptCopy", espacos)?.InnerText ?? "";
        return copia.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
    }
}
