using Desafio.Core;

namespace Desafio.Cli;

/// <summary>Os três comandos do desafio; usados tanto pelos subcomandos quanto pelo menu.</summary>
public static class Comandos
{
    public static void Comissao(string arquivo, bool detalhe = false, string? csv = null)
    {
        var resumo = CalculadoraComissao.Calcular(CalculadoraComissao.Carregar(arquivo));
        Console.WriteLine($"{"Vendedor",-18}{"Total vendido",16}{"Comissão",14}");
        Console.WriteLine(new string('-', 48));
        foreach (var r in resumo)
        {
            Console.WriteLine($"{r.Vendedor,-18}{Dinheiro.FormatarBrl(r.TotalVendido),16}{Dinheiro.FormatarBrl(r.Comissao),14}");
            if (!detalhe) continue;
            foreach (var f in r.Faixas)
                Console.WriteLine($"   {f.Vendas,2} venda(s) {f.Descricao,-26} ({f.Percentual:P0}) → {Dinheiro.FormatarBrl(f.Comissao, 4)}");
        }
        Console.WriteLine(new string('-', 48));
        Console.WriteLine($"{"Total",-18}{Dinheiro.FormatarBrl(resumo.Sum(r => r.TotalVendido)),16}{Dinheiro.FormatarBrl(resumo.Sum(r => r.Comissao)),14}");
        if (csv is null) return;
        File.WriteAllText(csv, CalculadoraComissao.ParaCsv(resumo), new System.Text.UTF8Encoding(true)); // BOM: Excel abre acentos certo
        Console.WriteLine($"\nCSV gravado em {csv}");
    }

    public static void Estoque(Deposito dep, string[] a)
    {
        switch (a)
        {
            case ["listar"]:
                foreach (var p in dep.Produtos) Console.WriteLine($"{p.Codigo}  {p.Descricao,-28}{p.Estoque,6}");
                break;
            case ["historico"]:
                if (dep.Historico.Count == 0) Console.WriteLine("Nenhuma movimentação ainda.");
                foreach (var m in dep.Historico)
                    Console.WriteLine($"#{m.Id,-4}{m.Data:yyyy-MM-dd HH:mm:ss}  {m.Tipo,-8}{m.Quantidade,5}  prod {m.CodigoProduto}  saldo {m.EstoqueFinal,5}  {m.Descricao}");
                break;
            case [var acao and ("entrada" or "saida"), var cod, var qtd, var desc]:
                if (!int.TryParse(cod, out var codigo) || !int.TryParse(qtd, out var quantidade))
                    throw new FormatException("código e quantidade devem ser inteiros");
                var tipo = acao == "entrada" ? TipoMovimentacao.Entrada : TipoMovimentacao.Saida;
                var mov = dep.Movimentar(codigo, tipo, quantidade, desc);
                Console.WriteLine($"Movimentação #{mov.Id} registrada ({acao}: {mov.Descricao})");
                Console.WriteLine($"Estoque final de '{dep.ObterProduto(codigo).Descricao}': {mov.EstoqueFinal}");
                break;
            default:
                throw new FormatException("uso: estoque listar | historico | entrada|saida <código> <qtd> \"<descrição>\"");
        }
    }

    public static void Juros(decimal valor, DateOnly vencimento, DateOnly? hoje)
    {
        var r = CalculadoraJuros.Calcular(valor, vencimento, hoje);
        Console.WriteLine($"Valor original : {Dinheiro.FormatarBrl(r.Valor)}");
        Console.WriteLine($"Vencimento     : {r.Vencimento:dd/MM/yyyy}  (hoje: {r.Hoje:dd/MM/yyyy})");
        Console.WriteLine($"Dias em atraso : {r.DiasAtraso}");
        Console.WriteLine();
        Console.WriteLine($"{"",-10}{"Juros",16}{"Total a pagar",18}");
        Console.WriteLine($"{"Simples",-10}{Dinheiro.FormatarBrl(r.JurosSimples),16}{Dinheiro.FormatarBrl(r.TotalSimples),18}");
        Console.WriteLine(r.JurosCompostos is { } jc
            ? $"{"Composto",-10}{Dinheiro.FormatarBrl(jc),16}{Dinheiro.FormatarBrl(r.TotalCompostos!.Value),18}"
            : $"{"Composto",-10}  indisponível: o atraso é longo demais e o valor excede o limite de cálculo");
    }
}
