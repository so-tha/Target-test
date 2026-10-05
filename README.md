# Desafio Dev (C# / .NET 8)

As 3 questões, em três formas de uso: **CLI com menu interativo**, **API REST com Swagger** e **testes automatizados** (27).

## Resultados (questão 1)
| Vendedor | Total vendido | Comissão |
|---|---|---|
| João Silva | R$ 10.754,70 | **R$ 495,68** |
| Maria Souza | R$ 9.874,30 | **R$ 465,95** |
| Carlos Oliveira | R$ 7.928,35 | **R$ 379,37** |
| Ana Lima | R$ 8.763,95 | **R$ 404,98** |

## Como rodar (só o .NET 8 SDK)
```bash
dotnet run --project src/Desafio.Cli            # menu interativo
dotnet run --project src/Desafio.Api            # API; Swagger na raiz (porta mostrada no console)
dotnet test
```
Subcomandos da CLI:
```bash
desafio comissao [--detalhe] [--csv saida.csv]
desafio estoque listar | historico
desafio estoque entrada|saida 101 50 "Compra NF 123"
desafio juros 1000 24/09/2026 [--hoje 04/10/2026]
```
API: `GET /comissoes[?formato=csv]`, `GET /estoque`, `GET /estoque/{codigo}`, `GET|POST /estoque/movimentacoes`, `GET /juros?valor=&vencimento=&hoje=`.
Erros viram respostas padronizadas (404 produto inexistente, 422 saldo insuficiente, 400 entrada inválida).

## Estrutura
- `src/Desafio.Core`: regras de negócio puras. CLI e API são camadas finas sobre ele.
- `src/Desafio.Cli`, `src/Desafio.Api`, `tests/Desafio.Tests`, `data/` (JSON do enunciado).

## Decisões
- **`decimal` em todo valor monetário**, nunca `double`; arredondamento comercial (meio para cima).
- **Q1**: faixas numa tabela (fácil de alterar); R$ 100 exatos → 1%, R$ 500 exatos → 5%. A comissão é somada sem arredondar e arredondada só no total do vendedor. `--detalhe` mostra quantas vendas caíram em cada faixa.
- **Q2**: ID sequencial único por movimentação, descrição obrigatória, tipo, data e saldo final. Saída acima do saldo é recusada sem alterar nada. Estado e histórico persistem em `estado_estoque.json` (gravação atômica; caminho via `DESAFIO_ESTADO` ou `Estoque:Estado`). O depósito é thread-safe para uso pela API.
- **Q3**: o enunciado não diz o regime, então calcula **simples** (`valor × 2,5% × dias`) e **composto** (`valor × (1,025^dias − 1)`). Ex.: R$ 1.000, 10 dias → R$ 250,00 e R$ 280,08. Sem atraso, sem juros. Em atrasos absurdos o composto estoura o `decimal` e aparece como indisponível; o simples continua sendo calculado.
