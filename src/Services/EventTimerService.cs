using System.Windows.Media;
using System.Windows.Threading;
using GW2TaskManager.ViewModels;

namespace GW2TaskManager.Services;

/// <summary>
/// Updates TimerText / TimerForeground / TimerBackground on TaskItemViewModels
/// that have an EventTimerId, based on hardcoded GW2 event schedules (UTC).
///
/// Also fires toast notifications shortly before each event, respecting
/// Settings.Notifications.Enabled and Settings.Notifications.MinutesBeforeEvent.
///
/// All times are "minutes since UTC midnight" for the 24-hour day.
/// Schedules sourced from https://www.gw2timer.com/ — verify there if something looks off.
/// </summary>
public class EventTimerService
{
    // ----------------------------------------------------------------
    // Dependencies
    // ----------------------------------------------------------------

    private readonly NotificationService _notifService;
    private readonly SettingsService     _settings;

    public EventTimerService(NotificationService notifService, SettingsService settings)
    {
        _notifService = notifService;
        _settings     = settings;
    }

    // ----------------------------------------------------------------
    // Schedule definitions
    // ----------------------------------------------------------------

    private record EventSchedule(int[] StartMinutes, int DurationMinutes, int WarnMinutes = 15);

    /// <summary>Generates 12 start times for a simple every-2h schedule.</summary>
    private static int[] Every2h(int offsetMinutes)
        => Enumerable.Range(0, 12).Select(i => (offsetMinutes + i * 120) % 1440).Order().ToArray();

    private static int[] EveryHour()
        => Enumerable.Range(0, 24).Select(i => i * 60).ToArray();

    // All times UTC. Adjust StartMinutes if GW2 reschedules events.
    private static readonly Dictionary<string, EventSchedule> Schedules = new()
    {
        // Jahai Bluffs — every 2h, odd hours at :00  (01:00, 03:00 … 23:00 UTC)
        ["death_branded_shatterer"] = new(Every2h(60),  DurationMinutes: 15),

        // Tangled Depths — every 2h at :30 offset    (00:30, 02:30 … 22:30 UTC)
        ["chak_gerent"]             = new(Every2h(30),  DurationMinutes: 20),

        // Auric Basin — every 2h at :45              (00:45, 02:45 … 22:45 UTC)
        ["octovine"]                = new(Every2h(45),  DurationMinutes: 30),

        // Dragon's Stand — every 2h at :20           (00:20, 02:20 … 22:20 UTC)
        ["dragonstorm"]             = new(Every2h(20),  DurationMinutes: 20),

        // Ley-line Anomaly — every 2h at :00         (00:00, 02:00 … 22:00 UTC)
        // Rotates: Gendarran Fields → Iron Marches → Timberline Falls
        ["ley_line_anomaly"]        = new(Every2h(0),   DurationMinutes: 20),

        // Casino Blitz → Pinata (Crown Pavilion, seasonal) — every hour
        ["pinata"]                  = new(EveryHour(),  DurationMinutes: 15),
    };

    // ----------------------------------------------------------------
    // Brushes (created once, frozen for performance)
    // ----------------------------------------------------------------

    private static readonly Brush BrushLiveText    = Freeze(new SolidColorBrush(Color.FromRgb(0x7E, 0xC9, 0x99)));
    private static readonly Brush BrushLiveBg      = Freeze(new SolidColorBrush(Color.FromArgb(0x33, 0x4F, 0x9E, 0x6A)));
    private static readonly Brush BrushSoonText    = Freeze(new SolidColorBrush(Color.FromRgb(0xD6, 0x89, 0x2E)));
    private static readonly Brush BrushSoonBg      = Freeze(new SolidColorBrush(Color.FromArgb(0x22, 0xD6, 0x89, 0x2E)));
    private static readonly Brush BrushLaterText   = Freeze(new SolidColorBrush(Color.FromRgb(0xE3, 0xA3, 0x5A)));
    private static readonly Brush BrushNone        = Freeze(new SolidColorBrush(Color.FromRgb(0x8A, 0x7A, 0x60)));
    private static readonly Brush BrushTransparent = Brushes.Transparent;

    private static Brush Freeze(SolidColorBrush b) { b.Freeze(); return b; }

    // ----------------------------------------------------------------
    // Runtime state
    // ----------------------------------------------------------------

    private List<TaskItemViewModel>? _vms;
    private DispatcherTimer?         _timer;

    /// <summary>Keys of already-notified event occurrences: "eventId@yyyyMMdd@startMin"</summary>
    private readonly HashSet<string> _notifiedOccurrences = new();

    // ----------------------------------------------------------------
    // Public surface
    // ----------------------------------------------------------------

    public void Start(IEnumerable<TaskItemViewModel> allVms)
    {
        _vms = allVms.ToList();
        UpdateAll();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
        _timer.Tick += (_, _) => UpdateAll();
        _timer.Start();
    }

    /// <summary>Call when the task list changes (catalog toggle, reload).</summary>
    public void SetVms(IEnumerable<TaskItemViewModel> allVms)
    {
        _vms = allVms.ToList();
        UpdateAll();
    }

    // ----------------------------------------------------------------
    // Core update loop
    // ----------------------------------------------------------------

    /// <summary>Fires on the UI thread after every timer tick (once per minute).</summary>
    public event Action? TimerTick;

    private void UpdateAll()
    {
        if (_vms == null) return;
        var now = DateTime.UtcNow;
        foreach (var vm in _vms)
        {
            var (minutesUntil, isLive) = SetTimerDisplay(vm, now);
            MaybeNotify(vm, minutesUntil, isLive, now);
        }
        TimerTick?.Invoke();
    }

    /// <summary>Updates the timer display properties on the VM and returns the computed values.</summary>
    private static (int minutesUntil, bool isLive) SetTimerDisplay(TaskItemViewModel vm, DateTime utcNow)
    {
        if (string.IsNullOrEmpty(vm.EventTimerId) || !Schedules.TryGetValue(vm.EventTimerId, out var sched))
        {
            vm.TimerText         = "—";
            vm.TimerForeground   = BrushNone;
            vm.TimerBackground   = BrushTransparent;
            vm.NextEventMinutes  = int.MaxValue;
            return (int.MaxValue, false);
        }

        var (minutesUntil, isLive) = NextOccurrence(sched, utcNow);
        vm.NextEventMinutes = isLive ? 0 : minutesUntil;

        if (isLive)
        {
            vm.TimerText       = "LIVE";
            vm.TimerForeground = BrushLiveText;
            vm.TimerBackground = BrushLiveBg;
        }
        else if (minutesUntil <= sched.WarnMinutes)
        {
            vm.TimerText       = $"{minutesUntil}min";
            vm.TimerForeground = BrushSoonText;
            vm.TimerBackground = BrushSoonBg;
        }
        else
        {
            int h = minutesUntil / 60, m = minutesUntil % 60;
            vm.TimerText       = h > 0 ? $"{h}h{m:D2}" : $"{m}min";
            vm.TimerForeground = BrushLaterText;
            vm.TimerBackground = BrushTransparent;
        }

        return (minutesUntil, isLive);
    }

    /// <summary>Fires a toast notification once per event occurrence when within the warn window.</summary>
    private void MaybeNotify(TaskItemViewModel vm, int minutesUntil, bool isLive, DateTime utcNow)
    {
        if (string.IsNullOrEmpty(vm.EventTimerId)) return;
        if (!vm.IsEnabled) return;                           // only notify for active objectives
        if (isLive || minutesUntil == int.MaxValue)  return;

        var notifSettings = _settings.Settings.Notifications;
        if (!notifSettings.Enabled) return;
        if (minutesUntil > notifSettings.MinutesBeforeEvent) return;

        // Stable key: event + date + which start minute we're heading toward
        int nextStartMin = (utcNow.Hour * 60 + utcNow.Minute + minutesUntil) % 1440;
        string key       = $"{vm.EventTimerId}@{utcNow:yyyyMMdd}@{nextStartMin}";

        if (_notifiedOccurrences.Contains(key)) return;
        _notifiedOccurrences.Add(key);

        _notifService.Show(vm.Icon, vm.Name, $"Dans {minutesUntil} min");
    }

    // ----------------------------------------------------------------
    // Schedule math
    // ----------------------------------------------------------------

    private static (int minutesUntil, bool isLive) NextOccurrence(EventSchedule sched, DateTime utcNow)
    {
        int nowMin = utcNow.Hour * 60 + utcNow.Minute;
        int dayMin = 1440;
        int best   = int.MaxValue;

        foreach (int start in sched.StartMinutes)
        {
            int elapsed = (nowMin - start + dayMin) % dayMin;
            if (elapsed < sched.DurationMinutes)
                return (0, true);

            int until = (start - nowMin + dayMin) % dayMin;
            if (until == 0) until = dayMin;
            if (until < best) best = until;
        }

        return (best, false);
    }
}
