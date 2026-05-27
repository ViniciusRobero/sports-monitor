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
        var bffDll = FindBffDll();
        if (bffDll is null) return;

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

    private static string? FindBffDll()
    {
        var appDir = AppContext.BaseDirectory;

        // when running from solution root (dev: dotnet run)
        var devPath = Path.Combine(appDir, "..", "..", "..", "..",
            "SportsMonitor.Bff", "bin", "Debug", "net10.0", "SportsMonitor.Bff.dll");
        if (File.Exists(devPath)) return Path.GetFullPath(devPath);

        // when deployed alongside BFF dll
        var sideBySide = Path.Combine(appDir, "SportsMonitor.Bff.dll");
        if (File.Exists(sideBySide)) return sideBySide;

        return null;
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        try { _bffProcess?.Kill(entireProcessTree: true); }
        catch { /* process already exited */ }
    }
}
