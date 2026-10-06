using System.Globalization;

namespace Desafio.Core;

public sealed record ResultadoJuros(
    decimal Valor, DateOnly Vencimento, DateOnly Hoje, int DiasAtraso,
    decimal JurosSimples, decimal? JurosCompostos)
{
    public decimal TotalSimples => Valor + JurosSimples;
    public decimal? TotalCompostos => Valor + JurosCompostos;
}

/// <summary>
/// Questão 3. O enunciado não diz o regime, então calcula os dois (2,5% ao dia, por dia
/// corrido de atraso = hoje − vencimento; sem atraso → sem juros):
///   simples   : valor × taxa × dias
///   composto  : valor × ((1 + taxa)^dias − 1)
/// </summary>
public static class CalculadoraJuros
{
    public const decimal TaxaDiaria = 0.025m;

    public static ResultadoJuros Calcular(
        decimal valor, DateOnly vencimento, DateOnly? hoje = null, decimal taxaDiaria = TaxaDiaria)
    {
        if (valor < 0) throw new ArgumentOutOfRangeException(nameof(valor), "o valor não pode ser negativo");
        var data = hoje ?? DateOnly.FromDateTime(DateTime.Today);
        var dias = Math.Max(data.DayNumber - vencimento.DayNumber, 0);
        return new(valor, vencimento, data, dias,
            Dinheiro.Arredondar(valor * taxaDiaria * dias),
            JurosCompostos(valor, taxaDiaria, dias));
    }

    private static decimal? JurosCompostos(decimal valor, decimal taxa, int dias)
    {
        try
        {
            var fator = 1m;
            for (var i = 0; i < dias; i++) fator *= 1 + taxa;
            return Dinheiro.Arredondar(valor * (fator - 1));
        }
        catch (OverflowException) { return null; }
    }

    public static DateOnly ParseData(string texto) =>
        DateOnly.TryParseExact(texto.Trim(), ["dd/MM/yyyy", "yyyy-MM-dd"],
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d
            : throw new FormatException($"data inválida: '{texto}' (use DD/MM/AAAA ou AAAA-MM-DD)");

    public static decimal ParseValor(string texto) =>
        decimal.TryParse(texto.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var v)
            ? v
            : throw new FormatException($"valor inválido: '{texto}'");
}
