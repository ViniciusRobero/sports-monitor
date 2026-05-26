# Phase 01 — Comparison Sources Research
## SofaScore, 365Scores, Google Live Scores

> Research date: 2026-05-26
> Scope: Informal/unofficial API endpoints for the primary comparison sources desired by Josias.
> Context: These sources have no official public APIs but are accessible via internal HTTP endpoints observable via browser DevTools.

---

## Summary

| Source | Viable | Access Method | Auth Required | Anti-bot | Risk |
|---|---|---|---|---|---|
| SofaScore | **Sim** | Internal REST API (api.sofascore.com/api/v1) | Não | CloudFlare básico | Médio — frágil + ToS |
| 365Scores | **Sim** | Internal REST API (webws.365scores.com/web/) | Não | Básico | Médio — frágil + ToS |
| Google live scores | **Não (automatizado)** | Sem endpoint JSON acessível sem serviço pago | N/A | Google bot detection | Alto — alternativa: link manual |

---

## 1. SofaScore

### Status
**Viável como fonte de comparação** via endpoints internos não oficiais.

### Base URL
```
https://api.sofascore.com/api/v1
```

### Endpoints confirmados

| Endpoint | Método | URL | Retorna |
|---|---|---|---|
| Partidas ao vivo | GET | `/sport/football/events/live` | Array de eventos ao vivo com score, status, times |
| Partidas por data | GET | `/sport/football/scheduled-events/{YYYY-MM-DD}` | Partidas do dia com times, scores, status |
| Detalhes do evento | GET | `/event/{eventId}` | Score completo, venue, árbitro, status ao vivo |
| Incidentes | GET | `/event/{eventId}/incidents` | Gols, cartões, substituições com jogador e minuto |
| Standings | GET | `/unique-tournament/{id}/season/{seasonId}/standings/total` | Tabela completa |
| Torneios | GET | `/config/unique-tournaments/{lang}/{sport}` | Lista de torneios |
| Categorias | GET | `/sport/{sport}/categories` | Categorias por região |
| Temporadas | GET | `/unique-tournament/{id}/seasons` | Temporadas disponíveis |
| Rodada | GET | `/unique-tournament/{id}/season/{seasonId}/events/round/{round}` | Partidas da rodada |

### Endpoint mais importante para o sistema
```
GET https://api.sofascore.com/api/v1/sport/football/events/live
```
Retorna todas as partidas de futebol em andamento. Polling a cada 25-30s.

```
GET https://api.sofascore.com/api/v1/event/{eventId}/incidents
```
Retorna todos os incidentes (gols, cartões, substituições) com:
- Tipo do incidente
- Minuto
- Nome do jogador
- Time (home/away)

### Autenticação
Não necessária — dados públicos acessíveis via GET simples.

### Proteção anti-bot
CloudFlare básico presente. Não requer bypass ativo — apenas:
- User-Agent de browser (ex: Mozilla/5.0...)
- Intervalo mínimo de 25-30s entre chamadas
- Sem técnicas de fingerprint spoofing necessárias

### Formato de resposta
JSON. Exemplo de incidente:
```json
{
  "incidents": [
    {
      "player": { "name": "Pedro", "id": 12345 },
      "isHome": true,
      "incidentType": "goal",
      "time": 32,
      "id": 67890
    }
  ]
}
```

### Integração .NET
```csharp
// HttpClient simples com User-Agent
client.DefaultRequestHeaders.UserAgent.ParseAdd(
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
);
var response = await client.GetAsync(
    "https://api.sofascore.com/api/v1/event/12345/incidents"
);
```

### Risco e limitações
- **ToS**: acesso não autorizado — risco médio
- **Fragilidade**: endpoints podem mudar sem aviso
- **Rate limit**: sem documentação oficial; 25-30s recomendado pela comunidade
- **Estabilidade**: comunidade usa ativamente há anos — relativamente estável

### Referências
- [sofascore-api (apdmatos)](https://github.com/apdmatos/sofascore-api) — documentação de endpoints
- [sofascore-php-sdk (devsmith88)](https://github.com/devsmith88/sofascore-php-sdk) — SDK PHP
- [LanusStats (federicorabanos)](https://github.com/federicorabanos/LanusStats) — implementação Python
- [Betfair forum thread](https://forum.betangel.com/viewtopic.php?t=30462) — discussão comunitária

---

## 2. 365Scores

### Status
**Viável como fonte de comparação** via endpoints internos não oficiais.

### Base URL
```
https://webws.365scores.com/web/
```

### Endpoints confirmados

| Endpoint | URL | Parâmetros chave | Retorna |
|---|---|---|---|
| Resultados por competição | `/games/results/` | `competitions={id}`, `langId`, `timezoneName`, `appTypeId=5` | Array de jogos com times e placares |
| Detalhes do jogo | `/game/` | `gameId={id}`, `matchupId={id}`, `topBookmaker=14` | Dados completos do jogo ao vivo |
| Estatísticas do jogo | `/game/stats/` | `games={id}` | Estatísticas detalhadas |
| Estatísticas da liga | `/stats/` | `competitions={id}`, `withSeasons=true` | Stats da liga/temporada |

### Parâmetros padrão (Brasil)
```
appTypeId=5
langId=31           (Português)
timezoneName=America/Sao_Paulo
userCountryId=-1    (neutro) OU Brazil specific ID
```

### Endpoints completos (exemplos)
```
Resultados Brasileirão:
GET https://webws.365scores.com/web/games/results/?appTypeId=5&langId=31&timezoneName=America/Sao_Paulo&userCountryId=-1&competitions=113

Detalhes do jogo:
GET https://webws.365scores.com/web/game/?appTypeId=5&langId=31&timezoneName=America/Sao_Paulo&userCountryId=-1&gameId={ID}&matchupId={ID}&topBookmaker=14

Estatísticas:
GET https://webws.365scores.com/web/game/stats/?appTypeId=5&langId=31&timezoneName=America/Sao_Paulo&userCountryId=-1&games={ID}
```

### Endpoint de partidas ao vivo
Não confirmado por pesquisa direta, mas padrão esperado (verificar via DevTools):
```
GET https://webws.365scores.com/web/games/?appTypeId=5&langId=31&timezoneName=America/Sao_Paulo&userCountryId=-1&onlyLive=true
```
OU filtro por status `statusGroup=1` (em andamento).

### Como obter o gameId
O ID do jogo aparece na URL da partida no 365Scores:
```
https://www.365scores.com/football/match/flamengo-vs-palmeiras-XXXXXX
```
O número no final é o matchup/game ID.

### Autenticação
Não necessária — todos os endpoints observados usam apenas query params, sem cookies ou tokens de autorização.

### Proteção anti-bot
Básica — requisições GET normais funcionam. Sem CloudFlare avançado detectado.

### Integração .NET
```csharp
var url = $"https://webws.365scores.com/web/game/?appTypeId=5&langId=31" +
          $"&timezoneName=America/Sao_Paulo&userCountryId=-1" +
          $"&gameId={gameId}&matchupId={matchupId}&topBookmaker=14";
var response = await client.GetAsync(url);
```

### Risco e limitações
- **ToS**: acesso não autorizado — risco médio
- **Fragilidade**: parâmetros e estrutura podem mudar; gameId precisa ser descoberto
- **Partidas ao vivo**: endpoint exato requer confirmação via DevTools manual
- **ID mapping**: IDs internos diferentes dos demais provedores — necessário normalização

### Referências
- [python-365scores (irwimscott)](https://github.com/irwimscott/python-365scores)
- [LanusStats threesixfivescores.py](https://github.com/federicorabanos/LanusStats/blob/main/LanusStats/threesixfivescores.py)
- [365scores GitHub org](https://github.com/365scores)

---

## 3. Google Live Scores

### Status
**Não viável como fonte automatizada.** Alternativa: link manual no dashboard.

### Por que não é viável

Google não expõe um endpoint JSON público para resultados esportivos ao vivo. Internamente, usa o Knowledge Graph com parcerias de dados (Sports Data providers como Stats Perform), mas isso não é acessível sem:
- Serviços pagos de scraping (SerpApi $50+/mo, ScrapingBee) — adicionam custo e dependência
- Scraping direto do HTML — extremamente frágil, contra ToS, sujeito a bloqueio de bot

### Alternativa recomendada: link manual no dashboard

Em vez de integrar o Google como fonte de dados, o dashboard deve oferecer:

```
[Verificar no Google]
```

Um link gerado dinamicamente que abre uma busca relevante no Google:
```
https://www.google.com/search?q=Flamengo+Palmeiras+placar+ao+vivo
```

Isso permite ao analista verificar o Google manualmente em 1 clique, sem nenhuma integração técnica.

### Quando o analista vê Google diferente de outras fontes

Se o analista perceber divergência com o que o Google mostra, ele pode marcar manualmente no campo "Google value" da tabela de verificação manual do dashboard.

### Nota sobre SerpApi (opcional futuro)
Se no futuro houver necessidade de automatizar a consulta ao Google, a API mais viável é SerpApi ($50+/mo para sports results). Esta integração pode ser adicionada em versão futura como fonte de verificação secundária.

---

## 4. Requisito de histórico de dados (confirmado 2026-05-26)

O usuário confirmou que quer o histórico salvo — seja em arquivo de texto ou banco de dados pequeno.

### O que isso significa na arquitetura

Cada coleta de dados deve ser persistida localmente, permitindo:
- Reconstruir o que cada fonte dizia em cada momento
- Auditar divergências passadas
- Debugar comportamento do sistema
- Análise de padrões futura

### Schema SQLite proposto (MVP)

```sql
-- Partidas monitoradas
CREATE TABLE matches (
    id INTEGER PRIMARY KEY,
    external_id TEXT,        -- ID da fonte primária
    home_team TEXT,
    away_team TEXT,
    competition TEXT,
    match_date TEXT,
    status TEXT,
    created_at TEXT
);

-- Leituras brutas por fonte (histórico completo)
CREATE TABLE source_readings (
    id INTEGER PRIMARY KEY,
    match_id INTEGER REFERENCES matches(id),
    source TEXT,             -- 'sofascore', '365scores', 'api_football', etc.
    collected_at TEXT,       -- timestamp ISO 8601
    payload TEXT,            -- JSON bruto da resposta
    home_score INTEGER,
    away_score INTEGER,
    match_status TEXT,
    FOREIGN KEY (match_id) REFERENCES matches(id)
);

-- Eventos detectados por fonte (gols, cartões, etc.)
CREATE TABLE source_events (
    id INTEGER PRIMARY KEY,
    reading_id INTEGER REFERENCES source_readings(id),
    match_id INTEGER REFERENCES matches(id),
    source TEXT,
    event_type TEXT,         -- 'goal', 'yellow_card', 'red_card', 'substitution'
    minute INTEGER,
    player_name TEXT,
    team TEXT,               -- 'home' | 'away'
    collected_at TEXT
);

-- Divergências detectadas
CREATE TABLE divergences (
    id INTEGER PRIMARY KEY,
    match_id INTEGER REFERENCES matches(id),
    detected_at TEXT,
    divergence_type TEXT,    -- 'GoalScorerMismatch', 'ScoreMismatch', etc.
    severity TEXT,           -- 'Low', 'Medium', 'High', 'Critical'
    source_a TEXT,
    source_a_value TEXT,
    source_b TEXT,
    source_b_value TEXT,
    official_source_value TEXT,
    replay_link TEXT,
    verification_status TEXT,-- 'Pending', 'Confirmed', 'FalsePositive', 'Ignored'
    analyst_notes TEXT,
    manual_action_status TEXT,
    resolved_at TEXT
);
```

### Alternativa: arquivo JSON por partida (mais simples para MVP)

Se SQLite for complexo demais para o MVP inicial, uma alternativa simples:
```
/data/
  matches/
    2026-05-26/
      flamengo_palmeiras_12345/
        api_football.jsonl    ← uma linha por polling
        sofascore.jsonl
        365scores.jsonl
        divergences.jsonl
```

Cada arquivo `.jsonl` (JSON Lines) tem uma linha por leitura, com timestamp.

---

## 5. Estratégia de acesso — .NET HttpClient

Para acessar SofaScore e 365Scores via .NET sem trigger anti-bot básico:

```csharp
var handler = new HttpClientHandler();
var client = new HttpClient(handler);

// User-Agent de browser real
client.DefaultRequestHeaders.UserAgent.ParseAdd(
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
    "AppleWebKit/537.36 (KHTML, like Gecko) " +
    "Chrome/120.0.0.0 Safari/537.36"
);

// Accept header padrão
client.DefaultRequestHeaders.Accept.ParseAdd("application/json, text/plain, */*");
client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("pt-BR,pt;q=0.9,en;q=0.8");

// Referer (imita navegação interna do site)
client.DefaultRequestHeaders.Referrer = new Uri("https://www.sofascore.com/");
```

### Intervalos de polling recomendados

| Fonte | Intervalo mínimo | Motivo |
|---|---|---|
| SofaScore | 25-30s | CloudFlare; comunidade recomenda |
| 365Scores | 15-20s | Sem CloudFlare; mais tolerante |
| API-Football | 15s (Pro plan) | Documentado na API oficial |
| Betfair | Streaming (WebSocket) | Push, não polling |

---

## 6. Normalização de IDs (problema crítico)

Cada fonte usa IDs internos diferentes para a mesma partida e os mesmos jogadores.

| Exemplo | SofaScore ID | 365Scores ID | API-Football ID |
|---|---|---|---|
| Flamengo | ssScore:ABC | 365:XYZ | af:123 |
| Pedro (jogador) | ss:11111 | 365:22222 | af:33333 |
| Brasileirão A | ss:tourney:111 | 365:comp:113 | af:league:71 |

O sistema precisará de um **Match Resolver** que correlaciona partidas entre fontes usando:
1. Nome dos times (normalizado)
2. Horário de início (com tolerância ±5min)
3. Competição (mapeada por tabela de equivalência)

---

## Conclusão

| Fonte | Decisão | Método | Prioridade implementação |
|---|---|---|---|
| SofaScore | **Integrar via API interna** | GET api.sofascore.com/api/v1 | Alta |
| 365Scores | **Integrar via API interna** | GET webws.365scores.com/web/ | Alta |
| Google | **Não integrar — link manual** | Dashboard gera link de busca | Baixa (apenas UI) |

A tensão de fontes identificada anteriormente está **resolvida**:
- SofaScore e 365Scores têm endpoints internos bem documentados pela comunidade
- Não requerem autenticação para dados públicos
- Podem ser acessados com HttpClient + User-Agent de browser
- Google não é viável como fonte de dados; será um link de verificação manual

O requisito de histórico é **arquiteturalmente simples**: SQLite já planejado, apenas requer schema adequado com tabela `source_readings` (uma linha por polling por fonte).
