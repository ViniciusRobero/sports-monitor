# Phase 04 — MVP Implementation Plan

> Date: 2026-05-26
> Branch: `feature/phase-04-mvp-implementation-plan`
> Referência de arquitetura: `PHASE_03_TECHNICAL_ARCHITECTURE.md`

---

## MVP Cutline

O sistema é considerado MVP funcional quando:

- [ ] App inicia localmente e abre o dashboard no browser/WebView2
- [ ] Pelo menos um provider (API-Football) coleta partidas ao vivo
- [ ] ScoreMismatch é detectado e aparece no dashboard
- [ ] Som de alerta toca quando divergência é detectada
- [ ] Histórico de leituras é salvo em JSONL

Tudo além disso (SofaScore, 365Scores, regras extras, configuração via UI) é pós-MVP.

---

## Épicos e tasks

---

### Epic 1 — Foundation

#### Task 1.1 — Criar solution e projetos

```
dotnet new sln -n SportsMonitor
dotnet new classlib -n SportsMonitor.Domain
dotnet new classlib -n SportsMonitor.Application
dotnet new classlib -n SportsMonitor.Infrastructure
dotnet new worker   -n SportsMonitor.Workers
dotnet new webapi   -n SportsMonitor.Bff
dotnet new wpf      -n SportsMonitor.Desktop
```

Referências:
```
Domain       ← sem referências externas
Application  → Domain
Infrastructure → Application, Domain
Workers      → Application, Domain, Infrastructure
Bff          → Application, Domain, Infrastructure
Desktop      → Bff (inicia o host)
```

Packages iniciais:
```
SportsMonitor.Infrastructure:
  Microsoft.Extensions.Http

SportsMonitor.Bff:
  Microsoft.AspNetCore.SignalR
  Microsoft.Extensions.Hosting

SportsMonitor.Desktop:
  Microsoft.Web.WebView2
  Microsoft.Extensions.Hosting
```

**Aceito quando:** `dotnet build SportsMonitor.sln` sem erros.

---

#### Task 1.2 — Entidades do Domain

Arquivo: `SportsMonitor.Domain/Models/`

```csharp
// MatchStatus.cs
public enum MatchStatus { NotStarted, Live, HalfTime, Finished, Postponed, Cancelled }

// EventType.cs
public enum EventType { Goal, YellowCard, RedCard, Substitution, OwnGoal, Penalty, VAR }

// Severity.cs
public enum Severity { Low, Medium, High, Critical }

// DivergenceType.cs
public enum DivergenceType
{
    ScoreMismatch, GoalScorerMismatch, MissingGoalEvent,
    YellowCardMismatch, RedCardMismatch, MatchStatusMismatch,
    SubstitutionMismatch, VARDecisionMismatch
}

// MatchEvent.cs
public record MatchEvent(
    EventType Type,
    int Minute,
    string PlayerName,
    string Team    // "home" | "away"
);

// NormalizedMatch.cs
public record NormalizedMatch(
    string MatchId,        // ID interno normalizado (gerado pelo Resolver)
    string HomeTeam,
    string AwayTeam,
    string Competition,
    DateTime KickOff,
    int HomeScore,
    int AwayScore,
    MatchStatus Status,
    IReadOnlyList<MatchEvent> Events,
    string Source,         // "sofascore" | "365scores" | "api_football"
    DateTime CollectedAt,
    string RawJson         // payload original da fonte
);

// Divergence.cs
public record Divergence(
    Guid Id,
    string MatchId,
    string HomeTeam,
    string AwayTeam,
    DivergenceType Type,
    Severity Severity,
    string SourceA,
    string SourceAValue,
    string SourceB,
    string SourceBValue,
    string? OfficialSourceValue,
    DateTime DetectedAt,
    VerificationStatus VerificationStatus = VerificationStatus.Pending
);

// VerificationStatus.cs
public enum VerificationStatus { Pending, InAnalysis, Confirmed, FalsePositive, Ignored }

// VerificationUpdate.cs
public record VerificationUpdate(
    VerificationStatus Status,
    string? ReplayLink,
    string? AnalystNotes,
    string? ManualActionStatus
);
```

**Aceito quando:** todos os arquivos compilam, sem referências externas.

---

#### Task 1.3 — Interfaces do Domain

Arquivo: `SportsMonitor.Domain/Interfaces/`

```csharp
// IMatchDataProvider.cs
public interface IMatchDataProvider
{
    string Name { get; }
    Task<IReadOnlyList<NormalizedMatch>> GetLiveMatchesAsync(CancellationToken ct);
}

// ISnapshotStore.cs
public interface ISnapshotStore
{
    void Upsert(NormalizedMatch match);
    event Action<NormalizedMatch>? SnapshotUpdated;
    IReadOnlyList<NormalizedMatch> GetAllForMatch(string matchId);
    IReadOnlyList<string> GetLiveMatchIds();
    void RemoveMatch(string matchId);
}

// IDivergenceRule.cs
public interface IDivergenceRule
{
    IEnumerable<Divergence> Check(NormalizedMatch a, NormalizedMatch b);
}

// IAlertChannel.cs
public interface IAlertChannel
{
    Task SendAsync(Divergence divergence, CancellationToken ct);
}

// IMatchHistoryRepository.cs
public interface IMatchHistoryRepository
{
    Task SaveSnapshotAsync(NormalizedMatch match, CancellationToken ct);
    Task SaveDivergenceAsync(Divergence divergence, CancellationToken ct);
    Task UpdateVerificationAsync(Guid divergenceId, VerificationUpdate update, CancellationToken ct);
    Task<IReadOnlyList<Divergence>> GetRecentDivergencesAsync(int limit, CancellationToken ct);
}

// IMatchResolver.cs
public interface IMatchResolver
{
    string ResolveMatchId(
        string sourceMatchId, string source,
        string homeTeam, string awayTeam,
        DateTime kickOff, string competition);
}
```

**Aceito quando:** interfaces compilam sem dependências externas.

---

#### Task 1.4 — Configuration models

Arquivo: `SportsMonitor.Domain/Configuration/`

```csharp
// ProviderOptions.cs
public class ProviderOptions
{
    public bool Enabled { get; set; } = true;
    public int PollingIntervalSeconds { get; set; } = 60;
}

// ProvidersConfig.cs
public class ProvidersConfig
{
    public ProviderOptions SofaScore { get; set; } = new();
    public ProviderOptions Api365Scores { get; set; } = new();
    public ProviderOptions ApiFootball { get; set; } = new();
    public ProviderOptions Betfair { get; set; } = new();
}

// ApiFootballOptions.cs
public class ApiFootballOptions : ProviderOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://v3.football.api-sports.io";
}
```

`appsettings.json` base:
```json
{
  "Providers": {
    "SofaScore":    { "Enabled": false, "PollingIntervalSeconds": 60 },
    "Api365Scores": { "Enabled": false, "PollingIntervalSeconds": 45 },
    "ApiFootball":  { "Enabled": true,  "PollingIntervalSeconds": 30, "ApiKey": "" },
    "Betfair":      { "Enabled": false }
  }
}
```

**Aceito quando:** `IOptions<ProvidersConfig>` injetável no DI.

---

### Epic 2 — Infrastructure: Storage e Resolver

#### Task 2.1 — InMemorySnapshotStore

Arquivo: `SportsMonitor.Infrastructure/Stores/InMemorySnapshotStore.cs`

```csharp
public class InMemorySnapshotStore : ISnapshotStore
{
    // matchId → (source → NormalizedMatch)
    private readonly ConcurrentDictionary<string,
        ConcurrentDictionary<string, NormalizedMatch>> _data = new();

    public event Action<NormalizedMatch>? SnapshotUpdated;

    public void Upsert(NormalizedMatch match)
    {
        var bySource = _data.GetOrAdd(match.MatchId, _ => new());
        bySource[match.Source] = match;
        SnapshotUpdated?.Invoke(match);
    }

    public IReadOnlyList<NormalizedMatch> GetAllForMatch(string matchId) =>
        _data.TryGetValue(matchId, out var d) ? d.Values.ToList() : [];

    public IReadOnlyList<string> GetLiveMatchIds() =>
        _data.Keys.ToList();

    public void RemoveMatch(string matchId) =>
        _data.TryRemove(matchId, out _);
}
```

**Aceito quando:** evento dispara imediatamente após `Upsert`, verificado em teste unitário.

---

#### Task 2.2 — JsonlMatchHistoryRepository

Arquivo: `SportsMonitor.Infrastructure/Repositories/JsonlMatchHistoryRepository.cs`

Estrutura de arquivos gerada:
```
/data/
  2026-05-26/
    snapshots/
      api_football.jsonl
      sofascore.jsonl
    divergences.jsonl
```

```csharp
public class JsonlMatchHistoryRepository : IMatchHistoryRepository
{
    private readonly string _basePath;
    private static readonly SemaphoreSlim _lock = new(1, 1);

    public async Task SaveSnapshotAsync(NormalizedMatch match, CancellationToken ct)
    {
        var path = Path.Combine(_basePath,
            match.CollectedAt.ToString("yyyy-MM-dd"),
            "snapshots",
            $"{match.Source}.jsonl");

        var line = JsonSerializer.Serialize(new
        {
            ts          = match.CollectedAt,
            matchId     = match.MatchId,
            homeTeam    = match.HomeTeam,
            awayTeam    = match.AwayTeam,
            homeScore   = match.HomeScore,
            awayScore   = match.AwayScore,
            status      = match.Status.ToString(),
            events      = match.Events,
            raw         = match.RawJson
        });

        await WriteLineAsync(path, line, ct);
    }

    public async Task SaveDivergenceAsync(Divergence d, CancellationToken ct)
    {
        var path = Path.Combine(_basePath,
            d.DetectedAt.ToString("yyyy-MM-dd"),
            "divergences.jsonl");

        await WriteLineAsync(path, JsonSerializer.Serialize(d), ct);
    }

    // GetRecentDivergencesAsync: lê as últimas N linhas do arquivo do dia atual
    // UpdateVerificationAsync: reescreve a linha correspondente (pelo Id)

    private static async Task WriteLineAsync(string path, string line, CancellationToken ct)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await _lock.WaitAsync(ct);
        try { await File.AppendAllTextAsync(path, line + "\n", ct); }
        finally { _lock.Release(); }
    }
}
```

**Aceito quando:** arquivos `.jsonl` são criados na pasta `/data/` após coleta.

---

#### Task 2.3 — FuzzyMatchResolver

Arquivo: `SportsMonitor.Infrastructure/Resolvers/FuzzyMatchResolver.cs`

```csharp
public class FuzzyMatchResolver : IMatchResolver
{
    // matchKey → internal matchId
    private readonly ConcurrentDictionary<string, string> _map = new();

    public string ResolveMatchId(string sourceMatchId, string source,
        string homeTeam, string awayTeam, DateTime kickOff, string competition)
    {
        // Chave: home_away_YYYYMMDDHH (tolerância de 1h)
        var key = $"{Norm(homeTeam)}_{Norm(awayTeam)}_{kickOff:yyyyMMddHH}";
        return _map.GetOrAdd(key, _ => Guid.NewGuid().ToString("N")[..12]);
    }

    private static string Norm(string s) =>
        s.ToLowerInvariant()
         .Normalize(NormalizationForm.FormD)         // remove acentos
         .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
         .Pipe(chars => new string(chars.ToArray()))
         .Replace(" ", "_")
         .Replace("-", "_");
}
```

**Aceito quando:** "Flamengo" e "flamengo" resolvem para o mesmo matchId.

---

### Epic 3 — Primeiro Provider: API-Football

#### Task 3.1 — ApiFootballProvider

Arquivo: `SportsMonitor.Infrastructure/Providers/ApiFootballProvider.cs`

Endpoints usados:
```
GET /fixtures?live=all
Headers: x-apisports-key: {ApiKey}

Resposta relevante por fixture:
{
  "fixture": { "id": 123, "status": { "short": "1H" } },
  "league":  { "name": "Brasileirão Serie A", "country": "Brazil" },
  "teams":   { "home": { "name": "Flamengo" }, "away": { "name": "Palmeiras" } },
  "goals":   { "home": 1, "away": 0 },
  "events":  [{ "time": { "elapsed": 32 }, "player": { "name": "Pedro" }, "type": "Goal", "team": { "name": "Flamengo" } }]
}
```

```csharp
public class ApiFootballProvider : IMatchDataProvider
{
    public string Name => "api_football";

    private readonly HttpClient _http;
    private readonly IOptionsMonitor<ApiFootballOptions> _options;
    private readonly IMatchResolver _resolver;

    public async Task<IReadOnlyList<NormalizedMatch>> GetLiveMatchesAsync(CancellationToken ct)
    {
        var response = await _http.GetAsync("/fixtures?live=all", ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var root = JsonDocument.Parse(json).RootElement;

        return root.GetProperty("response")
                   .EnumerateArray()
                   .Select(f => MapFixture(f, json))
                   .ToList();
    }

    private NormalizedMatch MapFixture(JsonElement f, string rawJson)
    {
        var homeTeam    = f.GetProperty("teams").GetProperty("home").GetProperty("name").GetString()!;
        var awayTeam    = f.GetProperty("teams").GetProperty("away").GetProperty("name").GetString()!;
        var competition = f.GetProperty("league").GetProperty("name").GetString()!;
        var kickOff     = DateTime.Parse(f.GetProperty("fixture").GetProperty("date").GetString()!);
        var sourceId    = f.GetProperty("fixture").GetProperty("id").GetInt32().ToString();

        var matchId = _resolver.ResolveMatchId(sourceId, Name, homeTeam, awayTeam, kickOff, competition);

        var events = f.GetProperty("events").EnumerateArray()
            .Select(e => new MatchEvent(
                MapEventType(e.GetProperty("type").GetString()!),
                e.GetProperty("time").GetProperty("elapsed").GetInt32(),
                e.GetProperty("player").GetProperty("name").GetString() ?? "",
                e.GetProperty("team").GetProperty("name").GetString() == homeTeam ? "home" : "away"
            ))
            .ToList();

        return new NormalizedMatch(
            matchId, homeTeam, awayTeam, competition, kickOff,
            f.GetProperty("goals").GetProperty("home").GetInt32OrDefault(),
            f.GetProperty("goals").GetProperty("away").GetInt32OrDefault(),
            MapStatus(f.GetProperty("fixture").GetProperty("status").GetProperty("short").GetString()!),
            events, Name, DateTime.UtcNow, rawJson
        );
    }

    private static EventType MapEventType(string t) => t switch
    {
        "Goal"        => EventType.Goal,
        "Card"        => EventType.YellowCard,  // refinar com subtype
        "subst"       => EventType.Substitution,
        _             => EventType.Goal
    };

    private static MatchStatus MapStatus(string s) => s switch
    {
        "1H" or "2H" or "ET" or "P" => MatchStatus.Live,
        "HT"                          => MatchStatus.HalfTime,
        "FT" or "AET" or "PEN"       => MatchStatus.Finished,
        "NS"                          => MatchStatus.NotStarted,
        _                             => MatchStatus.Live
    };
}
```

**Aceito quando:** retorna lista de `NormalizedMatch` com score e events para partidas ao vivo (testar com API key real ou mock).

---

#### Task 3.2 — PollingWorker base + ApiFootballWorker

Arquivo: `SportsMonitor.Workers/Base/PollingWorker.cs`

```csharp
public abstract class PollingWorker : BackgroundService
{
    protected abstract string WorkerName { get; }
    protected abstract bool IsEnabled { get; }
    protected abstract int PollingIntervalSeconds { get; }
    protected abstract Task<IReadOnlyList<NormalizedMatch>> CollectAsync(CancellationToken ct);

    private readonly ISnapshotStore _store;
    private readonly IMatchHistoryRepository _history;
    private readonly ILogger _logger;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!IsEnabled)
        {
            _logger.LogInformation("{Worker} is disabled. Skipping.", WorkerName);
            return;
        }

        _logger.LogInformation("{Worker} started. Interval: {Interval}s", WorkerName, PollingIntervalSeconds);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var matches = await CollectAsync(ct);
                _logger.LogDebug("{Worker} collected {Count} live matches.", WorkerName, matches.Count);

                foreach (var match in matches)
                {
                    await _history.SaveSnapshotAsync(match, ct);
                    _store.Upsert(match);  // dispara DivergenceEngine reativamente
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "{Worker} collection failed. Will retry.", WorkerName);
            }

            await Task.Delay(TimeSpan.FromSeconds(PollingIntervalSeconds), ct);
        }
    }
}
```

Arquivo: `SportsMonitor.Workers/ApiFootballWorker.cs`

```csharp
public class ApiFootballWorker : PollingWorker
{
    private readonly IMatchDataProvider _provider;
    private readonly IOptionsMonitor<ApiFootballOptions> _options;

    protected override string WorkerName => "ApiFootballWorker";
    protected override bool IsEnabled => _options.CurrentValue.Enabled;
    protected override int PollingIntervalSeconds => _options.CurrentValue.PollingIntervalSeconds;

    protected override Task<IReadOnlyList<NormalizedMatch>> CollectAsync(CancellationToken ct)
        => _provider.GetLiveMatchesAsync(ct);
}
```

**Aceito quando:** worker roda em loop, loga coletas, respeita intervalo configurado.

---

### Epic 4 — Divergence Engine

#### Task 4.1 — DivergenceEngine

Arquivo: `SportsMonitor.Application/DivergenceEngine.cs`

```csharp
public class DivergenceEngine
{
    private readonly IEnumerable<IDivergenceRule> _rules;
    private readonly IMatchHistoryRepository _history;
    private readonly ChannelWriter<Divergence> _alertQueue;
    private readonly ILogger<DivergenceEngine> _logger;

    // Chamado pelo ISnapshotStore.SnapshotUpdated — não tem loop próprio
    public async Task EvaluateAsync(NormalizedMatch updated, CancellationToken ct = default)
    {
        // Compara a partida atualizada contra todas as outras fontes do mesmo matchId
        // (não compara A vs A; não compara pares já comparados neste tick)
        var others = _store.GetAllForMatch(updated.MatchId)
                           .Where(m => m.Source != updated.Source)
                           .ToList();

        foreach (var other in others)
        foreach (var rule in _rules)
        {
            var divergences = rule.Check(updated, other).ToList();
            foreach (var d in divergences)
            {
                _logger.LogWarning("Divergence: {Type} in {Match} ({SourceA} vs {SourceB})",
                    d.Type, $"{d.HomeTeam} x {d.AwayTeam}", d.SourceA, d.SourceB);

                await _history.SaveDivergenceAsync(d, ct);
                await _alertQueue.WriteAsync(d, ct);
            }
        }
    }
}
```

Wiring no `InMemorySnapshotStore` (no `Program.cs`):
```csharp
snapshotStore.SnapshotUpdated += async match =>
    await divergenceEngine.EvaluateAsync(match);
```

**Aceito quando:** divergência é detectada e enfileirada imediatamente após `Upsert`.

---

#### Task 4.2 — ScoreMismatchRule

Arquivo: `SportsMonitor.Application/Rules/ScoreMismatchRule.cs`

```csharp
public class ScoreMismatchRule : IDivergenceRule
{
    public IEnumerable<Divergence> Check(NormalizedMatch a, NormalizedMatch b)
    {
        if (a.HomeScore == b.HomeScore && a.AwayScore == b.AwayScore) yield break;

        yield return new Divergence(
            Guid.NewGuid(),
            a.MatchId, a.HomeTeam, a.AwayTeam,
            DivergenceType.ScoreMismatch,
            Severity.Critical,
            a.Source, $"{a.HomeScore}-{a.AwayScore}",
            b.Source, $"{b.HomeScore}-{b.AwayScore}",
            OfficialSourceValue: null,
            DateTime.UtcNow
        );
    }
}
```

---

#### Task 4.3 — GoalScorerMismatchRule + MissingGoalRule

Arquivo: `SportsMonitor.Application/Rules/GoalScorerMismatchRule.cs`

```csharp
public class GoalScorerMismatchRule : IDivergenceRule
{
    public IEnumerable<Divergence> Check(NormalizedMatch a, NormalizedMatch b)
    {
        var goalsA = a.Events.Where(e => e.Type == EventType.Goal).ToList();
        var goalsB = b.Events.Where(e => e.Type == EventType.Goal).ToList();

        // Para cada gol de A, procura gol de B no mesmo minuto (±2)
        foreach (var goalA in goalsA)
        {
            var match = goalsB.FirstOrDefault(g => Math.Abs(g.Minute - goalA.Minute) <= 2);
            if (match is null) continue;  // MissingGoalRule trata ausência

            if (!NormalizeName(goalA.PlayerName).Equals(NormalizeName(match.PlayerName)))
                yield return new Divergence(
                    Guid.NewGuid(), a.MatchId, a.HomeTeam, a.AwayTeam,
                    DivergenceType.GoalScorerMismatch, Severity.Critical,
                    a.Source, $"{goalA.Minute}' {goalA.PlayerName}",
                    b.Source, $"{match.Minute}' {match.PlayerName}",
                    null, DateTime.UtcNow
                );
        }
    }

    private static string NormalizeName(string name) =>
        name.ToLowerInvariant().Split(' ').Last();  // compara sobrenome
}
```

**Aceito quando:** "Pedro" vs "Arrascaeta" no minuto 32 gera um `GoalScorerMismatch`.

---

### Epic 5 — Alert Pipeline

#### Task 5.1 — Channel setup

Em `Program.cs`:
```csharp
// Fila ilimitada — AlertWorker drena em background
services.AddSingleton(Channel.CreateUnbounded<Divergence>(
    new UnboundedChannelOptions { SingleReader = true }));
services.AddSingleton(sp => sp.GetRequiredService<Channel<Divergence>>().Writer);
services.AddSingleton(sp => sp.GetRequiredService<Channel<Divergence>>().Reader);
```

---

#### Task 5.2 — AlertWorker

Arquivo: `SportsMonitor.Workers/AlertWorker.cs`

```csharp
public class AlertWorker : BackgroundService
{
    private readonly ChannelReader<Divergence> _queue;
    private readonly IEnumerable<IAlertChannel> _channels;
    private readonly ILogger<AlertWorker> _logger;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await foreach (var divergence in _queue.ReadAllAsync(ct))
        {
            foreach (var channel in _channels)
            {
                try   { await channel.SendAsync(divergence, ct); }
                catch (Exception ex)
                    { _logger.LogWarning(ex, "Alert channel {Ch} failed.", channel.GetType().Name); }
            }
        }
    }
}
```

---

#### Task 5.3 — SignalRAlertChannel

Arquivo: `SportsMonitor.Infrastructure/Alerts/SignalRAlertChannel.cs`

```csharp
public class SignalRAlertChannel : IAlertChannel
{
    private readonly IHubContext<AlertHub> _hub;

    public Task SendAsync(Divergence divergence, CancellationToken ct)
        => _hub.Clients.All.SendAsync("DivergenceDetected", divergence, ct);
}
```

---

### Epic 6 — BFF (ASP.NET Core + SignalR)

#### Task 6.1 — Program.cs do Bff

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddControllers();

// Servir Angular como SPA
builder.Services.AddSpaStaticFiles(c => c.RootPath = "wwwroot");

// Workers como hosted services
builder.Services.AddHostedService<ApiFootballWorker>();
builder.Services.AddHostedService<AlertWorker>();

// Providers e infra
builder.Services.AddHttpClient<ApiFootballProvider>()
    .ConfigureHttpClient((sp, c) =>
    {
        var opts = sp.GetRequiredService<IOptionsMonitor<ApiFootballOptions>>().CurrentValue;
        c.BaseAddress = new Uri(opts.BaseUrl);
        c.DefaultRequestHeaders.Add("x-apisports-key", opts.ApiKey);
    });

builder.Services.AddSingleton<ISnapshotStore, InMemorySnapshotStore>();
builder.Services.AddSingleton<IMatchResolver, FuzzyMatchResolver>();
builder.Services.AddSingleton<IMatchHistoryRepository, JsonlMatchHistoryRepository>();
builder.Services.AddScoped<IMatchDataProvider, ApiFootballProvider>();

builder.Services.AddScoped<IDivergenceRule, ScoreMismatchRule>();
builder.Services.AddScoped<IDivergenceRule, GoalScorerMismatchRule>();
builder.Services.AddScoped<IDivergenceRule, MissingGoalRule>();

builder.Services.AddScoped<IAlertChannel, SignalRAlertChannel>();
builder.Services.AddScoped<DivergenceEngine>();

builder.Services.Configure<ProvidersConfig>(builder.Configuration.GetSection("Providers"));

var app = builder.Build();

// Wiring reativo
var store    = app.Services.GetRequiredService<ISnapshotStore>();
var engine   = app.Services.GetRequiredService<DivergenceEngine>();
store.SnapshotUpdated += async m => await engine.EvaluateAsync(m);

app.MapHub<AlertHub>("/hubs/alerts");
app.MapControllers();
app.UseSpaStaticFiles();
app.UseSpa(spa => spa.Options.SourcePath = "wwwroot");

app.Run();
```

---

#### Task 6.2 — AlertHub

Arquivo: `SportsMonitor.Bff/Hubs/AlertHub.cs`

```csharp
public class AlertHub : Hub
{
    // Servidor → cliente: "DivergenceDetected"
    // Sem métodos de cliente → servidor no MVP
}
```

---

#### Task 6.3 — REST endpoints

Arquivo: `SportsMonitor.Bff/Controllers/MatchesController.cs`

```csharp
[ApiController, Route("api/matches")]
public class MatchesController : ControllerBase
{
    [HttpGet("live")]
    public IActionResult GetLive([FromServices] ISnapshotStore store) =>
        Ok(store.GetLiveMatchIds()
                .Select(id => store.GetAllForMatch(id)));
}
```

Arquivo: `SportsMonitor.Bff/Controllers/DivergencesController.cs`

```csharp
[ApiController, Route("api/divergences")]
public class DivergencesController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetRecent(
        [FromServices] IMatchHistoryRepository repo,
        CancellationToken ct,
        int limit = 50) =>
        Ok(await repo.GetRecentDivergencesAsync(limit, ct));

    [HttpPost("{id:guid}/verify")]
    public async Task<IActionResult> Verify(
        Guid id,
        [FromBody] VerificationUpdate update,
        [FromServices] IMatchHistoryRepository repo,
        CancellationToken ct)
    {
        await repo.UpdateVerificationAsync(id, update, ct);
        return NoContent();
    }
}
```

**Aceito quando:** `GET /api/matches/live` retorna JSON, `GET /api/divergences` retorna lista.

---

### Epic 7 — Angular Dashboard (MVP mínimo)

#### Task 7.1 — Setup do projeto Angular

```bash
cd SportsMonitor.Bff
ng new web --routing --style=scss --skip-git
# Build output → wwwroot/
```

`angular.json` → `outputPath: "../SportsMonitor.Bff/wwwroot"`

Dependências:
```bash
npm install @microsoft/signalr
```

---

#### Task 7.2 — AlertService (SignalR)

Arquivo: `web/src/app/services/alert.service.ts`

```typescript
@Injectable({ providedIn: 'root' })
export class AlertService {
  private hub = new HubConnectionBuilder()
    .withUrl('/hubs/alerts')
    .withAutomaticReconnect()
    .build();

  divergences$ = new Subject<Divergence>();

  async start() {
    this.hub.on('DivergenceDetected', (d: Divergence) => {
      this.divergences$.next(d);
      this.playAlert(d.severity);
    });
    await this.hub.start();
  }

  private playAlert(severity: string) {
    const audio = new Audio();
    // arquivo de som na pasta assets/
    audio.src = severity === 'Critical'
      ? 'assets/alert-critical.mp3'
      : 'assets/alert-medium.mp3';
    audio.play().catch(() => {});  // ignora se browser bloquear autoplay
  }
}
```

---

#### Task 7.3 — Página de partidas ao vivo

Arquivo: `web/src/app/pages/matches/`

```typescript
// matches.component.ts
matches$ = this.http.get<MatchGroup[]>('/api/matches/live');
```

Template (simples, funcional):
```html
<div *ngFor="let group of matches$ | async">
  <h3>{{ group[0].homeTeam }} x {{ group[0].awayTeam }}</h3>
  <div *ngFor="let m of group">
    <span class="source">{{ m.source }}</span>
    <span class="score">{{ m.homeScore }}-{{ m.awayScore }}</span>
    <span class="status">{{ m.status }}</span>
  </div>
</div>
```

---

#### Task 7.4 — Página de divergências + som (MVP obrigatório)

Arquivo: `web/src/app/pages/divergences/`

```typescript
// divergences.component.ts
divergences: Divergence[] = [];

ngOnInit() {
  // carrega histórico
  this.http.get<Divergence[]>('/api/divergences').subscribe(d => this.divergences = d);

  // recebe novas em tempo real
  this.alertService.divergences$.subscribe(d => this.divergences.unshift(d));
}
```

Template:
```html
<div *ngFor="let d of divergences"
     [class]="'card severity-' + d.severity.toLowerCase()">
  <span class="type">{{ d.type }}</span>
  <span class="match">{{ d.homeTeam }} x {{ d.awayTeam }}</span>
  <span class="sources">{{ d.sourceA }}: {{ d.sourceAValue }} | {{ d.sourceB }}: {{ d.sourceBValue }}</span>
  <span class="time">{{ d.detectedAt | date:'HH:mm:ss' }}</span>
  <a [href]="googleSearchUrl(d)" target="_blank">Verificar no Google</a>
</div>
```

```typescript
googleSearchUrl(d: Divergence): string {
  return `https://www.google.com/search?q=${encodeURIComponent(d.homeTeam + ' ' + d.awayTeam + ' ao vivo')}`;
}
```

---

#### Task 7.5 — Manual verification form

Campo inline na card de divergência:

```html
<div class="verify-form" *ngIf="d.verificationStatus === 'Pending'">
  <input [(ngModel)]="d.replayLink"    placeholder="Link do replay" />
  <textarea [(ngModel)]="d.analystNotes" placeholder="Notas"></textarea>
  <select [(ngModel)]="d.verificationStatus">
    <option>Confirmed</option>
    <option>FalsePositive</option>
    <option>Ignored</option>
  </select>
  <button (click)="verify(d)">Salvar</button>
</div>
```

```typescript
verify(d: Divergence) {
  this.http.post(`/api/divergences/${d.id}/verify`, {
    status: d.verificationStatus,
    replayLink: d.replayLink,
    analystNotes: d.analystNotes
  }).subscribe();
}
```

**Aceito quando:** divergência aparece no dashboard, som toca, analista consegue verificar e salvar notas.

---

### Epic 8 — Desktop Shell (WPF + WebView2)

#### Task 8.1 — MainWindow.xaml

```xml
<Window x:Class="SportsMonitor.Desktop.MainWindow"
        Title="Sports Monitor" Height="900" Width="1400">
  <Grid>
    <wpf:WebView2 x:Name="WebView" />
  </Grid>
</Window>
```

```csharp
// MainWindow.xaml.cs
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            await WebView.EnsureCoreWebView2Async();
            WebView.Source = new Uri("http://localhost:5000");
        };
    }
}
```

---

#### Task 8.2 — App.xaml.cs — inicia o host ASP.NET Core

```csharp
public partial class App : System.Windows.Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder(e.Args)
            .ConfigureWebHostDefaults(web =>
                web.UseStartup<SportsMonitor.Bff.Startup>()
                   .UseUrls("http://localhost:5000"))
            .Build();

        await _host.StartAsync();
        new MainWindow().Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
            await _host.StopAsync(TimeSpan.FromSeconds(3));
        base.OnExit(e);
    }
}
```

**Aceito quando:** app WPF abre, WebView2 carrega `localhost:5000`, dashboard funciona.

---

### Epic 9 — Providers adicionais (pós-MVP cutline)

#### Task 9.1 — SofaScoreProvider + SofaScoreWorker

Endpoints:
```
GET https://api.sofascore.com/api/v1/sport/football/events/live
GET https://api.sofascore.com/api/v1/event/{id}/incidents
```

Headers necessários:
```csharp
client.DefaultRequestHeaders.UserAgent.ParseAdd(
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
client.DefaultRequestHeaders.Referrer = new Uri("https://www.sofascore.com/");
```

Worker: igual ao `ApiFootballWorker`, mas com `SofaScoreOptions` e sem API key.

---

#### Task 9.2 — Api365ScoresProvider + Api365ScoresWorker

Endpoints:
```
GET https://webws.365scores.com/web/games/results/
    ?appTypeId=5&langId=31&timezoneName=America/Sao_Paulo&userCountryId=-1&onlyLive=true

GET https://webws.365scores.com/web/game/
    ?appTypeId=5&langId=31&timezoneName=America/Sao_Paulo&userCountryId=-1&gameId={id}
```

Sem headers especiais necessários.

---

### Epic 10 — Regras restantes (pós-MVP cutline)

```csharp
// Uma classe por arquivo em SportsMonitor.Application/Rules/
YellowCardMismatchRule   : IDivergenceRule
RedCardMismatchRule      : IDivergenceRule
MatchStatusMismatchRule  : IDivergenceRule
SubstitutionMismatchRule : IDivergenceRule
```

Padrão idêntico ao `ScoreMismatchRule` — ~15 linhas cada.

---

## Ordem de implementação recomendada

```
Epic 1 → Epic 2 → Epic 3 → Epic 4 → Epic 5 → Epic 6 → Epic 7.1-7.4 → Epic 8
                                                                    ↓
                                                           MVP FUNCIONAL ✓
                                                                    ↓
                                              Epic 7.5 → Epic 9 → Epic 10
```

---

## Definition of Done — MVP

- [ ] `dotnet run` no projeto `Desktop` abre a janela WPF
- [ ] WebView2 carrega `http://localhost:5000` com Angular
- [ ] `ApiFootballWorker` coleta partidas ao vivo (ou mock) e loga
- [ ] Arquivos `.jsonl` são criados em `/data/` com os snapshots
- [ ] `ScoreMismatchRule` detecta divergência entre dois snapshots mockados
- [ ] Dashboard mostra a divergência em tempo real
- [ ] Som toca quando `DivergenceDetected` chega via SignalR
- [ ] `GET /api/divergences` retorna lista em JSON
- [ ] Analista consegue preencher o form de verificação e salvar

---

## Branches GitFlow

```
feature/phase-04-mvp-implementation-plan   ← este documento (merge → develop ao concluir)
feature/epic-1-foundation                  ← scaffold + domain + interfaces
feature/epic-2-storage                     ← JSONL + SnapshotStore + Resolver
feature/epic-3-api-football-provider       ← provider + worker
feature/epic-4-divergence-engine           ← engine + regras MVP
feature/epic-5-alert-pipeline              ← Channel + AlertWorker + SignalR
feature/epic-6-bff                         ← ASP.NET Core + endpoints
feature/epic-7-dashboard                   ← Angular MVP
feature/epic-8-desktop-shell               ← WPF + WebView2
```
