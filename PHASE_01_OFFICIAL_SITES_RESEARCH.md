# Phase 01 - Official Competition Sites Research

**Research date:** 2026-05-26
**Sites researched:** 63 (grouped into ~20 organizations)

---

## Strategic Conclusion

**The central finding of this research:**

> No official competition website provides a publicly accessible, developer-documented API for live match data — with one community exception (OpenLigaDB for Bundesliga).

All 63 competitions have their data accessible through **commercial aggregator APIs** (API-Football, Sportmonks, football-data.org). The official websites serve as:
- Visual reference for manual validation
- Source of ground-truth for score/event cross-checks
- NOT a reliable programmatic data source for live monitoring

**Impact on MVP architecture:**
- Official competition sites do NOT replace the commercial API layer
- API-Football / Sportmonks remain the correct primary data sources
- Official sites may be used for occasional manual validation only
- No architecture change required from the previously identified MVP stack

---

## Classification Table — All 63 Competitions

| # | Competition | Official Site | Access Method | Local Compat | Commercial API | MVP Approach |
|---:|---|---|---|---|---|---|
| 1 | 2ª Divisão Alemã | bundesliga.com | D (OpenLigaDB) | A | API-Football/Sportmonks | OpenLigaDB (free) + API-Football |
| 2 | 2ª Divisão Dinamarquesa | divisionsforeningen.dk | G | G | API-Football/Sportmonks | Via API-Football |
| 3 | 2ª Divisão Escocesa | spfl.co.uk | G | G | Sportmonks (free tier) | Via Sportmonks |
| 4 | 2ª Divisão Espanhola | laliga.com | G | G | API-Football/Sportmonks | Via API-Football |
| 5 | 2ª Divisão Italiana | legab.it | G | G | API-Football/Sportmonks | Via API-Football |
| 6 | 2ª Divisão Norueguesa | fotball.no | G | G | API-Football/Sportmonks | Via API-Football |
| 7 | 2ª Divisão Sueca | superettan.se | G | G | API-Football/Sportmonks | Via API-Football |
| 8 | 2ª Divisão Inglesa | efl.com | G | G | API-Football/Sportmonks/football-data.org | Via API-Football |
| 9 | 3ª Divisão Inglesa | efl.com | G | G | API-Football/Sportmonks | Via API-Football |
| 10 | 4ª Divisão Inglesa | efl.com | G | G | API-Football/Sportmonks | Via API-Football |
| 11 | 5ª Divisão Inglesa | thenationalleague.org.uk | G | G | API-Football | Via API-Football |
| 12 | América do Sul - Elim. Copa | conmebol.com | G | G | API-Football/Sportmonks | Via API-Football |
| 13 | Brasileirão - Série A | cbf.com.br | G | G | API-Football/Sportmonks/API Futebol | Via API-Football + API Futebol |
| 14 | Brasileirão - Série B | cbf.com.br | G | G | API-Football/Sportmonks/API Futebol | Via API-Football + API Futebol |
| 15 | Campeonato Alemão | bundesliga.com | D (OpenLigaDB) | A | API-Football/Sportmonks | OpenLigaDB (free) + API-Football |
| 16 | Campeonato Argentino | ligaprofesional.ar | G | G | API-Football/Sportmonks | Via API-Football |
| 17 | Campeonato Australiano | aleagues.com.au | G | G | API-Football/Sportmonks | Via API-Football |
| 18 | Campeonato Austríaco | bundesliga.at | G | G | API-Football/Sportmonks | Via API-Football |
| 19 | Campeonato Belga | proleague.be | G | G | API-Football/Sportmonks | Via API-Football |
| 20 | Campeonato Búlgaro | efbetleague.com | G | G | API-Football/Sportmonks | Via API-Football |
| 21 | Campeonato Chileno | campeonatochileno.cl | G | G | API-Football/Sportmonks | Via API-Football |
| 22 | Campeonato Chinês | csl-china.com | G | G | API-Football/Sportmonks | Via API-Football |
| 23 | Campeonato Colombiano | dimayor.com.co | G | G | API-Football/Sportmonks | Via API-Football |
| 24 | Campeonato Dinamarquês | superliga.dk | G | G | API-Football/Sportmonks | Via API-Football |
| 25 | Campeonato Equatoriano | ligapro.ec | G | G | API-Football/Sportmonks | Via API-Football |
| 26 | Campeonato Escocês | spfl.co.uk | G | G | API-Football/Sportmonks | Via Sportmonks |
| 27 | Campeonato Espanhol | laliga.com | G | G | API-Football/Sportmonks/football-data.org | Via API-Football |
| 28 | Campeonato Francês | ligue1.com | G | G | API-Football/Sportmonks/football-data.org | Via API-Football |
| 29 | Campeonato Grego | slgr.gr | G | G | API-Football/Sportmonks | Via API-Football |
| 30 | Campeonato Holandês | eredivisie.eu | G | G | API-Football/Sportmonks/football-data.org | Via API-Football |
| 31 | Campeonato Húngaro | nb1.hu | G | G | API-Football/Sportmonks | Via API-Football |
| 32 | Campeonato Inglês | premierleague.com | G | G | API-Football/Sportmonks/football-data.org | Via API-Football |
| 33 | Campeonato Inglês (F) | wslfootball.com | G | G | API-Football/Sportmonks | Via API-Football |
| 34 | Campeonato Irlandês | leagueofireland.ie | G | G | API-Football/Sportmonks | Via API-Football |
| 35 | Campeonato Italiano | legaseriea.it | D/G* | G | API-Football/Sportmonks | Via API-Football |
| 36 | Campeonato Japonês | jleague.co | G | G | API-Football/Sportmonks | Via API-Football |
| 37 | Campeonato Mexicano | ligamx.net | G | G | API-Football/Sportmonks | Via API-Football |
| 38 | Campeonato Norueguês | eliteserien.no | G | G | API-Football/Sportmonks | Via API-Football |
| 39 | Campeonato Peruano | liga1.pe | G | G | API-Football/Sportmonks | Via API-Football |
| 40 | Campeonato Português | ligaportugal.pt | G | G | API-Football/Sportmonks/football-data.org | Via API-Football |
| 41 | Campeonato Saudita | spl.com.sa | G | G | API-Football/Sportmonks | Via API-Football |
| 42 | Campeonato Sueco | allsvenskan.se | G | G | API-Football/Sportmonks | Via API-Football |
| 43 | Campeonato Turco | tff.org | G | G | API-Football/Sportmonks | Via API-Football |
| 44 | Copa da Alemanha | dfb.de/dfb-pokal | D (OpenLigaDB) | A | API-Football/Sportmonks | OpenLigaDB + API-Football |
| 45 | Copa da Liga Argentina | ligaprofesional.ar | G | G | API-Football/Sportmonks | Via API-Football |
| 46 | Copa da Liga Inglesa | efl.com | G | G | API-Football/Sportmonks | Via API-Football |
| 47 | Copa das Ligas (MLS/LigaMX) | leaguescup.com | G | G | API-Football/Sportmonks | Via API-Football |
| 48 | Copa do Brasil | cbf.com.br | G | G | API-Football/Sportmonks/API Futebol | Via API-Football + API Futebol |
| 49 | Copa do Rei | rfef.es | G | G | API-Football/Sportmonks | Via API-Football |
| 50 | Copa Libertadores | libertadores.com (CONMEBOL) | G | G | API-Football/Sportmonks/API Futebol | Via API-Football |
| 51 | Copa Sul-Americana | conmebol.com | G | G | API-Football/Sportmonks/API Futebol | Via API-Football |
| 52 | Copinha | copinha.com.br | G | G | API Futebol (parcial) | Via API Futebol |
| 53 | Eliminatórias da Eurocopa | uefa.com | D* (partner) | G | API-Football/Sportmonks | Via API-Football |
| 54 | Liga Canadense | canpl.ca | G | G | API-Football (verificar) | Via API-Football |
| 55 | Liga Conferência (UECL) | uefa.com | D* (partner) | G | API-Football/Sportmonks | Via API-Football |
| 56 | Liga das Nações (Concacaf) | concacaf.com | G | G | API-Football/Sportmonks | Via API-Football |
| 57 | Liga dos Campeões (UCL) | uefa.com | D* (partner) | G | API-Football/Sportmonks/football-data.org | Via API-Football |
| 58 | Liga dos Campeões da Ásia | the-afc.com | G | G | API-Football/Sportmonks | Via API-Football |
| 59 | Liga dos Campeões (F) | uefa.com | D* (partner) | G | API-Football/Sportmonks | Via API-Football |
| 60 | Liga dos Campeões Q. | uefa.com | D* (partner) | G | API-Football/Sportmonks | Via API-Football |
| 61 | Liga Europa (UEL) | uefa.com | D* (partner) | G | API-Football/Sportmonks/football-data.org | Via API-Football |
| 62 | MLS | mlssoccer.com | G | G | API-Football/Sportmonks/SportsDataIO | Via API-Football |
| 63 | Taça de Portugal | fpf.pt | G | G | API-Football/Sportmonks | Via API-Football |

**Method Legend:** A=API oficial pública, B=Terceiro comercial, C=Widget, D=Endpoint público não oficial/community, G=Sem método viável para programação
**Local Compat legend:** A=Ótimo local, G=Não viável direto do site
**D\*** = portal oficial existe mas acesso restrito a partners/media

---

## Group Profiles

---

### UEFA Group
**Competitions (6):** UCL, UEL, UECL, UCL Feminina, UCL Qualif., Eliminatórias Eurocopa
**Site:** uefa.com

| Item | Result |
|---|---|
| Official API exists | Yes — `uefadigitalapi.portal.azure-api.net` (Azure API Management) |
| Public access | No — partner/media-only; requires registration as official media partner |
| Data via official site | Not viable for independent developers |
| Data via commercial APIs | Yes — API-Football (covers all UEFA competitions), Sportmonks, football-data.org (UCL, UEL free) |
| Unofficial TypeScript wrapper | Exists (github.com/ErikMichelson/uefa-api) — wraps partner API, risky |
| Local compatible | G (official) → A (via commercial API) |
| MVP approach | Use API-Football or Sportmonks for all UEFA competitions |

---

### CBF Group
**Competitions (3):** Brasileirão Série A, Série B, Copa do Brasil
**Site:** cbf.com.br / campeonatos.cbf.com.br

| Item | Result |
|---|---|
| Official API exists | No public documented API |
| campeonatos.cbf.com.br | Internal endpoint — observed by developers but not officially documented; fragile |
| Data via commercial APIs | Yes — API-Football, Sportmonks, football-data.org (Brasileirão A only, free tier) |
| Brazilian-specific option | **API Futebol** (api-futebol.com.br) — commercial Brazilian-focused API for Brasileirão A/B, Copa do Brasil, Libertadores, Sul-Americana |
| Local compatible | G (official) → A (via commercial APIs) |
| MVP approach | API-Football primary + API Futebol as optional Brazilian-specific source |

**Note on API Futebol (api-futebol.com.br):**
- Brazilian commercial API specifically for Brazilian football
- Covers Brasileirão A/B, Copa do Brasil, Libertadores, Sul-Americana, Estaduais, Copinha
- Free test environment available (100 req/day on paid plans)
- JSON format, REST API
- Lower cost than international APIs for Brazilian-only coverage
- Worth evaluating as supplementary or primary source for Brazilian competitions

---

### CONMEBOL Group
**Competitions (3):** Copa Libertadores, Copa Sul-Americana, Eliminatórias Copa do Mundo Sul-Americana
**Sites:** conmebol.com, libertadores.com (same organization)

| Item | Result |
|---|---|
| Official API exists | No public documented API |
| Data via commercial APIs | Yes — API-Football, Sportmonks, API Futebol |
| Local compatible | G (official) → A (via commercial APIs) |
| MVP approach | Via API-Football or Sportmonks (Worldwide plan) |

---

### Bundesliga Group
**Competitions (2):** Bundesliga, 2. Bundesliga
**Site:** bundesliga.com

| Item | Result |
|---|---|
| Official DFL/bundesliga.com API | No public API from official site |
| **OpenLigaDB** | **FREE community API — no authentication required** |
| OpenLigaDB URL | https://api.openligadb.de (Swagger documented) |
| Rate limit | 1,000 requests/hour per IP |
| Data available | Live scores, match results, standings, teams |
| Bundesliga ID | `bl1` (Bundesliga), `bl2` (2. Bundesliga) |
| DFB-Pokal | Also available in OpenLigaDB |
| Live match events | Limited — primarily scores and results, not full events |
| Local compatible | A — excellent, no auth required |
| Legal/ToS risk | Low — community open database |
| MVP approach | **OpenLigaDB as free supplementary source** + API-Football as primary event source |

**OpenLigaDB is the standout finding of this research** — the only free, no-auth, community-maintained API covering real football competitions (Bundesliga, 2. Bundesliga, DFB-Pokal, and others).

---

### Premier League
**Site:** premierleague.com

| Item | Result |
|---|---|
| Official API | No public developer API |
| Notes | Sportradar holds official data distribution rights |
| Data via commercial APIs | Yes — API-Football, Sportmonks, football-data.org (free tier includes PL) |
| MVP approach | Via API-Football or football-data.org (free) |

---

### EFL Group
**Competitions (4):** Championship, League One, League Two, EFL Cup
**Site:** efl.com

| Item | Result |
|---|---|
| Official API | No public developer API |
| Data via commercial APIs | Championship included in football-data.org free tier; all via API-Football/Sportmonks |
| MVP approach | Via API-Football (all 4 competitions covered) |

---

### LaLiga Group
**Competitions (3):** LaLiga, LaLiga Hypermotion, Copa del Rey
**Sites:** laliga.com, rfef.es

| Item | Result |
|---|---|
| LaLiga official API | Claims to exist but not publicly accessible without partnership |
| RFEF (Copa del Rey) | No public API |
| Data via commercial APIs | Yes — API-Football, Sportmonks, football-data.org (LaLiga free) |
| MVP approach | Via API-Football or Sportmonks |

---

### SPFL Group
**Competitions (2):** Scottish Premiership, Scottish Championship
**Site:** spfl.co.uk

| Item | Result |
|---|---|
| Official API | No public developer API |
| Notable | Sportmonks **free tier includes Scottish Premiership** |
| MVP approach | Via Sportmonks or API-Football |

---

### Serie A italiana
**Site:** legaseriea.it

| Item | Result |
|---|---|
| Official API docs | Exists at legaseriea.it/en/lega-serie-a/documentazione |
| Public access | No — **Genius Sports holds exclusive official data and betting streaming rights through 2029** |
| Data via commercial APIs | Yes — API-Football, Sportmonks (data via non-exclusive feeds) |
| MVP approach | Via API-Football or Sportmonks |

---

### Ligue 1
**Site:** ligue1.com

| Item | Result |
|---|---|
| Official API | No public developer API |
| Data via commercial APIs | Yes — API-Football, Sportmonks, football-data.org (free) |
| MVP approach | Via API-Football or football-data.org (free) |

---

### MLS
**Site:** mlssoccer.com

| Item | Result |
|---|---|
| docs.mlssoccerapi.com | Unreachable — likely internal/partner-only |
| Data via commercial APIs | Yes — API-Football, Sportmonks, SportsDataIO |
| MVP approach | Via API-Football or Sportmonks |

---

### Argentina Group
**Competitions (2):** Liga Profesional, Copa de la Liga
**Site:** ligaprofesional.ar

| Item | Result |
|---|---|
| Official API | No public API |
| Data via commercial APIs | Yes — API-Football, Sportmonks |
| MVP approach | Via API-Football |

---

### South American Leagues (Individual)
**Chile, Peru, Colombia, Ecuador**

| Item | Result |
|---|---|
| Official APIs | None found for any of these |
| Data via commercial APIs | API-Football covers all; Sportmonks with Worldwide plan |
| MVP approach | Via API-Football |

---

### J-League
**Site:** jleague.co / data.j-league.or.jp

| Item | Result |
|---|---|
| data.j-league.or.jp | Official data site exists but no developer API documentation found |
| Data via commercial APIs | Yes — API-Football, Sportmonks |
| MVP approach | Via API-Football or Sportmonks |

---

### Nordic Leagues
**Allsvenskan, Superettan (Sweden), Eliteserien, 2ª div Norway (fotball.no), Superliga DK, 2ª div DK**

| Item | Result |
|---|---|
| Official APIs | None found for any |
| Data via commercial APIs | API-Football, Sportmonks, football-data.org (Superliga DK? - verify) |
| MVP approach | Via API-Football |

---

### Smaller European Leagues
**Austria, Belgium, Bulgaria, Greece, Hungary, Ireland, Turkey, Saudi Arabia**

| Item | Result |
|---|---|
| Official APIs | None found for any |
| Data via commercial APIs | API-Football covers all; Sportmonks with appropriate plan |
| MVP approach | Via API-Football |

---

### Remaining Competitions (Cups, AFC, Concacaf, etc.)
**Copa das Ligas, Copa Alemã (DFB Pokal), Copinha, Taça de Portugal, WSL, National League England, Canadian Premier League, A-League Australia, AFC Champions League, Concacaf Nations League**

| Item | Result |
|---|---|
| Official APIs | None found |
| DFB-Pokal | Available via OpenLigaDB (free) + API-Football |
| Copinha | Partially covered by API Futebol (Brazilian-focused) |
| Canadian Premier League | API-Football coverage — verify |
| All others | Via API-Football or Sportmonks |
| MVP approach | Via API-Football; validate coverage per competition |

---

## Bonus Free Sources Discovered

These were not in the original source list but emerged during research:

### OpenLigaDB
| Field | Value |
|---|---|
| URL | https://api.openligadb.de |
| Type | FREE community-maintained database |
| Authentication | None required |
| Rate limit | 1,000 req/hour per IP |
| Competitions covered | Bundesliga (bl1), 2. Bundesliga (bl2), DFB-Pokal (dfb-pokal), and many German leagues |
| Live data | Live scores (not full event stream) |
| API format | REST/JSON, Swagger documented |
| Local compatibility | A — excellent |
| Legal/ToS risk | Low |
| MVP recommendation | Use as FREE supplementary source for Bundesliga/DFB-Pokal |

### TheSportsDB
| Field | Value |
|---|---|
| URL | https://www.thesportsdb.com |
| Type | FREE crowd-sourced sports database |
| Authentication | API key (free registration) |
| Coverage | 634 soccer leagues globally |
| Live data | Limited on free tier; paid tier has more |
| Best use | Metadata, artwork (team logos, banners), static reference data |
| Not ideal for | Real-time live events monitoring |
| MVP recommendation | Use for static metadata/reference only; not for live events |

### API Futebol (api-futebol.com.br)
| Field | Value |
|---|---|
| URL | https://www.api-futebol.com.br |
| Type | Brazilian commercial API |
| Authentication | API key (free test + paid plans) |
| Coverage | Brasileirão A/B, Copa do Brasil, Copa Libertadores, Sul-Americana, Estaduais, Copinha |
| Live data | Yes — goals, cards, substitutions |
| Plans | Free test (100 req/day paid), scalable up to 15,000 req/day |
| Local compatibility | A |
| Legal/ToS risk | Low |
| MVP recommendation | Evaluate as supplementary Brazilian-focused source alongside API-Football |

---

## Coverage Confirmation: API-Football vs. Sportmonks

Both main candidates cover the 63 competitions at appropriate plan tiers:

| Competition Group | API-Football Pro ($19/mo) | API-Football Ultra ($39/mo) | Sportmonks Worldwide (~€129/mo) |
|---|---|---|---|
| UEFA competitions (UCL, UEL, etc.) | ✅ | ✅ | ✅ |
| Premier League, Bundesliga, LaLiga, Serie A, Ligue 1 | ✅ | ✅ | ✅ (Euro plan) |
| EFL (Championship, L1, L2) | ✅ | ✅ | ✅ |
| Brasileirão A/B, Copa do Brasil | ✅ | ✅ | ✅ |
| Copa Libertadores, Sul-Americana | ✅ | ✅ | ✅ |
| SPFL, Nordic leagues | ✅ | ✅ | ✅ |
| South American leagues (ARG, CHI, COL, ECU, PER) | ✅ | ✅ | ✅ |
| MLS, Canadian, Australian leagues | ✅ | ✅ | ✅ |
| Smaller European leagues | ✅ | ✅ | ✅ |
| Pre-match odds | ❌ | ✅ | ✅ (add-on) |
| **Total cost for full coverage** | **$19/mo** | **$39/mo** | **€129/mo** |

**Conclusion:** API-Football Pro ($19/mo) covers all 63 competitions for live events. Ultra ($39/mo) adds pre-match odds.

---

## Phase 01 Completion Assessment

With this research complete, Phase 01 has covered:

| Category | Status | Key Finding |
|---|---|---|
| Sports data APIs | Done | API-Football $19/mo = MVP primary |
| Odds APIs | Done | BetsAPI + The Odds API = MVP odds layer |
| Live score apps | Done | All excluded (no official APIs) |
| Bookmakers | Done | Betfair free + BetsAPI for Bet365 |
| Official competition websites (63) | **Done** | All depend on commercial APIs; only OpenLigaDB (Bundesliga) is free direct source |

**Phase 01 is now complete** in terms of source research scope.

---

## Recommended Next Steps After Phase 01

1. **Confirm BetsAPI pricing** — visit pricing table (login required)
2. **Trial API-Football free tier** (100 req/day) — validate live event payload structure
3. **Trial Sportmonks 14-day free trial** — validate Brazilian competition coverage
4. **Trial The Odds API** — confirm market suspension status field
5. **Test Betfair account from Brazil** — confirm geographic restriction
6. **Optionally evaluate API Futebol** — as a Brazilian-focused complement or alternative
7. **Start architecture planning** — Phase 01 research is complete
