using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace GW2TaskManager.Views;

public partial class ToastWindow : Window
{
    private const double ToastHeight = 80;
    private const double ToastGap    = 8;
    private const double ToastWidth  = 340;
    private const double MarginRight = 16;
    private const double MarginBottom= 16;

    private const int    DisplayMs   = 4000;
    private const int    SlideMs     = 280;
    private const int    FadeMs      = 350;

    public ToastWindow(string icon, string title, string subtitle, int slotIndex)
    {
        InitializeComponent();

        IconText.Text     = icon;
        TitleText.Text    = title;
        SubtitleText.Text = subtitle;

        // Position: bottom-right of working area, stacked upward
        var area = SystemParameters.WorkArea;
        double targetLeft = area.Right - ToastWidth - MarginRight;
        double targetTop  = area.Bottom - MarginBottom - (ToastHeight + ToastGap) * (slotIndex + 1);

        // Start off-screen to the right for the slide-in
        Left = area.Right + 10;
        Top  = targetTop;

        Loaded += (_, _) =>
        {
            SlideIn(targetLeft);
            StartAutoClose();
        };
    }

    // ----------------------------------------------------------------
    // Animations
    // ----------------------------------------------------------------

    private void SlideIn(double targetLeft)
    {
        var anim = new DoubleAnimation(Left, targetLeft, TimeSpan.FromMilliseconds(SlideMs))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        BeginAnimation(LeftProperty, anim);
    }

    private void StartAutoClose()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(DisplayMs) };
        timer.Tick += (_, _) => { timer.Stop(); FadeOut(); };
        timer.Start();
    }

    private void FadeOut()
    {
        var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(FadeMs));
        fade.Completed += (_, _) => Close();
        BeginAnimation(OpacityProperty, fade);
    }

    // ----------------------------------------------------------------
    // Interactions
    // ----------------------------------------------------------------

    private void OnClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        => FadeOut();
}
