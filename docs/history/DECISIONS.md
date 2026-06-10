## 2026-06-07 - Production Cleanup Decisions

| Date | Decision | Reason |
|---|---|---|
| 2026-06-07 | 365Scores must not persist `RawJson` in history | The 365Scores payload is large and caused extreme growth in `/opt/sportsmonitor/data` on GCP. The source is used for score/status comparison, so normalized fields are enough for production history. |
| 2026-06-07 | GCP account focus is SportsMonitor | User asked to focus only on SportsMonitor. Compute/Run/SQL/GKE/Functions outside `sportsmonitor-prod` had no active resources. Old buckets existed in other projects, but broad cross-project bucket deletion was not executed because it is irreversible and safety-blocked. |
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
| 2026-05-26 | Direct bookmaker API integration excluded (Bet365, Betano, Sportingbet) | No public APIs; ToS proibem automaÃ§Ã£o |
| 2026-05-26 | BetsAPI = primary path for Bet365 live odds + suspension status | Ãšnica fonte confirmada com Bet365 live + suspensÃ£o a 3-5s |
| 2026-05-26 | The Odds API = primary multi-bookmaker odds aggregator | $29/mo Pro (pre-match) ou $99/mo Business (live + Pinnacle + 50+ books) |
| 2026-05-26 | Betfair Exchange API = free bookmaker live streaming | Ãšnico bookmaker com API oficial gratuita cobrindo live odds + status de mercado |
| 2026-05-26 | Pinnacle odds via The Odds API Business | API direta da Pinnacle fechada ao pÃºblico desde jul/2025; Brasil bloqueado |
| 2026-05-26 | API-Football = primary sports data API candidate | $19/mo Pro; live events 15s; 1200+ competiÃ§Ãµes; BrasileirÃ£o/Libertadores ok |
| 2026-05-26 | Sportmonks = sports data API alternative | â‚¬129/mo Worldwide needed for Brazil; melhor cobertura CONMEBOL; trial 14 dias |
| 2026-05-26 | SofaScore: integrar via api.sofascore.com/api/v1 | Endpoints internos bem documentados; sem auth; User-Agent browser + 25-30s polling |
| 2026-05-26 | 365Scores: integrar via webws.365scores.com/web/ | Endpoints internos confirmados; sem auth; appTypeId=5, langId=31, timezoneName=America/Sao_Paulo |
| 2026-05-26 | Google: nÃ£o integrar como fonte de dados â€” gerar link manual no dashboard | Sem endpoint JSON acessÃ­vel; SerpApi ($50+/mo) inviÃ¡vel para MVP; link manual Ã© suficiente para verificaÃ§Ã£o do analista |
| 2026-05-26 | HistÃ³rico de leituras por fonte obrigatÃ³rio | UsuÃ¡rio quer todos os dados coletados salvos (SQLite); schema: source_readings (uma linha por poll por fonte) + payload JSON bruto |
| 2026-05-26 | Match Resolver obrigatÃ³rio | IDs internos do SofaScore, 365Scores, API-Football sÃ£o diferentes para a mesma partida â€” normalizaÃ§Ã£o por nome de time + horÃ¡rio + competiÃ§Ã£o necessÃ¡ria |
| 2026-05-26 | Workers independentes por fonte + DivergenceEngine reativo | DetecÃ§Ã£o dispara quando qualquer fonte atualiza (via SnapshotStore event) â€” nÃ£o espera ciclo completo; minimiza latÃªncia de detecÃ§Ã£o |
| 2026-05-26 | IOptionsMonitor<T> para intervalos de polling | Hot-reload via appsettings.json sem reiniciar app; UI settings em Phase 07 |
| 2026-05-26 | Channel<Divergence> para fila de alertas | Desacopla detecÃ§Ã£o de envio; substitui por RabbitMQ/Redis no futuro sem mudar cÃ³digo |
| 2026-05-26 | JSONL como storage MVP | Simples, sem dependÃªncia; trocar por SQLite = nova classe + trocar registro no DI |
| 2026-05-26 | PollingWorker<TOptions> base class | Evita repetiÃ§Ã£o nos workers concretos; todos herdam comportamento de retry, logging, intervalo configurÃ¡vel |
| 2026-06-07 | Demo disabled by default for real-provider validation | Next phase is real data and GCP deployment; demo remains available by setting `Demo.Enabled=true` |
| 2026-06-07 | Enable SofaScore and 365Scores without paid keys | Both sources are free/no-auth comparison sources and provide enough divergence signal for MVP validation |
| 2026-06-07 | Google Custom Search polling set to 300s | Reduces free-tier quota pressure while still giving analyst verification snippets |
| 2026-06-07 | Production secrets must live outside Git | `appsettings.Production.json` and environment variables are the supported path; real keys must not be committed |
| 2026-06-07 | Linux/GCP deploy uses self-contained BFF + systemd + Nginx | Keeps VM setup simple: app runs under systemd at localhost:5000 and Nginx exposes HTTP/SignalR on port 80 |
| 2026-06-07 | Active alerts exclude Confirmed, FalsePositive, and Ignored | Confirmed or dismissed divergences should not keep pulsing cards or global alert counts |
| 2026-06-07 | GCP configuration and deployment are CLI-first | VM creation, firewall, SSH, production config, service setup, and deploy should be executed with `gcloud`, shell commands, and repository scripts instead of relying on Console web steps |
| 2026-06-07 | Persist important session information for AI handoff | User explicitly reminded that the project is changing AI models; important shared information must be stored in repo docs (`PROJECT_CONTEXT.md` and `/ai-notes/`) instead of relying on chat memory |
| 2026-06-07 | Do not reuse old Google API key from local history | It was found but failed Custom Search with 403 PERMISSION_DENIED. Reuse `SearchEngineId=25c69f98aa10d4ba0`, but create a fresh restricted API key in the selected GCP project via CLI |
| 2026-06-07 | Use a formal release workflow before GCP publication | User wants a repeatable correction -> tests -> code review -> commit -> GCP publication flow. The workflow is documented in `ai-notes/RELEASE_WORKFLOW.md` and must be updated as deployment knowledge improves |
| 2026-06-07 | Prioritize fastest realistic data updates | User explicitly wants the fastest possible updates. Use aggressive polling for free score providers (10s) while respecting quota/blocking risk; keep Google slower for verification; consider paid/licensed providers for reliable sub-10s latency |
| 2026-06-07 | SofaScore official external API needs access confirmation before integration | User shared official external docs for a Betting Odds operation. Local CLI tests to docs/openapi/swagger and probable betting-odds endpoint returned HTTP 403. The docs appear access-controlled/partner-gated or blocked from this environment, and the referenced operation is odds-focused, not live incidents. Do not assume auth/token format until Swagger security scheme or generated cURL is available |
| 2026-06-07 | SofaScore migrado para Microsoft.Playwright 1.60.0 (headless Chrome) | HTTP 403 era TLS fingerprinting do .NET HttpClient (JA3/JA4 diferente do Chrome real). Playwright executa fetch() dentro de processo Chromium genuÃ­no â€” fingerprint idÃªntico ao browser. Sem fingerprint spoofing explÃ­cito; Ã© o Chrome real. |
| 2026-06-07 | SofaScoreMapper extraÃ­do como classe interna separada | Permite testar o mapeamento JSON sem instanciar Playwright; testes usam SofaScoreMapper.MapMatch() diretamente. InternalsVisibleTo("SportsMonitor.Tests") no Infrastructure project. |
| 2026-06-07 | Playwright CLI no GCP usa Node.js embutido (.playwright/node/linux-x64/node) | O publish output sÃ³ contÃ©m playwright.ps1 (PowerShell), nÃ£o playwright.sh. O Playwright empacota Node.js prÃ³prio em .playwright/node/. SoluÃ§Ã£o: chamar diretamente o node embutido com cli.js para install-deps e install chromium â€” sem precisar de pwsh ou dotnet SDK no servidor. |
| 2026-06-07 | deploy-gcp.ps1: instalar.sh sem BOM via System.IO.File.WriteAllText com UTF8Encoding(false) | PowerShell 5.1 Set-Content -Encoding UTF8 escreve BOM (EF BB BF) que quebra o shebang #!/bin/bash no Linux. UTF8Encoding(false) garante UTF-8 sem BOM. |
| 2026-06-07 | deploy-gcp.ps1: rm -rf + mkdir antes do SCP, cp de sportsmonitor-upload/publish-linux/ | pscp (PuTTY SCP) no Windows: se destino existe como dir, aninha source dir dentro dele. Se destino nÃ£o existe, falha. SoluÃ§Ã£o: rm -rf && mkdir cria dir vazio, SCP cria publish-linux/ dentro, install.sh usa esse caminho explÃ­cito. |
| 2026-06-07 | GCP deploy completo em sportsmonitor-prod, IP 34.151.245.70 | VM e2-small Ubuntu 22.04, zone southamerica-east1-b. Chromium 148 instalado em /opt/sportsmonitor/.playwright/. ServiÃ§o systemd ativo. nginx proxy na porta 80. URL: http://34.151.245.70/ |

