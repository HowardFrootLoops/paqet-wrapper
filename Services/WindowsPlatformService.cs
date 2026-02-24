using System.Diagnostics;

namespace PaqetWrapper.Services;

public class WindowsPlatformService : IPlatformService
{
    public string GetBinaryName() => "paqet.exe";

    public void EnsurePcapInstalled()
    {
        if (!Directory.Exists(@"C:\Windows\System32\Npcap"))
            throw new Exception("Npcap not installed.");
    }

    public string GetRouterMac(string gatewayIp)
    {
        return ArpHelper.ParseWindowsArp(gatewayIp);
    }

    public string GetGuidBlock(string adapterId)
    {
        return $"guid: '\\Device\\NPF_{adapterId}'";
    }
}