using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SportsMonitor.Domain.Configuration;
using SportsMonitor.Domain.Interfaces;
using SportsMonitor.Domain.Models;
using SportsMonitor.Infrastructure.Stores;

namespace SportsMonitor.Workers;

/// <summary>
/// Injects fake live matches and divergences so the dashboard works without real API keys.
/// Disable by setting Demo.Enabled = false in appsettings.json once real tokens are configured.
/// </summary>
public class DemoWorker : BackgroundService
{
    private readonly ISnapshotStore _store;
    private readonly GoogleSnapshotStore _googleStore;
    private readonly IOptionsMonitor<DemoOptions> _options;
    private readonly ILogger<DemoWorker> _logger;

    private static readonly DateTime Today = DateTime.UtcNow.Date;

    public DemoWorker(
        ISnapshotStore store,
        GoogleSnapshotStore googleStore,
        IOptionsMonitor<DemoOptions> options,
        ILogger<DemoWorker> logger)
    {
        _store = store;
        _googleStore = googleStore;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!_options.CurrentValue.Enabled)
        {
            _logger.LogInformation("DemoWorker disabled - using real providers.");
            return;
        }

        _logger.LogInformation("DemoWorker started - injecting demo data every {Interval}s",
            _options.CurrentValue.TickIntervalSeconds);

        SeedBaseMatches();
        InjectLiveMatchPhase(0);

        await Task.Delay(TimeSpan.FromSeconds(5), ct);
        InjectQuickScoreMismatch();

        var phase = 1;
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(_options.CurrentValue.TickIntervalSeconds), ct);
            InjectLiveMatchPhase(phase++ % 8);
        }
    }

    private void SeedBaseMatches()
    {
        var cityEvents = new[]
        {
            new MatchEvent(EventType.Goal, 21, "Haaland", "home"),
            new MatchEvent(EventType.YellowCard, 36, "Mac Allister", "away"),
            new MatchEvent(EventType.Goal, 44, "Salah", "away"),
            new MatchEvent(EventType.Goal, 58, "Foden", "home")
        };

        UpsertAcrossSources("demo-mci-liv", "Manchester City", "Liverpool", "Premier League",
            2, 1, cityEvents, "bet365", "google", "sofascore", "api_football");
        UpsertGoogle("demo-mci-liv", "Manchester City", "Liverpool",
            "Manchester City Liverpool ao vivo gols Haaland Salah Foden",
            ("Manchester City 2 x 1 Liverpool - tempo real",
                "Haaland abriu o placar, Salah empatou antes do intervalo e Foden recolocou o City na frente aos 58 minutos.",
                "https://www.google.com/search?q=Manchester+City+Liverpool+ao+vivo"),
            ("City x Liverpool: lance a lance",
                "Placar parcial 2-1, com cartao para Mac Allister e pressao do Liverpool no segundo tempo.",
                "https://www.google.com/search?q=City+Liverpool+lance+a+lance"));

        var brasilEvents = new[]
        {
            new MatchEvent(EventType.YellowCard, 29, "De Paul", "away")
        };

        UpsertAcrossSources("demo-bra-arg", "Brasil", "Argentina", "Copa do Mundo 2026",
            0, 0, brasilEvents, "bet365", "google", "365scores", "sofascore");
        UpsertGoogle("demo-bra-arg", "Brasil", "Argentina",
            "Brasil Argentina ao vivo resultado",
            ("Brasil x Argentina ao vivo: classico truncado",
                "Partida segue 0-0 aos 33 minutos, com muitas faltas no meio-campo e cartao para De Paul.",
                "https://www.google.com/search?q=Brasil+Argentina+ao+vivo"),
            ("Tempo real Brasil Argentina",
                "Sem gols ate agora. Brasil tenta acelerar pelos lados, Argentina responde em contra-ataques.",
                "https://www.google.com/search?q=Brasil+Argentina+tempo+real"));

        var madridEvents = new[]
        {
            new MatchEvent(EventType.Goal, 14, "Bellingham", "home"),
            new MatchEvent(EventType.Goal, 41, "Lewandowski", "away"),
            new MatchEvent(EventType.YellowCard, 67, "Bellingham", "home")
        };

        UpsertAcrossSources("demo-rma-bar", "Real Madrid", "Barcelona", "Champions League",
            1, 1, madridEvents, "bet365", "google", "api_football", "sofascore");
        UpsertGoogle("demo-rma-bar", "Real Madrid", "Barcelona",
            "Real Madrid Barcelona ao vivo placar",
            ("Real Madrid 1 x 1 Barcelona - jogo ao vivo",
                "Bellingham e Lewandowski marcaram. Segundo tempo tem muita disputa e cartao para Bellingham.",
                "https://www.google.com/search?q=Real+Madrid+Barcelona+ao+vivo"),
            ("El Clasico em tempo real",
                "Placar empatado em 1-1 aos 70 minutos, com volume maior do Barcelona nos ultimos lances.",
                "https://www.google.com/search?q=El+Clasico+tempo+real"));

        UpsertAcrossSources("demo-bot-cor", "Botafogo", "Corinthians", "Brasileirao Serie A",
            0, 0, [], "bet365", "google", "365scores", "sofascore");
        UpsertGoogle("demo-bot-cor", "Botafogo", "Corinthians",
            "Botafogo Corinthians ao vivo resultado",
            ("Botafogo x Corinthians ao vivo",
                "Partida comeca equilibrada, ainda sem gols nos primeiros minutos.",
                "https://www.google.com/search?q=Botafogo+Corinthians+ao+vivo"),
            ("Tempo real Botafogo Corinthians",
                "Placar inicial 0 x 0 enquanto as equipes ainda se estudam.",
                "https://www.google.com/search?q=Botafogo+Corinthians+tempo+real"));
    }

    private void InjectQuickScoreMismatch()
    {
        _logger.LogInformation("[Demo] Quick scenario - score mismatch in Botafogo x Corinthians");

        var goal = new[]
        {
            new MatchEvent(EventType.Goal, 9, "Tiquinho Soares", "home")
        };

        UpsertMatch("demo-bot-cor", "Botafogo", "Corinthians", "Brasileirao Serie A",
            0, 0, "bet365", []);
        UpsertMatch("demo-bot-cor", "Botafogo", "Corinthians", "Brasileirao Serie A",
            1, 0, "google", goal);
        UpsertMatch("demo-bot-cor", "Botafogo", "Corinthians", "Brasileirao Serie A",
            1, 0, "sofascore", goal);
        UpsertMatch("demo-bot-cor", "Botafogo", "Corinthians", "Brasileirao Serie A",
            1, 0, "365scores", goal);

        UpsertGoogle("demo-bot-cor", "Botafogo", "Corinthians",
            "Tiquinho Soares gol Botafogo Corinthians 9",
            ("Gol do Botafogo: Tiquinho marca aos 9'",
                "Google e fontes de tempo real ja mostram Botafogo 1 x 0 Corinthians, mas Bet365 ainda aparece 0 x 0.",
                "https://www.google.com/search?q=Tiquinho+Soares+gol+Botafogo+Corinthians+9"),
            ("Divergencia de placar detectada",
                "Placar atualizado em fontes externas: Botafogo 1 x 0 Corinthians. Bet365 segue atrasada no mock.",
                "https://www.google.com/search?q=Botafogo+1+0+Corinthians+Tiquinho"));
    }

    private void InjectLiveMatchPhase(int phase)
    {
        _logger.LogInformation("[Demo] Live simulation phase {Phase} - Flamengo x Palmeiras", phase);

        switch (phase)
        {
            case 0:
                UpsertMainMatch(0, 0, [], [], []);
                UpsertGoogle("demo-fla-pal", "Flamengo", "Palmeiras",
                    "Flamengo Palmeiras ao vivo",
                    ("Flamengo x Palmeiras ao vivo: inicio estudado",
                        "Jogo comecou em ritmo intenso, mas o placar segue 0-0 nos primeiros minutos.",
                        "https://www.google.com/search?q=Flamengo+Palmeiras+ao+vivo"),
                    ("Tempo real Flamengo Palmeiras",
                        "Bet365 e sites de tempo real indicam Flamengo 0 x 0 Palmeiras aos 8 minutos.",
                        "https://www.google.com/search?q=Flamengo+Palmeiras+tempo+real"));
                break;

            case 1:
                var yellow = new[] { new MatchEvent(EventType.YellowCard, 18, "Gustavo Gomez", "away") };
                UpsertMainMatch(0, 0, yellow, yellow, yellow);
                UpsertGoogle("demo-fla-pal", "Flamengo", "Palmeiras",
                    "Flamengo Palmeiras cartao Gustavo Gomez",
                    ("Cartao para Gustavo Gomez aos 18'",
                        "Defensor do Palmeiras recebe amarelo apos falta perto da area. Placar segue 0-0.",
                        "https://www.google.com/search?q=Gustavo+Gomez+cartao+Flamengo+Palmeiras"),
                    ("Flamengo pressiona, Palmeiras tem amarelo",
                        "Lance confirmado no tempo real; Bet365 atualiza o cartao sem alterar o placar.",
                        "https://www.google.com/search?q=Flamengo+Palmeiras+cartao+ao+vivo"));
                break;

            case 2:
                var officialGoal = new[]
                {
                    new MatchEvent(EventType.YellowCard, 18, "Gustavo Gomez", "away"),
                    new MatchEvent(EventType.Goal, 32, "Pedro", "home")
                };
                var bet365WrongScorer = new[]
                {
                    new MatchEvent(EventType.YellowCard, 18, "Gustavo Gomez", "away"),
                    new MatchEvent(EventType.Goal, 32, "Arrascaeta", "home")
                };
                UpsertMainMatch(1, 0, bet365WrongScorer, officialGoal, officialGoal);
                UpsertGoogle("demo-fla-pal", "Flamengo", "Palmeiras",
                    "Pedro gol Flamengo Palmeiras 32",
                    ("Gol do Flamengo: Pedro marca aos 32'",
                        "Centroavante completa cruzamento na pequena area. Google aponta Pedro como autor do gol.",
                        "https://www.google.com/search?q=Pedro+gol+Flamengo+Palmeiras+32"),
                    ("Flamengo abre 1 x 0 contra Palmeiras",
                        "Tempo real confirma gol de Pedro, enquanto uma fonte ainda mostra Arrascaeta no lance.",
                        "https://www.google.com/search?q=Flamengo+1+0+Palmeiras+Pedro"));
                break;

            case 3:
                var correctedGoal = new[]
                {
                    new MatchEvent(EventType.YellowCard, 18, "Gustavo Gomez", "away"),
                    new MatchEvent(EventType.Goal, 32, "Pedro", "home")
                };
                UpsertMainMatch(1, 0, correctedGoal, correctedGoal, correctedGoal);
                UpsertGoogle("demo-fla-pal", "Flamengo", "Palmeiras",
                    "Flamengo Palmeiras Pedro gol confirmado",
                    ("Gol revisado: Pedro confirmado",
                        "Atualizacao corrige autoria do gol do Flamengo. Placar segue Flamengo 1 x 0 Palmeiras.",
                        "https://www.google.com/search?q=Pedro+gol+confirmado+Flamengo+Palmeiras"),
                    ("Bet365 corrige marcador do gol",
                        "Fonte principal passa a exibir Pedro como autor do gol aos 32 minutos.",
                        "https://www.google.com/search?q=Bet365+Pedro+Flamengo+Palmeiras"));
                break;

            case 4:
                var halfTime = new[]
                {
                    new MatchEvent(EventType.YellowCard, 18, "Gustavo Gomez", "away"),
                    new MatchEvent(EventType.Goal, 32, "Pedro", "home")
                };
                UpsertMainMatch(1, 0, halfTime, halfTime, halfTime, MatchStatus.HalfTime);
                UpsertGoogle("demo-fla-pal", "Flamengo", "Palmeiras",
                    "Flamengo Palmeiras intervalo 1 0",
                    ("Intervalo: Flamengo 1 x 0 Palmeiras",
                        "Pedro fez o unico gol do primeiro tempo. Palmeiras tenta reorganizar a saida de bola.",
                        "https://www.google.com/search?q=Flamengo+Palmeiras+intervalo+1+0"),
                    ("Resumo do primeiro tempo",
                        "Flamengo vai vencendo com gol aos 32 minutos; cartao amarelo para Gustavo Gomez.",
                        "https://www.google.com/search?q=Flamengo+Palmeiras+resumo+primeiro+tempo"));
                break;

            case 5:
                var googleEqualizer = new[]
                {
                    new MatchEvent(EventType.YellowCard, 18, "Gustavo Gomez", "away"),
                    new MatchEvent(EventType.Goal, 32, "Pedro", "home"),
                    new MatchEvent(EventType.Goal, 57, "Veiga", "away")
                };
                var bet365Delayed = new[]
                {
                    new MatchEvent(EventType.YellowCard, 18, "Gustavo Gomez", "away"),
                    new MatchEvent(EventType.Goal, 32, "Pedro", "home")
                };
                UpsertMatch("demo-fla-pal", "Flamengo", "Palmeiras", "Brasileirao Serie A",
                    1, 0, "bet365", bet365Delayed);
                UpsertMatch("demo-fla-pal", "Flamengo", "Palmeiras", "Brasileirao Serie A",
                    1, 1, "google", googleEqualizer);
                UpsertMatch("demo-fla-pal", "Flamengo", "Palmeiras", "Brasileirao Serie A",
                    1, 1, "365scores", googleEqualizer);
                UpsertMatch("demo-fla-pal", "Flamengo", "Palmeiras", "Brasileirao Serie A",
                    1, 1, "sofascore", googleEqualizer);
                UpsertGoogle("demo-fla-pal", "Flamengo", "Palmeiras",
                    "Veiga gol Palmeiras Flamengo 57",
                    ("Gol do Palmeiras: Veiga empata aos 57'",
                        "Meia do Palmeiras aproveita rebote e deixa tudo igual. Google mostra 1 x 1.",
                        "https://www.google.com/search?q=Veiga+gol+Palmeiras+Flamengo+57"),
                    ("Divergencia no placar ao vivo",
                        "Alguns placares ja exibem 1-1, enquanto Bet365 ainda aparece com Flamengo 1 x 0 Palmeiras.",
                        "https://www.google.com/search?q=Flamengo+Palmeiras+1+1+Veiga"));
                break;

            case 6:
                var lateCardOfficial = new[]
                {
                    new MatchEvent(EventType.YellowCard, 18, "Gustavo Gomez", "away"),
                    new MatchEvent(EventType.Goal, 32, "Pedro", "home"),
                    new MatchEvent(EventType.Goal, 57, "Veiga", "away"),
                    new MatchEvent(EventType.YellowCard, 72, "Pulgar", "home")
                };
                var lateCardBet365 = new[]
                {
                    new MatchEvent(EventType.YellowCard, 18, "Gustavo Gomez", "away"),
                    new MatchEvent(EventType.Goal, 32, "Pedro", "home"),
                    new MatchEvent(EventType.Goal, 57, "Veiga", "away"),
                    new MatchEvent(EventType.YellowCard, 72, "Allan", "home")
                };
                UpsertMainMatch(1, 1, lateCardBet365, lateCardOfficial, lateCardOfficial);
                UpsertGoogle("demo-fla-pal", "Flamengo", "Palmeiras",
                    "Pulgar cartao Flamengo Palmeiras 72",
                    ("Cartao amarelo para Pulgar aos 72'",
                        "Arbitro pune Pulgar por falta tatica. Bet365 ainda aponta Allan no evento.",
                        "https://www.google.com/search?q=Pulgar+cartao+Flamengo+Palmeiras+72"),
                    ("Flamengo x Palmeiras segue 1 x 1",
                        "Segundo tempo tem mais faltas e nova divergencia de cartao entre as fontes.",
                        "https://www.google.com/search?q=Flamengo+Palmeiras+cartao+Pulgar"));
                break;

            case 7:
                var finished = new[]
                {
                    new MatchEvent(EventType.YellowCard, 18, "Gustavo Gomez", "away"),
                    new MatchEvent(EventType.Goal, 32, "Pedro", "home"),
                    new MatchEvent(EventType.Goal, 57, "Veiga", "away"),
                    new MatchEvent(EventType.YellowCard, 72, "Pulgar", "home")
                };
                UpsertMainMatch(1, 1, finished, finished, finished, MatchStatus.Finished);
                UpsertGoogle("demo-fla-pal", "Flamengo", "Palmeiras",
                    "Flamengo Palmeiras final 1 1",
                    ("Fim de jogo: Flamengo 1 x 1 Palmeiras",
                        "Pedro e Veiga marcaram. Partida termina empatada apos segundo tempo movimentado.",
                        "https://www.google.com/search?q=Flamengo+Palmeiras+final+1+1"),
                    ("Resultado final Flamengo x Palmeiras",
                        "Google, Bet365 e SofaScore convergem no placar final de 1-1.",
                        "https://www.google.com/search?q=resultado+final+Flamengo+Palmeiras+1+1"));
                break;
        }
    }

    private void UpsertMainMatch(
        int homeScore,
        int awayScore,
        IReadOnlyList<MatchEvent> bet365Events,
        IReadOnlyList<MatchEvent> googleEvents,
        IReadOnlyList<MatchEvent> referenceEvents,
        MatchStatus status = MatchStatus.Live)
    {
        const string matchId = "demo-fla-pal";
        const string home = "Flamengo";
        const string away = "Palmeiras";
        const string competition = "Brasileirao Serie A";

        UpsertMatch(matchId, home, away, competition, homeScore, awayScore, "bet365", bet365Events, status);
        UpsertMatch(matchId, home, away, competition, homeScore, awayScore, "google", googleEvents, status);
        UpsertMatch(matchId, home, away, competition, homeScore, awayScore, "365scores", referenceEvents, status);
    }

    private void UpsertAcrossSources(
        string matchId,
        string home,
        string away,
        string competition,
        int homeScore,
        int awayScore,
        IReadOnlyList<MatchEvent> events,
        params string[] sources)
    {
        foreach (var source in sources)
        {
            UpsertMatch(matchId, home, away, competition, homeScore, awayScore, source, events);
        }
    }

    private void UpsertGoogle(
        string matchId,
        string home,
        string away,
        string query,
        params (string Title, string Snippet, string Url)[] results)
    {
        _googleStore.Upsert(new GoogleSearchSnapshot(
            matchId,
            home,
            away,
            query,
            results.Select(r => new GoogleSearchResult(r.Title, r.Snippet, r.Url)).ToList(),
            DateTime.UtcNow
        ));
    }

    private void UpsertMatch(
        string matchId,
        string home,
        string away,
        string competition,
        int homeScore,
        int awayScore,
        string source,
        IReadOnlyList<MatchEvent>? events = null,
        MatchStatus status = MatchStatus.Live)
    {
        _store.Upsert(new NormalizedMatch(
            matchId,
            home,
            away,
            competition,
            Today,
            homeScore,
            awayScore,
            status,
            events ?? [],
            source,
            DateTime.UtcNow,
            RawJson: $"{{\"demo\":true,\"source\":\"{source}\"}}"
        ));
    }
}
