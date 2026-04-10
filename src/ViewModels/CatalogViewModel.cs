using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GW2TaskManager.Models;
using GW2TaskManager.Services;

namespace GW2TaskManager.ViewModels;

public partial class CatalogViewModel : ObservableObject
{
    private readonly TaskRepository _repo;
    public event Action? CatalogChanged;
    public void NotifyCatalogChanged() => CatalogChanged?.Invoke();

    public ObservableCollection<TaskItemViewModel> DailyItems   { get; } = new();
    public ObservableCollection<TaskItemViewModel> WeeklyItems  { get; } = new();

    /// <summary>Character names from the GW2 API. Always contains "Any" as first entry.</summary>
    public ObservableCollection<string> Characters { get; } = new() { "Any" };

    public void SetCharacters(IEnumerable<string> names)
    {
        Characters.Clear();
        Characters.Add("Any");
        foreach (var name in names.OrderBy(n => n))
            Characters.Add(name);
    }

    [ObservableProperty]
    private string _filterText = string.Empty;

    public CatalogViewModel(TaskRepository repo)
    {
        _repo = repo;
    }

    public void Load(List<TaskItemViewModel> allVms)
    {
        DailyItems.Clear();
        WeeklyItems.Clear();

        foreach (var vm in allVms.OrderBy(v => v.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            if (vm.Type == "Daily") DailyItems.Add(vm);
            else if (vm.Type == "Weekly") WeeklyItems.Add(vm);
        }
    }

    [RelayCommand]
    public void ToggleEnabled(TaskItemViewModel vm)
    {
        vm.IsEnabled = !vm.IsEnabled;
        Save();
        CatalogChanged?.Invoke();
    }

    [RelayCommand]
    public void Save() => SaveInternal();

    private void SaveInternal()
    {
        var tasks = DailyItems.Concat(WeeklyItems).Select(v => v.ToModel()).ToList();
        _repo.SaveTasks(tasks);
    }

    [RelayCommand]
    public void EnableAll()
    {
        foreach (var vm in DailyItems.Concat(WeeklyItems))
            vm.IsEnabled = true;
        Save();
        CatalogChanged?.Invoke();
    }

    [RelayCommand]
    public void DisableAll()
    {
        foreach (var vm in DailyItems.Concat(WeeklyItems))
            vm.IsEnabled = false;
        Save();
        CatalogChanged?.Invoke();
    }


    public List<TaskItemViewModel> GetEnabled()
        => DailyItems.Concat(WeeklyItems).Where(v => v.IsEnabled).ToList();

    /// <summary>Adds a newly created task (from the dialog) and persists it.</summary>
    public void AddTask(TaskItem task)
    {
        var vm = new TaskItemViewModel(task);
        if (task.Type == "Weekly") WeeklyItems.Add(vm);
        else                       DailyItems.Add(vm);
        SaveInternal();
        CatalogChanged?.Invoke();
    }
}
