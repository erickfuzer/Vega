// Gera os XMLs de teste pelo mesmo código do site, e cada um também dentro de uma .iso, como o botão do site.
// Rode dentro desta pasta: dotnet run gerar.cs -- xml
#:project ../../src/Vega.Core/Vega.Core.csproj
using Schneegans.Unattend;
using Vega.Core;

var saida = Path.GetFullPath(args.Length > 0 ? args[0] : "xml");
Directory.CreateDirectory(saida);
var gerador = new UnattendGenerator();

foreach (var id in new[] { "padrao", "diario", "maximo" })
{
    var estado = new EstadoVega();
    Perfis.Todos.Single(p => p.Id == id).Aplicar(estado);
    Salvar(id, estado.GerarXml(gerador));
}

// O mesmo Remoção máxima, mas como o motor original gera: com os dois rótulos :choice.
// Serve só para ver o bug do motor na VM. Ele apaga o disco escolhido: não use fora dela.
var bug = new EstadoVega();
Perfis.Todos.Single(p => p.Id == "maximo").Aplicar(bug);
Salvar("bug-choice", UnattendGenerator.Serialize(gerador.GenerateXml(bug.Montar(gerador))));

// O script de coleta numa .iso, para colocar na VM depois do primeiro login.
var coleta = Path.GetFullPath("coleta.ps1");
if (!File.Exists(coleta)) { Console.WriteLine("coleta.ps1 não encontrado: rode dentro da pasta do kit."); return; }
File.WriteAllBytes(Path.Combine(saida, "coleta.iso"), GeradorIso.Criar("coleta.ps1", File.ReadAllBytes(coleta)));
Console.WriteLine("coleta.iso: ok");

void Salvar(string nome, byte[] xml)
{
    File.WriteAllBytes(Path.Combine(saida, $"{nome}.xml"), xml);
    File.WriteAllBytes(Path.Combine(saida, $"{nome}.iso"), GeradorIso.Criar("autounattend.xml", xml));
    Console.WriteLine($"{nome}: {xml.Length:N0} bytes");
}
