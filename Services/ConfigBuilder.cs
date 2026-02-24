using System.Net.NetworkInformation;
using PaqetWrapper.Models;
using PaqetWrapper.Services;

public class ConfigBuilder
{
    public static void Build(
        UserSettings settings,
        NetworkInterface adapter,
        string routerMac,
        IPlatformService platform)
    {
        string template = File.ReadAllText("Templates/config.template.yaml");

        string guidBlock = platform.GetGuidBlock(adapter.Id);

        template = template
            .Replace("{{LogLevel}}", settings.LogLevel)
            .Replace("{{SocksPort}}", settings.SocksPort)
            .Replace("{{InterfaceName}}", adapter.Name)
            .Replace("{{IP}}",
                adapter.GetIPProperties().UnicastAddresses
                    .First(a => a.Address.AddressFamily ==
                        System.Net.Sockets.AddressFamily.InterNetwork)
                    .Address.ToString())
            .Replace("{{RouterMac}}", routerMac)
            .Replace("{{ServerIP}}", settings.ServerIP)
            .Replace("{{ServerPort}}", settings.ServerPort)
            .Replace("{{Secret}}", settings.Secret)
            .Replace("{{GuidBlock}}", guidBlock);

        File.WriteAllText("config.yaml", template);
    }
}