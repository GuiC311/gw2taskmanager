using System.Windows;
using GW2TaskManager.Models;

namespace GW2TaskManager.Views;

public partial class NewObjectiveDialog : Window
{
    /// <summary>Set after a successful "Créer" click. Null if cancelled.</summary>
    public TaskItem? Result { get; private set; }

    public NewObjectiveDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => NameBox.Focus();
    }

    /// <summary>Populates the character dropdown from the API character list.</summary>
    public void SetCharacters(IEnumerable<string> characters)
    {
        CharacterBox.ItemsSource = characters;
        CharacterBox.SelectedIndex = 0; // "Any"
    }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            NameBox.Focus();
            NameBox.BorderBrush = System.Windows.Media.Brushes.OrangeRed;
            return;
        }

        var type = (TypeBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString()
                   ?? "Daily";

        Result = new TaskItem
        {
            Id          = Guid.NewGuid().ToString(),
            Type        = type,
            Name        = name,
            Description = DescBox.Text.Trim(),
            Category    = "Misc",
            Icon        = string.IsNullOrWhiteSpace(IconBox.Text) ? "📋" : IconBox.Text.Trim(),
            Character   = string.IsNullOrWhiteSpace(CharacterBox.Text) ? "Any" : CharacterBox.Text.Trim(),
            LinkCode    = LinkBox.Text.Trim(),
            IsEnabled   = true,
        };

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
