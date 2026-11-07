using System.Text.RegularExpressions;

namespace Vega.Core;

/// <summary>Traduz as mensagens de validação do gerador, que vêm em inglês.</summary>
public static class ErrosDoMotor
{
    private static readonly (Regex Padrao, string Traducao)[] Traducoes =
    [
        (Padrao(@"^Parameter '(.+)' must be set\.$"), "Falta preencher um campo obrigatório ($1)."),
        (Padrao(@"^Value of parameter '(.+)' must not be less than (.+), but was .+\.$"), "O campo $1 não pode ser menor que $2."),
        (Padrao(@"^Value of parameter '(.+)' must not be greater than (.+), but was .+\.$"), "O campo $1 não pode ser maior que $2."),
        (Padrao(@"^Value of 'minSizeGiB'"), "O tamanho mínimo do disco não pode passar do tamanho máximo."),
        (Padrao(@"^Cannot create \.cmd script with custom components"), "O modo script não aceita componentes XML na etapa windowsPE. Para carregar drivers, use a pasta $$WinPEDriver$$ da mídia."),
        (Padrao(@"^Your diskpart script must contain a line such as ‘ASSIGN LETTER=(.)’"), "Seu script do diskpart precisa ter uma linha ASSIGN LETTER=$1."),
        (Padrao(@"^Computer name '(.*)' is invalid\.$"), "O nome de computador \"$1\" é inválido. Use até 15 caracteres, sem espaços nem símbolos."),
        (Padrao(@"^To delete sensitive files"), "Para apagar os arquivos de instalação, o Windows precisa entrar sozinho em uma conta de administrador. Ligue o login automático em Conta e rede, ou Manter os arquivos de instalação em Avançado."),
        (Padrao(@"^AppLocker policy XML is invalid"), "A política do AppLocker não é um XML válido."),
        (Padrao(@"^Your XML markup"), "O XML de um componente está malformado."),
        (Padrao(@"^You must not include elements 'settings' or 'component'"), "O XML de um componente não pode incluir os elementos settings ou component."),
        (Padrao(@"^Line '.*' is too long"), "Uma linha de script é longa demais. Quebre o texto em mais linhas."),
        (Padrao(@"^Could not find an element of type"), "A configuração aponta para um item que não existe. Confira o arquivo que você abriu."),
        (Padrao(@"^Taskbar XML is not well-formed"), "O layout da barra de tarefas não é um XML válido."),
        (Padrao(@"^The string '.*' is not valid JSON\.$"), "A lista de fixados do Iniciar não é um JSON válido."),
        (Padrao(@"^Start menu XML is not well-formed"), "O layout dos blocos do Iniciar não é um XML válido."),
        (Padrao(@"^Scripts in phase '(.+)' must not have type '(.+)'\.$"), "Um script usa um tipo que não funciona nessa etapa: $2 em $1."),
        (Padrao(@"^Value of 'LockoutWindow'"), "O tempo para zerar a contagem não pode passar do tempo de bloqueio."),
        (Padrao(@"^Product key '(.*)' is ill-formed\.$"), "A chave do produto \"$1\" está em um formato inválido. Use XXXXX-XXXXX-XXXXX-XXXXX-XXXXX."),
        (Padrao(@"^At least one processor architecture"), "Marque pelo menos uma arquitetura de processador."),
        (Padrao(@"^WLAN profile XML is invalid"), "O perfil de Wi-Fi não é um XML válido."),
        (Padrao(@"^Account name\(s\) (.+) specified more than once\.$"), "Mais de uma conta usa o nome $1."),
        (Padrao(@"^Must have at least one administrator account\.$"), "É preciso ter pelo menos uma conta de administrador."),
        (Padrao(@"^Username '(.*)' is invalid\.$"), "O nome de usuário \"$1\" é inválido."),
        (Padrao(@"^Account name '(.*)' must not be the same as the computer name\.$"), "A conta \"$1\" não pode ter o mesmo nome do computador."),
    ];

    /// <summary>A mensagem em português, ou a original se ela não for conhecida.</summary>
    public static string Traduzir(string mensagem)
    {
        foreach (var (padrao, traducao) in Traducoes)
        {
            var achado = padrao.Match(mensagem);
            if (achado.Success)
            {
                return achado.Result(traducao);
            }
        }
        return mensagem;
    }

    private static Regex Padrao(string padrao) => new(padrao, RegexOptions.Singleline);
}
