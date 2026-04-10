using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using GW2TaskManager.Models;

namespace GW2TaskManager.ViewModels;

public partial class TaskItemViewModel : ObservableObject
{
    private readonly TaskItem _model;

    public string Id => _model.Id;
    public string Type => _model.Type ?? string.Empty;
    public string Category => _model.Category ?? string.Empty;
    public string Name => _model.Name;
    public string Description => _model.Description;
    public string LinkCode => _model.LinkCode;
    public string Icon => _model.Icon ?? "📋";
    public string? ApiTracker    => _model.ApiTracker;
    public string? EventTimerId  => _model.EventTimerId;

    /// <summary>True when this task is auto-checked via the GW2 API.</summary>
    public bool IsApiTracked => !string.IsNullOrEmpty(_model.ApiTracker);

    [ObservableProperty]
    private string _character;

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private bool _isDone;

    // Timer display (set by EventTimerService)
    [ObservableProperty]
    private string _timerText = "—";

    [ObservableProperty]
    private Brush _timerForeground = Brushes.Transparent;

    [ObservableProperty]
    private Brush _timerBackground = Brushes.Transparent;

    /// <summary>Minutes until next event occurrence. 0 = live. int.MaxValue = no event timer.</summary>
    [ObservableProperty]
    private int _nextEventMinutes = int.MaxValue;

    public TaskItemViewModel(TaskItem model)
    {
        _model = model;
        Character = model.Character;
        IsEnabled = model.IsEnabled;
    }

    public TaskItem ToModel()
    {
        _model.Character = Character;
        _model.IsEnabled = IsEnabled;
        return _model;
    }
}
