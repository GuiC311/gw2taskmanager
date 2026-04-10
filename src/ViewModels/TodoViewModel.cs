using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GW2TaskManager.Models;
using GW2TaskManager.Services;

namespace GW2TaskManager.ViewModels;

public partial class TodoViewModel : ObservableObject
{
    private readonly TaskRepository _repo;
    private readonly SettingsService _settings;

    public ObservableCollection<TaskItemViewModel> DailyTodo { get; } = new();
    public ObservableCollection<TaskItemViewModel> WeeklyTodo { get; } = new();
    public ObservableCollection<TaskItemViewModel> DailyDone { get; } = new();
    public ObservableCollection<TaskItemViewModel> WeeklyDone { get; } = new();

    [ObservableProperty]
    private string _apiKeyDisplay = string.Empty;

    [ObservableProperty]
    private string _apiStatusText = "Not connected";

    [ObservableProperty]
    private bool _isApiConnected = false;

    [ObservableProperty]
    private string _dailyResetIn = "—";

    [ObservableProperty]
    private string _weeklyResetIn = "—";

    public TodoViewModel(TaskRepository repo, SettingsService settings)
    {
        _repo = repo;
        _settings = settings;
    }

    public void LoadFromState(List<TaskItemViewModel> allEnabled, AppState state)
    {
        DailyTodo.Clear();
        WeeklyTodo.Clear();
        DailyDone.Clear();
        WeeklyDone.Clear();

        foreach (var vm in allEnabled)
        {
            vm.IsDone = state.DoneTaskIds.Contains(vm.Id);

            if (vm.Type == "Daily")
            {
                if (vm.IsDone) DailyDone.Add(vm);
                else           DailyTodo.Add(vm);
            }
            else if (vm.Type == "Weekly")
            {
                if (vm.IsDone) WeeklyDone.Add(vm);
                else           WeeklyTodo.Add(vm);
            }
        }

        ResortByTimer();
        UpdateResetCountdowns();
    }

    [RelayCommand]
    public void ToggleDone(TaskItemViewModel vm)
    {
        vm.IsDone = !vm.IsDone;

        if (vm.Type == "Daily")
        {
            if (vm.IsDone) { DailyTodo.Remove(vm); DailyDone.Add(vm); }
            else           { DailyDone.Remove(vm); DailyTodo.Add(vm); ResortByTimer(); }
        }
        else
        {
            if (vm.IsDone) { WeeklyTodo.Remove(vm); WeeklyDone.Add(vm); }
            else           { WeeklyDone.Remove(vm); WeeklyTodo.Add(vm); ResortByTimer(); }
        }

        PersistState();
    }

    /// <summary>
    /// Re-sorts DailyTodo and WeeklyTodo so event-linked tasks (sorted by imminence) appear first.
    /// Uses Move() to avoid flickering. Safe to call on the UI thread (DispatcherTimer).
    /// </summary>
    public void ResortByTimer()
    {
        SortByTimer(DailyTodo);
        SortByTimer(WeeklyTodo);
    }

    private static void SortByTimer(System.Collections.ObjectModel.ObservableCollection<TaskItemViewModel> col)
    {
        var sorted = col.OrderBy(v => v.NextEventMinutes).ToList();
        for (int i = 0; i < sorted.Count; i++)
        {
            int cur = col.IndexOf(sorted[i]);
            if (cur != i) col.Move(cur, i);
        }
    }

    private void PersistState()
    {
        var state = _repo.LoadState();
        state.DoneTaskIds = DailyDone.Concat(WeeklyDone).Select(v => v.Id).ToList();
        _repo.SaveState(state);
    }

    public void RefreshCountdowns() => UpdateResetCountdowns();

    private void UpdateResetCountdowns()
    {
        var now = DateTime.UtcNow;

        var nextDaily = now.Date.AddDays(1); // next midnight UTC
        var diff = nextDaily - now;
        DailyResetIn = $"{(int)diff.TotalHours:D2}h {diff.Minutes:D2}m";

        // Next weekly: Monday 07:30 UTC
        int daysUntilMonday = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
        var nextWeekly = now.Date.AddDays(daysUntilMonday == 0 && now.TimeOfDay < TimeSpan.FromHours(7.5) ? 0 : daysUntilMonday).Add(TimeSpan.FromHours(7.5));
        var wDiff = nextWeekly - now;
        WeeklyResetIn = wDiff.TotalDays >= 1
            ? $"{(int)wDiff.TotalDays}d {wDiff.Hours:D2}h"
            : $"{wDiff.Hours:D2}h {wDiff.Minutes:D2}m";
    }
}
