using System.Diagnostics;

namespace PaqetWrapper.Services;

public static class ArpHelper
{
    public static string ParseWindowsArp(string gatewayIp)
    {
        return RunAndParse("arp", "-a", gatewayIp);
    }

    public static string ParseLinuxArp(string gatewayIp)
    {
        return RunAndParse("arp", "-n", gatewayIp);
    }

    public static string ParseMacArp(string gatewayIp)
    {
        return RunAndParse("arp", "-n", gatewayIp);
    }

    private static string RunAndParse(string file, string args, string gatewayIp)
    {
        var psi = new ProcessStartInfo(file, args)
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = Process.Start(psi);
        if (process == null) return "";
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        var line = output.Split('\n')
            .FirstOrDefault(l => l.Contains(gatewayIp));

        if (line == null) return "";

        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return parts.FirstOrDefault(p => p.Contains(":") || p.Contains("-")) ?? "";
    }
}