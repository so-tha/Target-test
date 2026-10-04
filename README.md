# Desafio Dev (C# / .NET 8)

Solução das 3 questões do desafio, sem dependências além do SDK (xunit só nos testes).

```
dotnet test                                   # 20 testes
dotnet run --project src/Desafio.Cli -- comissao
dotnet run --project src/Desafio.Cli -- estoque entrada 101 50 "Compra NF 123"
dotnet run --project src/Desafio.Cli -- estoque saida 101 30 "Venda balcão"
dotnet run --project src/Desafio.Cli -- estoque listar | historico
dotnet run --project src/Desafio.Cli -- juros 1000 24/09/2026 [--hoje 04/10/2026]
```

## Estrutura
- `src/Desafio.Core`: regras de negócio puras e testáveis (`Comissao`, `Estoque`, `Juros`, `Dinheiro`).
- `src/Desafio.Cli`: interface de linha de comando fina.
- `tests/Desafio.Tests`: testes de limites, regras de negócio e dados reais do desafio.
- `data/`: os JSON do enunciado.

## Decisões
- **`decimal` em todo valor monetário**, nunca `double`; arredondamento comercial (meio para cima).
- **Q1**: faixas em tabela (fácil de alterar). Limites exatos: R$100 → 1%, R$500 → 5%. A comissão é calculada venda a venda e arredondada só no total do vendedor.
  Resultado: João R$ 495,68 · Maria R$ 465,95 · Carlos R$ 379,37 · Ana R$ 404,98.
- **Q2**: cada movimentação tem ID sequencial único, descrição obrigatória, tipo, data e saldo final. Saída acima do saldo é recusada sem alterar nada. Estado e histórico persistem em `estado_estoque.json` (criado a partir do JSON semente, gravação atômica). Caminho configurável com `DESAFIO_ESTADO`.
- **Q3**: o enunciado é ambíguo ("multa/juros de 2,5% ao dia"), então a CLI mostra **os dois regimes**, por dia corrido de atraso, sem juros se não houver atraso:
  - simples: `valor × 2,5% × dias`
  - composto: `valor × ((1,025^dias) − 1)`
  Exemplo (R$ 1.000, 10 dias): simples R$ 250,00; composto R$ 280,08.
