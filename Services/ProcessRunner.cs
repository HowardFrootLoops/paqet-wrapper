using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

public class ProcessRunner
{
    private static Process? _process = null;
    private static bool _userStopped = false;
    public static bool _ctrlC = false;

    public static void Run(string binary, string configFile, string logFile, string socksPort)
    {
        int restartCount = 0;
        const int maxRestarts = 5;
        _userStopped = false;

        var handler = new ConsoleCancelEventHandler((s, e) =>
        {
            Console.WriteLine("Ctrl+C detected. Stopping...");
            e.Cancel = false;
            _userStopped = true;
            _ctrlC = true;
            if (_process != null && !_process.HasExited)
                _process.Kill(entireProcessTree: true);
        });

        Console.CancelKeyPress += handler;

        AppDomain.CurrentDomain.ProcessExit += (_, __) =>
        {
            try
            {
                if (_process != null && !_process.HasExited)
                    _process.Kill(entireProcessTree: true);
            }
            catch { /* ignore */ }
        };

        var cts = new CancellationTokenSource();
        StartLogRotationThread(logFile, 10 * 1024 * 1024, cts.Token);

        while (restartCount < maxRestarts)
        {
            _process = new Process();
            _process.StartInfo = new ProcessStartInfo(binary, $"run -c {configFile}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            _process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null)
                    File.AppendAllText(logFile, e.Data + Environment.NewLine);
            };

            _process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null)
                    File.AppendAllText(logFile, "[ERR] " + e.Data + Environment.NewLine);
            };

            RotateLogIfNeeded(logFile, 10 * 1024 * 1024);

            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            Console.WriteLine($"Paqet running on 127.0.0.1:{socksPort}");
            Console.WriteLine("Press Q to stop");

            while (!_process.HasExited)
            {
                if (_userStopped)
                    break;

                if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Q)
                {
                    _userStopped = true;
                    _process.Kill(entireProcessTree: true);
                    break;
                }

                Thread.Sleep(200);
            }

            _process.WaitForExit();

            int exitCode = _process.ExitCode;

            _process.Dispose();
            _process = null;

            if (_userStopped)
            {
                Console.WriteLine("Stopped by user.");
                break;
            }

            restartCount++;

            if (restartCount >= maxRestarts)
            {
                Console.WriteLine("Too many crashes. Giving up.");
                break;
            }

            Console.WriteLine($"Paqet crashed (code {exitCode}). Restarting in 3 seconds... ({restartCount}/{maxRestarts})");
            Thread.Sleep(3000);
        }

        cts.Cancel();
        Console.CancelKeyPress -= handler;
    }

    private static void StartLogRotationThread(string logFile, int maxSizeBytes, CancellationToken token)
    {
        new Thread(() =>
        {
            while (!token.IsCancellationRequested)
            {
                try { RotateLogIfNeeded(logFile, maxSizeBytes); }
                catch { }
                Thread.Sleep(5 * 60 * 1000);
            }
        })
        { IsBackground = true }.Start();
    }

    private static void RotateLogIfNeeded(string logFile, int maxSizeBytes)
    {
        if (!File.Exists(logFile)) return;

        if (new FileInfo(logFile).Length > maxSizeBytes)
        {
            string oldLog = logFile + ".old";
            if (File.Exists(oldLog)) File.Delete(oldLog);
            File.Move(logFile, oldLog);
        }
    }
}