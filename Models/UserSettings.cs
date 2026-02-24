namespace PaqetWrapper.Models;

public class UserSettings
{
    public string ServerIP { get; set; } = "";
    public string ServerPort { get; set; } = "";
    public string Secret { get; set; } = "";
    public string SocksPort { get; set; } = "1080";
    public string LogLevel { get; set; } = "info";
}