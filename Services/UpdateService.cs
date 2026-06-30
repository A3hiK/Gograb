using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace EasyDL.Services;

public static class UpdateService
{
    private const string GitHubApiUrl = "https://api.github.com/repos/A3hiK/Gograb/releases/latest";
    private static readonly HttpClient _http = new();

    public static async Task<(bool HasUpdate, string LatestVersion, string DownloadUrl)> CheckForUpdateAsync()
    {
        try
        {
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("Gograb-Updater");
            var response = await _http.GetStringAsync(GitHubApiUrl);
            var json = JsonDocument.Parse(response);

            var tag = json.RootElement.GetProperty("tag_name").GetString() ?? "";
            var latestVersion = tag.Replace("v", "").Trim();
            var currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

            if (new Version(latestVersion) > new Version(currentVersion))
            {
                string downloadUrl = "";
                if (json.RootElement.TryGetProperty("assets", out var assets) && assets.GetArrayLength() > 0)
                {
                    downloadUrl = assets[0].GetProperty("browser_download_url").GetString() ?? "";
                }
                return (true, latestVersion, downloadUrl);
            }

            return (false, latestVersion, "");
        }
        catch
        {
            return (false, "", "");
        }
    }

    public static void OpenReleasePage()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/A3hiK/Gograb/releases/latest",
                UseShellExecute = true
            });
        }
        catch { }
    }
}
