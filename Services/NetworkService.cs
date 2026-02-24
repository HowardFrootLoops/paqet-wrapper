using System.Net.NetworkInformation;

public class NetworkService
{
    public NetworkInterface GetActiveAdapter()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .First(n =>
                n.OperationalStatus == OperationalStatus.Up &&
                n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                n.GetIPProperties().GatewayAddresses.Any());
    }
}