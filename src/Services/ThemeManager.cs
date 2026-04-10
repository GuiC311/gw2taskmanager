using System.Windows;
using System.Windows.Media;

namespace GW2TaskManager.Services;

public enum AppTheme { Warm, Slate }
public enum ThemeMode { Warm, Slate, Auto }

/// <summary>
/// Swaps the live color palette at runtime by mutating SolidColorBrush.Color on the
/// existing resource objects. StaticResource bindings keep their object reference, so
/// all controls repaint automatically when a brush's Color changes.
/// </summary>
public class ThemeManager
{
    public AppTheme Current  { get; private set; } = AppTheme.Warm;
    public ThemeMode Mode    { get; private set; } = ThemeMode.Warm;

    /// <summary>
    /// GW2 Tyrian day/night cycle: 80-minute loop on UTC clock.
    /// 70 min day (Warm) then 10 min night (Slate).
    /// </summary>
    public static AppTheme GetGw2Theme()
    {
        var minutes = (int)DateTime.UtcNow.TimeOfDay.TotalMinutes % 80;
        return minutes < 70 ? AppTheme.Warm : AppTheme.Slate;
    }

    // (Warm/Proposée, Slate/Ardoise)
    private static readonly (string Key, Color Warm, Color Slate)[] Palette =
    [
        ("Bg0Brush",         C(0x1C,0x16,0x0F), C(0x14,0x18,0x20)),
        ("Bg1Brush",         C(0x25,0x1C,0x12), C(0x1C,0x21,0x30)),
        ("Bg2Brush",         C(0x32,0x26,0x1A), C(0x25,0x2B,0x3C)),
        ("Bg3Brush",         C(0x3E,0x30,0x22), C(0x2E,0x35,0x48)),
        ("LineBrush",        C(0x5A,0x46,0x33), C(0x3E,0x48,0x60)),
        ("Text0Brush",       C(0xF5,0xEB,0xDA), C(0xEE,0xE8,0xF8)),
        ("Text1Brush",       C(0xD4,0xC4,0xA4), C(0xC4,0xBC,0xD8)),
        ("Text2Brush",       C(0xA0,0x88,0x68), C(0x88,0x80,0xA8)),
        ("GoldBrush",        C(0xD4,0xA0,0x4A), C(0xC8,0xA8,0x58)),
        ("GoldSoftBrush",    C(0xB8,0x8A,0x3A), C(0xA8,0x88,0x40)),
        ("GoldGlowBrush",    C(0xFF,0xCE,0x6B), C(0xE8,0xC8,0x70)),
        ("AmberBrush",       C(0xD6,0x89,0x2E), C(0xD0,0x90,0x30)),
        ("EmeraldBrush",     C(0x4F,0x9E,0x6A), C(0x3A,0x80,0x60)),
        ("EmeraldTextBrush", C(0x7E,0xC9,0x99), C(0x70,0xC8,0x98)),
        ("TimerGreenBrush",  C(0x7E,0xC9,0x99), C(0x70,0xC8,0x98)),
        ("TimerYellowBrush", C(0xE3,0xA3,0x5A), C(0xC8,0xA8,0x58)),
        ("TimerOrangeBrush", C(0xF0,0xB2,0x66), C(0xD0,0x90,0x30)),
        ("TimerRedBrush",    C(0xE0,0x70,0x70), C(0xE0,0x70,0x70)),
    ];

    public void Apply(AppTheme theme)
    {
        Current = theme;
        var res = Application.Current.Resources;
        foreach (var (key, warm, slate) in Palette)
        {
            // Replace the resource object — DynamicResource bindings follow automatically.
            // Never mutate .Color directly: brushes get frozen by WPF templates.
            res[key] = new SolidColorBrush(theme == AppTheme.Warm ? warm : slate);
        }
    }

    /// <summary>Set a fixed mode (Warm/Slate) or Auto.</summary>
    public void SetMode(ThemeMode mode)
    {
        Mode = mode;
        Apply(mode == ThemeMode.Slate ? AppTheme.Slate
            : mode == ThemeMode.Auto  ? GetGw2Theme()
            :                           AppTheme.Warm);
    }

    /// <summary>Cycle Warm → Slate → Auto → Warm.</summary>
    public ThemeMode CycleMode()
    {
        var next = Mode switch
        {
            ThemeMode.Warm  => ThemeMode.Slate,
            ThemeMode.Slate => ThemeMode.Auto,
            _               => ThemeMode.Warm,
        };
        SetMode(next);
        return Mode;
    }

    /// <summary>Called every minute in Auto mode to follow the GW2 day/night cycle.</summary>
    public void TickAuto()
    {
        if (Mode == ThemeMode.Auto)
            Apply(GetGw2Theme());
    }

    private static Color C(byte r, byte g, byte b) => Color.FromRgb(r, g, b);
}
