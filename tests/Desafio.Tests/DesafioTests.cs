using Xunit;
using Desafio.Core;

namespace Desafio.Tests;

public class ComissaoTests
{
    [Theory]
    [InlineData("0", "0")]
    [InlineData("99.99", "0")]
    [InlineData("100", "0.01")]
    [InlineData("499.99", "0.01")]
    [InlineData("500", "0.05")]
    [InlineData("1000", "0.05")]
    public void Faixas_e_limites(string valor, string esperado) =>
        Assert.Equal(decimal.Parse(esperado), CalculadoraComissao.Percentual(decimal.Parse(valor)));

    [Fact]
    public void Venda_negativa_e_rejeitada() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => CalculadoraComissao.ComissaoDaVenda(-1));

    [Fact]
    public void Dados_do_desafio()
    {
        var r = CalculadoraComissao.Calcular(
            CalculadoraComissao.Carregar(Path.Combine(AppContext.BaseDirectory, "data", "vendas.json")))
            .ToDictionary(x => x.Vendedor, x => x.Comissao);
        Assert.Equal(495.68m, r["João Silva"]);
        Assert.Equal(465.95m, r["Maria Souza"]);
        Assert.Equal(379.37m, r["Carlos Oliveira"]);
        Assert.Equal(404.98m, r["Ana Lima"]);
    }
}

public class EstoqueTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory().FullName;
    private string Estado => Path.Combine(_dir, "estado.json");
    private static string Semente => Path.Combine(AppContext.BaseDirectory, "data", "estoque.json");
    private Deposito Novo() => new(Estado, Semente);
    public void Dispose() => Directory.Delete(_dir, true);

    [Fact]
    public void Entrada_e_saida_retornam_saldo_final()
    {
        var d = Novo();
        Assert.Equal(200, d.Movimentar(101, TipoMovimentacao.Entrada, 50, "Compra").EstoqueFinal);
        Assert.Equal(170, d.Movimentar(101, TipoMovimentacao.Saida, 30, "Venda").EstoqueFinal);
    }

    [Fact]
    public void Ids_unicos_e_persistencia()
    {
        var a = Novo().Movimentar(101, TipoMovimentacao.Entrada, 1, "x");
        var b = new Deposito(Estado).Movimentar(102, TipoMovimentacao.Saida, 1, "y");
        Assert.NotEqual(a.Id, b.Id);
        Assert.Equal(151, new Deposito(Estado).ObterProduto(101).Estoque);
    }

    [Theory]
    [InlineData(999, TipoMovimentacao.Entrada, 1, "x")]
    [InlineData(101, TipoMovimentacao.Saida, 151, "x")]
    [InlineData(101, TipoMovimentacao.Entrada, 0, "x")]
    [InlineData(101, TipoMovimentacao.Entrada, 1, "  ")]
    public void Regras_de_negocio_nao_alteram_o_estado(int cod, TipoMovimentacao t, int q, string desc)
    {
        var d = Novo();
        Assert.Throws<EstoqueException>(() => d.Movimentar(cod, t, q, desc));
        Assert.Equal(150, d.ObterProduto(101).Estoque);
        Assert.Empty(d.Historico);
    }
}

public class JurosTests
{
    private static readonly DateOnly Hoje = new(2026, 10, 4);

    [Fact]
    public void Com_atraso()
    {
        var r = CalculadoraJuros.Calcular(1000m, new(2026, 9, 24), Hoje);
        Assert.Equal((10, 250.00m, 1250.00m), (r.DiasAtraso, r.JurosSimples, r.TotalSimples));
        // 1000 × (1,025^10 − 1) = 280,08
        Assert.Equal((280.08m, 1280.08m), (r.JurosCompostos, r.TotalCompostos));
    }

    [Fact]
    public void Um_dia_simples_e_composto_coincidem() =>
        Assert.Equal(25.00m, CalculadoraJuros.Calcular(1000m, new(2026, 10, 3), Hoje).JurosCompostos);

    [Theory]
    [InlineData(2026, 10, 4)]
    [InlineData(2026, 12, 1)]
    public void Sem_atraso_sem_juros(int a, int m, int d) =>
        Assert.Equal(0m, CalculadoraJuros.Calcular(100m, new(a, m, d), Hoje).JurosCompostos);

    [Fact]
    public void Formatos_de_data()
    {
        Assert.Equal(CalculadoraJuros.ParseData("04/10/2026"), CalculadoraJuros.ParseData("2026-10-04"));
        Assert.Throws<FormatException>(() => CalculadoraJuros.ParseData("2026/10/04"));
    }

    [Fact]
    public void Valor_aceita_virgula() => Assert.Equal(1500.50m, CalculadoraJuros.ParseValor("1500,50"));
}

public class FormatoTests
{
    [Fact]
    public void Brl() => Assert.Equal("R$ 1.234.567,80", Dinheiro.FormatarBrl(1234567.8m));
}
