using System.Windows;
using System.Windows.Controls;
using GW2TaskManager.ViewModels;

namespace GW2TaskManager.Views;

public partial class CatalogView : UserControl
{
    public CatalogView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is CatalogViewModel vm)
        {
            vm.DailyItems.CollectionChanged  += (_, _) => UpdateCounters(vm);
            vm.WeeklyItems.CollectionChanged += (_, _) => UpdateCounters(vm);

            // Also refresh when IsEnabled changes on any individual item
            // (covers Enable All / Disable All which bypass Toggle_Changed)
            foreach (var item in vm.DailyItems.Concat(vm.WeeklyItems))
                item.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(TaskItemViewModel.IsEnabled))
                        UpdateCounters(vm);
                };

            UpdateCounters(vm);
        }
    }

    private void UpdateCounters(CatalogViewModel vm)
    {
        int dailyEnabled  = vm.DailyItems.Count(i => i.IsEnabled);
        int weeklyEnabled = vm.WeeklyItems.Count(i => i.IsEnabled);

        if (DailyEnabledCount != null)
            DailyEnabledCount.Text = $"{dailyEnabled}/{vm.DailyItems.Count}";
        if (WeeklyEnabledCount != null)
            WeeklyEnabledCount.Text = $"{weeklyEnabled}/{vm.WeeklyItems.Count}";
    }

    private void Toggle_Changed(object sender, RoutedEventArgs e)
    {
        if (DataContext is not CatalogViewModel vm) return;
        // Binding already updated IsEnabled on the VM; just persist and refresh counters
        vm.SaveCommand.Execute(null);
        vm.NotifyCatalogChanged();
        UpdateCounters(vm);
    }

    private void FilterBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (DataContext is not CatalogViewModel vm) return;
        var filter = FilterBox.Text.Trim().ToLowerInvariant();
        ApplyFilter(DailyList, filter);
        ApplyFilter(WeeklyList, filter);
    }

    private static void ApplyFilter(ItemsControl list, string filter)
    {
        if (list.ItemsSource is not System.Collections.IEnumerable items) return;
        foreach (var item in items)
        {
            if (item is not TaskItemViewModel vm) continue;
            var container = list.ItemContainerGenerator.ContainerFromItem(vm) as FrameworkElement;
            if (container == null) continue;
            bool visible = string.IsNullOrEmpty(filter)
                || vm.Name.ToLowerInvariant().Contains(filter)
                || vm.Description.ToLowerInvariant().Contains(filter)
                || vm.Category.ToLowerInvariant().Contains(filter);
            container.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void AddObjective_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not CatalogViewModel vm) return;
        var dialog = new NewObjectiveDialog { Owner = Window.GetWindow(this) };
        dialog.SetCharacters(vm.Characters);
        if (dialog.ShowDialog() == true && dialog.Result != null)
        {
            vm.AddTask(dialog.Result);
            UpdateCounters(vm);
        }
    }

    private void CopyLink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string link && !string.IsNullOrWhiteSpace(link))
        {
            Clipboard.SetText(link);
            // Brief tooltip feedback
            btn.ToolTip = "Copied!";
            var tt = new System.Windows.Controls.ToolTip { Content = "Copied to clipboard!" };
            btn.ToolTip = tt;
            tt.IsOpen = true;
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1.5)
            };
            timer.Tick += (_, _) => { tt.IsOpen = false; timer.Stop(); };
            timer.Start();
        }
    }
}
