using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging.Abstractions;
using SportsMonitor.Domain.Configuration;
using SportsMonitor.Infrastructure.Providers;
using SportsMonitor.Infrastructure.Resolvers;

if (args.Contains("--help") || args.Contains("-h"))
{
    Console.WriteLine("""
        SportsMonitor.LocalAgent — relay SofaScore data to a remote BFF

        Usage:
          SportsMonitor.LocalAgent.exe [options]

        Options:
          --bff-url <url>      BFF server URL  (default: value in appsettings.json)
          --interval <seconds> Polling interval (default: 30)
          --help, -h           Show this help

        Config file (edit to change defaults):
          appsettings.json  — must be in the same folder as the .exe
          Key: "BffUrl"     — URL of the remote BFF (e.g. "http://34.151.245.70")

        Example — point to a different server:
          SportsMonitor.LocalAgent.exe --bff-url http://NOVO-IP
        """);
    return;
}

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

using var sofaHttp = new HttpClient();
var provider = new SofaScoreHttpProvider(
    sofaHttp,
    options.SofaScore,
    new FuzzyMatchResolver(),
    NullLogger<SofaScoreHttpProvider>.Instance);

var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
jsonOptions.Converters.Add(new JsonStringEnumConverter());

Console.WriteLine($"SportsMonitor.LocalAgent iniciado.");
Console.WriteLine($"  BFF:      {options.BffUrl}");
Console.WriteLine($"  Intervalo: {options.IntervalSeconds}s");
Console.WriteLine($"  Para mudar o servidor: edite appsettings.json (BffUrl) ou use --bff-url <url>");
Console.WriteLine("Pressione Ctrl+C para parar.");
Console.WriteLine();

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

Console.WriteLine("SportsMonitor.LocalAgent parado.");

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
