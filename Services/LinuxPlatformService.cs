using System.Diagnostics;

namespace PaqetWrapper.Services;

public class LinuxPlatformService : IPlatformService
{
    public string GetBinaryName() => "./paqet";

    public void EnsurePcapInstalled()
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dpkg",
                Arguments = "-l | grep libpcap",  // دستور برای چک کردن libpcap
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        if (string.IsNullOrEmpty(output) || !output.Contains("libpcap"))
        {
            throw new Exception("libpcap is not installed. Please install libpcap.");
        }
    }

    public string GetRouterMac(string gatewayIp)
    {
        return ArpHelper.ParseLinuxArp(gatewayIp);
    }

    public string GetGuidBlock(string adapterId)
    {
        return ""; // not needed
    }
}