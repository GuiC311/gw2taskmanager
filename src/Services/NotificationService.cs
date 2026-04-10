using GW2TaskManager.Views;

namespace GW2TaskManager.Services;

/// <summary>
/// Creates and manages toast notification windows.
/// Toasts stack upward from the bottom-right corner; each closes independently.
/// Must be called from the UI thread.
/// </summary>
public class NotificationService
{
    private readonly List<ToastWindow> _active = new();

    public void Show(string icon, string title, string subtitle)
    {
        // Remove windows that have already been closed
        _active.RemoveAll(w => !w.IsLoaded);

        var toast = new ToastWindow(icon, title, subtitle, slotIndex: _active.Count);
        _active.Add(toast);
        toast.Closed += (_, _) => _active.Remove(toast);
        toast.Show();
    }
}
