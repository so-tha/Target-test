using System.Text.Json;
using System.Text.Json.Serialization;

namespace Desafio.Core;

public enum TipoMovimentacao { Entrada, Saida }

public class EstoqueException(string mensagem) : Exception(mensagem);

public sealed class ProdutoNaoEncontradoException(int codigo)
    : EstoqueException($"produto {codigo} não encontrado");

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
    private readonly Estado _estado;
    private readonly object _trava = new(); // a API é concorrente; o Deposito é singleton

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

    // Cópias sob trava: leituras concorrentes nunca enxergam uma lista (ou produto) pela metade.
    public IReadOnlyList<Produto> Produtos { get { lock (_trava) return _estado.Produtos.Select(Copiar).ToList(); } }
    public IReadOnlyList<Movimentacao> Historico { get { lock (_trava) return _estado.Movimentacoes.ToList(); } }

    private static Produto Copiar(Produto p) => new() { Codigo = p.Codigo, Descricao = p.Descricao, Estoque = p.Estoque };

    public Produto ObterProduto(int codigo) { lock (_trava) return Copiar(Buscar(codigo)); }

    private Produto Buscar(int codigo) =>
        _estado.Produtos.FirstOrDefault(p => p.Codigo == codigo)
        ?? throw new ProdutoNaoEncontradoException(codigo);

    public Movimentacao Movimentar(int codigo, TipoMovimentacao tipo, int quantidade, string descricao)
    {
        lock (_trava) return MovimentarSemTrava(codigo, tipo, quantidade, descricao);
    }

    private Movimentacao MovimentarSemTrava(int codigo, TipoMovimentacao tipo, int quantidade, string descricao)
    {
        if (quantidade <= 0) throw new EstoqueException("A quantidade deve ser um inteiro positivo");
        if (string.IsNullOrWhiteSpace(descricao)) throw new EstoqueException("A descrição da movimentação é obrigatória");

        var produto = Buscar(codigo);
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
