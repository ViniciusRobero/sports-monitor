# API Reference — Sports Data Sources

Status por fonte, estrutura JSON real, campos mapeados e pendências.

---

## 1. SofaScore

**Status:** ✅ Implementado e validado contra estrutura real  
**Custo:** Grátis (API interna não-oficial)  
**Auth:** Nenhuma. Apenas `User-Agent` de browser no header.

### Endpoints usados

```
GET https://api.sofascore.com/api/v1/sport/football/events/live
GET https://api.sofascore.com/api/v1/event/{id}/incidents
```

### Estrutura — evento ao vivo

```json
{
  "events": [
    {
      "id": 99001,
      "homeTeam": { "name": "Flamengo", "slug": "flamengo", "nameCode": "FLA" },
      "awayTeam": { "name": "Palmeiras", "slug": "palmeiras", "nameCode": "PAL" },
      "tournament": { "name": "Brasileirao Serie A", "slug": "brasileirao-serie-a" },
      "startTimestamp": 1748995200,
      "homeScore": { "current": 1, "display": 1, "period1": 1, "period2": 0 },
      "awayScore": { "current": 0, "display": 0, "period1": 0, "period2": 0 },
      "status": {
        "code": 6,
        "description": "1st half",
        "type": "inprogress"
      },
      "time": {
        "currentPeriodStartTimestamp": 1748995200,
        "initial": 0,
        "max": 90,
        "extra": 0
      }
    }
  ]
}
```

### Estrutura — incidentes

```json
{
  "incidents": [
    {
      "id": 1001,
      "time": 32,
      "isHome": true,
      "incidentType": "goal",
      "incidentClass": "regular",
      "player": { "id": 501, "name": "Pedro", "slug": "pedro", "shortName": "Pedro" }
    },
    {
      "id": 1002,
      "time": 45,
      "isHome": false,
      "incidentType": "card",
      "incidentClass": "yellow",
      "reason": "Foul",
      "player": { "id": 502, "name": "Zé Rafael", "slug": "ze-rafael" }
    },
    {
      "id": 1003,
      "time": 55,
      "isHome": false,
      "incidentType": "goal",
      "incidentClass": "ownGoal",
      "player": { "id": 503, "name": "Murilo" }
    },
    {
      "id": 1004,
      "time": 70,
      "isHome": true,
      "incidentType": "substitution",
      "player": { "name": "Pedro" },
      "playerIn": { "name": "Gabigol" }
    }
  ]
}
```

### Mapeamento de status (`status.type` + `status.description`)

| type | description | → MatchStatus |
|---|---|---|
| `notstarted` | * | NotStarted |
| `inprogress` | "1st half" | Live |
| `inprogress` | "2nd half" | Live |
| `inprogress` | "Halftime" ou "HT" | HalfTime |
| `inprogress` | "Extra Time" | Live |
| `finished` | * | Finished |
| `postponed` | * | Postponed |
| `cancelled` | * | Cancelled |

### Mapeamento de incidentes

| incidentType | incidentClass | → EventType |
|---|---|---|
| `goal` | `regular` | Goal |
| `goal` | `ownGoal` | OwnGoal |
| `goal` | `penalty` | Penalty |
| `card` | `yellow` | YellowCard |
| `card` | `red` ou `yellowRed` | RedCard |
| `substitution` | * | Substitution |

---

## 2. 365Scores

**Status:** ✅ Implementado e validado contra estrutura real  
**Custo:** Grátis (API interna não-oficial)  
**Auth:** Nenhuma. `User-Agent` de browser + `Accept` header.  
**Limitação:** Sem incidentes individuais — apenas placar e status.

### Endpoint usado

```
GET https://webws.365scores.com/web/games/?appTypeId=5&langId=31&timezoneName=America%2FSao_Paulo&userCountryId=-1&onlyLive=true
```

### Estrutura — resposta completa

```json
{
  "liveGamesCount": 2,
  "games": [
    {
      "id": 4632001,
      "sportId": 1,
      "competitionId": 113,
      "statusGroup": 1,
      "gameTime": 34.0,
      "startTime": "2026-05-27T21:00:00-03:00",
      "homeCompetitor": {
        "id": 1,
        "name": "Flamengo",
        "score": 1.0,
        "isHome": true
      },
      "awayCompetitor": {
        "id": 2,
        "name": "Palmeiras",
        "score": 0.0,
        "isHome": false
      }
    }
  ]
}
```

### Filtros aplicados no provider

| Campo | Valor esperado | Significado |
|---|---|---|
| `sportId` | `1` | Futebol |
| `statusGroup` | `1` | Ao vivo |
| `statusGroup` | `4` | Encerrado (ignorado) |

### Campos importantes

- `homeCompetitor.score` / `awayCompetitor.score` → placar (double, cast para int)
- `gameTime` → minuto atual (não usado — apenas para referência)
- `competitionId` → ID numérico da competição (sem nome)
- Sem `events`, sem incidentes — fonte exclusivamente de placar

---

## 3. API-Football

**Status:** ✅ Implementado e validado contra documentação oficial  
**Custo:** $19/mês (plano Pro — necessário para dados ao vivo)  
**Auth:** Header `x-apisports-key: SUA_CHAVE`  
**Documentação oficial:** `https://www.api-football.com/documentation-v3`

### Endpoint usado

```
GET https://v3.football.api-sports.io/fixtures?live=all
```

### Estrutura — fixture ao vivo

```json
{
  "response": [
    {
      "fixture": {
        "id": 123456,
        "date": "2026-06-15T18:00:00+00:00",
        "status": {
          "long": "First Half",
          "short": "1H",
          "elapsed": 32
        }
      },
      "league": {
        "id": 71,
        "name": "Serie A",
        "country": "Brazil",
        "season": 2026,
        "round": "Regular Season - 10"
      },
      "teams": {
        "home": { "id": 127, "name": "Flamengo", "logo": "..." },
        "away": { "id": 121, "name": "Palmeiras", "logo": "..." }
      },
      "goals": { "home": 1, "away": 0 },
      "score": {
        "halftime": { "home": 1, "away": 0 },
        "fulltime": { "home": null, "away": null }
      },
      "events": [
        {
          "time": { "elapsed": 32, "extra": null },
          "team": { "id": 127, "name": "Flamengo" },
          "player": { "id": 8834, "name": "Pedro" },
          "assist": { "id": 8836, "name": "Arrascaeta" },
          "type": "Goal",
          "detail": "Normal Goal",
          "comments": null
        },
        {
          "time": { "elapsed": 45 },
          "team": { "id": 121, "name": "Palmeiras" },
          "player": { "id": 9012, "name": "Zé Rafael" },
          "type": "Card",
          "detail": "Yellow Card"
        },
        {
          "time": { "elapsed": 67 },
          "team": { "id": 121, "name": "Palmeiras" },
          "player": { "id": 9015, "name": "Murilo" },
          "type": "Card",
          "detail": "Red Card"
        }
      ]
    }
  ]
}
```

### Mapeamento de status (`fixture.status.short`)

| short | → MatchStatus |
|---|---|
| `NS` | NotStarted |
| `1H`, `2H`, `ET`, `P` | Live |
| `HT` | HalfTime |
| `FT`, `AET`, `PEN` | Finished |
| `PST` | Postponed |
| `CANC` | Cancelled |

### Mapeamento de eventos

| type | detail | → EventType |
|---|---|---|
| `Goal` | * | Goal |
| `Card` | `Yellow Card` | YellowCard |
| `Card` | `Red Card` | RedCard |
| `subst` | * | Substitution |

### Como ativar

```json
"ApiFootball": {
  "Enabled": true,
  "PollingIntervalSeconds": 30,
  "ApiKey": "SUA_CHAVE",
  "BaseUrl": "https://v3.football.api-sports.io"
}
```

---

## 4. BetsAPI (Bet365)

**Status:** ⚠️ Implementado — estrutura dos nós EV/ST confirmada, **formato do campo LA pendente de validação com token real**  
**Custo:** Pago — verificar plano InPlay em `betsapi.com/mm/pricing_table`  
**Auth:** Query param `?token=SEU_TOKEN`  
**Documentação:** `https://betsapi.com/docs/bet365/`

### Endpoints usados

```
# 1. Lista de eventos ao vivo (futebol = sport_id=1)
GET https://api.b365api.com/v1/bet365/inplay_filter?sport_id=1&token=TOKEN

# 2. Detalhes do evento (estatísticas + timeline)
GET https://api.b365api.com/v1/bet365/event?FI={event_id}&stats=1&token=TOKEN
```

### Estrutura — inplay_filter (passo 1)

```json
{
  "success": 1,
  "results": [
    { "id": "12345", "league": { "name": "Brasileirao Serie A" } }
  ]
}
```

### Estrutura — event (passo 2) — **parcialmente confirmada**

```json
{
  "success": 1,
  "results": [
    {
      "type": "EV",
      "NA": "Flamengo v Palmeiras",
      "CT": "Brasileirao Serie A",
      "SS": "1-0",
      "LM": "32"
    },
    {
      "type": "ST",
      "ET": "IGoal",
      "TM": "32",
      "TI": "1",
      "LA": "Pedro - Goal 32'"
    },
    {
      "type": "ST",
      "ET": "IYellowCard",
      "TM": "45",
      "TI": "2",
      "LA": "Zé Rafael - Yellow Card 45'"
    }
  ]
}
```

### Campos confirmados pela documentação

| Campo | Descrição |
|---|---|
| `type: "EV"` | Nó principal do evento (partida) |
| `type: "ST"` | Evento de timeline (gol, cartão, etc.) |
| `NA` | Nome do evento: "Home v Away" |
| `CT` | Nome da competição |
| `SS` | Placar atual: "1-0" |
| `LM` | Minuto atual |
| `ET` | Tipo do evento de timeline |
| `TM` | Minuto do evento |
| `TI` | Time: "1" = home, "2" = away |
| `LA` | Label do evento (contém nome do jogador) |

### ⚠️ Campos pendentes de validação com token real

| Campo | Valor no mock | Incerteza |
|---|---|---|
| `ET` para gol | `"IGoal"` | Pode ser `"Goal"`, `"GOAL"` ou outro valor |
| `ET` para cartão amarelo | `"IYellowCard"` | Pode ser diferente |
| `ET` para cartão vermelho | `"IRedCard"` | Pode ser diferente |
| `LA` format | `"Pedro - Goal 32'"` | Pode ser só `"Pedro"` ou formato diferente |

**Ação necessária:** após obter token, fazer uma chamada com partida ao vivo e me enviar o JSON bruto de um evento `type: "ST"` com gol para confirmar `ET` e `LA`. O parser `ExtractPlayerName` será atualizado com base no formato real.

### Como ativar

```json
"BetsApi": {
  "Enabled": true,
  "PollingIntervalSeconds": 30,
  "Token": "SEU_TOKEN",
  "BaseUrl": "https://api.b365api.com"
}
```

---

## 5. Google Custom Search

**Status:** ✅ Implementado e configurado  
**Custo:** Grátis até 100 buscas/dia; $5 por 1.000 acima disso  
**Auth:** Query param `?key=API_KEY&cx=SEARCH_ENGINE_ID`

### Endpoint usado

```
GET https://www.googleapis.com/customsearch/v1?key=KEY&cx=CX&q=QUERY&num=3
```

### Query gerada por partida

```
"{HomeTeam} {AwayTeam} ao vivo resultado"
```

Exemplo: `"Flamengo Palmeiras ao vivo resultado"`

### Estrutura da resposta

```json
{
  "kind": "customsearch#search",
  "items": [
    {
      "title": "Flamengo 2 x 1 Palmeiras - Brasileirão 2026",
      "snippet": "Pedro marcou aos 32' do 1º tempo. Acompanhe ao vivo...",
      "link": "https://ge.globo.com/futebol/brasileirao/..."
    },
    {
      "title": "Gols e melhores momentos: Flamengo x Palmeiras",
      "snippet": "Assista aos gols da partida pelo Brasileirão Serie A...",
      "link": "https://espn.com.br/..."
    }
  ]
}
```

### Search Engine configurado

Sites indexados no mecanismo (`cx: 25c69f98aa10d4ba0`):
- `sofascore.com/*`
- `ge.globo.com/*`
- `espn.com.br/*`
- `ogol.com.br/*`
- `flashscore.com/*`
- `goal.com/*`
- `bbc.com/sport/*`

### Como ativar

```json
"Google": {
  "Enabled": true,
  "PollingIntervalSeconds": 120,
  "ApiKey": "SUA_API_KEY",
  "SearchEngineId": "SEU_CX",
  "ResultsPerMatch": 3
}
```

---

## Resumo geral

| Fonte | Auth necessária | Tem gols/nomes | Tem cartões | Tem placar | Status |
|---|---|---|---|---|---|
| SofaScore | Não | ✅ | ✅ | ✅ | ✅ Pronto |
| 365Scores | Não | ❌ | ❌ | ✅ | ✅ Pronto |
| API-Football | API Key ($19/mês) | ✅ | ✅ | ✅ | ✅ Pronto (falta key) |
| BetsAPI | Token (pago) | ✅⚠️ | ✅⚠️ | ✅ | ⚠️ Pronto (falta token + validar LA) |
| Google | API Key (grátis) | Snippets | Snippets | Snippets | ✅ Pronto |
