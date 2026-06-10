# Arquitetura

## Produto

SportsMonitor monitora partidas de futebol ao vivo e compara dados entre fontes. Quando detecta divergência (placar, gol, autor do gol, cartão, status), toca um apito e mostra um card de alerta no dashboard. O analista então verifica manualmente e decide se age — o sistema **não** aposta.

Fonte oficial da competição é a referência preferencial quando disponível. A Copa do Mundo 2026 é prioridade alta.

## Fluxo do analista

1. Sistema monitora a partida ao vivo e compara as fontes.
2. Divergência detectada → apito + card mostrando exatamente onde divergiu e quais fontes concordam.
3. Analista abre as páginas das fontes, busca replay/vídeo, confirma se o evento foi real.
4. Analista decide manualmente se age na casa de apostas (ação 100% manual).

## Arquitetura técnica

```
[365Scores]  --HTTP-->  Worker  --\
[Google]     --API--->  Worker  ---> SnapshotStore --> DivergenceEngine --> Channel<Divergence>
[SofaScore]  --LocalAgent (PC do operador) --POST /api/relay/sofascore--/         |
                                                                          AlertWorker --> SignalR --> Dashboard Angular
```

- **Workers independentes por fonte:** cada fonte tem seu próprio polling. A detecção é reativa — dispara quando qualquer fonte atualiza (evento do SnapshotStore), não espera um ciclo completo. Minimiza latência.
- **DivergenceEngine + regras:** `ScoreMismatch`, `GoalScorerMismatch`, `MissingGoal`, `CardMismatch` (amarelo+vermelho), `MatchStatusMismatch`.
- **MatchResolver (`FuzzyMatchResolver`):** IDs internos de cada fonte são diferentes para a mesma partida. O matchId é `SHA256(timeCasa|timeFora|yyyyMMddHH)[..16]` — **sem** competição no hash (os nomes de competição divergem entre fontes e quebrariam o agrupamento). Há canonização de seleções nacionais (England/Inglaterra→ENG etc.).
- **Channel\<Divergence\>:** desacopla detecção do envio de alertas.
- **Histórico:** leituras persistidas localmente (JSONL/SQLite). 365Scores **não** persiste `RawJson` (payload gigante estourou o disco da VM).
- **Real-time:** SignalR empurra alertas pro dashboard.

## Padrões de design

- `PollingWorker<TOptions>` base — retry, logging, intervalo configurável herdados.
- `IOptionsMonitor<T>` para intervalos de polling com hot-reload via appsettings.
- Providers implementam `IMatchDataProvider`; regras implementam `IDivergenceRule`.

## Stack

ASP.NET Core (.NET 10) · Worker Services · SignalR · Angular · SQLite/JSONL · xUnit. O BFF serve o dashboard como estáticos e roda em `localhost:5000` (dev) / atrás do nginx na porta 80 (produção).

## Modo demo

`Demo.Enabled: true` gera partidas e divergências simuladas (placar atrasado, gol divergente, cartão divergente) sem precisar de APIs reais. Desligado em produção.

> Detalhe histórico de arquitetura aprovada na Fase 03 preservado em [history/PHASE_03_TECHNICAL_ARCHITECTURE.md](history/PHASE_03_TECHNICAL_ARCHITECTURE.md).
