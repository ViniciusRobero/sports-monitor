# Source Research Status

Last updated: 2026-06-07 (real-provider config + Linux/GCP deploy prep)

| Source | Category | Status | Best Access Method | Local Compatible | MVP Candidate | Notes |
|---|---|---|---|---|---|---|
| API-Football | Sports data API | **Done** | A — Official REST API | A | **Sim** | $19/mo Pro; live events 15s; 1200+ competitions; Brasileirão/Libertadores ok |
| Sportmonks | Sports data API | **Done** | A — Official REST API | A | **Sim** | €129/mo Worldwide needed for Brazil; 14-day free trial; no WebSocket |
| football-data.org | Sports data API | **Done** | A — Official REST API | A | Secundário | Free tier inclui Brasileirão; sem Libertadores; sem odds |
| Sportradar | Sports data API + odds | **Done** | A — Enterprise only | C | Não | $10k+/mo; enterprise; inviável para MVP local |
| SportsDataIO | Sports data API | Pendente | A — Commercial | B | Pesquisar mais | Trial disponível; foco em EUA; soccer coverage verificar |
| The Odds API | Odds API | **Done** | B — Commercial REST | A | **Sim** | $29/mo Pro; $99/mo Business para live+Pinnacle; Brasileirão Series A ok |
| OpticOdds | Odds API | **Done** | B — Enterprise | C | Não | Enterprise; sem preço público; 200+ bookmakers; inviável para pequeno sistema |
| BetsAPI / b365api | Odds API + events | **Done** | B — Commercial REST | A | **Sim** | Bet365 live odds + suspensão de mercado 3-5s; pricing via login |
| SofaScore | Live score app | **Implemented via Microsoft.Playwright (headless Chrome)** | F — Browser automation (headless Chromium via Playwright) | A | **Sim (comparação)** | Enabled in appsettings; no token; requests run inside real Chromium via page.EvaluateAsync fetch() — genuine Chrome TLS fingerprint (JA3/JA4); HTTP 403 was caused by TLS fingerprinting of .NET HttpClient, not IP block; `playwright install chromium` required on first run; `--no-sandbox --disable-dev-shm-usage` flags required on Linux/GCP |
| Flashscore | Live score app | **Done** | G — Sem API viável | D | Não | Excluído — não faz parte do grupo primário; substituído por SofaScore |
| FotMob | Live score app | **Done** | G — Sem API viável | D | Não | Excluído — não faz parte do grupo primário |
| 365Scores | Live score app | **Implemented / endpoint reachable** | B — API interna não oficial (webws.365scores.com/web/) | A | **Sim (comparação)** | Enabled in appsettings; no token; 20s polling; local curl test returned HTTP 200 on 2026-06-07; parâmetros: appTypeId=5, langId=31, timezoneName=America/Sao_Paulo; risco: frágil + ToS médio |
| Google (verification snippets) | Search | **Implemented / credentials pending** | B — Custom Search JSON API / mock snippets in demo | A | **Sim (verificação)** | Enabled with 300s polling and placeholders. Não é fonte estruturada de placar ao vivo; usado para snippets e links de verificação no dashboard. Requires real API key + Search Engine ID |
| OneFootball | Live score app | Pendente | TBD | TBD | TBD | Pesquisar |
| Betfair Exchange | Bookmaker (exchange) | **Done** | A — Official REST + WebSocket | A | **Sim** | API gratuita; live streaming; suspension status; distinção: exchange, não bookmaker tradicional |
| Pinnacle | Bookmaker | **Done** | G — API fechada | — | Não (direto) | API fechada jul/2025; Brasil bloqueado; acessar via The Odds API Business |
| Bet365 | Bookmaker | **Done** | G — Sem API pública | — | Via BetsAPI | Sem API; via BetsAPI para live odds + suspensão |
| Betano | Bookmaker | **Done** | G — Sem API pública | — | Via BetsAPI | Sem API; licenciada no Brasil pós-2024 |
| Sportingbet | Bookmaker | **Done** | G — Sem API pública | — | Via BetsAPI | Sem API; grupo Entain |
| KTO | Bookmaker | Assumido | G — Sem API pública | — | Via BetsAPI | Sem API pública; grupo Rush Street; via BetsAPI |
| Superbet | Bookmaker | Assumido | G — Sem API pública | — | Via BetsAPI | Sem API pública; via BetsAPI |
| Stake | Bookmaker | Assumido | G — Sem API pública | — | Via BetsAPI | Sem API pública; via BetsAPI |
| Betway | Bookmaker | Assumido | G — Sem API pública | — | Via BetsAPI | Sem API pública; BetsAPI tem Betway API dedicada |
| Betsson | Bookmaker | Assumido | G — Sem API pública | — | Via BetsAPI | Sem API pública; via BetsAPI |
| **OpenLigaDB** | Community API (bonus) | **Done** | D — Community free API | A | **Sim (Bundesliga)** | Grátis, sem auth, 1000 req/h, cobre Bundesliga/2.Bundesliga/DFB-Pokal |
| **API Futebol** | Brazilian sports data API (bonus) | **Done** | B — Commercial API | A | Avaliar | api-futebol.com.br; foco em futebol brasileiro; 100 req/dia paid plan |
| **TheSportsDB** | Community metadata (bonus) | Done | B — Community API | A | Não (metadata only) | Grátis; 634 ligas; melhor para logos/artwork; não para live events |
| Official competition websites (63) | Official sources | **Done** | G para quase todos; D para Bundesliga (OpenLigaDB) | G/A | Via APIs comerciais | Detalhes em PHASE_01_OFFICIAL_SITES_RESEARCH.md |

## SofaScore External API Check - 2026-06-07

User shared official external docs URL:

`https://api.sofascore.com/api/docs/external#tag/Betting-Odds/operation/get_sofascore_app_external_api_v1_bettingodds_list`

Result from local CLI tests:

- `https://api.sofascore.com/api/docs/external` returned HTTP 403.
- `https://api.sofascore.com/api/docs/external/openapi.json` returned HTTP 403.
- `https://api.sofascore.com/api/docs/external/swagger.json` returned HTTP 403.
- Probable no-auth endpoint `https://api.sofascore.com/api/v1/betting-odds/list` returned HTTP 403.

Interpretation:

- The referenced operation is under `Betting Odds`, not directly under live score/incidents.
- The external docs/API appear access-controlled, partner-gated, or blocked from the current environment.
- Do not assume the auth header/token format until the Swagger/OpenAPI security scheme or a generated cURL command is available.
- Current app provider still uses internal live endpoints (`/api/v1/sport/football/events/live` and `/api/v1/event/{id}/incidents`), which also returned HTTP 403 locally.

## Method Legend

- A = Official documented API
- B = Commercial third-party API
- C = Widget/embed only
- D = Public but undocumented endpoint
- E = HTML scraping
- F = Browser automation
- G = Not viable / no acceptable method

## Local Compatibility Legend
- A = Good for local execution
- B = Works locally with limitations
- C = Better suited for server/cloud
- D = Not recommended for local
- E = Not viable
