namespace GW2TaskManager.Models;

public class AppSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Language { get; set; } = "fr";
    public string Theme    { get; set; } = "auto";  // "warm" | "slate" | "auto"
    public NotificationSettings Notifications { get; set; } = new();
}

public class NotificationSettings
{
    public bool Enabled { get; set; } = true;
    public int MinutesBeforeEvent { get; set; } = 5;
    public bool SoundOnCheck { get; set; } = false;
}
