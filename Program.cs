using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;
using System.Runtime.InteropServices;

class Program
{
    static string settingsPath = "user-settings.json";

    static void Main()
    {
        while (true)
        {
            Console.WriteLine("\n=== Paqet Wrapper ===");
            Console.WriteLine("1) Run");
            Console.WriteLine("2) Reconfigure");
            Console.WriteLine("3) Regenerate Secret");
            Console.WriteLine("4) View Current Settings");
            Console.WriteLine("5) Exit");
            Console.Write("Select: ");

            var key = Console.ReadLine();

            switch (key)
            {
                case "1":
                    EnsureNpcap();
                    var settings = LoadOrCreateSettings(false);
                    BuildConfig(settings);
                    RunPaqet(settings);
                    break;

                case "2":
                    LoadOrCreateSettings(true);
                    break;

                case "3":
                    RegenerateSecret();
                    break;
                
                case "4":
                    ShowSettings();
                    break;

                case "5":
                    return;
            }
        }
    }

    // ================= SETTINGS =================

    static UserSettings LoadOrCreateSettings(bool forceReconfigure)
    {
        UserSettings settings = new UserSettings();

        if (File.Exists(settingsPath))
            settings = JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(settingsPath));

        if (!File.Exists(settingsPath) || forceReconfigure)
        {
            Console.Write($"Server IP ({settings.ServerIP}): ");
            var input = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(input))
                settings.ServerIP = input;

            Console.Write($"Server Port ({settings.ServerPort}): ");
            input = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(input))
                settings.ServerPort = input;

            Console.Write($"SOCKS Port ({settings.SocksPort ?? "1080"}): ");
            input = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(input))
                settings.SocksPort = input;
            else if (string.IsNullOrWhiteSpace(settings.SocksPort))
                settings.SocksPort = "1080";

            Console.Write($"Log Level ({settings.LogLevel ?? "info"}): ");
            input = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(input))
                settings.LogLevel = input.ToLower();
            else if (string.IsNullOrWhiteSpace(settings.LogLevel))
                settings.LogLevel = "info";

            Console.Write("Change Secret? (y/N): ");
            input = Console.ReadLine();
            if (input?.ToLower() == "y")
            {
                Console.Write("Enter new secret (leave empty to auto-generate): ");
                var sec = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(sec))
                {
                    settings.Secret = GenerateSecret();
                    Console.WriteLine($"Generated: {settings.Secret}");
                }
                else
                {
                    settings.Secret = sec;
                }
            }

            SaveSettings(settings);
        }

        return settings;
    }

    static void SaveSettings(UserSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(settingsPath, json);
    }

    static void RegenerateSecret()
    {
        if (!File.Exists(settingsPath))
        {
            Console.WriteLine("No settings found.");
            return;
        }

        var settings = JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(settingsPath));
        settings.Secret = GenerateSecret();
        SaveSettings(settings);

        Console.WriteLine($"New Secret: {settings.Secret}");
    }

    static void ShowSettings()
    {
        if (!File.Exists(settingsPath))
        {
            Console.WriteLine("No settings found.");
            return;
        }

        var settings = JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(settingsPath));

        Console.WriteLine("\n--- Current Settings ---");
        Console.WriteLine($"Server: {settings.ServerIP}:{settings.ServerPort}");
        Console.WriteLine($"SOCKS Port: {settings.SocksPort}");
        Console.WriteLine($"Log Level: {settings.LogLevel}");
        Console.WriteLine($"Secret: {settings.Secret}");
    }

    // ================= NETWORK =================

    static NetworkInterface GetActiveAdapter()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(n =>
                n.OperationalStatus == OperationalStatus.Up &&
                n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                n.GetIPProperties().GatewayAddresses.Count > 0)
            .FirstOrDefault();
    }

    static string GetRouterMac(string gatewayIp)
    {
        var psi = new ProcessStartInfo("arp", "-a")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var p = Process.Start(psi);
        string output = p.StandardOutput.ReadToEnd();

        var line = output.Split('\n')
            .FirstOrDefault(l => l.Contains(gatewayIp));

        if (line == null) return "";

        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 ? parts[1] : "";
    }

    // ================= CONFIG =================

    static void BuildConfig(UserSettings settings)
    {
        var adapter = GetActiveAdapter();

        var ip = adapter.GetIPProperties()
            .UnicastAddresses
            .First(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            .Address.ToString();

        var gateway = adapter.GetIPProperties()
            .GatewayAddresses.First().Address.ToString();

        var routerMac = GetRouterMac(gateway);

        var npfGuid = $"\\Device\\NPF_{adapter.Id}";

        string yaml = $@"
role: ""client""

log:
  level: ""info""

socks5:
  - listen: ""127.0.0.1:{settings.SocksPort}""
    username: """"
    password: """"

network:
  interface: ""{adapter.Name}""
  guid: '{npfGuid}'

  ipv4:
    addr: ""{ip}:0""
    router_mac: ""{routerMac}""

  tcp:
    local_flag: [""PA""]
    remote_flag: [""PA""]
  
  pcap:
    sockbuf: 4194304

server:
  addr: ""{settings.ServerIP}:{settings.ServerPort}""

transport:
  protocol: ""kcp""
  conn: 1

  kcp:
    mode: ""fast""
    mtu: 1200
    rcvwnd: 512
    sndwnd: 512

    block: ""aes""
    key: ""{settings.Secret}""

    smuxbuf: 4194304
    streambuf: 2097152
";

        File.WriteAllText("config.yaml", yaml);
    }

    // ================= EXECUTION =================

    static string GenerateSecret()
    {
        var psi = new ProcessStartInfo("paqet.exe", "secret")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var p = Process.Start(psi);
        return p.StandardOutput.ReadToEnd().Trim();
    }

    static void RunPaqet(UserSettings settings)
    {
        int restartCount = 0;
        const int maxRestarts = 5;
        bool userStopped = false;

        while (true)
        {
            RotateLogIfNeeded();

            var psi = new ProcessStartInfo("paqet.exe", "run -c config.yaml")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var process = new Process();
            process.StartInfo = psi;
            process.EnableRaisingEvents = true;

            process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null)
                    File.AppendAllText("paqet.log", e.Data + Environment.NewLine);
            };

            process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null)
                    File.AppendAllText("paqet.log", e.Data + Environment.NewLine);
            };

            process.Start();

            // var handle = process.Handle;
            var job = new JobObject();
            job.AddProcess(process);

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            Console.WriteLine($"\nRunning on 127.0.0.1:{settings.SocksPort}");
            Console.WriteLine("Press Q to stop.\n");

            while (!process.HasExited)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Q)
                    {
                        userStopped = true;
                        process.Kill(true);
                        break;
                    }
                }

                Thread.Sleep(200);
            }

            process.WaitForExit();

            if (userStopped)
            {
                Console.WriteLine("Stopped by user.");
                break;
            }

            if (process.ExitCode == 0)
            {
                Console.WriteLine("Paqet exited normally.");
                break;
            }

            restartCount++;

            if (restartCount >= maxRestarts)
            {
                Console.WriteLine("Too many crashes. Giving up.");
                break;
            }

            Console.WriteLine($"Paqet crashed (code {process.ExitCode}). Restarting in 3 seconds... ({restartCount}/{maxRestarts})");
            Thread.Sleep(3000);
        }
    }

    static void RotateLogIfNeeded()
    {
        string logPath = "paqet.log";

        if (File.Exists(logPath))
        {
            var size = new FileInfo(logPath).Length;

            if (size > 5 * 1024 * 1024) // 5MB
            {
                if (File.Exists("paqet_old.log"))
                    File.Delete("paqet_old.log");

                File.Move(logPath, "paqet_old.log");
            }
        }
    }

    // ================= NPCAP =================

    static void EnsureNpcap()
    {
        // bool installed = ServiceController.GetServices()
        //    .Any(s => s.ServiceName.ToLower().Contains("npcap"));

        //if (!installed)
        if (!Directory.Exists(@"C:\Windows\System32\Npcap"))
        {
            Console.WriteLine("Npcap not installed.");
            if (File.Exists("npcap-installer.exe"))
            {
                Console.WriteLine("Running installer...");
                Process.Start("npcap-installer.exe").WaitForExit();
            }
            else
            {
                Console.WriteLine("npcap-installer.exe not found.");
                Environment.Exit(1);
            }
        }
    }
}

class UserSettings
{
    public string ServerIP { get; set; }
    public string ServerPort { get; set; }
    public string Secret { get; set; }
    public string SocksPort { get; set; }
    public string LogLevel { get; set; }
}

class JobObject : IDisposable
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string name);

    [DllImport("kernel32.dll")]
    static extern bool SetInformationJobObject(IntPtr hJob, int infoType, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

    const int JobObjectExtendedLimitInformation = 9;
    const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x00002000;

    IntPtr handle;

    public JobObject()
    {
        handle = CreateJobObject(IntPtr.Zero, null);

        var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
        info.BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;

        int length = Marshal.SizeOf(info);
        IntPtr extendedInfoPtr = Marshal.AllocHGlobal(length);
        Marshal.StructureToPtr(info, extendedInfoPtr, false);

        SetInformationJobObject(handle, JobObjectExtendedLimitInformation, extendedInfoPtr, (uint)length);
    }

    public void AddProcess(Process process)
    {
        AssignProcessToJobObject(handle, process.Handle);
    }

    public void Dispose()
    {
        Marshal.FreeHGlobal(handle);
    }

    [StructLayout(LayoutKind.Sequential)]
    struct JOBOBJECT_BASIC_LIMIT_INFORMATION
    {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public uint LimitFlags;
        public UIntPtr MinimumWorkingSetSize;
        public UIntPtr MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public long Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct IO_COUNTERS
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
    {
        public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
        public IO_COUNTERS IoInfo;
        public UIntPtr ProcessMemoryLimit;
        public UIntPtr JobMemoryLimit;
        public UIntPtr PeakProcessMemoryUsed;
        public UIntPtr PeakJobMemoryUsed;
    }
}