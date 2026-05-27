using System.Diagnostics;
using System.IO;
using System.Windows;

namespace SportsMonitor.Desktop;

public partial class MainWindow : Window
{
    private const string BffUrl = "http://localhost:5000";
    private Process? _bffProcess;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        StartBff();
        await WebView.EnsureCoreWebView2Async();
        WebView.CoreWebView2.Navigate(BffUrl);
    }

    private void StartBff()
    {
        var appDir = AppContext.BaseDirectory;

        // published mode: SportsMonitor.Bff.exe sits alongside the Desktop exe
        var bffExe = Path.Combine(appDir, "SportsMonitor.Bff.exe");
        if (File.Exists(bffExe))
        {
            _bffProcess = new Process
            {
                StartInfo = new ProcessStartInfo(bffExe)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = appDir
                }
            };
            _bffProcess.Start();
            return;
        }

        // dev mode: launch via dotnet SportsMonitor.Bff.dll
        var bffDll = Path.GetFullPath(Path.Combine(appDir, "..", "..", "..", "..",
            "SportsMonitor.Bff", "bin", "Debug", "net10.0", "SportsMonitor.Bff.dll"));
        if (File.Exists(bffDll))
        {
            _bffProcess = new Process
            {
                StartInfo = new ProcessStartInfo("dotnet", $"\"{bffDll}\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(bffDll)!
                }
            };
            _bffProcess.Start();
        }
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        try { _bffProcess?.Kill(entireProcessTree: true); }
        catch { /* process already exited */ }
    }
}
