using Desafio.Cli;
using Desafio.Core;

const string Uso = """
    Uso (sem argumentos abre um menu interativo):
      desafio comissao [arquivo.json] [--detalhe] [--csv saida.csv]
      desafio estoque listar|historico
      desafio estoque entrada|saida <codigo> <quantidade> "<descricao>"
      desafio juros <valor> <vencimento DD/MM/AAAA> [--hoje DD/MM/AAAA]
    """;

var dados = Path.Combine(AppContext.BaseDirectory, "data");
var estado = Environment.GetEnvironmentVariable("DESAFIO_ESTADO")
             ?? Path.Combine(Environment.CurrentDirectory, "estado_estoque.json");

try
{
    switch (args)
    {
        case []:
            Menu.Executar(dados, estado);
            break;
        case ["comissao", .. var resto]:
            var arquivo = resto.FirstOrDefault(a => !a.StartsWith("--") && !a.EndsWith(".csv")) ?? Path.Combine(dados, "vendas.json");
            var i = Array.IndexOf(resto, "--csv");
            if (i >= 0 && i + 1 >= resto.Length) throw new FormatException("--csv exige o nome do arquivo");
            Comandos.Comissao(arquivo, resto.Contains("--detalhe"), i >= 0 ? resto[i + 1] : null);
            break;
        case ["estoque", .. var resto]:
            Comandos.Estoque(new Deposito(estado, Path.Combine(dados, "estoque.json")), resto);
            break;
        case ["juros", var valor, var venc, .. var resto]:
            var hoje = resto is ["--hoje", var h] ? CalculadoraJuros.ParseData(h) : (DateOnly?)null;
            Comandos.Juros(CalculadoraJuros.ParseValor(valor), CalculadoraJuros.ParseData(venc), hoje);
            break;
        default:
            Console.WriteLine(Uso);
            return 2;
    }
    return 0;
}
catch (Exception e) when (Erros.Esperado(e))
{
    Console.Error.WriteLine($"Erro: {e.Message}");
    return 1;
}

namespace Desafio.Cli
{
    internal static class Erros
    {
        public static bool Esperado(Exception e) =>
            e is EstoqueException or FormatException or ArgumentException or IOException
                or System.Text.Json.JsonException;
    }
}
