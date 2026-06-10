# AGENTS.md — SportsMonitor

Guia canônico para agentes de IA e devs neste repositório. Siga o `AGENTS.md` mais próximo do arquivo que você está editando (há versões aninhadas em `src/SportsMonitor.LocalAgent/` e `src/SportsMonitor.Web/`). Prompts explícitos do usuário sempre têm prioridade.

## O que é o projeto

Monitor de divergência de dados esportivos ao vivo. Compara placar, gols, cartões e status de uma mesma partida entre fontes (365Scores, SofaScore, Google) e dispara um alerta sonoro + card no dashboard quando elas discordam. **Apoia** a verificação manual de um analista — nunca decide nem aposta.

- **Produção:** http://34.151.245.70/ (GCP, projeto `sportsmonitor-prod`)
- Visão de produto e fluxo do analista: [docs/architecture.md](docs/architecture.md)

## Regras de ouro (inegociáveis)

- **Nunca** automatizar apostas, login, CAPTCHA, ou burlar paywalls/anti-bot/controles de acesso.
- Apenas dados publicamente acessíveis. Sem spoofing de fingerprint — quando precisamos de um navegador real, usamos Chrome real, não disfarce.
- **Segredos fora do Git.** Credenciais vivem em `appsettings.Production.json` (gitignored) ou variáveis de ambiente.
- O sistema alerta; o humano decide e age.

## Stack

.NET 10 (ASP.NET Core BFF + Worker Services + SignalR) · Angular · xUnit · Playwright (SofaScore) · SQLite/JSONL para histórico. Solution: `src/SportsMonitor.slnx`.

## Comandos

```powershell
# Testes (sempre antes de commitar)
dotnet test src\SportsMonitor.slnx

# Rodar o BFF (dashboard em http://localhost:5000)
dotnet run --project src\SportsMonitor.Bff\SportsMonitor.Bff.csproj

# Dashboard em dev (Angular)
cd src\SportsMonitor.Web; npm install; ng serve     # http://localhost:4200

# Build de produção do Angular (gera src/SportsMonitor.Bff/wwwroot)
cd src\SportsMonitor.Web; npm run build -- --configuration production

# Deploy completo no GCP (BFF + Web + LocalAgent zip)
.\deploy-gcp.ps1 -ProjectId sportsmonitor-prod

# LocalAgent (scraping do SofaScore — roda na máquina do operador)
.\publish-local-agent.ps1        # gera publish-local-agent\ + local-agent.zip
```

## Mapa da solução

| Projeto | Responsabilidade |
|---|---|
| `SportsMonitor.Domain` | Modelos e interfaces |
| `SportsMonitor.Application` | DivergenceEngine + regras de divergência |
| `SportsMonitor.Infrastructure` | Providers (365Scores, SofaScore, Google), resolvers, stores |
| `SportsMonitor.Workers` | Polling por fonte + AlertWorker + GoogleSearchWorker + DemoWorker |
| `SportsMonitor.Bff` | Host ASP.NET Core: API REST, SignalR, serve o dashboard |
| `SportsMonitor.Web` | Dashboard Angular — ver [AGENTS aninhado](src/SportsMonitor.Web/AGENTS.md) |
| `SportsMonitor.LocalAgent` | Relay do SofaScore via Playwright — ver [AGENTS aninhado](src/SportsMonitor.LocalAgent/AGENTS.md) |
| `SportsMonitor.Tests` | xUnit |

## Fontes de dados

Detalhes, endpoints e a saga anti-bloqueio do SofaScore: [docs/providers.md](docs/providers.md).

- **365Scores** — HTTP GET direto, sem auth. Placar/status.
- **SofaScore** — bloqueado por IP de datacenter; chega via **LocalAgent** (roda no IP residencial do operador) que faz POST em `/api/relay/sofascore`. Placar, gols, cartões.
- **Google Custom Search** — snippets/links de verificação. Precisa de API key + Search Engine ID.

## Convenções

- **GitFlow sem PRs:** trabalho direto em `develop`, merges com `--no-ff` para preservar histórico de branch.
- Mensagens de commit terminam com `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.
- Antes de commitar produção: rode os testes, confira `git diff`, garanta que `appsettings.Production.json` continua untracked.
- Fluxo de release e validação em produção: [docs/deployment.md](docs/deployment.md).

## Protocolo de continuidade entre modelos de IA

Este projeto troca de modelo/agente. **Não confie só na memória do chat.** Antes de encerrar um trabalho relevante, registre o que foi aprendido nos docs vivos:

- Decisões técnicas/produto → [docs/decisions.md](docs/decisions.md)
- Aprendizado de fonte/provider → [docs/providers.md](docs/providers.md)
- Deploy/operação → [docs/deployment.md](docs/deployment.md)
- Arquitetura → [docs/architecture.md](docs/architecture.md)

Histórico anterior (fases de pesquisa, session logs) preservado em [docs/history/](docs/history/).
