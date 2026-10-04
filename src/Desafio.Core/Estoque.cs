using System.Text.Json;
using System.Text.Json.Serialization;

namespace Desafio.Core;

public enum TipoMovimentacao { Entrada, Saida }

/// <summary>Violação de regra de negócio (produto inexistente, saldo insuficiente...).</summary>
public sealed class EstoqueException(string mensagem) : Exception(mensagem);

public sealed class Produto
{
    [JsonPropertyName("codigoProduto")] public int Codigo { get; set; }
    [JsonPropertyName("descricaoProduto")] public string Descricao { get; set; } = "";
    [JsonPropertyName("estoque")] public int Estoque { get; set; }
}

public sealed record Movimentacao(
    int Id, int CodigoProduto, TipoMovimentacao Tipo, int Quantidade,
    string Descricao, DateTime Data, int EstoqueFinal);

/// <summary>
/// Questão 2. Estado (produtos + histórico) persistido em JSON; criado a partir do JSON
/// semente do desafio na primeira execução. Gravação atômica (arquivo temporário + move).
/// </summary>
public sealed class Deposito
{
    private static readonly JsonSerializerOptions Opcoes = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly string _caminho;
    private Estado _estado;

    public Deposito(string caminhoEstado, string? caminhoSemente = null)
    {
        _caminho = caminhoEstado;
        if (!File.Exists(caminhoEstado))
        {
            if (caminhoSemente is null)
                throw new FileNotFoundException($"sem estado ({caminhoEstado}) e sem semente");
            var semente = JsonSerializer.Deserialize<Estado>(File.ReadAllText(caminhoSemente), Opcoes)!;
            Gravar(semente);
        }
        _estado = JsonSerializer.Deserialize<Estado>(File.ReadAllText(caminhoEstado), Opcoes)!;
    }

    public IReadOnlyList<Produto> Produtos => _estado.Produtos;
    public IReadOnlyList<Movimentacao> Historico => _estado.Movimentacoes;

    public Produto ObterProduto(int codigo) =>
        _estado.Produtos.FirstOrDefault(p => p.Codigo == codigo)
        ?? throw new EstoqueException($"produto {codigo} não encontrado");

    public Movimentacao Movimentar(int codigo, TipoMovimentacao tipo, int quantidade, string descricao)
    {
        if (quantidade <= 0) throw new EstoqueException("A quantidade deve ser um inteiro positivo");
        if (string.IsNullOrWhiteSpace(descricao)) throw new EstoqueException("A descrição da movimentação é obrigatória");

        var produto = ObterProduto(codigo);
        var saldo = produto.Estoque + (tipo == TipoMovimentacao.Entrada ? quantidade : -quantidade);
        if (saldo < 0)
            throw new EstoqueException($"Saldo insuficiente: há {produto.Estoque} un. e a saída pede {quantidade}");

        var mov = new Movimentacao(
            Id: _estado.Movimentacoes.Count + 1, 
            codigo, tipo, quantidade, descricao.Trim(),
            DateTime.Now.AddTicks(-(DateTime.Now.Ticks % TimeSpan.TicksPerSecond)), saldo);

        produto.Estoque = saldo;
        _estado.Movimentacoes.Add(mov);
        Gravar(_estado);
        return mov;
    }

    private void Gravar(Estado estado)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(_caminho))!;
        Directory.CreateDirectory(dir);
        var tmp = Path.Combine(dir, Path.GetRandomFileName() + ".tmp");
        try
        {
            File.WriteAllText(tmp, JsonSerializer.Serialize(estado, Opcoes));
            File.Move(tmp, _caminho, overwrite: true);
        }
        finally { if (File.Exists(tmp)) File.Delete(tmp); }
    }

    private sealed class Estado
    {
        [JsonPropertyName("estoque")] public List<Produto> Produtos { get; set; } = [];
        [JsonPropertyName("movimentacoes")] public List<Movimentacao> Movimentacoes { get; set; } = [];
    }
}
