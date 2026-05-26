# Phase 01 - Research Results

**Research date:** 2026-05-26
**Status:** In progress — Sports Data APIs, Odds APIs, Live Score Apps, Bookmakers researched. Official competition websites pending.

---

## Summary Table

| Source | Category | Method | Local Compat | Live Data | Odds | Brazil | Cost/mo | MVP |
|---|---|---|---|---|---|---|---|---|
| API-Football | Sports data API | A | A | ✅ 15s | Pre-match | ✅ | $19-$99 | **Sim** |
| Sportmonks | Sports data API | A | A | ✅ real-time | Add-on | ✅ | €129-219 | **Sim** |
| football-data.org | Sports data API | A | A | ✅ paid | Não | Parcial | €12-49 | Secundário |
| Sportradar | Sports data API | A | C | ✅ | ✅ | ✅ | $10k+ | Não |
| SportsDataIO | Sports data API | A | B | ✅ | ✅ | Parcial | Sales | Pesquisar mais |
| The Odds API | Odds API | B | A | ✅ Business | ✅ $99/mo | ✅ | $29-$99 | **Sim** |
| OpticOdds | Odds API | B | C | ✅ | ✅ | Verificar | Enterprise | Não |
| BetsAPI | Odds API + events | B | A | ✅ 3-5s | ✅ Bet365 | ✅ | Variável | **Sim** |
| SofaScore | Live score app | G | D | — | — | ✅ | — | Não |
| Flashscore | Live score app | G | D | — | — | ✅ | — | Não |
| FotMob | Live score app | G | D | — | — | ✅ | — | Não |
| Betfair Exchange | Bookmaker (exchange) | A | A | ✅ WebSocket | ✅ live | Restrito | Grátis | **Sim** |
| Pinnacle | Bookmaker | G | — | — | — | Bloqueado | — | Não (direto) |
| Bet365 | Bookmaker | G | — | — | — | ✅ | — | Via BetsAPI |
| Betano | Bookmaker | G | — | — | — | ✅ | — | Via BetsAPI |

**Método:** A=API oficial, B=Terceiro comercial, C=Widget, D=Endpoint não documentado, E=Scraping, F=Automação browser, G=Inviável/sem API
**Local Compat:** A=Ótimo, B=Funciona com limitações, C=Melhor em server/cloud, D=Não recomendado local, E=Inviável

---

## MVP Candidates (Ranked)

| Rank | Source | Role no MVP | Custo estimado | Observação |
|---|---|---|---|---|
| 1 | API-Football | Dados esportivos primários | $19-39/mo | Melhor custo-benefício; live events; cobertura global |
| 2 | BetsAPI | Odds ao vivo + Bet365 + suspensão de mercado | Variável | Única fonte com suspensão de mercado de Bet365 |
| 3 | The Odds API | Odds pré-jogo + múltiplos bookmakers | $29/mo (Pro) | Boa cobertura, documentação excelente |
| 4 | Betfair Exchange API | Odds de exchange ao vivo + status de mercado | Grátis | Única API gratuita de bookmaker com live streaming |
| 5 | Sportmonks | Alternativa/backup para dados esportivos | €129/mo | Melhor cobertura Brasil; mais caro |
| 6 | football-data.org | Fonte complementar para competições principais | €12-29/mo | Custo baixo; cobertura limitada |

**Custo mínimo MVP estimado:** $19 (API-Football Pro) + variável BetsAPI ≈ $50-100/mo

---

## Sources to Avoid Initially

| Source | Motivo | Revisitar? |
|---|---|---|
| Sportradar | Enterprise $10k+/mo — inviável para sistema pequeno | Sim, se escalar |
| OpticOdds | Enterprise sem preço público — inviável para sistema pequeno | Sim, se escalar |
| SofaScore | Sem API oficial; scrapers violam ToS | Não (MVP) |
| Flashscore | Sem API oficial; apenas scrapers | Não (MVP) |
| FotMob | Sem API oficial; apenas wrappers não oficiais | Não (MVP) |
| Pinnacle (direto) | API fechada para público desde jul/2025; Brasil bloqueado | Via The Odds API Business |
| Bet365 (direto) | Sem API pública; ToS proíbe automação | Via BetsAPI |
| Betano (direto) | Sem API pública; ToS proíbe automação | Via BetsAPI |
| Sportingbet (direto) | Sem API pública; ToS proíbe automação | Via BetsAPI |

---

## Detailed Source Profiles

---

### API-Football

#### Basic Information

| Field | Value |
|---|---|
| Source name | API-Football |
| Category | Sports data API |
| Official website | https://www.api-football.com |
| Documentation URL | https://www.api-football.com/documentation-v3 |
| Also available via | https://rapidapi.com/api-sports/api/api-football |
| Source priority | High |
| Research status | Done |

#### Access Summary

| Item | Result | Notes |
|---|---|---|
| Official API exists | Yes | REST JSON |
| API type | REST | HTTP polling |
| Public access | Yes | API key required |
| Paid access | Yes | Free tier + paid plans |
| Partner-only access | No | Self-serve via website or RapidAPI |
| Requires API key | Yes | Register at api-football.com |
| Requires login | No | API key only |
| Requires browser session | No | |
| Requires fixed IP/IP whitelist | No | Residential IP ok |
| Local machine compatible | Yes | |

#### Plans & Pricing

| Plan | Price | Requests/day | Live Data | Odds |
|---|---|---|---|---|
| Free | $0 | 100 req/day | No | No |
| Pro | $19/mo | 7,500 req/day | Yes (15s) | No |
| Ultra | $39/mo | 75,000 req/day | Yes (15s) | Yes (pre-match) |
| Mega | $99/mo | 150,000 req/day | Yes (15s) | Yes + player stats |
| Custom | Variable | Up to 1.5M req/day | Yes | Yes |

Note: Free plan limited to 10 leagues, no live data.

#### Available Data

| Data Type | Available | Notes |
|---|---|---|
| Live matches | Yes | `GET /fixtures?live=all` |
| Score | Yes | |
| Match status | Yes | |
| Match clock | Yes | |
| Goal events | Yes | event type `Goal` |
| Goal scorer | Yes | player name included |
| Own goals | Yes | |
| Penalties | Yes | |
| VAR events | Yes | |
| Yellow cards | Yes | |
| Red cards | Yes | |
| Substitutions | Yes | |
| Corners | Yes | |
| Lineups | Yes | |
| Match statistics | Yes | |
| Odds | Yes | Ultra+ plan; pre-match |
| Live odds | No | Not available in API-Football |
| Bookmaker markets | Partial | Pre-match only |
| Market suspension status | No | |

#### Coverage

| Item | Result | Notes |
|---|---|---|
| Football/soccer support | Yes | Primary sport |
| Brazil competitions | Yes | Brasileirão A/B, Copa do Brasil, Estaduais |
| Copa Libertadores | Yes | |
| Copa Sul-Americana | Yes | |
| European competitions | Yes | PL, LaLiga, Bundesliga, Serie A, Ligue 1, UCL, UEL |
| Lower divisions | Yes | |
| Women's competitions | Yes | |
| Cup competitions | Yes | |
| Total competitions | 1,200+ | |

#### Local Execution Compatibility

| Item | Result | Notes |
|---|---|---|
| Works from normal residential IP | Yes | |
| Requires cloud/server | No | |
| Requires fixed IP | No | |
| Requires browser login | No | |
| Supports local polling | Yes | |
| Suitable for 24/7 local execution | Yes | |
| Stores credentials locally | Yes | API key in config file |

#### Risks

| Risk | Level | Notes |
|---|---|---|
| Technical risk | Low | Stable REST API, good docs |
| Legal/terms risk | Low | Official commercial API |
| Stability risk | Low | Established provider |
| Cost risk | Low | $19/mo Pro is affordable |
| Data quality risk | Low | Well-reviewed by community |
| Local execution risk | Low | Simple HTTP polling |

#### Recommended Access Method

A — Official documented API

#### MVP Recommendation

**Use in MVP** — Best starting point for match events and live data. Pro plan ($19/mo) sufficient for live events (goals, cards, substitutions). Ultra ($39/mo) adds pre-match odds. No live odds available — complement with The Odds API or BetsAPI for odds layer.

---

### Sportmonks

#### Basic Information

| Field | Value |
|---|---|
| Source name | Sportmonks |
| Category | Sports data API |
| Official website | https://www.sportmonks.com |
| Documentation URL | https://docs.sportmonks.com |
| Plans page | https://www.sportmonks.com/football-api/plans-pricing/ |
| Source priority | High |
| Research status | Done |

#### Access Summary

| Item | Result | Notes |
|---|---|---|
| Official API exists | Yes | REST JSON |
| API type | REST | No WebSocket — polling only |
| Public access | Yes | API key required |
| Paid access | Yes | Free 14-day trial (no credit card) |
| Partner-only access | No | Self-serve |
| Requires API key | Yes | |
| Requires login | No | API key only |
| Requires browser session | No | |
| Requires fixed IP/IP whitelist | No | |
| Local machine compatible | Yes | |

#### Plans & Pricing

| Plan | Price | Leagues | Notes |
|---|---|---|---|
| Starter | ~€29/mo | 5 leagues | Limited for Brazil+CONMEBOL |
| Euro Basic | €39/mo | European leagues | No South America |
| Euro Standard | €59/mo | European leagues | |
| Euro Advanced | €69/mo | European leagues | |
| Worldwide Basic | ~€129/mo | 111 worldwide leagues | Needed for Brazil |
| Worldwide Advanced | ~€219/mo | More leagues | |
| Odds add-on | €14–69/mo extra | Depends on plan | Pre-match + some live |
| Free Trial | 14 days | Full access | No credit card required |

#### Available Data

| Data Type | Available | Notes |
|---|---|---|
| Live matches | Yes | Real-time |
| Score | Yes | |
| Match status | Yes | |
| Match clock | Yes | |
| Goal events | Yes | |
| Goal scorer | Yes | Player name included |
| Own goals | Yes | |
| Penalties | Yes | |
| VAR events | Yes | |
| Yellow cards | Yes | |
| Red cards | Yes | |
| Substitutions | Yes | |
| Ball coordinate tracking | Yes | Unique feature |
| Corners | Yes | |
| Lineups | Yes | |
| Match statistics | Yes | |
| Odds | Yes | Add-on required |
| Live odds | Partial | Add-on, not full live streaming |
| Bookmaker markets | Yes | 140+ bookmakers via Premium Odds add-on |
| Market suspension status | No | Not documented |

#### Coverage

| Item | Result | Notes |
|---|---|---|
| Football/soccer support | Yes | Primary sport |
| Brazil competitions | Yes | Brasileirão A/B, Copa do Brasil, Estaduais |
| Copa Libertadores | Yes | Confirmed |
| Copa Sul-Americana | Yes | |
| European competitions | Yes | All major leagues |
| Total competitions | 2,500+ | |
| Women's competitions | Yes | |

#### Local Execution Compatibility

| Item | Result | Notes |
|---|---|---|
| Works from normal residential IP | Yes | |
| Requires cloud/server | No | |
| WebSocket available | No | REST polling only |
| Suitable for 24/7 local execution | Yes | |
| Uptime SLA | 99.98% | Documented |

#### Risks

| Risk | Level | Notes |
|---|---|---|
| Technical risk | Low | Stable, well-documented |
| Legal/terms risk | Low | Official commercial API |
| Stability risk | Low | Good uptime SLA |
| Cost risk | Medium | €129-219/mo for Brazil coverage |
| Data quality risk | Low | |
| Local execution risk | Low | |

#### Recommended Access Method

A — Official documented API

#### MVP Recommendation

**Use in MVP** (as alternative or secondary to API-Football). Better Brazilian and CONMEBOL coverage. Worldwide plan (~€129/mo) required for full Brazil coverage, which is more expensive than API-Football. Best used if API-Football coverage proves insufficient. Free 14-day trial allows validation before committing.

---

### football-data.org

#### Basic Information

| Field | Value |
|---|---|
| Source name | football-data.org |
| Category | Sports data API |
| Official website | https://www.football-data.org |
| Documentation URL | https://docs.football-data.org |
| Source priority | Medium |
| Research status | Done |

#### Access Summary

| Item | Result | Notes |
|---|---|---|
| Official API exists | Yes | REST JSON |
| API type | REST | |
| Public access | Yes | API key required (free) |
| Paid access | Optional | Free tier available |
| Requires API key | Yes | Free registration |
| Local machine compatible | Yes | |

#### Plans & Pricing

| Plan | Price | Rate Limit | Notes |
|---|---|---|---|
| Free | €0 | 10 req/min | 12 competitions, no live data, delayed scores |
| Livescores | €12/mo | 30 req/min | Live scores added |
| Deep data pack | €29/mo | 30 req/min | Cards, substitutions, lineups |
| Statistics add-on | €15/mo | Only on Standard+ (€49/mo) | Corners, possession, shots |
| Standard | €49/mo | 60 req/min | Combined plan |

#### Free Tier Competitions

PL, UCL, Bundesliga, Serie A, LaLiga, Ligue 1, Eredivisie, Primeira Liga, Championship, **Brasileirão Serie A**, World Cup, European Championship.

#### Available Data

| Data Type | Available | Notes |
|---|---|---|
| Live matches | Paid | Livescores plan €12/mo |
| Score | Paid | |
| Goal events | Paid | Deep data pack |
| Goal scorer | Paid | Deep data pack |
| Yellow cards | Paid | Deep data pack |
| Red cards | Paid | Deep data pack |
| Substitutions | Paid | Deep data pack |
| Odds | No | Not available |
| Live odds | No | Not available |

#### Coverage

| Item | Result | Notes |
|---|---|---|
| Brasileirão Serie A | Yes | Free tier includes it |
| Copa Libertadores | No | Not covered |
| Copa do Brasil | No | Not covered |
| European leagues | Yes | Major 5 leagues |
| Lower divisions | Limited | Only Championship (England) |
| Women's competitions | No | |

#### Risks

| Risk | Level | Notes |
|---|---|---|
| Technical risk | Low | |
| Legal/terms risk | Low | |
| Cost risk | Low | Very affordable |
| Coverage risk | Medium | Limited competitions, no CONMEBOL |

#### Recommended Access Method

A — Official documented API

#### MVP Recommendation

**Use as secondary/supplementary source** for competitions it covers (especially Brasileirão Serie A on free tier). Does not replace API-Football/Sportmonks for full coverage. No odds layer.

---

### Sportradar

#### Basic Information

| Field | Value |
|---|---|
| Source name | Sportradar |
| Category | Sports data API + Odds provider |
| Official website | https://sportradar.com |
| Developer portal | https://developer.sportradar.com |
| Source priority | Low (for MVP) |
| Research status | Done |

#### Access Summary

| Item | Result | Notes |
|---|---|---|
| Official API exists | Yes | REST + WebSocket + HTTP push |
| Paid access | Yes | Enterprise contracts |
| Self-serve | No | Requires sales negotiation |
| Trial/sandbox | Yes | Developer portal sandbox available |

#### Pricing

Enterprise only. Starting at $10,000+/month. Custom contracts. Clients: DraftKings, FanDuel, ESPN, Sky Sports.

#### MVP Recommendation

**Do not use in MVP.** Enterprise pricing ($10,000+/mo) is incompatible with a local small system. Consider only if the project scales to a commercial-grade product. Developer sandbox may be used for reference/testing only.

---

### The Odds API

#### Basic Information

| Field | Value |
|---|---|
| Source name | The Odds API |
| Category | Odds API |
| Official website | https://the-odds-api.com |
| Documentation URL | https://the-odds-api.com/liveapi/guides/v4/ |
| Source priority | High |
| Research status | Done |

#### Access Summary

| Item | Result | Notes |
|---|---|---|
| Official API exists | Yes | REST JSON |
| API type | REST | Credit-based billing |
| Public access | Yes | API key |
| Paid access | Yes | Free tier + paid plans |
| Requires API key | Yes | |
| Requires fixed IP | No | |
| Local machine compatible | Yes | |

#### Plans & Pricing

| Plan | Price | Credits/mo | Bookmakers | Live Odds |
|---|---|---|---|---|
| Free | $0 | 500 credits | Very limited | No |
| Professional | $29/mo | 20,000 credits | 24 sports, US books | Limited |
| Business | $99/mo | 200,000 credits | 50+ books (UK/EU/AU) + Pinnacle | Yes |

Note: 1 API call to `/odds` costs multiple credits depending on markets/regions queried. Live (in-play) odds available on Business plan.

#### Available Data

| Data Type | Available | Notes |
|---|---|---|
| Pre-match odds | Yes | All plans |
| Live (in-play) odds | Yes | Business plan ($99/mo) |
| H2H/moneyline | Yes | |
| Spreads/handicaps | Yes | |
| Totals (over/under) | Yes | |
| Player props | Yes | Business plan |
| Bet365 coverage | Yes | Business plan (international) |
| Pinnacle coverage | Yes | Business plan |
| Betfair coverage | Yes | Business plan |
| Market suspension status | Partial | Not explicitly documented as a field |
| Brazilian competitions | Yes | Brasileirão Serie A confirmed |

#### Bookmakers Covered (Business plan)

~50 bookmakers including: Bet365, Betfair, Pinnacle, Unibet, William Hill, Betway, 1xBet, and others across UK/EU/AU/US regions.

#### Local Execution Compatibility

| Item | Result | Notes |
|---|---|---|
| Works from residential IP | Yes | |
| WebSocket | No | REST polling only |
| Suitable for 24/7 local | Yes | |

#### Risks

| Risk | Level | Notes |
|---|---|---|
| Technical risk | Low | |
| Legal/terms risk | Low | Licensed redistributor |
| Cost risk | Low-Medium | $99/mo for full live odds |
| Coverage risk (suspension) | Medium | Market suspension not clearly documented |

#### Recommended Access Method

B — Commercial third-party API

#### MVP Recommendation

**Use in MVP.** Professional plan ($29/mo) for pre-match odds across many bookmakers. Business plan ($99/mo) for live in-play odds + Pinnacle + international coverage. Best documented odds API for developers. Complement with BetsAPI if Bet365 live odds and suspension status are critical.

---

### BetsAPI

#### Basic Information

| Field | Value |
|---|---|
| Source name | BetsAPI / b365api |
| Category | Odds API + live score data |
| Official website | https://betsapi.com |
| Documentation URL | https://betsapi.com/docs/ |
| Source priority | High |
| Research status | Done |

#### Access Summary

| Item | Result | Notes |
|---|---|---|
| Official API exists | Yes | REST JSON |
| API type | REST | |
| Public access | Yes | Paid subscription + API key |
| Trial available | Yes | $1 for 1-day trial |
| Requires API key | Yes | |
| Requires fixed IP | No | |
| Local machine compatible | Yes | |

#### Plans & Pricing

Plans available by sport or combined:
- **Events API** — soccer, basketball, tennis separately or combined
- **Bet365 API** — dedicated Bet365 odds (separate plan)
- **BWin API**, **Betfair API**, **SboBet API** — separate plans
- **Everything API** — all sports + volume package included
- **Volume Packages** — increase rate limit from default 3,600 req/hour up to 799,999 req/hour

Exact pricing requires visiting betsapi.com/mm/pricing_table (login required to see prices).

#### Available Data

| Data Type | Available | Notes |
|---|---|---|
| Live matches | Yes | Soccer, basketball, tennis, etc. |
| Score | Yes | |
| Match status | Yes | |
| Goal events | Yes | |
| Cards | Yes | |
| Bet365 pre-match odds | Yes | Bet365 API plan |
| Bet365 live odds | Yes | Updates every 3-5 seconds |
| Bet365 market suspension status | Yes | Included in live feeds |
| BWin odds | Yes | |
| Betfair odds | Yes | |
| SboBet odds | Yes | |
| 1xBet odds | Yes | |
| Betway odds | Yes | |
| Brazilian competitions | Yes | Confirmed |

#### Local Execution Compatibility

| Item | Result | Notes |
|---|---|---|
| Works from residential IP | Yes | |
| WebSocket | No | REST polling only |
| Update frequency | 3-5 seconds | Live data |
| Suitable for 24/7 local | Yes | |

#### Risks

| Risk | Level | Notes |
|---|---|---|
| Technical risk | Low | |
| Legal/terms risk | Medium | Commercial redistributor; verify licensing agreements with source bookmakers |
| Stability risk | Low-Medium | Less established than The Odds API |
| Cost risk | Medium | Pricing not publicly disclosed |
| Data quality risk | Low | |

#### Notes on Legitimacy

BetsAPI is a paid commercial API service, not a scraper. It redistributes data from bookmakers under commercial agreements. Using the official paid API (not scraping their site) is the appropriate access method. Medium legal/ToS risk reflects the fact that the specific licensing arrangements between BetsAPI and source bookmakers (particularly Bet365) are not fully transparent. This is a well-known provider used by many arbitrage and monitoring services.

#### Recommended Access Method

B — Commercial third-party API

#### MVP Recommendation

**Use in MVP** — Best option for Bet365 live odds and market suspension status. Unique value: suspension status is explicitly included in live feeds, which is a key requirement for divergence detection. Complement with The Odds API for broader bookmaker coverage and better documentation.

---

### SofaScore

#### Basic Information

| Field | Value |
|---|---|
| Source name | SofaScore |
| Category | Live score app |
| Official website | https://sofascore.com |
| Source priority | Low (for MVP) |
| Research status | Done |

#### Access Summary

| Item | Result | Notes |
|---|---|---|
| Official public API | No | Explicitly stated in FAQ |
| Media widgets | Yes | Via corporate.sofascore.com/widgets |
| Unofficial wrappers | Yes | GitHub, PyPI — not affiliated |
| Official API policy | Restricted | Cannot share data due to data provider agreements |

#### MVP Recommendation

**Do not use in MVP.** No official API. Data provider agreements prevent API access. Media widgets provide limited structured data. Unofficial wrappers are high-risk for a production system. SofaScore data is available indirectly via commercial sports data APIs (API-Football, Sportmonks) which source from similar providers.

---

### Flashscore

#### Basic Information

| Field | Value |
|---|---|
| Source name | Flashscore |
| Category | Live score app |
| Official website | https://flashscore.com |
| Source priority | Low (for MVP) |
| Research status | Done |

#### Access Summary

| Item | Result | Notes |
|---|---|---|
| Official public API | No | No official API exists |
| Third-party scrapers | Yes | Via Apify (paid scraping service) |

#### MVP Recommendation

**Do not use in MVP.** No official API. Third-party scrapers via Apify are paid scraping services — fragile, rate-limited, potentially against ToS, and unsuitable for 24/7 local production monitoring.

---

### FotMob

#### Basic Information

| Field | Value |
|---|---|
| Source name | FotMob |
| Category | Live score app |
| Official website | https://fotmob.com |
| Source priority | Low (for MVP) |
| Research status | Done |

#### Access Summary

| Item | Result | Notes |
|---|---|---|
| Official public API | No | No official developer API |
| Unofficial wrappers | Yes | Python (pyfotmob), Ruby gems — not affiliated |

#### MVP Recommendation

**Do not use in MVP.** No official API. Unofficial wrappers are undocumented, fragile, may break on any FotMob update.

---

### Betfair Exchange API

#### Basic Information

| Field | Value |
|---|---|
| Source name | Betfair Exchange |
| Category | Bookmaker (peer-to-peer exchange) |
| Official website | https://developer.betfair.com |
| Documentation URL | https://betfair-developer-docs.atlassian.net |
| Source priority | High |
| Research status | Done |

#### Access Summary

| Item | Result | Notes |
|---|---|---|
| Official public API | Yes | REST (APING) + Exchange Stream API (WebSocket) |
| API key required | Yes | Application Key via Betfair account |
| Requires Betfair account | Yes | |
| Requires fixed IP | No | |
| Local machine compatible | Yes | |

#### Plans & Pricing

| Plan | Cost | Notes |
|---|---|---|
| Delayed App Key | Free | For development/testing. Delayed data. |
| Personal Live App Key | £499 activation | One-time fee for live data; for personal betting use |
| Vendor App Key | Commercial | For software vendors/apps |

For monitoring/research purposes (non-betting), the Delayed App Key may be sufficient for testing, but live monitoring requires a funded account and Live App Key.

#### Available Data

| Data Type | Available | Notes |
|---|---|---|
| Exchange odds (back/lay) | Yes | |
| Pre-match markets | Yes | |
| Live (in-play) markets | Yes | Via Stream API (WebSocket) |
| Market status | Yes | OPEN, SUSPENDED, CLOSED |
| Trading volume | Yes | |
| Market depth | Yes | |
| Event IDs | Yes | |
| Match events (goals, cards) | No | This is an exchange, not a data feed |

#### Important Distinction

Betfair is an exchange, not a traditional bookmaker. It exposes odds from bettors placing back/lay bets, not from a bookmaker's margin. This is valuable for:
- Detecting odds movements
- Identifying market suspension signals
- Comparing exchange vs. traditional bookmaker odds

It does NOT provide: goal scorers, cards, substitutions, or match event data — only odds and market status.

#### Local Execution Compatibility

| Item | Result | Notes |
|---|---|---|
| Works from residential IP | Yes | |
| WebSocket available | Yes | Exchange Stream API |
| Suitable for 24/7 local | Yes | |
| Brazil access | Restricted | Betfair not formally licensed in Brazil post-2024 regulation; account registration may be restricted |

#### Risks

| Risk | Level | Notes |
|---|---|---|
| Technical risk | Low | Well-documented API |
| Legal/terms risk | Low | Official API; ToS allows monitoring use |
| Geographic restriction | Medium | Brazil account registration restricted |
| Data type risk | Low | Clear scope: exchange odds and market status only |

#### Recommended Access Method

A — Official documented API

#### MVP Recommendation

**Use in MVP** — The only bookmaker with a free official API providing live market streaming and suspension status. Key limitation: it is an exchange (not Bet365/Betano-style bookmaker), so odds are peer-to-peer and not directly comparable to traditional bookmaker odds. Geographic restriction for Brazil should be validated manually. If Brazil restriction blocks account creation, fall back to BetsAPI for Betfair data.

---

### Bet365 / Betano / Sportingbet

No public API. Access via:
- **BetsAPI** for live odds, market suspension, 3-5s updates
- **The Odds API Business** for pre-match and live odds aggregation

Direct access (scraping, browser automation) is explicitly prohibited by their Terms of Service and is not viable for MVP.

---

### Pinnacle

API closed to general public since July 2025. Brazil geographically blocked.

Access Pinnacle odds via:
- **The Odds API Business plan** ($99/mo)

Direct API access not viable for MVP.

---

## Technical Unknowns — Requires Manual Validation

| Unknown | Source | Method | Priority |
|---|---|---|---|
| BetsAPI exact pricing tiers | BetsAPI | Visit betsapi.com/mm/pricing_table (requires login) | High |
| BetsAPI covers which Brazilian competitions | BetsAPI | API trial ($1) | High |
| The Odds API: market suspension status field | The Odds API | API trial (free) | High |
| Betfair account creation from Brazil | Betfair | Manual test | High |
| API-Football: exact event types in live payload | API-Football | Free tier test (100 req/day) | Medium |
| Sportmonks: live odds update frequency on add-on | Sportmonks | 14-day free trial | Medium |
| football-data.org: response latency for live data | football-data.org | €12/mo trial | Low |

---

## Architecture Implications (Draft — Pending Full Phase 01)

| Finding | Implication |
|---|---|
| All data APIs are REST (no WebSocket except Betfair Stream) | Use polling workers with configurable interval per source |
| Betfair has WebSocket Stream API | Add persistent connection worker for Betfair alongside polling workers |
| Different rate limits per source | Add per-source request budget and scheduler |
| BetsAPI updates every 3-5s | High-frequency polling worker for odds/suspension layer |
| Sources use different competition IDs and team names | Build Match Resolver + Normalization Engine early |
| Live score apps have no official API | Do not plan to integrate SofaScore/Flashscore in MVP |
| Bookmakers without official API | Access via BetsAPI or The Odds API only |
| Sportradar/OpticOdds too expensive | Exclude from MVP architecture |
| Market suspension status available via BetsAPI | Prioritize BetsAPI integration for odds layer |

---

## Pending Research

- [ ] Official competition websites (63 listed in PHASE_01_DATA_SOURCE_RESEARCH.md)
- [ ] API-Football: manual validation of live event payload
- [ ] BetsAPI: pricing confirmation and coverage test
- [ ] The Odds API: suspension status field confirmation
- [ ] Betfair: Brazil account creation feasibility
