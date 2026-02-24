namespace PaqetWrapper.Services;

public interface IPlatformService
{
    string GetBinaryName();
    void EnsurePcapInstalled();
    string GetRouterMac(string gatewayIp);
    string GetGuidBlock(string adapterId);
}