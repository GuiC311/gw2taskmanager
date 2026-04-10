using System.Windows.Threading;
using GW2TaskManager.Models;
using GW2TaskManager.ViewModels;

namespace GW2TaskManager.Services;

/// <summary>
/// Checks for GW2 daily/weekly resets on startup and every 30 seconds while the app is running.
/// Daily reset  : 00:00 UTC every day.
/// Weekly reset : Monday 07:30 UTC.
/// </summary>
public class ResetScheduler
{
    private readonly TaskRepository _repo;
    private readonly TodoViewModel _todoVm;
    private DispatcherTimer? _timer;
    private Action? _refreshTodo;

    public ResetScheduler(TaskRepository repo, TodoViewModel todoVm)
    {
        _repo = repo;
        _todoVm = todoVm;
    }

    public void Start(Action refreshTodo)
    {
        _refreshTodo = refreshTodo;

        // Immediate check: app may have been closed across a reset boundary
        CheckResets();
        _todoVm.RefreshCountdowns();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _timer.Tick += (_, _) => { CheckResets(); _todoVm.RefreshCountdowns(); };
        _timer.Start();
    }

    // ----------------------------------------------------------------
    // Reset logic
    // ----------------------------------------------------------------

    private void CheckResets()
    {
        var now = DateTime.UtcNow;
        var state = _repo.LoadState();
        bool changed = false;

        var lastExpectedDaily = GetLastDailyResetTime(now);
        if (state.LastDailyReset < lastExpectedDaily)
        {
            ClearTasksByType(state, "Daily");
            state.LastDailyReset = lastExpectedDaily;
            changed = true;
        }

        var lastExpectedWeekly = GetLastWeeklyResetTime(now);
        if (state.LastWeeklyReset < lastExpectedWeekly)
        {
            ClearTasksByType(state, "Weekly");
            state.LastWeeklyReset = lastExpectedWeekly;
            changed = true;
        }

        if (changed)
        {
            _repo.SaveState(state);
            _refreshTodo?.Invoke();
        }
    }

    private void ClearTasksByType(AppState state, string type)
    {
        var tasks = _repo.LoadTasks();
        var ids = tasks
            .Where(t => string.Equals(t.Type, type, StringComparison.OrdinalIgnoreCase))
            .Select(t => t.Id)
            .ToHashSet();
        state.DoneTaskIds.RemoveAll(id => ids.Contains(id));
    }

    // ----------------------------------------------------------------
    // Reset time helpers
    // ----------------------------------------------------------------

    /// <summary>Returns the most recent daily reset boundary before utcNow (today 00:00 UTC).</summary>
    private static DateTime GetLastDailyResetTime(DateTime utcNow)
        => utcNow.Date;

    /// <summary>Returns the most recent weekly reset boundary before utcNow (Monday 07:30 UTC).</summary>
    private static DateTime GetLastWeeklyResetTime(DateTime utcNow)
    {
        int daysSinceMonday = ((int)utcNow.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        var lastMonday = utcNow.Date.AddDays(-daysSinceMonday);
        var resetTime = lastMonday.Add(TimeSpan.FromHours(7.5)); // 07:30 UTC

        // If this Monday's reset hasn't passed yet, roll back one week
        return utcNow >= resetTime ? resetTime : resetTime.AddDays(-7);
    }
}
