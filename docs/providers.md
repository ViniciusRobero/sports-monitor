# Fontes de dados (Providers)

## Resumo

| Fonte | Acesso | Auth | Dados | Status |
|---|---|---|---|---|
| 365Scores | HTTP GET direto | Não | Placar, status | ✅ Ativo |
| SofaScore | Playwright via **LocalAgent** | Não | Placar, gols, cartões, incidentes | ✅ Ativo (relay) |
| Google Custom Search | API JSON | API key + Search Engine ID | Snippets/links de verificação | ✅ Ativo |
| API-Football | HTTP | Paga ($19+/mês) | Eventos estruturados | ❌ Fora de escopo |
| BetsAPI | HTTP | Paga | Bet365 odds + suspensão | ❌ Fora de escopo |

Limite: apenas dados públicos. Sem login, CAPTCHA, ou bypass de controle de acesso.

## 365Scores

- Base: `https://webws.365scores.com`
- Live: `/web/games/?appTypeId=5&langId=31&timezoneName=America%2FSao_Paulo&userCountryId=-1&onlyLive=true`
- Sem auth; headers de navegador (User-Agent + Accept). Retornou HTTP 200 consistente.
- Provider lê o nome da competição de `competitionDisplayName` (string direta no game). **Não** usar `competitionId` (número) nem `competition.name` (não existe). Sem incidentes individuais — só placar/status.
- Não persiste `RawJson` (payload grande estourou o disco da VM em produção).

## SofaScore — arquitetura relay + saga anti-bloqueio

### Por que LocalAgent
`api.sofascore.com` é protegida por edge anti-bot (Fastly/Varnish + desafio tipo Cloudflare). **IPs de datacenter (GCP) são bloqueados na hora.** Logo, o BFF na nuvem não consegue acessar. A solução é o **LocalAgent**: roda na máquina residencial do operador (IP com boa reputação), faz o scraping e envia via `POST http://<bff>/api/relay/sofascore`. O BFF re-resolve o matchId via `SofaScoreRelayController` para corrigir versões antigas do agente.

Distribuído como **ZIP** (não single-exe — Playwright quebra em single-file porque `Assembly.Location` fica vazio). `publish-local-agent.ps1` gera a pasta + `local-agent.zip`, servido em `/downloads/local-agent.zip`.

### Como a busca funciona (transport ladder)
Resolver os 403 exigiu entender que **a API só aceita certos formatos de requisição**. O provider tenta, em ordem, parando no primeiro que funciona (e lembra qual foi):

1. **HttpClient .NET** — GET simples. Costuma tomar 403 pelo fingerprint TLS (JA3) do .NET no edge Fastly.
2. **Navegação top-level** (`page.GotoAsync` + `IResponse.TextAsync`) — **é a que funciona.** Uma navegação manda `Sec-Fetch-Mode: navigate` sem `Origin`, exatamente como abrir a URL no navegador.
3. **Playwright APIRequest** — cookies da sessão, sem Origin.
4. **fetch() in-page** — manda `Origin`/`Sec-Fetch-Site: cors` → **403**.
5. **Interceptar** a XHR `/events/live` que a própria página dispara.

Endpoints: `/api/v1/sport/football/events/live` e `/api/v1/event/{id}/incidents`.

### Aprendizados críticos (não regredir)
- **Navegação top-level passa; `fetch()` cross-origin não** — o `Origin`/`Sec-Fetch-Site: cors` dispara o 403 da API. Não voltar a depender de `fetch()` in-page como caminho principal.
- **Reputação de IP é real e frágil.** Polling agressivo (30s por horas) + Chromium headless **derrubam a reputação do IP** e geram 403 `challenge` até no navegador manual. Por isso: **headful com Chrome real** (`Channel=chrome`, nunca headless), intervalo **90s**, e **backoff exponencial** (2→4→8→16 min, teto 30) ao bloquear. Reseta no primeiro sucesso.
- **Proxy/VPN corporativo** (ex: Palo Alto GlobalProtect) pode alterar o caminho de rede e causar bloqueio mesmo em IP residencial. Teste de isolamento: comparar o IP público e testar a URL da API no 5G do celular vs Wi-Fi.
- **Sem token/auth.** A API é pública; 403 é sempre reputação/fingerprint, nunca falta de credencial (a resposta vem como `403 challenge`/`Forbidden`, não `401`).
- **Sem stealth plugins** — Chrome real é a solução, não disfarce.

### Iteração no SofaScore
O provider roda **inteiramente dentro do LocalAgent** (máquina do operador). Para testar mudanças no scraping, **recompile local** (`publish-local-agent.ps1` ou `dotnet publish` em `publish-local-agent\`) e rode `start-local-agent.bat`. **Não** faça deploy GCP a cada iteração — o BFF na nuvem só recebe o relay. Deploy GCP do ZIP só quando o fix estiver confirmado.

Mapeamento JSON isolado em `SofaScoreMapper` (testável sem Playwright; `InternalsVisibleTo("SportsMonitor.Tests")`).

## Google Custom Search

- Precisa de API key + Search Engine ID. Search Engine ID conhecido: `25c69f98aa10d4ba0`.
- Gerar key restrita via `setup-google-search-key.ps1 -ProjectId <id>` (habilita a API, cria key restrita ao Custom Search, escreve `appsettings.Production.json` gitignored).
- Polling 300s (quota/custo). É fonte de **verificação** (snippets/links), não placar estruturado de baixa latência.
- Links do Google levam `?authuser=1` (a conta `viniciusroberto17@gmail.com` é o índice 1).

> Endpoints internos detalhados e schema JSONL histórico: [history/PHASE_01_COMPARISON_SOURCES_RESEARCH.md](history/PHASE_01_COMPARISON_SOURCES_RESEARCH.md).
