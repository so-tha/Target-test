using Desafio.Core;

const string Uso = """
    Uso:
      desafio comissao [arquivo.json]
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
        case ["comissao", ..]:
            Comissao(args.Length > 1 ? args[1] : Path.Combine(dados, "vendas.json"));
            break;
        case ["estoque", ..] when args.Length > 1:
            Estoque(new Deposito(estado, Path.Combine(dados, "estoque.json")), args[1..]);
            break;
        case ["juros", var valor, var venc, ..]:
            var hoje = args is [_, _, _, "--hoje", var h] ? CalculadoraJuros.ParseData(h) : (DateOnly?)null;
            Juros(CalculadoraJuros.ParseValor(valor), CalculadoraJuros.ParseData(venc), hoje);
            break;
        default:
            Console.WriteLine(Uso);
            return 2;
    }
    return 0;
}
catch (Exception e) when (e is EstoqueException or FormatException or ArgumentException or IOException or OverflowException or System.Text.Json.JsonException)
{
    Console.Error.WriteLine($"Erro: {e.Message}");
    return 1;
}

static void Comissao(string arquivo)
{
    var resumo = CalculadoraComissao.Calcular(CalculadoraComissao.Carregar(arquivo));
    Console.WriteLine($"{"Vendedor",-18}{"Total vendido",16}{"Comissão",14}");
    Console.WriteLine(new string('-', 48));
    foreach (var r in resumo)
        Console.WriteLine($"{r.Vendedor,-18}{Dinheiro.FormatarBrl(r.TotalVendido),16}{Dinheiro.FormatarBrl(r.Comissao),14}");
    Console.WriteLine(new string('-', 48));
    Console.WriteLine($"{"Total",-18}{Dinheiro.FormatarBrl(resumo.Sum(r => r.TotalVendido)),16}{Dinheiro.FormatarBrl(resumo.Sum(r => r.Comissao)),14}");
}

static void Estoque(Deposito dep, string[] a)
{
    switch (a)
    {
        case ["listar"]:
            foreach (var p in dep.Produtos) Console.WriteLine($"{p.Codigo}  {p.Descricao,-28}{p.Estoque,6}");
            break;
        case ["historico"]:
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
            Console.WriteLine(Uso);
            break;
    }
}

static void Juros(decimal valor, DateOnly vencimento, DateOnly? hoje)
{
    var r = CalculadoraJuros.Calcular(valor, vencimento, hoje);
    Console.WriteLine($"Valor original  : {Dinheiro.FormatarBrl(r.Valor)}");
    Console.WriteLine($"Vencimento      : {r.Vencimento:dd/MM/yyyy}  (hoje: {r.Hoje:dd/MM/yyyy})");
    Console.WriteLine($"Dias em atraso  : {r.DiasAtraso}");
    Console.WriteLine();
    Console.WriteLine($"{"",-10}{"Juros",16}{"Total a pagar",18}");
    Console.WriteLine($"{"Simples",-10}{Dinheiro.FormatarBrl(r.JurosSimples),16}{Dinheiro.FormatarBrl(r.TotalSimples),18}");
    Console.WriteLine($"{"Composto",-10}{Dinheiro.FormatarBrl(r.JurosCompostos),16}{Dinheiro.FormatarBrl(r.TotalCompostos),18}");
}
