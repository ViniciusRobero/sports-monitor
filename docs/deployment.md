# Deploy & Operação

## Produção

Não há servidor de produção ativo. A VM GCP abaixo é histórica e está
indisponível; não execute os scripts de deploy até o novo destino ser definido.

| Campo | Valor |
|---|---|
| URL | indisponível |
| Project ID | `sportsmonitor-prod` |
| VM | `sportsmonitor-vm` (e2-small, Ubuntu 22.04) |
| Zone | `southamerica-east1-b` |
| Billing | `01FE90-8618C8-3CEE8F` |
| App | systemd `sportsmonitor` em `localhost:5000`, nginx proxy na porta 80 |

Se GCP voltar a ser escolhido, a operação é **CLI-first**
(gcloud/ssh/scp + scripts do repo), não Console web.

## Deploy

```powershell
.\deploy-gcp.ps1 -ProjectId sportsmonitor-prod
```

Faz: build do Angular → publish do BFF (linux-x64) → upload via scp → instala systemd + nginx → reinicia → sobe `local-agent.zip` para `/downloads/`.

**Quando NÃO fazer deploy GCP:** mudanças só no SofaScore/LocalAgent não precisam de deploy — o provider roda na máquina do operador. Recompile local e teste (ver [providers.md](providers.md#iteração-no-sofascore)). Deploy só para mudanças no BFF/Web/Workers, ou para publicar o ZIP final já validado.

## Validação em produção

```powershell
gcloud compute ssh sportsmonitor-vm --zone southamerica-east1-b --project sportsmonitor-prod --command "sudo systemctl status sportsmonitor --no-pager"
gcloud compute ssh sportsmonitor-vm --zone southamerica-east1-b --project sportsmonitor-prod --command "sudo journalctl -u sportsmonitor -f"
```

No novo servidor, checar: URL pública carrega, SignalR conecta, 365Scores
recebe dados em jogos ao vivo, ações de alerta funcionam
(Confirmar/Falso positivo/Ignorar/Ignorar todos), layout mobile usável e
SofaScore reportado como ativo, atrasado ou desconectado.

## Fluxo de release

`correção → testes → revisão → commit → publicação GCP → validação → atualizar docs`.

```powershell
dotnet test src\SportsMonitor.slnx                              # testes
npm run build -- --configuration production                     # (em src/SportsMonitor.Web) build do dashboard
git status --short; git diff                                    # revisão
```

Revisão: nenhum segredo commitado, `appsettings.Production.json` continua untracked, assets Angular em `wwwroot` batem com o último build, config de provider correta, mudanças não-relacionadas preservadas.

**GitFlow:** trabalho direto em `develop`, merges `--no-ff`, **sem PRs**. Commits terminam com `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.

## Segredos & credenciais

- `appsettings.Production.json` (raiz, gitignored) guarda credenciais de produção. **Nunca** commitar.
- **Relay SofaScore:** gere uma chave aleatória forte e configure o mesmo valor em
  `RelayOptions__AgentKey` no servidor e em `AgentKey` no `appsettings.json`
  distribuído ao LocalAgent. Sem essa chave, o endpoint recusa todos os envios.
- **Google:** `setup-google-search-key.ps1 -ProjectId sportsmonitor-prod` cria a key e escreve o appsettings. Search Engine ID: `25c69f98aa10d4ba0`. Não reusar a key antiga (deu 403).
- **API-Football:** `setup-api-football.ps1` solicita a chave sem exibi-la,
  preserva as outras configurações e ativa o provider. Para o teste ao vivo, o
  intervalo padrão de 60s permite uma sessão de até 90 minutos com 10 das 100
  consultas diárias preservadas como reserva. Uma chamada traz todos os jogos
  ao vivo; pare o monitor ao final da sessão.
- **365Scores / SofaScore:** sem token.
- **BetsAPI:** paga, fora do escopo atual.

## Validação local antes de subir

- Testes: `dotnet test src\SportsMonitor.slnx` (78 passando atualmente).
- Se mexeu em scripts `.ps1` de deploy, valide sintaxe com `[System.Management.Automation.Language.Parser]::ParseFile(...)` antes de commitar.

> Workflow e guia de credenciais originais preservados em [history/](history/) (`RELEASE_WORKFLOW.md`, `CREDENTIALS_GUIDE.md`).
