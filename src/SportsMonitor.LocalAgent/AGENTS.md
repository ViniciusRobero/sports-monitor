# AGENTS.md — LocalAgent

Agente Windows que faz scraping do SofaScore no **IP residencial do operador** e relaya pro BFF na nuvem (que tem IP bloqueado). Leia também o [AGENTS.md raiz](../../AGENTS.md) e [docs/providers.md](../../docs/providers.md).

## Por que isto existe
`api.sofascore.com` bloqueia IPs de datacenter. Este agente roda na máquina do operador, busca os jogos e faz `POST /api/relay/sofascore`.

## Regras críticas (não regredir)
- **Navegação top-level é o transporte que funciona** (`page.GotoAsync` + `IResponse.TextAsync`). `fetch()` cross-origin toma 403 (manda `Origin`/`Sec-Fetch-Site: cors`). A ordem de tentativa: HttpClient → navegação → APIRequest → fetch in-page → intercept.
- **Chrome real headful** (`Channel=chrome`, `Headless=false`). Headless é detectado e derruba a reputação do IP.
- **Reputação de IP é frágil:** intervalo **90s** + **backoff exponencial** (2→4→8→16 min, teto 30) ao bloquear. Nunca voltar a polling agressivo nem headless.
- **Sem auth, sem stealth/spoofing.** A API é pública; 403 é sempre reputação/fingerprint.

## Iteração (NÃO faça deploy GCP por mudança aqui)
O scraping roda 100% nesta máquina; o BFF só recebe o relay. Para testar:

```powershell
.\publish-local-agent.ps1     # (na raiz) gera publish-local-agent\ + local-agent.zip
# rodar:
publish-local-agent\start-local-agent.bat
```

Deploy GCP do ZIP só quando o fix estiver confirmado funcionando.

## Distribuição
ZIP, nunca single-exe (Playwright quebra em single-file: `Assembly.Location` vazio).
