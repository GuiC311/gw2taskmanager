using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using GW2TaskManager.ViewModels;

namespace GW2TaskManager.Views;

public partial class TodoView : UserControl
{
    private bool _dailyDoneCollapsed = false;
    private bool _weeklyDoneCollapsed = false;

    public TodoView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is TodoViewModel vm)
        {
            vm.DailyTodo.CollectionChanged  += (_, _) => UpdateCounters(vm);
            vm.WeeklyTodo.CollectionChanged += (_, _) => UpdateCounters(vm);
            vm.DailyDone.CollectionChanged  += (_, _) => UpdateCounters(vm);
            vm.WeeklyDone.CollectionChanged += (_, _) => UpdateCounters(vm);
            UpdateCounters(vm);
        }
    }

    private void UpdateCounters(TodoViewModel vm)
    {
        DailyTodoCount.Text  = vm.DailyTodo.Count.ToString();
        WeeklyTodoCount.Text = vm.WeeklyTodo.Count.ToString();
        DailyDoneCount.Text  = vm.DailyDone.Count.ToString();
        WeeklyDoneCount.Text = vm.WeeklyDone.Count.ToString();
    }

    // ============================================================
    // Check / Uncheck — with dopamine animation
    // ============================================================
    private void Check_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not TodoViewModel vm) return;
        if (sender is not CheckBox cb) return;
        if (cb.DataContext is not TaskItemViewModel task) return;

        // Find the row Border to animate
        var row = FindRowBorder(cb);

        if (!task.IsDone)
        {
            // Checking → animate then move
            PlayDopamineAnimation(row, () => vm.ToggleDoneCommand.Execute(task));
        }
        else
        {
            // Unchecking → move back immediately
            vm.ToggleDoneCommand.Execute(task);
        }
    }

    private static void PlayDopamineAnimation(Border? row, Action onComplete)
    {
        if (row == null) { onComplete(); return; }

        // Scale pulse + glow
        var scaleX = new DoubleAnimation(1.0, 1.025, TimeSpan.FromMilliseconds(120))
        {
            AutoReverse = true, EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        var scaleY = new DoubleAnimation(1.0, 1.025, TimeSpan.FromMilliseconds(120))
        {
            AutoReverse = true, EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        var transform = new ScaleTransform(1, 1);
        row.RenderTransform = transform;
        row.RenderTransformOrigin = new Point(0.5, 0.5);

        // Gold border flash
        var originalBrush = row.BorderBrush;
        row.BorderBrush = new SolidColorBrush(Color.FromRgb(0xD4, 0xA0, 0x4A));
        var borderAnim = new ColorAnimation(
            Color.FromRgb(0xD4, 0xA0, 0x4A),
            Color.FromRgb(0x4A, 0x3A, 0x2A),
            TimeSpan.FromMilliseconds(600));

        bool completed = false;
        scaleX.Completed += (_, _) =>
        {
            if (completed) return;
            completed = true;
            onComplete();
        };

        transform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
        transform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
        if (row.BorderBrush is SolidColorBrush scb)
            scb.BeginAnimation(SolidColorBrush.ColorProperty, borderAnim);
    }

    /// <summary>
    /// Walks up the visual tree to the ContentPresenter (ItemsControl item container),
    /// then returns its first child — always the root Border of the DataTemplate (RowBorder).
    /// More robust than searching for the first Border, which could be an inner element.
    /// </summary>
    private static Border? FindRowBorder(DependencyObject child)
    {
        var parent = VisualTreeHelper.GetParent(child);
        while (parent != null)
        {
            if (parent is ContentPresenter cp)
            {
                return VisualTreeHelper.GetChildrenCount(cp) > 0
                    ? VisualTreeHelper.GetChild(cp, 0) as Border
                    : null;
            }
            parent = VisualTreeHelper.GetParent(parent);
        }
        return null;
    }

    // ============================================================
    // Waypoint copy
    // ============================================================
    private void CopyLink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string link && !string.IsNullOrWhiteSpace(link))
        {
            Clipboard.SetText(link);
            var tt = new ToolTip { Content = "Copied to clipboard!" };
            btn.ToolTip = tt;
            tt.IsOpen = true;
            var timer = new System.Windows.Threading.DispatcherTimer
                { Interval = TimeSpan.FromSeconds(1.5) };
            timer.Tick += (_, _) => { tt.IsOpen = false; timer.Stop(); };
            timer.Start();
        }
    }

    // ============================================================
    // Collapse / expand done sub-sections
    // ============================================================
    private void CollapseDailyDone_Click(object sender, RoutedEventArgs e)
    {
        _dailyDoneCollapsed = !_dailyDoneCollapsed;
        DailyDoneList.Visibility = _dailyDoneCollapsed ? Visibility.Collapsed : Visibility.Visible;
        DailyDoneChevron.Text = _dailyDoneCollapsed ? "▸ " : "▾ ";
    }

    private void CollapseWeeklyDone_Click(object sender, RoutedEventArgs e)
    {
        _weeklyDoneCollapsed = !_weeklyDoneCollapsed;
        WeeklyDoneList.Visibility = _weeklyDoneCollapsed ? Visibility.Collapsed : Visibility.Visible;
        WeeklyDoneChevron.Text = _weeklyDoneCollapsed ? "▸ " : "▾ ";
    }
}
