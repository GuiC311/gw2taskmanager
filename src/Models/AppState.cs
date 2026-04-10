namespace GW2TaskManager.Models;

public class AppState
{
    public DateTime LastDailyReset { get; set; } = DateTime.MinValue;
    public DateTime LastWeeklyReset { get; set; } = DateTime.MinValue;
    public DateTime LastApiSync { get; set; } = DateTime.MinValue;
    public List<string> DoneTaskIds { get; set; } = new();
}
