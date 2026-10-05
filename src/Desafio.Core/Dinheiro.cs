using System.Globalization;

namespace Desafio.Core;

/// <summary>Utilitários monetários compartilhados (sempre decimal, nunca double).</summary>
public static class Dinheiro
{
    private static readonly NumberFormatInfo Brasil = new()
    {
        NumberDecimalSeparator = ",",
        NumberGroupSeparator = ".",
    };

    /// <summary>Arredonda para centavos (meio para cima, como no comércio).</summary>
    public static decimal Arredondar(decimal valor) =>
        Math.Round(valor, 2, MidpointRounding.AwayFromZero);

    /// <summary>Formata como moeda brasileira: R$ 1.234,56 (independe da cultura do SO).</summary>
    public static string FormatarBrl(decimal valor, int casas = 2) =>
        "R$ " + Math.Round(valor, casas, MidpointRounding.AwayFromZero).ToString($"N{casas}", Brasil);
}
