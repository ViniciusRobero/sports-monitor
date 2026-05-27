using System.Text.Json.Serialization;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using SportsMonitor.Application;
using SportsMonitor.Application.Rules;
using SportsMonitor.Bff.Alerts;
using SportsMonitor.Bff.Hubs;
using SportsMonitor.Domain.Configuration;
using SportsMonitor.Domain.Interfaces;
using SportsMonitor.Domain.Models;
using SportsMonitor.Infrastructure.Providers;
using SportsMonitor.Infrastructure.Repositories;
using SportsMonitor.Infrastructure.Resolvers;
using SportsMonitor.Infrastructure.Stores;
using SportsMonitor.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSignalR();

builder.Services.Configure<ApiFootballOptions>(
    builder.Configuration.GetSection("Providers:ApiFootball"));
builder.Services.Configure<BetsApiOptions>(
    builder.Configuration.GetSection("Providers:BetsApi"));

builder.Services.AddSingleton(Channel.CreateUnbounded<Divergence>(
    new UnboundedChannelOptions { SingleReader = true }));
builder.Services.AddSingleton(sp => sp.GetRequiredService<Channel<Divergence>>().Reader);
builder.Services.AddSingleton(sp => sp.GetRequiredService<Channel<Divergence>>().Writer);

builder.Services.AddSingleton<ISnapshotStore, InMemorySnapshotStore>();
builder.Services.AddSingleton<IMatchResolver, FuzzyMatchResolver>();
builder.Services.AddSingleton<IMatchHistoryRepository>(_ =>
    new JsonlMatchHistoryRepository(Path.Combine(AppContext.BaseDirectory, "data")));

builder.Services.AddSingleton<IDivergenceRule, ScoreMismatchRule>();
builder.Services.AddSingleton<IDivergenceRule, GoalScorerMismatchRule>();
builder.Services.AddSingleton<IDivergenceRule, MissingGoalRule>();
builder.Services.AddSingleton<DivergenceEngine>();

builder.Services.AddSingleton<IAlertChannel, SignalRAlertChannel>();
builder.Services.AddHostedService<AlertWorker>();
builder.Services.AddHostedService<ApiFootballWorker>();
builder.Services.AddHostedService<BetsApiWorker>();

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

var app = builder.Build();

var store = app.Services.GetRequiredService<ISnapshotStore>();
var engine = app.Services.GetRequiredService<DivergenceEngine>();
store.SnapshotUpdated += match => _ = engine.EvaluateAsync(match);

app.MapControllers();
app.MapHub<AlertHub>("/hubs/alerts");

app.Run();
