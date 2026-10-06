using System.Globalization;

namespace Desafio.Core;

public static class Dinheiro
{
    private static readonly NumberFormatInfo Brasil = new()
    {
        NumberDecimalSeparator = ",",
        NumberGroupSeparator = ".",
    };

    public static decimal Arredondar(decimal valor) =>
        Math.Round(valor, 2, MidpointRounding.AwayFromZero);

    /// <summary>Formata como moeda brasileira: R$ 1.234,56 (independe da cultura do SO).</summary>
    public static string FormatarBrl(decimal valor, int casas = 2) =>
        "R$ " + Math.Round(valor, casas, MidpointRounding.AwayFromZero).ToString($"N{casas}", Brasil);
}
