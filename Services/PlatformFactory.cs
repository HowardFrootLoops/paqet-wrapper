using System.Runtime.InteropServices;

namespace PaqetWrapper.Services;

public static class PlatformFactory
{
    public static IPlatformService GetPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new WindowsPlatformService();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return new LinuxPlatformService();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new MacPlatformService();

        throw new Exception("Unsupported OS");
    }
}