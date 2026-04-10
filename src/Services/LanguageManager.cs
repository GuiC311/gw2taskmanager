using System;
using System.Linq;
using System.Windows;

namespace GW2TaskManager.Services;

public enum AppLanguage { French, English }

public class LanguageManager
{
    public AppLanguage Current { get; private set; } = AppLanguage.French;

    public void SetLanguage(AppLanguage lang)
    {
        Current = lang;
        Apply(lang);
    }

    public AppLanguage CycleLanguage()
    {
        SetLanguage(Current == AppLanguage.French ? AppLanguage.English : AppLanguage.French);
        return Current;
    }

    /// <summary>Returns the localized string for the given resource key.</summary>
    public string Get(string key) =>
        Application.Current.TryFindResource(key) as string ?? key;

    private static void Apply(AppLanguage lang)
    {
        var file = lang == AppLanguage.French ? "Strings.fr.xaml" : "Strings.en.xaml";
        var uri  = new Uri($"pack://application:,,,/Resources/{file}", UriKind.Absolute);

        var merged = Application.Current.Resources.MergedDictionaries;

        // Remove any existing strings dict
        var existing = merged.FirstOrDefault(d =>
            d.Source?.OriginalString.Contains("Strings.") == true);
        if (existing != null) merged.Remove(existing);

        // Add new strings dict — triggers DynamicResource updates on all bound elements
        merged.Add(new ResourceDictionary { Source = uri });
    }
}
