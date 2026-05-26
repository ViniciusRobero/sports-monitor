# Phase 03 — Technical Architecture
## Sports Data Divergence Monitor

> Date: 2026-05-26
> Status: Approved
> Princípio: simples, extensível, fácil de modificar

---

## 1. Visão geral

```
┌─────────────────────────────────────────────────────────────┐
│                    User Machine                             │
│                                                             │
│  ┌──────────────┐    ┌──────────────────────────────────┐  │
│  │ Desktop Shell│    │    ASP.NET Core (localhost:5000)  │  │
│  │  WPF/WinForms│───▶│  ┌──────────┐  ┌─────────────┐  │  │
│  │  + WebView2  │    │  │ SignalR  │  │  REST API   │  │  │
│  └──────────────┘    │  │   Hub    │  │    (BFF)    │  │  │
│                      │  └────┬─────┘  └─────────────┘  │  │
│                      │       │         ┌─────────────┐  │  │
│                      │       │         │   Angular   │  │  │
│                      │       │         │  Dashboard  │  │  │
│                      │  ┌────▼──────────────────────┐│  │  │
│                      │  │     .NET Worker Services  ││  │  │
│                      │  │  SofaScoreWorker           ││  │  │
│                      │  │  Api365ScoresWorker        ││  │  │
│                      │  │  ApiFootballWorker         ││  │  │
│                      │  │  BetfairStreamWorker       ││  │  │
│                      │  │  AlertWorker               ││  │  │
│                      │  └────────────────────────────┘│  │  │
│                      │       │                         │  │  │
│                      │  ┌────▼──────┐  ┌───────────┐  │  │  │
│                      │  │ Snapshot  │  │   JSONL   │  │  │  │
│                      │  │  Store    │  │  /SQLite  │  │  │  │
│                      │  │(in-memory)│  │  (disco)  │  │  │  │
│                      │  └───────────┘  └───────────┘  │  │  │
│                      └──────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. Estrutura da solution .NET

```
SportsMonitor.sln
├── SportsMonitor.Domain/
├── SportsMonitor.Application/
├── SportsMonitor.Infrastructure/
├── SportsMonitor.Workers/
├── SportsMonitor.Bff/
├── SportsMonitor.Web/          ← Angular (projeto separado, servido pelo Bff)
└── SportsMonitor.Desktop/
```

### Regra de dependência

```
Desktop → Bff → Application → Domain
Workers → Application → Domain
Infrastructure implementa interfaces de Domain/Application
```

`Domain` e `Application` não referenciam nenhum projeto externo.

---

## 3. Domain — interfaces e entidades

Nenhuma dependência externa. Apenas modelos e contratos.

### Entidades principais

```csharp
// Partida normalizada (fonte independente)
public record NormalizedMatch(
    string MatchId,          // ID interno do sistema (gerado pelo Resolver)
    string HomeTeam,
    string AwayTeam,
    string Competition,
    DateTime KickOff,
    int HomeScore,
    int AwayScore,
    MatchStatus Status,
    IReadOnlyList<MatchEvent> Events,
    string Source,           // "sofascore" | "365scores" | "api_football"
    DateTime CollectedAt
);

public record MatchEvent(
    EventType Type,          // Goal, YellowCard, RedCard, Substitution
    int Minute,
    string PlayerName,
    string Team             // "home" | "away"
);

public record Divergence(
    string MatchId,
    DivergenceType Type,
    Severity Severity,
    string SourceA,
    string SourceAValue,
    string SourceB,
    string SourceBValue,
    string? OfficialSourceValue,
    DateTime DetectedAt
);
```

### Interfaces principais

```csharp
// Fonte de dados
public interface IMatchDataProvider
{
    string Name { get; }
    Task<IReadOnlyList<NormalizedMatch>> GetLiveMatchesAsync(CancellationToken ct);
}

// Armazenamento in-memory de snapshots
public interface ISnapshotStore
{
    void Upsert(NormalizedMatch match);
    event Action<NormalizedMatch> SnapshotUpdated;
    IReadOnlyList<NormalizedMatch> GetAllForMatch(string matchId);
    IReadOnlyList<string> GetLiveMatchIds();
}

// Regra de divergência (Strategy)
public interface IDivergenceRule
{
    IEnumerable<Divergence> Check(NormalizedMatch a, NormalizedMatch b);
}

// Canal de alerta (Strategy)
public interface IAlertChannel
{
    Task SendAsync(Divergence divergence, CancellationToken ct);
}

// Histórico em disco
public interface IMatchHistoryRepository
{
    Task SaveSnapshotAsync(NormalizedMatch match, string rawJson, CancellationToken ct);
    Task SaveDivergenceAsync(Divergence divergence, CancellationToken ct);
}

// Correlação de partidas entre fontes
public interface IMatchResolver
{
    string? ResolveMatchId(string sourceMatchId, string source,
                           string homeTeam, string awayTeam,
                           DateTime kickOff, string competition);
}
```

---

## 4. Application — lógica de negócio

### DivergenceEngine

Não é um worker. É um serviço chamado reativamente pelo `SnapshotStore`.

```csharp
public class DivergenceEngine
{
    private readonly IEnumerable<IDivergenceRule> _rules;
    private readonly ISnapshotStore _store;
    private readonly Channel<Divergence> _alertQueue;
    private readonly IMatchHistoryRepository _history;

    // Chamado quando qualquer snapshot atualiza
    public async Task EvaluateAsync(NormalizedMatch updatedMatch)
    {
        var others = _store.GetAllForMatch(updatedMatch.MatchId)
                           .Where(m => m.Source != updatedMatch.Source);

        foreach (var other in others)
        foreach (var rule in _rules)
        foreach (var divergence in rule.Check(updatedMatch, other))
        {
            await _history.SaveDivergenceAsync(divergence);
            await _alertQueue.Writer.WriteAsync(divergence);
        }
    }
}
```

### Regras de divergência (MVP — 6 regras)

```csharp
// Registo no DI — adicionar nova regra = nova classe + nova linha aqui
services.AddScoped<IDivergenceRule, ScoreMismatchRule>();
services.AddScoped<IDivergenceRule, GoalScorerMismatchRule>();
services.AddScoped<IDivergenceRule, MissingGoalRule>();
services.AddScoped<IDivergenceRule, YellowCardMismatchRule>();
services.AddScoped<IDivergenceRule, RedCardMismatchRule>();
services.AddScoped<IDivergenceRule, MatchStatusMismatchRule>();
```

Exemplo de regra:

```csharp
public class ScoreMismatchRule : IDivergenceRule
{
    public IEnumerable<Divergence> Check(NormalizedMatch a, NormalizedMatch b)
    {
        if (a.HomeScore != b.HomeScore || a.AwayScore != b.AwayScore)
            yield return new Divergence(
                a.MatchId, DivergenceType.ScoreMismatch, Severity.Critical,
                a.Source, $"{a.HomeScore}-{a.AwayScore}",
                b.Source, $"{b.HomeScore}-{b.AwayScore}",
                null, DateTime.UtcNow
            );
    }
}
```

---

## 5. Infrastructure

### Providers (Adapter por fonte)

```csharp
public class SofaScoreProvider : IMatchDataProvider
{
    public string Name => "sofascore";

    // GET https://api.sofascore.com/api/v1/sport/football/events/live
    // GET https://api.sofascore.com/api/v1/event/{id}/incidents
    public async Task<IReadOnlyList<NormalizedMatch>> GetLiveMatchesAsync(CancellationToken ct)
    {
        // 1. Busca partidas ao vivo
        // 2. Para cada partida, busca incidentes
        // 3. Mapeia para NormalizedMatch
        // 4. Usa IMatchResolver para correlacionar IDs
    }
}

public class Api365ScoresProvider : IMatchDataProvider
{
    public string Name => "365scores";
    // GET https://webws.365scores.com/web/game/?...&gameId={id}
}

public class ApiFootballProvider : IMatchDataProvider
{
    public string Name => "api_football";
    // GET https://v3.football.api-sports.io/fixtures?live=all
}
```

Adicionar nova fonte = criar nova classe que implementa `IMatchDataProvider` + registrar no DI.

### SnapshotStore (in-memory)

```csharp
public class InMemorySnapshotStore : ISnapshotStore
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, NormalizedMatch>> _data = new();
    // key: matchId → source → NormalizedMatch

    public event Action<NormalizedMatch>? SnapshotUpdated;

    public void Upsert(NormalizedMatch match)
    {
        _data.AddOrUpdate(...);
        SnapshotUpdated?.Invoke(match);  // dispara DivergenceEngine imediatamente
    }
}
```

### MatchHistoryRepository — JSONL (MVP)

```csharp
public class JsonlMatchHistoryRepository : IMatchHistoryRepository
{
    // /data/{yyyy-MM-dd}/{source}.jsonl
    // Uma linha por snapshot: { "ts": "...", "matchId": "...", "raw": "..." }

    public async Task SaveSnapshotAsync(NormalizedMatch match, string rawJson, CancellationToken ct)
    {
        var path = Path.Combine("data", DateTime.UtcNow.ToString("yyyy-MM-dd"),
                                $"{match.Source}.jsonl");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var line = JsonSerializer.Serialize(new { ts = match.CollectedAt, match, raw = rawJson });
        await File.AppendAllTextAsync(path, line + "\n", ct);
    }
}
```

Trocar por SQLite = criar `SqliteMatchHistoryRepository : IMatchHistoryRepository` e trocar o registro no DI. Zero mudança no resto do código.

### AlertChannels

```csharp
// MVP
public class SignalRAlertChannel : IAlertChannel
{
    private readonly IHubContext<AlertHub> _hub;

    public async Task SendAsync(Divergence divergence, CancellationToken ct)
        => await _hub.Clients.All.SendAsync("DivergenceDetected", divergence, ct);
    // Angular recebe via SignalR e toca o som
}

// Futuro — adicionar sem mudar nada
public class TelegramAlertChannel : IAlertChannel { ... }
public class DiscordAlertChannel : IAlertChannel { ... }
```

---

## 6. Workers

### Configuração de intervalos (IOptionsMonitor)

```json
// appsettings.json
{
  "Providers": {
    "SofaScore": {
      "Enabled": true,
      "PollingIntervalSeconds": 60
    },
    "Api365Scores": {
      "Enabled": true,
      "PollingIntervalSeconds": 45
    },
    "ApiFootball": {
      "Enabled": true,
      "PollingIntervalSeconds": 30
    },
    "Betfair": {
      "Enabled": false
    }
  }
}
```

### Worker base (evita repetição)

```csharp
public abstract class PollingWorker<TOptions> : BackgroundService
    where TOptions : ProviderOptions
{
    protected abstract string ProviderName { get; }
    protected abstract Task<IReadOnlyList<NormalizedMatch>> CollectAsync(CancellationToken ct);

    private readonly IOptionsMonitor<TOptions> _options;
    private readonly ISnapshotStore _store;
    private readonly IMatchHistoryRepository _history;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var matches = await CollectAsync(ct);
                foreach (var match in matches)
                {
                    await _history.SaveSnapshotAsync(match, rawJson: "", ct);
                    _store.Upsert(match);  // dispara DivergenceEngine reativamente
                }
            }
            catch (Exception ex)
            {
                // log + continua — falha numa fonte não derruba as outras
            }

            var interval = _options.CurrentValue.PollingIntervalSeconds;
            await Task.Delay(TimeSpan.FromSeconds(interval), ct);
        }
    }
}
```

Workers concretos são triviais:

```csharp
public class SofaScoreWorker : PollingWorker<SofaScoreOptions>
{
    protected override string ProviderName => "sofascore";
    protected override Task<IReadOnlyList<NormalizedMatch>> CollectAsync(CancellationToken ct)
        => _provider.GetLiveMatchesAsync(ct);
}
```

### AlertWorker

```csharp
public class AlertWorker : BackgroundService
{
    private readonly Channel<Divergence> _queue;
    private readonly IEnumerable<IAlertChannel> _channels;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await foreach (var divergence in _queue.Reader.ReadAllAsync(ct))
        foreach (var channel in _channels)
            await channel.SendAsync(divergence, ct);
    }
}
```

---

## 7. BFF — ASP.NET Core + SignalR

```csharp
// Minimal API endpoints
app.MapGet("/api/matches/live", (ISnapshotStore store) =>
    store.GetLiveMatchIds().Select(id => store.GetAllForMatch(id)));

app.MapGet("/api/divergences", (IDivergenceRepository repo) =>
    repo.GetRecentAsync());

app.MapPost("/api/divergences/{id}/verify", (string id, VerificationDto dto) =>
    repo.UpdateVerificationAsync(id, dto));

// SignalR hub
app.MapHub<AlertHub>("/hubs/alerts");
```

---

## 8. Dashboard Angular

Comunicação em tempo real via SignalR:

```typescript
// Angular service
this.hubConnection = new HubConnectionBuilder()
  .withUrl('/hubs/alerts')
  .build();

this.hubConnection.on('DivergenceDetected', (divergence) => {
  this.playAlertSound();           // toca o "apito"
  this.divergences.unshift(divergence);  // aparece no topo da lista
});
```

Páginas principais:
- `/matches` — partidas ao vivo monitoradas, status de cada fonte
- `/divergences` — lista de divergências com severidade, fontes, links
- `/settings` — configuração dos intervalos de polling (edita appsettings via API)

---

## 9. Desktop Shell

WPF com WebView2 — código mínimo:

```csharp
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        WebView.Source = new Uri("http://localhost:5000");
    }
}
```

O `App.xaml.cs` inicia o host ASP.NET Core em background:
```csharp
protected override async void OnStartup(StartupEventArgs e)
{
    _host = Host.CreateDefaultBuilder()
        .ConfigureWebHostDefaults(web => web.UseStartup<Startup>())
        .Build();

    await _host.StartAsync();
    new MainWindow().Show();
}
```

Migração para web = remover o projeto `Desktop/`. O restante não muda.

---

## 10. Match Resolver — MVP

```csharp
public class FuzzyMatchResolver : IMatchResolver
{
    // MVP: correlaciona por nome de time normalizado + horário ±5min + competição
    public string? ResolveMatchId(string sourceMatchId, string source,
                                  string homeTeam, string awayTeam,
                                  DateTime kickOff, string competition)
    {
        var key = $"{Normalize(homeTeam)}_{Normalize(awayTeam)}_{kickOff:yyyyMMddHH}";
        return _idMap.GetOrAdd(key, _ => Guid.NewGuid().ToString("N"));
    }

    private static string Normalize(string name)
        => name.ToLowerInvariant()
               .Replace(" ", "_")
               .Replace("-", "_");
}
```

Evolução futura: tabela de mapeamento explícito de IDs por liga (ex: SofaScore ID 12345 = API-Football ID 678 = Brasileirão, Flamengo vs Palmeiras).

---

## 11. Sequência de fluxo completo

```
1. SofaScoreWorker.ExecuteAsync() dispara
2. → SofaScoreProvider.GetLiveMatchesAsync()
       GET api.sofascore.com/api/v1/sport/football/events/live
       GET api.sofascore.com/api/v1/event/{id}/incidents
3. → IMatchResolver.ResolveMatchId() → matchId normalizado
4. → IMatchHistoryRepository.SaveSnapshotAsync() → linha no .jsonl
5. → ISnapshotStore.Upsert(match)
6.   SnapshotUpdated event dispara
7. → DivergenceEngine.EvaluateAsync(match)
       Busca todos os outros snapshots do mesmo matchId
       Roda cada IDivergenceRule contra cada par de fontes
8. → Se divergência: Channel<Divergence>.Writer.WriteAsync()
9. → AlertWorker lê o Channel
10. → IAlertChannel.SendAsync() para cada canal registrado
11. → SignalRAlertChannel → hub → Angular
12. → Angular toca o apito + exibe card de divergência
```

---

## 12. Registro no DI (Program.cs do Workers/Bff)

```csharp
// Providers
services.AddHttpClient<SofaScoreProvider>();
services.AddHttpClient<Api365ScoresProvider>();
services.AddHttpClient<ApiFootballProvider>();
services.AddScoped<IMatchDataProvider, SofaScoreProvider>();
services.AddScoped<IMatchDataProvider, Api365ScoresProvider>();
services.AddScoped<IMatchDataProvider, ApiFootballProvider>();

// Core
services.AddSingleton<ISnapshotStore, InMemorySnapshotStore>();
services.AddSingleton<IMatchResolver, FuzzyMatchResolver>();
services.AddSingleton(Channel.CreateUnbounded<Divergence>());

// Regras — ordem define prioridade
services.AddScoped<IDivergenceRule, ScoreMismatchRule>();
services.AddScoped<IDivergenceRule, GoalScorerMismatchRule>();
services.AddScoped<IDivergenceRule, MissingGoalRule>();
services.AddScoped<IDivergenceRule, YellowCardMismatchRule>();
services.AddScoped<IDivergenceRule, RedCardMismatchRule>();
services.AddScoped<IDivergenceRule, MatchStatusMismatchRule>();

// Canais de alerta — adicionar novos sem mudar AlertWorker
services.AddScoped<IAlertChannel, SignalRAlertChannel>();
// services.AddScoped<IAlertChannel, TelegramAlertChannel>();

// Storage — trocar esta linha para mudar de JSONL para SQLite
services.AddSingleton<IMatchHistoryRepository, JsonlMatchHistoryRepository>();

// Workers
services.AddHostedService<SofaScoreWorker>();
services.AddHostedService<Api365ScoresWorker>();
services.AddHostedService<ApiFootballWorker>();
services.AddHostedService<AlertWorker>();

// Config
services.Configure<ProvidersConfig>(config.GetSection("Providers"));
```

---

## 13. Onde adicionar coisas sem modificar código existente

| O que adicionar | O que fazer |
|---|---|
| Nova fonte de dados | Criar `XyzProvider : IMatchDataProvider` + registrar no DI |
| Nova regra de divergência | Criar `XyzRule : IDivergenceRule` + registrar no DI |
| Novo canal de alerta | Criar `XyzAlertChannel : IAlertChannel` + registrar no DI |
| Trocar JSONL por SQLite | Criar `SqliteMatchHistoryRepository` + trocar registro no DI |
| Trocar SQLite por Postgres | Criar `PostgresMatchHistoryRepository` + trocar registro no DI |
| Novo tipo de evento | Adicionar ao enum `EventType` |
| Migrar para web | Remover projeto `Desktop/` |

---

## 14. O que não implementar ainda

- Automação de apostas
- Login em bookmakers
- Bypass de qualquer proteção
- Cloud deployment
- Múltiplos usuários
- Machine learning
- Mobile app
- Replay discovery automático
- Odds comparison (fora do MVP de divergência esportiva)

---

## 15. Próximos passos

**Phase 04 — MVP Implementation Plan**

Criar `PHASE_04_MVP_IMPLEMENTATION_PLAN.md` quebrando este design em tasks implementáveis:
1. Scaffold da solution (projetos, referências, DI base)
2. Domain: entidades e interfaces
3. Infrastructure: JSONL repository + InMemorySnapshotStore
4. Provider: ApiFootballProvider (primeiro provider — tem API documentada)
5. DivergenceEngine + ScoreMismatchRule
6. AlertWorker + SignalRAlertChannel
7. BFF: endpoints básicos + SignalR hub
8. Angular: dashboard mínimo + conexão SignalR + som
9. Desktop shell: WPF + WebView2
10. Integrar SofaScoreProvider
11. Integrar Api365ScoresProvider
12. Demais regras de divergência
