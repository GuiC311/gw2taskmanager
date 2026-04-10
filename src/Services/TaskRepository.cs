using System.IO;
using System.Text.Json;
using GW2TaskManager.Models;

namespace GW2TaskManager.Services;

public class TaskRepository
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _tasksPath;
    private readonly string _statePath;

    public TaskRepository()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GW2TaskManager");
        Directory.CreateDirectory(appData);

        // tasks.json shipped with app (Data/), copied to AppData on first run
        var bundled = Path.Combine(AppContext.BaseDirectory, "Data", "tasks.json");
        _tasksPath = Path.Combine(appData, "tasks.json");
        _statePath = Path.Combine(appData, "state.json");

        // First run: copy bundled tasks to AppData
        if (!File.Exists(_tasksPath) && File.Exists(bundled))
            File.Copy(bundled, _tasksPath);
    }

    public List<TaskItem> LoadTasks()
    {
        if (!File.Exists(_tasksPath)) return new();
        var json = File.ReadAllText(_tasksPath);
        return JsonSerializer.Deserialize<List<TaskItem>>(json, JsonOpts) ?? new();
    }

    public void SaveTasks(List<TaskItem> tasks)
    {
        var json = JsonSerializer.Serialize(tasks, JsonOpts);
        File.WriteAllText(_tasksPath, json);
    }

    public AppState LoadState()
    {
        if (!File.Exists(_statePath)) return new();
        var json = File.ReadAllText(_statePath);
        return JsonSerializer.Deserialize<AppState>(json, JsonOpts) ?? new();
    }

    public void SaveState(AppState state)
    {
        var json = JsonSerializer.Serialize(state, JsonOpts);
        File.WriteAllText(_statePath, json);
    }
}
