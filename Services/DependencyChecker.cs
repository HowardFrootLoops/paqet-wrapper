namespace PaqetWrapper.Services;

public class DependencyChecker
{
    private readonly IPlatformService _platformService;

    public DependencyChecker()
    {
        _platformService = PlatformFactory.GetPlatform();
    }

    public void CheckDependencies()
    {
        try
        {
            _platformService.EnsurePcapInstalled();
            Console.WriteLine("All required dependencies are installed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Dependency check failed: {ex.Message}");
            Environment.Exit(1);
        }
    }
}