using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Desafio.Tests;

public class ApiTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory().FullName;
    private readonly HttpClient _http;

    public ApiTests(WebApplicationFactory<Program> fabrica) =>
        _http = fabrica.WithWebHostBuilder(b =>
            b.UseSetting("Estoque:Estado", Path.Combine(_dir, "estado.json"))).CreateClient();

    public void Dispose() => Directory.Delete(_dir, true);

    [Fact]
    public async Task Movimentacao_retorna_estoque_final()
    {
        var r = await _http.PostAsJsonAsync("/estoque/movimentacoes",
            new { codigoProduto = 101, tipo = "saida", quantidade = 20, descricao = "Venda" });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var corpo = await r.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(130, corpo.GetProperty("estoqueFinal").GetInt32());
    }

    [Theory]
    [InlineData(999, 1, HttpStatusCode.NotFound)]
    [InlineData(101, 9999, HttpStatusCode.UnprocessableEntity)]
    public async Task Erros_de_negocio_viram_status_http(int produto, int qtd, HttpStatusCode esperado)
    {
        var r = await _http.PostAsJsonAsync("/estoque/movimentacoes",
            new { codigoProduto = produto, tipo = "saida", quantidade = qtd, descricao = "x" });
        Assert.Equal(esperado, r.StatusCode);
    }

    [Fact]
    public async Task Juros_e_comissoes()
    {
        var j = await _http.GetFromJsonAsync<System.Text.Json.JsonElement>(
            "/juros?valor=1000&vencimento=24/09/2026&hoje=04/10/2026");
        Assert.Equal(250m, j.GetProperty("jurosSimples").GetDecimal());
        Assert.Equal(280.08m, j.GetProperty("jurosCompostos").GetDecimal());
        Assert.Equal(HttpStatusCode.BadRequest, (await _http.GetAsync("/juros?valor=1&vencimento=lixo")).StatusCode);

        var csv = await _http.GetStringAsync("/comissoes?formato=csv");
        Assert.Contains("João Silva;10754,70;495,68", csv);
    }
}
