using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Windows;
using GW2TaskManager.Models;

namespace GW2TaskManager.Services;

/// <summary>
/// Calls the GW2 API v2 and matches ApiTracker fields from tasks.json to determine
/// which tasks are already done in-game.
///
/// Supported ApiTracker prefixes:
///   worldboss:&lt;id&gt;            → /v2/account/worldbosses
///   mapchest:&lt;id&gt;             → /v2/account/mapchests
///   dailycrafting:all          → /v2/account/dailycrafting (non-empty = done)
///   wizardvault:daily_meta_reward  → /v2/account/wizardsvault/daily (meta_reward_claimed)
///   wizardvault:weekly_meta_reward → /v2/account/wizardsvault/weekly
/// </summary>
public class Gw2ApiClient
{
    private const string BaseUrl = "https://api.guildwars2.com/v2/";

    // Read a localized string from the active resource dictionary (safe for background threads: reads only)
    private static string S(string key) =>
        Application.Current?.TryFindResource(key) as string ?? key;

    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ----------------------------------------------------------------
    // Public API
    // ----------------------------------------------------------------

    /// <summary>
    /// Validates the key and checks which tracked tasks are done in-game.
    /// Only considers tasks where IsEnabled == true and ApiTracker != null.
    /// </summary>
    public async Task<ApiSyncResult> SyncAsync(string apiKey, List<TaskItem> allTasks)
    {
        var result = new ApiSyncResult();

        // Step 1: validate key
        var (valid, permissions, error) = await ValidateKeyAsync(apiKey);
        if (!valid)
        {
            result.Error = error ?? S("Str_ErrorInvalidKey");
            return result;
        }
        result.Success = true;

        // Step 2: characters (best effort)
        if (permissions.Contains("characters"))
            result.Characters = await GetCharactersAsync(apiKey) ?? new();

        // Step 3: resolve tracked tasks
        var tracked = allTasks
            .Where(t => t.IsEnabled && !string.IsNullOrEmpty(t.ApiTracker))
            .ToList();

        if (tracked.Count == 0) return result;

        // Determine which endpoints are needed
        bool needWorldbosses    = tracked.Any(t => t.ApiTracker!.StartsWith("worldboss:"));
        bool needMapchests      = tracked.Any(t => t.ApiTracker!.StartsWith("mapchest:"));
        bool needDailycrafting  = tracked.Any(t => t.ApiTracker!.StartsWith("dailycrafting:"));
        bool needWvDaily        = tracked.Any(t => t.ApiTracker == "wizardvault:daily_meta_reward");
        bool needWvWeekly       = tracked.Any(t => t.ApiTracker == "wizardvault:weekly_meta_reward");

        // Fetch in parallel
        var wbTask  = needWorldbosses   ? FetchStringSetAsync(apiKey, "account/worldbosses")             : Task.FromResult<HashSet<string>?>(null);
        var mcTask  = needMapchests     ? FetchStringSetAsync(apiKey, "account/mapchests")               : Task.FromResult<HashSet<string>?>(null);
        var dcTask  = needDailycrafting ? FetchStringSetAsync(apiKey, "account/dailycrafting")           : Task.FromResult<HashSet<string>?>(null);
        var wvdTask = needWvDaily       ? FetchWvRewardClaimedAsync(apiKey, "account/wizardsvault/daily") : Task.FromResult<bool?>(null);
        var wvwTask = needWvWeekly      ? FetchWvRewardClaimedAsync(apiKey, "account/wizardsvault/weekly"): Task.FromResult<bool?>(null);

        await Task.WhenAll(wbTask, mcTask, dcTask, wvdTask, wvwTask);

        var worldbosses          = await wbTask;
        var mapchests            = await mcTask;
        var dailycrafting        = await dcTask;
        var wvDailyRewardClaimed = await wvdTask;
        var wvWeeklyRewardClaimed= await wvwTask;

        // Match each tracked task against API data
        foreach (var task in tracked)
        {
            var tracker = task.ApiTracker!;

            bool done = tracker switch
            {
                var t when t.StartsWith("worldboss:")   => worldbosses?.Contains(t["worldboss:".Length..]) ?? false,
                var t when t.StartsWith("mapchest:")    => mapchests?.Contains(t["mapchest:".Length..])    ?? false,
                "dailycrafting:all"                     => (dailycrafting?.Count ?? 0) > 0,
                "wizardvault:daily_meta_reward"         => wvDailyRewardClaimed  ?? false,
                "wizardvault:weekly_meta_reward"        => wvWeeklyRewardClaimed ?? false,
                _                                       => false
            };

            if (done) result.DoneTaskIds.Add(task.Id);
        }

        return result;
    }

    // ----------------------------------------------------------------
    // Private helpers
    // ----------------------------------------------------------------

    private static async Task<(bool valid, HashSet<string> permissions, string? error)> ValidateKeyAsync(string apiKey)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Get, "tokeninfo", apiKey);
            using var resp = await Http.SendAsync(req);

            if (!resp.IsSuccessStatusCode)
                return (false, new(), string.Format(S("Str_ErrorHttp"), (int)resp.StatusCode));

            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var perms = doc.RootElement.TryGetProperty("permissions", out var p)
                ? p.EnumerateArray().Select(e => e.GetString() ?? "").ToHashSet(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>();

            return (true, perms, null);
        }
        catch (TaskCanceledException) { return (false, new(), "Timeout"); }
        catch (Exception ex)          { return (false, new(), ex.Message); }
    }

    private static async Task<List<string>?> GetCharactersAsync(string apiKey)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Get, "characters", apiKey);
            using var resp = await Http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return null;

            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(json, JsonOpts);
        }
        catch { return null; }
    }

    /// <summary>Returns a case-insensitive set of IDs from an array endpoint.</summary>
    private static async Task<HashSet<string>?> FetchStringSetAsync(string apiKey, string endpoint)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Get, endpoint, apiKey);
            using var resp = await Http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return null;

            var json = await resp.Content.ReadAsStringAsync();
            var list = JsonSerializer.Deserialize<List<string>>(json, JsonOpts);
            return list?.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        catch { return null; }
    }

    /// <summary>Returns meta_reward_claimed from a Wizard's Vault endpoint.</summary>
    private static async Task<bool?> FetchWvRewardClaimedAsync(string apiKey, string endpoint)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Get, endpoint, apiKey);
            using var resp = await Http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return null;

            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("meta_reward_claimed", out var v) ? v.GetBoolean() : null;
        }
        catch { return null; }
    }

    private static HttpRequestMessage BuildRequest(HttpMethod method, string endpoint, string apiKey)
    {
        var req = new HttpRequestMessage(method, BaseUrl + endpoint);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        return req;
    }
}
