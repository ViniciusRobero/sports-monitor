using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
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
        """);
    return;
}

var options = LocalAgentOptions.Load(args);

Console.Title = "SportsMonitor LocalAgent";
Console.WriteLine("============================================");
Console.WriteLine("  SportsMonitor LocalAgent");
Console.WriteLine("============================================");
Console.WriteLine($"  BFF:       {options.BffUrl}");
Console.WriteLine($"  Intervalo: {options.IntervalSeconds}s");
Console.WriteLine("  Para parar: feche esta janela ou Ctrl+C");
Console.WriteLine("============================================");
Console.WriteLine();

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

// Loop externo — reinicia tudo (inclusive Playwright) em caso de falha
while (!cts.Token.IsCancellationRequested)
{
    try
    {
        await RunAsync(options, cts.Token);
    }
    catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
    {
        break;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine();
        Console.Error.WriteLine($"[ERRO] {ex.Message}");
        Console.Error.WriteLine("[INFO] Reiniciando em 15s... (Ctrl+C para parar)");
        Console.Error.WriteLine();
        try { await Task.Delay(TimeSpan.FromSeconds(15), cts.Token); }
        catch (OperationCanceledException) { break; }
    }
}

Console.WriteLine();
Console.WriteLine("Agente parado. Pressione qualquer tecla para fechar...");
Console.ReadKey(intercept: true);

static async Task RunAsync(LocalAgentOptions options, CancellationToken ct)
{
    // Environment.ProcessPath aponta para o exe real (não a pasta temp do single-file)
    var exeDir = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
    var playwrightDir = Path.Combine(exeDir, ".playwright");

    // Força Playwright a instalar/usar browsers na pasta .playwright ao lado do exe.
    // Sem isso, ele procura em %USERPROFILE%\.playwright ou %LOCALAPPDATA%\ms-playwright
    // e falha com "Driver not found" na primeira execução.
    Environment.SetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH", playwrightDir);
    Directory.CreateDirectory(playwrightDir);

    Console.WriteLine("Verificando navegador (pode baixar ~150MB na primeira vez)...");
    var prev = Directory.GetCurrentDirectory();
    Directory.SetCurrentDirectory(exeDir);
    var exitCode = Microsoft.Playwright.Program.Main(["install", "chromium"]);
    Directory.SetCurrentDirectory(prev);
    if (exitCode != 0)
        throw new Exception("Falha ao instalar o Playwright. Verifique sua conexao com a internet.");
    Console.WriteLine("Navegador OK.");
    Console.WriteLine();

    Console.WriteLine("Iniciando monitoramento...");
    Console.WriteLine();

    using var bffHttp = new HttpClient
    {
        BaseAddress = new Uri(options.BffUrl),
        Timeout = TimeSpan.FromSeconds(30)
    };

    if (!string.IsNullOrWhiteSpace(options.AgentKey))
        bffHttp.DefaultRequestHeaders.Add("X-Agent-Key", options.AgentKey);

    using var loggerFactory = LoggerFactory.Create(b =>
        b.AddSimpleConsole(o => { o.SingleLine = true; o.TimestampFormat = "HH:mm:ss "; })
         .SetMinimumLevel(LogLevel.Debug));

    await using var provider = new SofaScoreProvider(
        options.SofaScore,
        new FuzzyMatchResolver(),
        loggerFactory.CreateLogger<SofaScoreProvider>());

    var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    jsonOptions.Converters.Add(new JsonStringEnumConverter());

    while (!ct.IsCancellationRequested)
    {
        try
        {
            var matches = await provider.GetLiveMatchesAsync(ct);
            using var response = await bffHttp.PostAsJsonAsync("/api/relay/sofascore", matches, jsonOptions, ct);
            response.EnsureSuccessStatusCode();
            Console.WriteLine($"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss} relayed {matches.Count} SofaScore matches.");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss} [AVISO] {ex.Message}");
        }

        try { await Task.Delay(TimeSpan.FromSeconds(options.IntervalSeconds), ct); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { return; }
    }
}

internal sealed class LocalAgentOptions
{
    public string BffUrl { get; set; } = "http://34.151.245.70";
    public int IntervalSeconds { get; set; } = 90;
    public string AgentKey { get; set; } = "";
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
