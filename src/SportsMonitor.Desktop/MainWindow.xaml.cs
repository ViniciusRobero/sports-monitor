using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;

namespace SportsMonitor.Desktop;

public partial class MainWindow : Window
{
    private const string BffUrl = "http://localhost:5000";
    private Process? _bffProcess;
    private readonly string _logPath;

    public MainWindow()
    {
        InitializeComponent();
        _logPath = Path.Combine(AppContext.BaseDirectory, "logs", "bff.log");
        Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Log("Desktop started.");
        StartBff();

        await WebView.EnsureCoreWebView2Async();
        WebView.CoreWebView2.Navigate("about:blank");

        Log("Waiting for BFF to be ready...");
        var ready = await WaitForBffAsync();

        if (ready)
        {
            Log("BFF ready — navigating.");
            WebView.CoreWebView2.Navigate(BffUrl);
        }
        else
        {
            Log("ERROR: BFF did not respond after 30s. Check logs/bff.log");
            MessageBox.Show(
                $"O BFF não respondeu.\nVerifique o arquivo de log:\n{_logPath}",
                "Sports Monitor — Erro de inicialização",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task<bool> WaitForBffAsync()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        for (int i = 0; i < 60; i++)
        {
            try
            {
                var r = await http.GetAsync($"{BffUrl}/api/matches/live");
                if (r.IsSuccessStatusCode) return true;
            }
            catch { }
            await Task.Delay(500);
        }
        return false;
    }

    private void StartBff()
    {
        var appDir = AppContext.BaseDirectory;

        // published mode: SportsMonitor.Bff.exe alongside Desktop
        var bffExe = Path.Combine(appDir, "SportsMonitor.Bff.exe");
        if (File.Exists(bffExe))
        {
            LaunchProcess(bffExe, null, appDir);
            return;
        }

        // dev mode: dotnet SportsMonitor.Bff.dll
        var bffDll = Path.GetFullPath(Path.Combine(appDir, "..", "..", "..", "..",
            "SportsMonitor.Bff", "bin", "Debug", "net10.0", "SportsMonitor.Bff.dll"));
        if (File.Exists(bffDll))
        {
            LaunchProcess("dotnet", $"\"{bffDll}\"", Path.GetDirectoryName(bffDll)!);
            return;
        }

        Log("ERROR: SportsMonitor.Bff.exe not found. Make sure it is in the same folder.");
    }

    private void LaunchProcess(string exe, string? args, string workDir)
    {
        var si = new ProcessStartInfo(exe)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        if (args is not null) si.Arguments = args;

        _bffProcess = new Process { StartInfo = si };
        _bffProcess.OutputDataReceived += (_, e) => { if (e.Data != null) Log(e.Data); };
        _bffProcess.ErrorDataReceived += (_, e) => { if (e.Data != null) Log("[ERR] " + e.Data); };
        _bffProcess.Start();
        _bffProcess.BeginOutputReadLine();
        _bffProcess.BeginErrorReadLine();

        Log($"BFF process started (PID {_bffProcess.Id}).");
    }

    private void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        try { File.AppendAllText(_logPath, line + Environment.NewLine); }
        catch { }
        Debug.WriteLine(line);
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        Log("Desktop closing — killing BFF.");
        try { _bffProcess?.Kill(entireProcessTree: true); }
        catch { }
    }
}
