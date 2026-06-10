# AGENTS.md — Dashboard (Angular)

Dashboard do operador. Mostra jogos ao vivo por fonte, divergências, e snippets de verificação do Google. Leia também o [AGENTS.md raiz](../../AGENTS.md).

## Comandos
```powershell
npm install
ng serve                                  # dev em http://localhost:4200
npm run build -- --configuration production   # gera ../SportsMonitor.Bff/wwwroot
```
O build de produção emite os estáticos direto em `src/SportsMonitor.Bff/wwwroot` — o BFF os serve. Sempre rode o build antes de commitar mudanças de UI, e garanta que o `wwwroot` commitado bate com o build.

## Arquitetura (Angular standalone + signals)
- `alert.service.ts` — estado central via `signal()`/`computed()`, SignalR (`/hubs/alerts`), polling de `/api/matches/live`, `/api/providers/status`, `/api/google-results`.
- Tempo real: `ReceiveAlert` toca o apito (WebAudio, 3 apitos de árbitro).

## Convenções de agrupamento (não regredir)
- Partidas agrupam por `matchId` (vem do backend: `SHA256(times+hora)`), depois por competição.
- Ao escolher o nome da competição de um grupo, **preferir o primeiro nome não-numérico** entre as fontes (`snapList.map(s => s.competition).find(c => c && !/^\d+$/.test(c))`) — evita mostrar IDs numéricos do 365Scores.
- Grupos cujo nome de competição é só dígitos são filtrados fora.
- Alertas ativos excluem `Confirmed`, `FalsePositive`, `Ignored`.
