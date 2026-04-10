namespace GW2TaskManager.Models;

public class ApiSyncResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }

    /// <summary>Task IDs that the API confirms as done for the current reset period.</summary>
    public HashSet<string> DoneTaskIds { get; set; } = new();

    /// <summary>Character names fetched from the API (empty if not available).</summary>
    public List<string> Characters { get; set; } = new();

    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
}
