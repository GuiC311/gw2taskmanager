using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using GW2TaskManager.Models;
using GW2TaskManager.Services;
using GW2TaskManager.ViewModels;

namespace GW2TaskManager;

public partial class MainWindow : Window
{
    private readonly MainViewModel     _vm;
    private readonly SettingsService   _settings;
    private readonly TaskRepository    _repo;
    private readonly CatalogViewModel  _catalogVm;
    private readonly TodoViewModel     _todoVm;
    private readonly ResetScheduler    _resetScheduler;
    private readonly Gw2ApiClient      _gw2Client;
    private readonly EventTimerService _eventTimerService;
    private readonly ThemeManager      _themeManager;
    private readonly LanguageManager   _langManager;

    private DispatcherTimer? _syncTimer;
    private DateTime?        _lastSyncAt;

    public MainWindow(MainViewModel vm, SettingsService settings, TaskRepository repo,
                      CatalogViewModel catalogVm, TodoViewModel todoVm,
                      ResetScheduler resetScheduler, Gw2ApiClient gw2Client,
                      EventTimerService eventTimerService, ThemeManager themeManager,
                      LanguageManager langManager)
    {
        InitializeComponent();

        _vm                = vm;
        _settings          = settings;
        _repo              = repo;
        _catalogVm         = catalogVm;
        _todoVm            = todoVm;
        _resetScheduler    = resetScheduler;
        _gw2Client         = gw2Client;
        _eventTimerService = eventTimerService;
        _themeManager      = themeManager;
        _langManager       = langManager;

        DataContext = _vm;
        Loaded += OnLoaded;

        // PublishSingleFile extrait dans %TEMP% — AppContext.BaseDirectory ne pointe pas
        // vers le dossier de l'exe. On charge le logo depuis le vrai dossier de l'exe.
        var exeDir = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
        var logoPath = Path.Combine(exeDir, "Resources", "logo.png");
        if (File.Exists(logoPath))
            LogoImage.Source = new BitmapImage(new Uri(logoPath, UriKind.Absolute));
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Load settings
        _settings.Load();

        // Apply saved theme (before anything is rendered)
        var savedMode = _settings.Settings.Theme switch
        {
            "slate" => ThemeMode.Slate,
            "auto"  => ThemeMode.Auto,
            _       => ThemeMode.Warm,
        };
        _themeManager.SetMode(savedMode);
        UpdateThemeButton();
        UpdateLanguageButton();

        // Auto-theme timer: check GW2 day/night cycle every 60s
        var autoThemeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
        autoThemeTimer.Tick += (_, _) => { _themeManager.TickAuto(); UpdateThemeButton(); };
        autoThemeTimer.Start();

        var apiKey = _settings.GetApiKey();
        if (!string.IsNullOrEmpty(apiKey))
            ApiKeyBox.Text = apiKey;

        // Load tasks into catalog
        var tasks  = _repo.LoadTasks();
        var allVms = tasks.Select(t => new TaskItemViewModel(t)).ToList();
        _catalogVm.Load(allVms);

        // Wire catalog changes → refresh todo
        _catalogVm.CatalogChanged += RefreshTodo;

        // Start event timer service — feeds TimerText on VMs (all tasks, not just enabled)
        _eventTimerService.Start(allVms);
        _eventTimerService.TimerTick += _todoVm.ResortByTimer;

        // Start reset scheduler (checks missed resets + starts 30s countdown timer)
        _resetScheduler.Start(RefreshTodo);
        RefreshTodo();

        // Set views' DataContexts
        TodoPage.DataContext    = _todoVm;
        CatalogPage.DataContext = _catalogVm;

        // Auto-sync every 5 minutes
        _syncTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(5) };
        _syncTimer.Tick += async (_, _) => await TriggerSyncAsync();
        _syncTimer.Start();

        // Refresh "il y a X min" label every 30s (cosmetic only)
        var ageRefresh = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        ageRefresh.Tick += (_, _) => { if (_lastSyncAt.HasValue) SetApiStatus(ApiStatusState.Connected); };
        ageRefresh.Start();

        // Initial sync if a key is already stored
        if (!string.IsNullOrEmpty(apiKey))
        {
            SetApiStatus(ApiStatusState.Syncing);
            _ = TriggerSyncAsync();
        }
    }

    // ----------------------------------------------------------------
    // GW2 API sync
    // ----------------------------------------------------------------

    private async Task TriggerSyncAsync()
    {
        var key = _settings.GetApiKey();
        if (string.IsNullOrEmpty(key)) return;

        SetApiStatus(ApiStatusState.Syncing);
        var tasks  = _repo.LoadTasks();
        var result = await _gw2Client.SyncAsync(key, tasks);
        ApplySyncResult(result);
    }

    private void ApplySyncResult(ApiSyncResult result)
    {
        if (!result.Success)
        {
            SetApiStatus(ApiStatusState.Error, result.Error ?? "Erreur API");
            return;
        }

        _lastSyncAt = result.SyncedAt;

        // Populate character list if the API returned any
        if (result.Characters.Count > 0)
            _catalogVm.SetCharacters(result.Characters);

        // Merge API-confirmed done IDs into state (additive — never removes manual checks)
        if (result.DoneTaskIds.Count > 0)
        {
            var state   = _repo.LoadState();
            bool changed = false;

            foreach (var id in result.DoneTaskIds)
            {
                if (!state.DoneTaskIds.Contains(id))
                {
                    state.DoneTaskIds.Add(id);
                    changed = true;
                }
            }

            if (changed)
            {
                _repo.SaveState(state);
                RefreshTodo();
            }
        }

        SetApiStatus(ApiStatusState.Connected);
    }

    // ----------------------------------------------------------------
    // API status display
    // ----------------------------------------------------------------

    private enum ApiStatusState { NoKey, Syncing, Connected, Error }

    private void SetApiStatus(ApiStatusState state, string? detail = null)
    {
        switch (state)
        {
            case ApiStatusState.NoKey:
                ApiDot.Fill          = (SolidColorBrush)FindResource("Text2Brush");
                ApiStatusText.Text   = _langManager.Get("Str_ApiStatusNoKey");
                ApiStatusText.Foreground = (SolidColorBrush)FindResource("Text2Brush");
                break;

            case ApiStatusState.Syncing:
                ApiDot.Fill          = (SolidColorBrush)FindResource("AmberBrush");
                ApiStatusText.Text   = _langManager.Get("Str_StatusSyncing");
                ApiStatusText.Foreground = (SolidColorBrush)FindResource("AmberBrush");
                break;

            case ApiStatusState.Connected:
                ApiDot.Fill          = (SolidColorBrush)FindResource("EmeraldTextBrush");
                ApiStatusText.Text   = _lastSyncAt.HasValue
                    ? string.Format(_langManager.Get("Str_StatusConnectedSync"), FormatSyncAge(_lastSyncAt.Value))
                    : _langManager.Get("Str_StatusConnected");
                ApiStatusText.Foreground = (SolidColorBrush)FindResource("EmeraldTextBrush");
                break;

            case ApiStatusState.Error:
                ApiDot.Fill          = (SolidColorBrush)FindResource("TimerRedBrush");
                ApiStatusText.Text   = detail ?? _langManager.Get("Str_StatusError");
                ApiStatusText.Foreground = (SolidColorBrush)FindResource("TimerRedBrush");
                break;
        }
    }

    private string FormatSyncAge(DateTime syncedAt)
    {
        var age = DateTime.UtcNow - syncedAt;
        if (age.TotalSeconds < 10)  return _langManager.Get("Str_AgeJustNow");
        if (age.TotalMinutes < 1)   return string.Format(_langManager.Get("Str_AgeSec"), (int)age.TotalSeconds);
        if (age.TotalHours < 1)     return string.Format(_langManager.Get("Str_AgeMin"), (int)age.TotalMinutes);
        return string.Format(_langManager.Get("Str_AgeHour"), (int)age.TotalHours);
    }

    // ----------------------------------------------------------------
    // Tab + key events
    // ----------------------------------------------------------------

    private void RefreshTodo()
    {
        var state = _repo.LoadState();
        _todoVm.LoadFromState(_catalogVm.GetEnabled(), state);
    }

    private void SyncNow_Click(object sender, RoutedEventArgs e)
        => _ = TriggerSyncAsync();

    private void TabTodo_Checked(object sender, RoutedEventArgs e)
    {
        if (TodoPage    != null) TodoPage.Visibility    = Visibility.Visible;
        if (CatalogPage != null) CatalogPage.Visibility = Visibility.Collapsed;
    }

    private void TabCatalog_Checked(object sender, RoutedEventArgs e)
    {
        if (TodoPage    != null) TodoPage.Visibility    = Visibility.Collapsed;
        if (CatalogPage != null) CatalogPage.Visibility = Visibility.Visible;
    }

    private void ThemeToggle_Click(object sender, RoutedEventArgs e)
    {
        var newMode = _themeManager.CycleMode();
        _settings.Settings.Theme = newMode switch
        {
            ThemeMode.Slate => "slate",
            ThemeMode.Auto  => "auto",
            _               => "warm",
        };
        _settings.Save();
        UpdateThemeButton();
    }

    private void UpdateThemeButton()
    {
        (ThemeToggleBtn.Content, ThemeToggleBtn.ToolTip) = _themeManager.Mode switch
        {
            ThemeMode.Warm  => ("☀️", _langManager.Get("Str_ThemeWarmTip")),
            ThemeMode.Slate => ("🌙", _langManager.Get("Str_ThemeSlateTip")),
            ThemeMode.Auto  => ("🔄", _langManager.Get("Str_ThemeAutoTip")),
            _               => ("☀️", _langManager.Get("Str_ThemeTooltip")),
        };
    }

    private void LanguageToggle_Click(object sender, RoutedEventArgs e)
    {
        var newLang = _langManager.CycleLanguage();
        _settings.Settings.Language = newLang == AppLanguage.English ? "en" : "fr";
        _settings.Save();
        // Refresh runtime strings that are set in code-behind
        UpdateThemeButton();
        UpdateLanguageButton();
        if (_lastSyncAt.HasValue) SetApiStatus(ApiStatusState.Connected);
    }

    private void UpdateLanguageButton()
    {
        // Flag image updates automatically via {DynamicResource FlagCurrent} on the Image Source.
        // Only runtime strings (theme tooltip etc.) need a manual refresh.
    }

    private void ApiKeyBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        var key = ApiKeyBox.Text.Trim();
        _settings.SetApiKey(key);
        _settings.Save();

        if (string.IsNullOrEmpty(key))
        {
            SetApiStatus(ApiStatusState.NoKey);
        }
        else
        {
            SetApiStatus(ApiStatusState.Syncing);
            _ = TriggerSyncAsync();
        }
    }
}
