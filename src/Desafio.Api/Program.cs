using System.Text.Json.Serialization;
using Desafio.Core;
using Microsoft.AspNetCore.Http.Json;

var builder = WebApplication.CreateBuilder(args);

var dados = Path.Combine(AppContext.BaseDirectory, "data");
var estado = builder.Configuration["Estoque:Estado"]
             ?? Path.Combine(Environment.CurrentDirectory, "estado_estoque.json");

builder.Services.AddSingleton(new Deposito(estado, Path.Combine(dados, "estoque.json")));
builder.Services.Configure<JsonOptions>(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o => o.SwaggerDoc("v1", new() { Title = "Desafio Dev API", Version = "v1" }));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(o => { o.SwaggerEndpoint("/swagger/v1/swagger.json", "Desafio Dev API"); o.RoutePrefix = ""; });

// Erros de regra de negócio e de entrada viram respostas HTTP padronizadas (RFC 7807).
app.Use(async (ctx, next) =>
{
    try { await next(ctx); }
    catch (Exception e) when (e is EstoqueException or FormatException or ArgumentException)
    {
        var status = e switch
        {
            ProdutoNaoEncontradoException => StatusCodes.Status404NotFound,
            EstoqueException => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status400BadRequest,
        };
        await Results.Problem(detail: e.Message, statusCode: status).ExecuteAsync(ctx);
    }
});

// ---- Q1: comissões --------------------------------------------------------
app.MapGet("/comissoes", (string? formato) =>
{
    var resumo = CalculadoraComissao.Calcular(
        CalculadoraComissao.Carregar(Path.Combine(dados, "vendas.json")));
    return formato == "csv"
        ? Results.File(System.Text.Encoding.UTF8.GetBytes(CalculadoraComissao.ParaCsv(resumo)),
            "text/csv", "comissoes.csv")
        : Results.Ok(resumo);
}).WithTags("Comissões").WithSummary("Comissão por vendedor (use ?formato=csv para exportar)");

// ---- Q2: estoque ----------------------------------------------------------
var estoque = app.MapGroup("/estoque").WithTags("Estoque");
estoque.MapGet("/", (Deposito d) => d.Produtos).WithSummary("Produtos e saldos");
estoque.MapGet("/{codigo:int}", (int codigo, Deposito d) => d.ObterProduto(codigo)).WithSummary("Saldo de um produto");
estoque.MapGet("/movimentacoes", (Deposito d) => d.Historico).WithSummary("Histórico de movimentações");
estoque.MapPost("/movimentacoes", (NovaMovimentacao m, Deposito d) =>
{
    var mov = d.Movimentar(m.CodigoProduto, m.Tipo, m.Quantidade, m.Descricao ?? "");
    return Results.Created($"/estoque/movimentacoes/{mov.Id}", mov); // inclui estoqueFinal
}).WithSummary("Lança entrada ou saída; retorna a movimentação com o estoque final");

// ---- Q3: juros ------------------------------------------------------------
app.MapGet("/juros", (string valor, string vencimento, string? hoje) =>
    CalculadoraJuros.Calcular(
        CalculadoraJuros.ParseValor(valor),
        CalculadoraJuros.ParseData(vencimento),
        hoje is null ? null : CalculadoraJuros.ParseData(hoje)))
    .WithTags("Juros").WithSummary("Juros simples e compostos (2,5% ao dia). Datas: DD/MM/AAAA ou AAAA-MM-DD");

app.Run();

record NovaMovimentacao(int CodigoProduto, TipoMovimentacao Tipo, int Quantidade, string? Descricao);

public partial class Program; // visível para os testes de integração
