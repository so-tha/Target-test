using Desafio.Core;

namespace Desafio.Cli;

/// <summary>Menu interativo: roda quando o programa é aberto sem argumentos.</summary>
public static class Menu
{
    public static void Executar(string dados, string estado)
    {
        while (true)
        {
            Console.WriteLine("""

                === Target Dev ===
                1) Comissão por vendedor
                2) Movimentar estoque
                3) Juros por atraso
                0) Sair
                """);
            var opcao = Ler("Escolha");
            if (opcao is null or "0") return;
            try
            {
                switch (opcao)
                {
                    case "1": Comandos.Comissao(Path.Combine(dados, "vendas.json"), detalhe: true); break;
                    case "2": Estoque(new Deposito(estado, Path.Combine(dados, "estoque.json"))); break;
                    case "3": Juros(); break;
                    default: Console.WriteLine("Opção inválida."); break;
                }
            }
            catch (Exception e) when (Erros.Esperado(e)) { Console.WriteLine($"Erro: {e.Message}"); }
        }
    }

    private static void Estoque(Deposito dep)
    {
        Comandos.Estoque(dep, ["listar"]);
        var tipo = Ler("\nTipo (entrada/saida)")?.ToLowerInvariant();
        var codigo = Ler("Código do produto");
        var qtd = Ler("Quantidade");
        var desc = Ler("Descrição (ex.: Compra NF 123)");
        if (tipo is null || codigo is null || qtd is null || desc is null) return;
        Comandos.Estoque(dep, [tipo, codigo, qtd, desc]);
    }

    private static void Juros()
    {
        var valor = Ler("Valor");
        var venc = Ler("Vencimento (DD/MM/AAAA)");
        if (valor is null || venc is null) return;
        Comandos.Juros(CalculadoraJuros.ParseValor(valor), CalculadoraJuros.ParseData(venc), null);
    }

    private static string? Ler(string rotulo)
    {
        Console.Write($"{rotulo}: ");
        return Console.ReadLine()?.Trim();
    }
}
