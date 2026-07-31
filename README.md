# SportsMonitor

Painel de monitoramento de partidas ao vivo que compara múltiplas fontes e alerta quando os dados divergem.

O sistema observa placar, gols, cartões e status de cada partida em todas as fontes configuradas. Quando duas fontes discordam — por exemplo, uma mostra gol de Pedro e outra mostra gol de Arrascaeta — aparece um alerta com severidade, badges por fonte e links de apoio do Google para verificação manual.

> **Contribuindo / trabalhando com IA?** A documentação técnica segue o padrão [AGENTS.md](AGENTS.md). Detalhes em [docs/](docs/): [arquitetura](docs/architecture.md) · [fontes/providers](docs/providers.md) · [deploy](docs/deployment.md) · [decisões](docs/decisions.md).

---

## Para que serve

Durante uma partida ao vivo fontes diferentes podem divergir por segundos ou por dados errados. O SportsMonitor responde perguntas simples em tempo real:

- O placar está igual em todas as fontes?
- O gol apareceu em todas? O jogador é o mesmo?
- O cartão foi para o mesmo jogador?
- A partida está ao vivo, no intervalo ou encerrada?

Quando algo parece estranho, o painel toca um aviso sonoro e cria um card de alerta.

---

## Arquitetura

```
[SofaScore]  ←── LocalAgent (PC residencial) ──→  POST /api/relay/sofascore
[365Scores]  ←─────────────────────────────────→  BFF (GCP VM)  ──→  Dashboard Angular
[Google]     ←─────────────────────────────────→  BFF (GCP VM)
```

**Por que LocalAgent?**
O SofaScore usa Cloudflare Bot Management e bloqueia requisições de IPs de datacenter com erro 403. O LocalAgent roda na máquina residencial do operador, burla a detecção com navegação real via Playwright e envia os dados ao BFF em nuvem.

**Estratégia de coleta (LocalAgent):**
1. Tenta HTTP direto para a API pública.
2. Se houver bloqueio, usa navegação normal no Chrome real.
3. Aplica backoff progressivo para preservar a reputação do IP.

---

## Fontes configuradas

| Fonte | Dados fornecidos | Habilitada por padrão |
|---|---|:---:|
| SofaScore via LocalAgent | Placar, gols, cartões, incidentes | Sim |
| 365Scores | Placar e status | Sim |
| Google Search | Links e trechos para verificação | Sim |
| API-Football | Eventos estruturados | Não |
| BetsAPI / Bet365 | Placar e referência operacional | Não |

---

## Intervalos de polling

| Fonte | Intervalo |
|---|---:|
| SofaScore (LocalAgent → BFF) | 90 segundos |
| 365Scores | 20 segundos |
| Google Search | 300 segundos |

---

## Deployment (produção)

O servidor GCP antigo está desativado. Os comandos abaixo são preservados
apenas como referência para uma eventual nova implantação no Google Cloud;
não execute deploy até o novo servidor ser definido.

### Deploy completo

```powershell
.\deploy-gcp.ps1 -ProjectId sportsmonitor-prod
```

Esse script:
1. Faz build do Angular (`ng build --configuration production`)
2. Publica o BFF para `linux-x64`
3. Sobe os arquivos via `gcloud compute scp`
4. Instala o serviço systemd e reinicia nginx + app
5. Faz upload do `local-agent.zip` para `/downloads/`

### Publicar apenas o LocalAgent

```powershell
.\publish-local-agent.ps1
```

O relay exige uma chave privada. Configure o mesmo valor em
`RelayOptions__AgentKey` no servidor e em `AgentKey` no
`appsettings.json` do LocalAgent. Nunca salve a chave no Git.

Cria `local-agent.zip` com o binário Windows x64 + Playwright + `start-local-agent.bat`.  
O ZIP é copiado para `src/SportsMonitor.Bff/wwwroot/downloads/` e servido em `/downloads/local-agent.zip` após o deploy.

---

## Como rodar em desenvolvimento

Requisitos: .NET 10 SDK, Node.js 18+, Angular CLI

```powershell
# Dependências do frontend
cd src\SportsMonitor.Web
npm install

# BFF
dotnet run --project src\SportsMonitor.Bff\SportsMonitor.Bff.csproj

# Dashboard (em outro terminal)
cd src\SportsMonitor.Web
ng serve
```

Abra `http://localhost:4200`

---

## LocalAgent — instalação no PC do operador

O LocalAgent precisa rodar na máquina residencial do operador.

**Download:** será disponibilizado quando o novo servidor for escolhido.
Durante os testes, gere o pacote com `.\publish-local-agent.ps1`.

**Instalação:**
1. Extraia o ZIP em qualquer pasta (ex: `C:\SportsMonitor\`)
2. Edite `appsettings.json` e confirme que `BffUrl` aponta para o servidor
3. Execute `start-local-agent.bat` (abre console com logs visíveis)

Na primeira execução, o Chromium (~150 MB) é baixado automaticamente na subpasta `.playwright\`.

**Logs esperados:**
```
Verificando navegador (pode baixar ~150MB na primeira vez)...
Navegador OK.
Iniciando monitoramento...
2026-01-01 00:00:00 HH:mm:ss dbug: SofaScore: strategy 1 captured 32KB
2026-01-01 00:00:00 relayed 8 SofaScore matches.
```

Se aparecer `strategy 1 blocked`, o agente tenta automaticamente a estratégia de intercepção via página.

---

## Modo demonstração

Para testar sem APIs reais, habilite o modo demo em `appsettings.json`:

```json
"Demo": { "Enabled": true }
```

Cria partidas e divergências simuladas (placar atrasado, gol divergente, cartão divergente, etc.).

---

## Estrutura do projeto

| Projeto | Responsabilidade |
|---|---|
| `SportsMonitor.Bff` | Servidor .NET — agrega dados, serve API + SignalR + dashboard |
| `SportsMonitor.Web` | Dashboard Angular — interface do operador |
| `SportsMonitor.Infrastructure` | Provedores de dados (SofaScore, 365Scores, Google) |
| `SportsMonitor.Workers` | Background workers — polling de cada fonte |
| `SportsMonitor.Application` | Regras de divergência (placar, gols, cartões, status) |
| `SportsMonitor.Domain` | Modelos e interfaces |
| `SportsMonitor.LocalAgent` | Agente Windows — relay do SofaScore via Playwright |
| `SportsMonitor.Tests` | Testes automatizados |

---

## O que o sistema não faz

- Não faz apostas automaticamente
- Não acessa conta da Bet365 ou qualquer plataforma de apostas
- Não tenta burlar login, captcha ou conta bloqueada
- Não garante que uma fonte está certa — alerta que existe diferença
- Não substitui confirmação humana
