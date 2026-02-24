using PaqetWrapper.Services;
using PaqetWrapper.Models;
using System.Runtime.InteropServices;

namespace PaqetWrapper;

class Program
{
    static void Main()
    {
        var dependencyChecker = new DependencyChecker();
        dependencyChecker.CheckDependencies();
        
        var settingsService = new SettingsService();
        var settings = settingsService.Load();

        while (true)
        {
            if (!ProcessRunner._ctrlC) {
                Console.WriteLine("\n=== Paqet Wrapper ===");
                Console.WriteLine("1) Run");
                Console.WriteLine("2) Reconfigure");
                Console.WriteLine("3) View Settings");
                Console.WriteLine("4) Exit");
                Console.Write("Select: ");

                var key = Console.ReadLine();

                switch (key)
                {
                    case "1":
                        Run(settings);
                        break;

                    case "2":
                        settings = settingsService.Configure(settings);
                        break;

                    case "3":
                        settingsService.Show(settings);
                        break;

                    case "4":
                        return;
                }
            }
        }
    }

    static void Run(UserSettings settings)
    {
        var platform = PlatformFactory.GetPlatform();
        platform.EnsurePcapInstalled();

        var networkService = new NetworkService();
        var adapter = networkService.GetActiveAdapter();

        var gateway = adapter.GetIPProperties()
            .GatewayAddresses.First().Address.ToString();

        var routerMac = platform.GetRouterMac(gateway);

        ConfigBuilder.Build(settings, adapter, routerMac, platform);

        ProcessRunner.Run(platform.GetBinaryName(), "config.yaml", "paqet.log", settings.SocksPort);
    }
}