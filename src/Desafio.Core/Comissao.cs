using System.Text.Json;
using System.Text.Json.Serialization;

namespace Desafio.Core;

public sealed record Venda(
    [property: JsonPropertyName("vendedor")] string Vendedor,
    [property: JsonPropertyName("valor")] decimal Valor);

public sealed record DetalheFaixa(string Descricao, decimal Percentual, int Vendas, decimal TotalVendido, decimal Comissao);

public sealed record ResumoVendedor(
    string Vendedor, decimal TotalVendido, decimal Comissao, IReadOnlyList<DetalheFaixa> Faixas);

/// <summary>
/// Questão 1. Regra por venda: &lt; R$100 → 0%; &lt; R$500 → 1%; ≥ R$500 → 5%.
/// </summary>
public static class CalculadoraComissao
{
    private sealed record Faixa(decimal? Limite, decimal Taxa, string Descricao);

    // Em ordem crescente; limite superior exclusivo. A última é aberta.
    private static readonly Faixa[] Faixas =
    [
        new(100m, 0m, "abaixo de R$ 100,00"),
        new(500m, 0.01m, "de R$ 100,00 a R$ 499,99"),
        new(null, 0.05m, "a partir de R$ 500,00"),
    ];

    private static Faixa FaixaDe(decimal valor) =>
        Faixas.First(f => f.Limite is null || valor < f.Limite);

    public static decimal Percentual(decimal valor) => FaixaDe(valor).Taxa;

    public static decimal ComissaoDaVenda(decimal valor) =>
        valor < 0
            ? throw new ArgumentOutOfRangeException(nameof(valor), "valor de venda negativo")
            : valor * Percentual(valor);

    /// <summary>Soma por vendedor (ordem de aparição). Arredonda só no total, sem acumular erro.</summary>
    public static IReadOnlyList<ResumoVendedor> Calcular(IEnumerable<Venda> vendas) =>
        vendas
            .GroupBy(v => v.Vendedor)
            .Select(g => new ResumoVendedor(
                g.Key,
                Dinheiro.Arredondar(g.Sum(v => v.Valor)),
                Dinheiro.Arredondar(g.Sum(v => ComissaoDaVenda(v.Valor))),
                Detalhar(g)))
            .ToList();

    private static List<DetalheFaixa> Detalhar(IEnumerable<Venda> vendas)
    {
        var porFaixa = vendas.ToLookup(v => FaixaDe(v.Valor));
        return Faixas.Select(f => new DetalheFaixa(
            f.Descricao, f.Taxa, porFaixa[f].Count(),
            Dinheiro.Arredondar(porFaixa[f].Sum(v => v.Valor)),
            porFaixa[f].Sum(v => ComissaoDaVenda(v.Valor)))).ToList(); // exata: só o total do vendedor é arredondado
    }

    /// <summary>CSV para Excel pt-BR: separador ";" e vírgula decimal.</summary>
    public static string ParaCsv(IEnumerable<ResumoVendedor> resumo)
    {
        var pt = new System.Globalization.CultureInfo("pt-BR");
        var linhas = resumo.Select(r =>
            $"{r.Vendedor};{r.TotalVendido.ToString("F2", pt)};{r.Comissao.ToString("F2", pt)}");
        return string.Join('\n', linhas.Prepend("Vendedor;TotalVendido;Comissao"));
    }

    public static IReadOnlyList<Venda> Carregar(string caminho)
    {
        using var stream = File.OpenRead(caminho);
        var raiz = JsonSerializer.Deserialize<Raiz>(stream)
                   ?? throw new InvalidDataException("JSON de vendas vazio");
        return raiz.Vendas;
    }

    private sealed record Raiz([property: JsonPropertyName("vendas")] List<Venda> Vendas);
}
