# Decisões

Log de decisões técnicas e de produto. Mais recentes no topo. Adicione novas aqui ao aprender algo que muda como o projeto funciona.

| Data | Decisão | Motivo |
|---|---|---|
| 2026-06-10 | Migrar contexto de IA para o padrão **AGENTS.md** | Padrão de mercado (Agentic AI Foundation/Linux Foundation), lido por Codex/Cursor/Copilot/Gemini/Claude Code. Substitui a estrutura de fases + `PROJECT_CONTEXT.md` + `ai-notes/`. Histórico arquivado em `docs/history/`. |
| 2026-06-10 | SofaScore: **navegação top-level** como transporte principal + backoff de reputação | `fetch()` cross-origin manda `Origin`/`Sec-Fetch-Site: cors` e toma 403; navegação (`GotoAsync`) passa. IP residencial é flagado por polling agressivo + headless. Solução: Chrome real headful, intervalo 90s, backoff exponencial. Ver [providers.md](providers.md). |
| 2026-06-10 | SofaScore distribuído como **ZIP**, não single-exe | Playwright quebra em single-file (`Assembly.Location` vazio → driver não encontrado). |
| 2026-06-09 | SofaScore via **LocalAgent relay** (não no BFF) | IP de datacenter do GCP é bloqueado pelo edge anti-bot. LocalAgent roda no IP residencial e faz POST em `/api/relay/sofascore`. |
| 2026-06-07 | 365Scores **não** persiste `RawJson` no histórico | Payload gigante estourou o disco de 20GB da VM. Campos normalizados bastam para comparação. |
| 2026-06-07 | Segredos de produção **fora do Git** | `appsettings.Production.json` e variáveis de ambiente são o caminho suportado. |
| 2026-06-07 | Alertas ativos excluem Confirmed/FalsePositive/Ignored | Divergências resolvidas/descartadas não devem continuar pulsando. |
| 2026-06-07 | GCP é **CLI-first** | Criação de VM, firewall, SSH, deploy via `gcloud`/scripts, não Console web. |
| 2026-06-07 | Deploy Linux/GCP = BFF self-contained + systemd + nginx | Mantém a VM simples: app sob systemd em localhost:5000, nginx expõe HTTP/SignalR na porta 80. |
| 2026-05-26 | Local-first / desktop-first | Evitar custo recorrente de nuvem; core Angular garante migração web trivial. |
| 2026-05-26 | matchId por `times + horário` (sem competição no hash) | IDs internos divergem entre fontes; nome de competição também diverge e quebraria o agrupamento. |
| 2026-05-26 | Workers independentes por fonte + DivergenceEngine reativo | Detecção dispara quando qualquer fonte atualiza; minimiza latência. |
| 2026-05-26 | Endpoints internos de SofaScore/365Scores são aceitáveis para comparação | Usuário aceitou o risco de ToS/fragilidade pela fidelidade do dado. Limite: dados públicos, sem login bypass/CAPTCHA/spoofing. |
| 2026-05-26 | Apito sonoro é obrigatório no MVP | Confirmado pelo usuário. |
| 2026-05-26 | Aposta é 100% manual | O sistema alerta; o humano decide e age. |
| 2026-05-26 | Manter arquivos de continuidade para handoff entre IAs | O projeto troca de modelo/agente; não confiar só na memória do chat. |
| 2026-05-26 | Site oficial da competição = fonte de referência preferencial | Confirmado pelo usuário como a verdade. |

> Histórico completo de decisões (incluindo avaliação de provedores pagos, odds APIs e bookmakers da fase de pesquisa) em [history/DECISIONS.md](history/DECISIONS.md).
