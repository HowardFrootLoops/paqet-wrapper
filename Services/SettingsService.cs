using System.Text.Json;
using PaqetWrapper.Models;

namespace PaqetWrapper.Services;

public class SettingsService
{
    private const string SettingsPath = "user-settings.json";

    public UserSettings Load()
    {
        if (!File.Exists(SettingsPath))
            return Configure(new UserSettings());

        var settings = JsonSerializer.Deserialize<UserSettings>(
            File.ReadAllText(SettingsPath));

        return settings ?? new UserSettings();
    }

    public void Save(UserSettings settings)
    {
        var json = JsonSerializer.Serialize(
            settings,
            new JsonSerializerOptions { WriteIndented = true });

        File.WriteAllText(SettingsPath, json);
    }

    public UserSettings Configure(UserSettings existing)
    {
        Console.WriteLine("\n=== Configuration ===");

        Console.Write($"Server IP ({existing.ServerIP}): ");
        var input = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(input))
            existing.ServerIP = input;

        Console.Write($"Server Port ({existing.ServerPort}): ");
        input = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(input))
            existing.ServerPort = input;

        Console.Write($"SOCKS Port ({existing.SocksPort ?? "1080"}): ");
        input = Console.ReadLine();
        existing.SocksPort = string.IsNullOrWhiteSpace(input)
            ? existing.SocksPort ?? "1080"
            : input;

        Console.Write($"Log Level ({existing.LogLevel ?? "info"}): ");
        input = Console.ReadLine();
        existing.LogLevel = string.IsNullOrWhiteSpace(input)
            ? existing.LogLevel ?? "info"
            : input.ToLower();

        Console.Write("Change Secret? (y/N): ");
        input = Console.ReadLine();
        if (input?.ToLower() == "y")
        {
            Console.Write("Enter new secret: ");
            existing.Secret = Console.ReadLine() ?? "";
        }

        Save(existing);

        return existing;
    }

    public void Show(UserSettings settings)
    {
        Console.WriteLine("\n--- Current Settings ---");
        Console.WriteLine($"Server: {settings.ServerIP}:{settings.ServerPort}");
        Console.WriteLine($"SOCKS Port: {settings.SocksPort}");
        Console.WriteLine($"Log Level: {settings.LogLevel}");
        Console.WriteLine($"Secret: {settings.Secret}");
    }
}