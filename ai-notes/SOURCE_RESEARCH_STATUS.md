# Source Research Status

Last updated: 2026-05-26 (Phase 01 COMPLETE + Comparison Sources Research COMPLETE)

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
| SofaScore | Live score app | **Done** | B — API interna não oficial (api.sofascore.com/api/v1) | A | **Sim (comparação)** | GET /sport/football/events/live + /event/{id}/incidents; CloudFlare básico — User-Agent browser + 25-30s polling; sem auth; risco: frágil + ToS médio |
| Flashscore | Live score app | **Done** | G — Sem API viável | D | Não | Excluído — não faz parte do grupo primário; substituído por SofaScore |
| FotMob | Live score app | **Done** | G — Sem API viável | D | Não | Excluído — não faz parte do grupo primário |
| 365Scores | Live score app | **Done** | B — API interna não oficial (webws.365scores.com/web/) | A | **Sim (comparação)** | GET /game/?gameId={id} + /games/results/?competitions={id}; sem auth; parâmetros: appTypeId=5, langId=31, timezoneName=America/Sao_Paulo; risco: frágil + ToS médio |
| Google (live scores) | Search | **Done** | G — Sem endpoint JSON acessível sem serviço pago | D | Não (link manual) | Sem API pública; scraping via SerpApi ($50+/mo) inviável para MVP; solução: dashboard gera link de busca Google para verificação manual do analista |
| 365Scores | Live score app | Pendente | TBD | TBD | TBD | Pesquisar |
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
