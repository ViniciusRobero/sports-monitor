using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging.Abstractions;
using SportsMonitor.Domain.Configuration;
using SportsMonitor.Infrastructure.Providers;
using SportsMonitor.Infrastructure.Resolvers;

var options = LocalAgentOptions.Load(args);
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cts.Cancel();
};

using var bffHttp = new HttpClient
{
    BaseAddress = new Uri(options.BffUrl),
    Timeout = TimeSpan.FromSeconds(30)
};

await using var provider = new SofaScoreProvider(
    options.SofaScore,
    new FuzzyMatchResolver(),
    NullLogger<SofaScoreProvider>.Instance);

var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
jsonOptions.Converters.Add(new JsonStringEnumConverter());

Console.WriteLine($"SportsMonitor.LocalAgent started. BFF: {options.BffUrl}. Interval: {options.IntervalSeconds}s.");

while (!cts.Token.IsCancellationRequested)
{
    try
    {
        var matches = await provider.GetLiveMatchesAsync(cts.Token);
        using var response = await bffHttp.PostAsJsonAsync("/api/relay/sofascore", matches, jsonOptions, cts.Token);
        response.EnsureSuccessStatusCode();

        Console.WriteLine($"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss} relayed {matches.Count} SofaScore matches.");
    }
    catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
    {
        break;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss} relay failed: {ex.Message}");
    }

    try
    {
        await Task.Delay(TimeSpan.FromSeconds(options.IntervalSeconds), cts.Token);
    }
    catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
    {
        break;
    }
}

Console.WriteLine("SportsMonitor.LocalAgent stopped.");

internal sealed class LocalAgentOptions
{
    public string BffUrl { get; set; } = "http://34.151.245.70";
    public int IntervalSeconds { get; set; } = 30;
    public SofaScoreOptions SofaScore { get; set; } = new();

    public static LocalAgentOptions Load(string[] args)
    {
        var options = LoadFromFile();

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--bff-url" when i + 1 < args.Length:
                    options.BffUrl = args[++i];
                    break;
                case "--interval" when i + 1 < args.Length && int.TryParse(args[i + 1], out var interval):
                    options.IntervalSeconds = interval;
                    i++;
                    break;
                case "--sofascore-url" when i + 1 < args.Length:
                    options.SofaScore.BaseUrl = args[++i];
                    break;
            }
        }

        options.BffUrl = options.BffUrl.TrimEnd('/');
        options.IntervalSeconds = Math.Max(1, options.IntervalSeconds);
        return options;
    }

    private static LocalAgentOptions LoadFromFile()
    {
        const string fileName = "appsettings.json";
        var path = File.Exists(fileName)
            ? fileName
            : Path.Combine(AppContext.BaseDirectory, fileName);

        if (!File.Exists(path))
            return new LocalAgentOptions();

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<LocalAgentOptions>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new LocalAgentOptions();
    }
}
