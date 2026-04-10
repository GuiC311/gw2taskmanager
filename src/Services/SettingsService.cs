using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GW2TaskManager.Models;

namespace GW2TaskManager.Services;

public class SettingsService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    private readonly string _settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "GW2TaskManager", "settings.json");

    private AppSettings _settings = new();

    public AppSettings Settings => _settings;

    public void Load()
    {
        try
        {
            if (!File.Exists(_settingsPath)) return;
            var json = File.ReadAllText(_settingsPath);
            _settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOpts) ?? new();
        }
        catch { _settings = new(); }
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        var json = JsonSerializer.Serialize(_settings, JsonOpts);
        File.WriteAllText(_settingsPath, json);
    }

    // DPAPI encrypt/decrypt for the API key
    public void SetApiKey(string plainKey)
    {
        if (string.IsNullOrWhiteSpace(plainKey))
        {
            _settings.ApiKey = string.Empty;
            return;
        }
        var bytes = Encoding.UTF8.GetBytes(plainKey);
        var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        _settings.ApiKey = Convert.ToBase64String(encrypted);
    }

    public string GetApiKey()
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey)) return string.Empty;
        try
        {
            var encrypted = Convert.FromBase64String(_settings.ApiKey);
            var bytes = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        catch { return string.Empty; }
    }
}
