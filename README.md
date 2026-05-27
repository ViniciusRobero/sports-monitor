# Sports Data Divergence Monitor

> 🇧🇷 [Leia em Português](#monitor-de-divergências-em-dados-esportivos) · 🇺🇸 [Read in English](#sports-data-divergence-monitor-1)

---

## Monitor de Divergências em Dados Esportivos

Sistema **local, desktop-first** que monitora partidas de futebol ao vivo em múltiplas fontes de dados, detecta divergências em tempo real e alerta o analista com um som ("apito") para que ele verifique manualmente e atue na Bet365.

> **Status:** MVP implementado — 4 provedores, 5 regras de divergência, dashboard Angular, shell WPF. 66 testes passando.

---

### O que faz

O sistema acompanha partidas ao vivo simultaneamente na **Bet365** (via BetsAPI), **SofaScore**, **API-Football** e **365Scores**. Quando as fontes divergem — marcador de gol diferente, placar diferente, cartão no jogador errado, status de partida inconsistente — dispara um alerta sonoro e exibe um card de divergência no dashboard.

O analista então busca evidências de replay no **Google**, confirma se o evento é real e decide se age manualmente na **Bet365**. **O sistema nunca realiza apostas automaticamente.**

### Exemplo

```
Partida: Flamengo x Palmeiras — 32'

SofaScore:    Gol de Pedro
Bet365:       Gol de Arrascaeta
API-Football: Gol de Pedro

→ ALERTA CRÍTICO: GoalScorerMismatch
  SofaScore + API-Football: Pedro
  Bet365: Arrascaeta
  → Buscar no Google → confirmar replay → agir manualmente na Bet365
```

---

### Arquitetura

```
Máquina do Usuário
│
├── Desktop Shell (WPF + WebView2)
│   └── Inicia o BFF automaticamente e abre http://localhost:5000
│
├── ASP.NET Core BFF (localhost:5000)
│   ├── REST API  GET /api/matches/live
│   │            GET /api/divergences
│   │            POST /api/divergences/{id}/verify
│   ├── SignalR Hub /hubs/alerts  ←── push em tempo real para o dashboard
│   └── Dashboard Angular
│
├── Worker Services .NET (polling em background)
│   ├── ApiFootballWorker   (padrão 30s — API oficial)
│   ├── BetsApiWorker       (padrão 30s — Bet365 via BetsAPI)
│   ├── SofaScoreWorker     (padrão 30s — API interna SofaScore)
│   ├── Scores365Worker     (padrão 20s — API interna 365Scores)
│   └── AlertWorker         (consome fila de divergências → SignalR)
│
├── In-Memory Snapshot Store   ← dispara SnapshotUpdated a cada atualização
├── Divergence Engine          ← reativo, avalia todas as regras a cada snapshot
│
└── Storage (JSONL por fonte por dia)
    data/2026-05-27/
      snapshots/api_football.jsonl
      snapshots/bet365.jsonl
      snapshots/sofascore.jsonl
      snapshots/365scores.jsonl
      divergences.jsonl
```

### Padrões de design

| Padrão | Onde | Propósito |
|---|---|---|
| Adapter | `IMatchDataProvider` | Adicionar fonte = adicionar uma classe |
| Strategy | `IDivergenceRule` | Adicionar tipo de divergência = adicionar uma classe |
| Strategy | `IAlertChannel` | Adicionar alerta Telegram/SMS = adicionar uma classe |
| Repository | `IMatchHistoryRepository` | JSONL agora, SQLite depois — uma linha no DI |
| Observer | `ISnapshotStore.SnapshotUpdated` | Detecção dispara instantaneamente na atualização |
| Options | `IOptionsMonitor<T>` | Intervalos de polling recarregáveis a quente via `appsettings.json` |

---

### Fontes de dados

| Fonte | Eventos (gols, cartões) | Placar | Método | Custo |
|---|---|---|---|---|
| **Bet365** (via BetsAPI) | ✅ marcadores, cartões | ✅ | API licenciada — `api.b365api.com` | Pago |
| **SofaScore** | ✅ incidentes completos | ✅ | API interna — `api.sofascore.com/api/v1` | Grátis |
| **API-Football** | ✅ eventos completos | ✅ | API oficial — `v3.football.api-sports.io` | $19/mês (Pro) |
| **365Scores** | ❌ apenas placar | ✅ | API interna — `webws.365scores.com/web/` | Grátis |

### Fontes de verificação (manual)

| Fonte | Papel |
|---|---|
| **Google** | O dashboard gera um link de busca com um clique para encontrar replay/highlights. Não automatizado — o analista verifica manualmente. |
| **Bet365** | Plataforma onde o analista age manualmente após confirmar a divergência. |

---

### Regras de divergência implementadas

| Regra | Severidade | O que detecta |
|---|---|---|
| `ScoreMismatchRule` | Crítica | Fontes reportam placares diferentes |
| `GoalScorerMismatchRule` | Crítica | Gol no mesmo minuto atribuído a jogadores diferentes |
| `MissingGoalRule` | Alta | Uma fonte tem um gol que a outra não tem |
| `CardMismatchRule` | Alta | Cartão amarelo ou vermelho atribuído a jogador diferente |
| `MatchStatusMismatchRule` | Média | Uma fonte diz ao vivo, outra diz encerrado/suspenso |

---

### Como rodar localmente (modo debug / desenvolvimento)

**Requisitos:** .NET 10 SDK · Node 18+ · Angular CLI 21+

#### Passo a passo

**1. Clone e instale dependências do Angular**
```bash
git clone https://github.com/ViniciusRobero/sports-monitor.git
cd sports-monitor/src/SportsMonitor.Web
npm install
```

**2. Habilite pelo menos um provedor em `src/SportsMonitor.Bff/appsettings.json`**

SofaScore e 365Scores não precisam de chave — é o mais rápido para testar:
```json
"SofaScore": { "Enabled": true, "PollingIntervalSeconds": 30 },
"Scores365": { "Enabled": true, "PollingIntervalSeconds": 20 }
```

**3. Inicie o BFF** (Terminal 1)
```bash
dotnet run --project src/SportsMonitor.Bff/SportsMonitor.Bff.csproj
```
Aguarde a mensagem `Now listening on: http://localhost:5000`.

**4. Inicie o dashboard Angular** (Terminal 2)
```bash
cd src/SportsMonitor.Web && ng serve
```
Aguarde `Application bundle generation complete`.

**5. Abra o dashboard**

`http://localhost:4200`

O dashboard conecta ao BFF via SignalR. Quando houver partidas ao vivo e uma divergência for detectada, um card aparece e um beep toca.

**Para testar os endpoints REST diretamente:**
```bash
curl http://localhost:5000/api/matches/live
curl http://localhost:5000/api/divergences
```

---

### Como gerar o pacote para uso sem ambiente de desenvolvimento

```powershell
.\publish.ps1
```

Gera a pasta `publish\` com dois arquivos. Basta executar `publish\SportsMonitor.Desktop.exe` — ele inicia o BFF automaticamente e abre o dashboard.

---

### Configuração (`src/SportsMonitor.Bff/appsettings.json`)

Todos os provedores ficam desabilitados por padrão. Habilite e configure as chaves antes de rodar:

```json
{
  "Providers": {
    "ApiFootball": {
      "Enabled": true,
      "PollingIntervalSeconds": 30,
      "ApiKey": "SUA_CHAVE_API_FOOTBALL"
    },
    "BetsApi": {
      "Enabled": true,
      "PollingIntervalSeconds": 30,
      "Token": "SEU_TOKEN_BETSAPI"
    },
    "SofaScore": {
      "Enabled": true,
      "PollingIntervalSeconds": 30
    },
    "Scores365": {
      "Enabled": true,
      "PollingIntervalSeconds": 20
    }
  }
}
```

---

### Estrutura da solução

```
src/
├── SportsMonitor.Domain/          # Entidades e interfaces — zero dependências externas
├── SportsMonitor.Application/     # DivergenceEngine + 5 regras
├── SportsMonitor.Infrastructure/  # 4 provedores, repositório JSONL, resolver, store
├── SportsMonitor.Workers/         # 5 workers hospedados (4 polling + 1 alerta)
├── SportsMonitor.Bff/             # Host ASP.NET Core — REST API + SignalR hub
├── SportsMonitor.Web/             # Dashboard Angular 21
├── SportsMonitor.Desktop/         # Shell WPF + WebView2
└── SportsMonitor.Tests/           # 66 testes xUnit
```

---

### Restrições importantes

- **Sem apostas automáticas** — o sistema alerta; o humano decide e age na Bet365
- **Sem bypass de login** — sem resolver CAPTCHA, sem fingerprint spoofing, sem Playwright stealth
- **Sem necessidade de nuvem** — roda inteiramente na máquina local
- **Acesso local:** `http://localhost:5000`

---

### Stack tecnológica

| Camada | Tecnologia |
|---|---|
| Backend | ASP.NET Core (.NET 10) |
| Workers | .NET `IHostedService` / `BackgroundService` |
| Tempo real | SignalR |
| Frontend | Angular 21 |
| Storage | Arquivos JSONL (MVP) |
| Shell desktop | WPF + WebView2 |
| Testes | xUnit + FluentAssertions |

---

## Sports Data Divergence Monitor

A **local-first, desktop-first** system that monitors live football/soccer matches across multiple data sources, detects divergences in real time, and alerts the analyst with an audible sound so they can manually verify and act on Bet365.

> **Status:** MVP implemented — 4 providers, 5 divergence rules, Angular dashboard, WPF desktop shell. 66 tests passing.

---

### What it does

The system watches live matches simultaneously on **Bet365** (via BetsAPI), **SofaScore**, **API-Football** and **365Scores**. When the sources disagree — different goal scorer, different score, missing event, card on the wrong player, inconsistent match status — it triggers an audible alert and shows a divergence card in the dashboard.

The analyst then searches for replay evidence on **Google**, confirms whether the event was real, and decides whether to act manually on **Bet365**. **The system never places bets automatically.**

### Example

```
Match: Flamengo vs Palmeiras — 32'

SofaScore:    Goal by Pedro
Bet365:       Goal by Arrascaeta
API-Football: Goal by Pedro

→ CRITICAL ALERT: GoalScorerMismatch
  SofaScore + API-Football: Pedro
  Bet365: Arrascaeta
  → Search on Google → confirm replay → act manually on Bet365
```

---

### Architecture

```
User Machine
│
├── Desktop Shell (WPF + WebView2)
│   └── Auto-starts BFF and opens http://localhost:5000
│
├── ASP.NET Core BFF (localhost:5000)
│   ├── REST API  GET /api/matches/live
│   │            GET /api/divergences
│   │            POST /api/divergences/{id}/verify
│   ├── SignalR Hub /hubs/alerts  ←── real-time push to dashboard
│   └── Angular Dashboard
│
├── .NET Worker Services (background polling)
│   ├── ApiFootballWorker   (default 30s — official API)
│   ├── BetsApiWorker       (default 30s — Bet365 via BetsAPI)
│   ├── SofaScoreWorker     (default 30s — SofaScore internal API)
│   ├── Scores365Worker     (default 20s — 365Scores internal API)
│   └── AlertWorker         (consumes divergence queue → SignalR)
│
├── In-Memory Snapshot Store   ← fires SnapshotUpdated on every update
├── Divergence Engine          ← reactive, evaluates all rules on each snapshot
│
└── Storage (JSONL per source per day)
    data/2026-05-27/
      snapshots/api_football.jsonl
      snapshots/bet365.jsonl
      snapshots/sofascore.jsonl
      snapshots/365scores.jsonl
      divergences.jsonl
```

### Data sources

| Source | Events (goals, cards) | Scores | Method | Cost |
|---|---|---|---|---|
| **Bet365** (via BetsAPI) | ✅ goal scorers, cards | ✅ | Licensed API — `api.b365api.com` | Paid |
| **SofaScore** | ✅ full incidents | ✅ | Internal API — `api.sofascore.com/api/v1` | Free |
| **API-Football** | ✅ full events | ✅ | Official API — `v3.football.api-sports.io` | $19/mo (Pro) |
| **365Scores** | ❌ score only | ✅ | Internal API — `webws.365scores.com/web/` | Free |

### Divergence rules

| Rule | Severity | Detects |
|---|---|---|
| `ScoreMismatchRule` | Critical | Sources report different scores |
| `GoalScorerMismatchRule` | Critical | Goal at same minute attributed to different players |
| `MissingGoalRule` | High | One source has a goal the other doesn't |
| `CardMismatchRule` | High | Yellow or red card attributed to different player |
| `MatchStatusMismatchRule` | Medium | One source says live, another says finished/postponed |

### Running locally (debug / dev mode)

**Requirements:** .NET 10 SDK · Node 18+ · Angular CLI 21+

**1. Install Angular dependencies**
```bash
cd src/SportsMonitor.Web && npm install
```

**2. Enable at least one provider** in `src/SportsMonitor.Bff/appsettings.json`

SofaScore and 365Scores require no API key — fastest way to test:
```json
"SofaScore": { "Enabled": true, "PollingIntervalSeconds": 30 },
"Scores365": { "Enabled": true, "PollingIntervalSeconds": 20 }
```

**3. Start the BFF** (Terminal 1)
```bash
dotnet run --project src/SportsMonitor.Bff/SportsMonitor.Bff.csproj
```

**4. Start the Angular dashboard** (Terminal 2)
```bash
cd src/SportsMonitor.Web && ng serve
```

**5. Open** `http://localhost:4200`

### Building a distributable package

```powershell
.\publish.ps1
```

Outputs a `publish\` folder. Run `publish\SportsMonitor.Desktop.exe` — it auto-starts the BFF and opens the dashboard.

### Important constraints

- **No automated betting** — the system alerts; the human decides and acts on Bet365
- **No login bypass** — no CAPTCHA solving, no fingerprint spoofing
- **No cloud required** — runs entirely on the local machine
