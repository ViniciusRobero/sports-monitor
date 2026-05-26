# Decisions

| Date | Decision | Reason |
|---|---|---|
| 2026-05-26 | The project is local-first | Avoid recurring cloud costs |
| 2026-05-26 | Phase 01 is research-only | Architecture depends on real source access methods |
| 2026-05-26 | Architecture starts only after source viability report | Avoid designing based on assumptions |
| 2026-05-26 | Maintain continuity files in the repository | Allow switching between ChatGPT, Claude, Codex, or other models/tools without losing progress |
| 2026-05-26 | `PROJECT_CONTEXT.md` is mandatory | It is the main handoff file for any AI agent or developer |
| 2026-05-26 | Live score apps (SofaScore, Flashscore, FotMob) excluded as *official API sources* | No official APIs |
| 2026-05-26 | HTML scraping and unofficial internal API endpoints are acceptable for comparison sources | User confirmed: data fidelity is worth the ToS/fragility risk for SofaScore, 365Scores, Google. Boundary: publicly accessible data only (no login bypass, no CAPTCHA solving, no fingerprint spoofing). Approach: observe internal API calls via DevTools + simple HTTP GET. Risk classification: fragile, medium ToS risk. |
| 2026-05-26 | Sportradar and OpticOdds excluded from MVP | Enterprise pricing ($10k+/mo) incompatible with local small system |
| 2026-05-26 | Direct bookmaker API integration excluded (Bet365, Betano, Sportingbet) | No public APIs; ToS proibem automação |
| 2026-05-26 | BetsAPI = primary path for Bet365 live odds + suspension status | Única fonte confirmada com Bet365 live + suspensão a 3-5s |
| 2026-05-26 | The Odds API = primary multi-bookmaker odds aggregator | $29/mo Pro (pre-match) ou $99/mo Business (live + Pinnacle + 50+ books) |
| 2026-05-26 | Betfair Exchange API = free bookmaker live streaming | Único bookmaker com API oficial gratuita cobrindo live odds + status de mercado |
| 2026-05-26 | Pinnacle odds via The Odds API Business | API direta da Pinnacle fechada ao público desde jul/2025; Brasil bloqueado |
| 2026-05-26 | API-Football = primary sports data API candidate | $19/mo Pro; live events 15s; 1200+ competições; Brasileirão/Libertadores ok |
| 2026-05-26 | Sportmonks = sports data API alternative | €129/mo Worldwide needed for Brazil; melhor cobertura CONMEBOL; trial 14 dias |
| 2026-05-26 | SofaScore: integrar via api.sofascore.com/api/v1 | Endpoints internos bem documentados; sem auth; User-Agent browser + 25-30s polling |
| 2026-05-26 | 365Scores: integrar via webws.365scores.com/web/ | Endpoints internos confirmados; sem auth; appTypeId=5, langId=31, timezoneName=America/Sao_Paulo |
| 2026-05-26 | Google: não integrar como fonte de dados — gerar link manual no dashboard | Sem endpoint JSON acessível; SerpApi ($50+/mo) inviável para MVP; link manual é suficiente para verificação do analista |
| 2026-05-26 | Histórico de leituras por fonte obrigatório | Usuário quer todos os dados coletados salvos (SQLite); schema: source_readings (uma linha por poll por fonte) + payload JSON bruto |
| 2026-05-26 | Match Resolver obrigatório | IDs internos do SofaScore, 365Scores, API-Football são diferentes para a mesma partida — normalização por nome de time + horário + competição necessária |
| 2026-05-26 | Workers independentes por fonte + DivergenceEngine reativo | Detecção dispara quando qualquer fonte atualiza (via SnapshotStore event) — não espera ciclo completo; minimiza latência de detecção |
| 2026-05-26 | IOptionsMonitor<T> para intervalos de polling | Hot-reload via appsettings.json sem reiniciar app; UI settings em Phase 07 |
| 2026-05-26 | Channel<Divergence> para fila de alertas | Desacopla detecção de envio; substitui por RabbitMQ/Redis no futuro sem mudar código |
| 2026-05-26 | JSONL como storage MVP | Simples, sem dependência; trocar por SQLite = nova classe + trocar registro no DI |
| 2026-05-26 | PollingWorker<TOptions> base class | Evita repetição nos workers concretos; todos herdam comportamento de retry, logging, intervalo configurável |
