using CommunityToolkit.Mvvm.ComponentModel;
using GW2TaskManager.Services;

namespace GW2TaskManager.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public TodoViewModel TodoViewModel { get; }
    public CatalogViewModel CatalogViewModel { get; }
    public SettingsService SettingsService { get; }

    [ObservableProperty]
    private int _selectedTabIndex = 0;

    public MainViewModel(TodoViewModel todoVm, CatalogViewModel catalogVm, SettingsService settingsService)
    {
        TodoViewModel = todoVm;
        CatalogViewModel = catalogVm;
        SettingsService = settingsService;
    }
}
