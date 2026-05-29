using System.Text.Json.Serialization;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using SportsMonitor.Application;
using SportsMonitor.Application.Rules;
using SportsMonitor.Workers;
using SportsMonitor.Bff.Alerts;
using SportsMonitor.Bff.Hubs;
using SportsMonitor.Domain.Configuration;
using SportsMonitor.Domain.Interfaces;
using SportsMonitor.Domain.Models;
using SportsMonitor.Infrastructure.Providers;
using SportsMonitor.Infrastructure.Repositories;
using SportsMonitor.Infrastructure.Resolvers;
using SportsMonitor.Infrastructure.Services;
using SportsMonitor.Infrastructure.Stores;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSignalR();

builder.Services.Configure<ApiFootballOptions>(
    builder.Configuration.GetSection("Providers:ApiFootball"));
builder.Services.Configure<BetsApiOptions>(
    builder.Configuration.GetSection("Providers:BetsApi"));
builder.Services.Configure<SofaScoreOptions>(
    builder.Configuration.GetSection("Providers:SofaScore"));
builder.Services.Configure<Scores365Options>(
    builder.Configuration.GetSection("Providers:Scores365"));
builder.Services.Configure<GoogleSearchOptions>(
    builder.Configuration.GetSection("Providers:Google"));
builder.Services.Configure<DemoOptions>(
    builder.Configuration.GetSection("Demo"));

builder.Services.AddSingleton(Channel.CreateUnbounded<Divergence>(
    new UnboundedChannelOptions { SingleReader = true }));
builder.Services.AddSingleton(sp => sp.GetRequiredService<Channel<Divergence>>().Reader);
builder.Services.AddSingleton(sp => sp.GetRequiredService<Channel<Divergence>>().Writer);

builder.Services.AddSingleton<ISnapshotStore, InMemorySnapshotStore>();
builder.Services.AddSingleton<GoogleSnapshotStore>();
builder.Services.AddSingleton<IMatchResolver, FuzzyMatchResolver>();
builder.Services.AddSingleton<IMatchHistoryRepository>(_ =>
    new JsonlMatchHistoryRepository(Path.Combine(AppContext.BaseDirectory, "data")));

builder.Services.AddSingleton<IDivergenceRule, ScoreMismatchRule>();
builder.Services.AddSingleton<IDivergenceRule, GoalScorerMismatchRule>();
builder.Services.AddSingleton<IDivergenceRule, MissingGoalRule>();
builder.Services.AddSingleton<IDivergenceRule, CardMismatchRule>();
builder.Services.AddSingleton<IDivergenceRule, MatchStatusMismatchRule>();
builder.Services.AddSingleton<DivergenceEngine>();

builder.Services.AddSingleton<IAlertChannel, SignalRAlertChannel>();
builder.Services.AddHostedService<AlertWorker>();
builder.Services.AddHostedService<DemoWorker>();
builder.Services.AddHostedService<GoogleSearchWorker>();

// Register polling workers both as IHostedService and IRefreshable (for manual refresh)
builder.Services.AddSingleton<ApiFootballWorker>();
builder.Services.AddSingleton<BetsApiWorker>();
builder.Services.AddSingleton<SofaScoreWorker>();
builder.Services.AddSingleton<Scores365Worker>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<ApiFootballWorker>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<BetsApiWorker>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<SofaScoreWorker>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<Scores365Worker>());
builder.Services.AddSingleton<IRefreshable>(sp => sp.GetRequiredService<ApiFootballWorker>());
builder.Services.AddSingleton<IRefreshable>(sp => sp.GetRequiredService<BetsApiWorker>());
builder.Services.AddSingleton<IRefreshable>(sp => sp.GetRequiredService<SofaScoreWorker>());
builder.Services.AddSingleton<IRefreshable>(sp => sp.GetRequiredService<Scores365Worker>());

builder.Services.AddHttpClient<ApiFootballProvider>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptionsMonitor<ApiFootballOptions>>().CurrentValue;
    client.BaseAddress = new Uri(options.BaseUrl);
    if (!string.IsNullOrWhiteSpace(options.ApiKey))
        client.DefaultRequestHeaders.Add("x-apisports-key", options.ApiKey);
});
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IOptionsMonitor<ApiFootballOptions>>().CurrentValue);

builder.Services.AddHttpClient<BetsApiProvider>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptionsMonitor<BetsApiOptions>>().CurrentValue;
    client.BaseAddress = new Uri(options.BaseUrl);
});
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IOptionsMonitor<BetsApiOptions>>().CurrentValue);

builder.Services.AddHttpClient<SofaScoreProvider>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptionsMonitor<SofaScoreOptions>>().CurrentValue;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.DefaultRequestHeaders.Add("User-Agent", options.UserAgent);
});
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IOptionsMonitor<SofaScoreOptions>>().CurrentValue);

builder.Services.AddHttpClient<Scores365Provider>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptionsMonitor<Scores365Options>>().CurrentValue;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.DefaultRequestHeaders.Add("User-Agent", options.UserAgent);
    client.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
});
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IOptionsMonitor<Scores365Options>>().CurrentValue);

builder.Services.AddHttpClient<GoogleSearchService>();
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IOptionsMonitor<GoogleSearchOptions>>().CurrentValue);

var app = builder.Build();

var store = app.Services.GetRequiredService<ISnapshotStore>();
var engine = app.Services.GetRequiredService<DivergenceEngine>();
store.SnapshotUpdated += match => _ = engine.EvaluateAsync(match);

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();
app.MapHub<AlertHub>("/hubs/alerts");
app.MapFallbackToFile("index.html");

app.Run();
