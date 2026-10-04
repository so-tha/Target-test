using System.Text.Json;
using System.Text.Json.Serialization;

namespace Desafio.Core;

public sealed record Venda(
    [property: JsonPropertyName("vendedor")] string Vendedor,
    [property: JsonPropertyName("valor")] decimal Valor);

public sealed record ResumoVendedor(string Vendedor, decimal TotalVendido, decimal Comissao);

/// <summary>
/// Questão 1. Regra por venda: &lt; R$100 → 0%; &lt; R$500 → 1%; ≥ R$500 → 5%.
/// </summary>
public static class CalculadoraComissao
{
    // Faixas em ordem crescente: (limite superior exclusivo, percentual). A última é aberta.
    private static readonly (decimal? Limite, decimal Taxa)[] Faixas =
    [
        (100m, 0m),
        (500m, 0.01m),
        (null, 0.05m),
    ];

    public static decimal Percentual(decimal valor)
    {
        foreach (var (limite, taxa) in Faixas)
            if (limite is null || valor < limite) return taxa;
        throw new InvalidOperationException("inalcançável: a última faixa é aberta");
    }

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
                Dinheiro.Arredondar(g.Sum(v => ComissaoDaVenda(v.Valor)))))
            .ToList();

    public static IReadOnlyList<Venda> Carregar(string caminho)
    {
        using var stream = File.OpenRead(caminho);
        var raiz = JsonSerializer.Deserialize<Raiz>(stream)
                   ?? throw new InvalidDataException("JSON de vendas vazio");
        return raiz.Vendas;
    }

    private sealed record Raiz([property: JsonPropertyName("vendas")] List<Venda> Vendas);
}
