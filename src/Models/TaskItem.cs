namespace GW2TaskManager.Models;

public class TaskItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string? Type { get; set; }         // "Daily" | "Weekly"
    public string? Category { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LinkCode { get; set; } = string.Empty;
    public string Character { get; set; } = "Any";

    // v2 fields
    public bool IsEnabled { get; set; } = false;
    public string? Icon { get; set; }         // Emoji fallback
    public string? IconUrl { get; set; }      // Future: GW2 API icon URL
    public string? ApiTracker { get; set; }   // e.g. "worldboss:death_branded_shatterer"
    public string? EventTimerId { get; set; } // e.g. "shatterer" — links to EventSchedule
}
