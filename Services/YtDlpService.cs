using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EasyDL.Models;

namespace EasyDL.Services;

public class YtDlpService
{
    private readonly string _baseDir = AppDomain.CurrentDomain.BaseDirectory;
    private readonly string _ytDlpPath;
    private readonly string _ffmpegDir;
    private readonly string _ffmpegExe;

    public YtDlpService()
    {
        _ytDlpPath = Path.Combine(_baseDir, "yt-dlp.exe");
        _ffmpegDir = Path.Combine(_baseDir, "ffmpeg");
        _ffmpegExe = Path.Combine(_ffmpegDir, "bin", "ffmpeg.exe");
    }

    public async Task EnsureDependenciesAsync()
    {
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromMinutes(5);

        if (!File.Exists(_ytDlpPath))
        {
            var ytDlpUrl = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";
            var bytes = await httpClient.GetByteArrayAsync(ytDlpUrl);
            await File.WriteAllBytesAsync(_ytDlpPath, bytes);
        }

        if (!File.Exists(_ffmpegExe))
        {
            try
            {
                var ffmpegUrl = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip";
                var zipPath = Path.Combine(_baseDir, "ffmpeg.zip");
                var bytes = await httpClient.GetByteArrayAsync(ffmpegUrl);
                await File.WriteAllBytesAsync(zipPath, bytes);

                if (Directory.Exists(_ffmpegDir))
                    Directory.Delete(_ffmpegDir, true);

                ZipFile.ExtractToDirectory(zipPath, _baseDir);

                var extractedDir = Path.Combine(_baseDir, "ffmpeg-master-latest-win64-gpl");
                if (Directory.Exists(extractedDir))
                    Directory.Move(extractedDir, _ffmpegDir);

                if (File.Exists(zipPath))
                    File.Delete(zipPath);
            }
            catch { }
        }
    }

    public async Task FetchMetadataAsync(DownloadItem item)
    {
        var s = SettingsManager.Settings;
        string cookieArg = GetCookieArg(s);

        var psi = new ProcessStartInfo
        {
            FileName = _ytDlpPath,
            Arguments = $"{cookieArg}--no-warnings --js-runtimes node -j --no-playlist \"{item.Url}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.EnvironmentVariables["PATH"] = $"C:\\Program Files\\nodejs;{psi.EnvironmentVariables["PATH"]}" ?? "";

        using var process = Process.Start(psi);
        if (process == null) return;

        var jsonStrTask = Task.Run(async () => await process.StandardOutput.ReadToEndAsync());
        var errStrTask = Task.Run(async () => await process.StandardError.ReadToEndAsync());
        await process.WaitForExitAsync();
        var jsonStr = await jsonStrTask;
        var errStr = await errStrTask;

        if (string.IsNullOrWhiteSpace(jsonStr))
        {
            item.Title = "Error fetching metadata";
            item.Eta = string.IsNullOrWhiteSpace(errStr) ? "Unknown error" : errStr;
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(jsonStr);
            var root = doc.RootElement;

            if (root.TryGetProperty("title", out var titleProp))
                item.Title = titleProp.GetString() ?? "Unknown";

            if (root.TryGetProperty("thumbnail", out var thumbProp))
                item.ThumbnailUrl = thumbProp.GetString() ?? "";

            if (root.TryGetProperty("channel", out var channelProp))
                item.Channel = channelProp.GetString() ?? "";

            if (root.TryGetProperty("duration", out var durProp) && durProp.TryGetDouble(out var durSec))
                item.Duration = TimeSpan.FromSeconds(durSec).ToString(@"mm\:ss");
        }
        catch { }
    }

    public async Task DownloadAsync(DownloadItem item)
    {
        item.Status = "Downloading";

        var s = SettingsManager.Settings;
        string cookieArg = GetCookieArg(s);
        bool isAudioOnly = item.SelectedResolution == "Audio (MP3)";
        string targetFolder = s.VideoFolder;

        if (string.IsNullOrWhiteSpace(targetFolder))
            targetFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "Gograb");

        Directory.CreateDirectory(targetFolder);

        string outputTemplate = Path.Combine(targetFolder, "%(title)s.%(ext)s");

        if (isAudioOnly)
        {
            string args = $"{cookieArg}--extract-audio --audio-format mp3 --no-warnings --js-runtimes node --no-playlist -o \"{outputTemplate}\" --newline \"{item.Url}\"";
            await RunYtDlp(item, args);
            return;
        }

        string resFilter = item.SelectedResolution.Contains('x')
            ? item.SelectedResolution.Split('x')[1]
            : item.SelectedResolution.Replace("p", "");
        bool needKeypad = item.ConvertToKeypad;

        if (needKeypad && File.Exists(_ffmpegExe))
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "EasyDL_Temp");
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
            Directory.CreateDirectory(tempDir);

            string uniqueName = $"mobile_{Guid.NewGuid():N}";
            string tempFile = Path.Combine(tempDir, $"{uniqueName}.mp4");
            string tempTemplate = Path.Combine(tempDir, $"{uniqueName}.%(ext)s");

            string formatArgs = $"-f \"bestvideo[height<=240]+bestaudio/best[height<=240]/best\" --merge-output-format mp4";
            string ffmpegFlag = $"--ffmpeg-location \"{Path.Combine(_ffmpegDir, "bin")}\" ";
            string args = $"{cookieArg}{ffmpegFlag}{formatArgs} --no-warnings --js-runtimes node --no-playlist -o \"{tempTemplate}\" --newline \"{item.Url}\"";

            bool dlOk = await RunYtDlp(item, args);

            if (item.Status == "Cancelled") return;

            if (!File.Exists(tempFile))
            {
                var found = new DirectoryInfo(tempDir).GetFiles($"{uniqueName}.*")
                    .Where(f => f.Extension is ".mp4" or ".webm" or ".mkv")
                    .FirstOrDefault();
                if (found != null) tempFile = found.FullName;
            }

            if (dlOk && File.Exists(tempFile))
            {
                item.Status = "Converting for mobile (128x160)...";
                item.Progress = 0;

                string finalFile = Path.Combine(targetFolder, $"{uniqueName}_mobile.mp4");
                bool convOk = await ConvertResolution(tempFile, finalFile, item, "160", "128");

                if (item.Status == "Cancelled") { try { File.Delete(tempFile); } catch { } return; }

                try { File.Delete(tempFile); } catch { }

                if (convOk && File.Exists(finalFile))
                {
                    item.Status = "Completed";
                    item.Progress = 100;
                }
                else
                {
                    item.Status = "Error";
                    if (string.IsNullOrEmpty(item.Eta) || item.Eta == "Unknown")
                        item.Eta = "Mobile conversion failed";
                }
            }
            else
            {
                item.Status = "Error";
                item.Eta = dlOk ? "Downloaded file not found" : "Download failed";
            }
        }
        else
        {
            string formatArgs = $"-f \"bestvideo[height<={resFilter}]+bestaudio/best[height<={resFilter}]\" --merge-output-format mp4";
            string speedLimitFlag = s.SpeedLimitModeEnabled ? $"--limit-rate {s.SpeedLimitModeKBps}K " : "";
            string ffmpegFlag = File.Exists(_ffmpegExe) ? $"--ffmpeg-location \"{Path.Combine(_ffmpegDir, "bin")}\" " : "";
            string args = $"{cookieArg}{ffmpegFlag}{speedLimitFlag}{formatArgs} --no-warnings --js-runtimes node --no-playlist -o \"{outputTemplate}\" --newline \"{item.Url}\"";
            await RunYtDlp(item, args);
        }
    }

    private string FindTempFile(string tempDir)
    {
        try
        {
            var files = new DirectoryInfo(tempDir).GetFiles("dl_temp.*", SearchOption.TopDirectoryOnly);
            var videoFiles = files.Where(f => f.Extension is ".mp4" or ".webm" or ".mkv").ToArray();
            if (videoFiles.Length > 0)
            {
                Array.Sort(videoFiles, (a, b) => b.LastWriteTime.CompareTo(a.LastWriteTime));
                return videoFiles[0].FullName;
            }
        }
        catch { }
        return "";
    }

    private static string GetCookieArg(Models.AppSettings s)
    {
        if (!s.UseCookies) return "";
        if (!string.IsNullOrWhiteSpace(s.CookiesFilePath) && File.Exists(s.CookiesFilePath))
            return $"--cookies \"{s.CookiesFilePath}\" ";
        if (!string.IsNullOrWhiteSpace(s.CookiesBrowser))
            return $"--cookies-from-browser {s.CookiesBrowser} ";
        return "";
    }

    private async Task<bool> RunYtDlp(DownloadItem item, string args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _ytDlpPath,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.EnvironmentVariables["PATH"] = $"C:\\Program Files\\nodejs;{psi.EnvironmentVariables["PATH"]}" ?? "";

        using var process = Process.Start(psi);
        if (process == null)
        {
            item.Status = "Error";
            item.Eta = "Failed to start yt-dlp";
            return false;
        }

        item.ActiveProcess = process;

        var progressRegex = new Regex(@"(?<percent>\d+\.?\d*)%", RegexOptions.Compiled);
        var speedRegex = new Regex(@"at\s+(?<speed>\S+/s)", RegexOptions.Compiled);
        var etaRegex = new Regex(@"ETA\s+(?<eta>\S+)", RegexOptions.Compiled);
        var sizeRegex = new Regex(@"of\s+~?(?<size>\S+)", RegexOptions.Compiled);

        var stderrLines = new System.Collections.Concurrent.ConcurrentBag<string>();

        var stdoutTask = Task.Run(async () =>
        {
            while (true)
            {
                var line = await process.StandardOutput.ReadLineAsync();
                if (line == null) break;
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (item.Status == "Cancelled") break;
                stderrLines.Add(line);

                var match = progressRegex.Match(line);
                if (match.Success && double.TryParse(match.Groups["percent"].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var progress))
                {
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(() => item.Progress = progress);
                }
            }
        });

        var stderrTask = Task.Run(async () =>
        {
            while (true)
            {
                var line = await process.StandardError.ReadLineAsync();
                if (line == null) break;
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (item.Status == "Cancelled") break;

                stderrLines.Add(line);

                var match = progressRegex.Match(line);
                if (match.Success)
                {
                    if (double.TryParse(match.Groups["percent"].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var progress))
                        System.Windows.Application.Current.Dispatcher.BeginInvoke(() => item.Progress = progress);

                    var sizeMatch = sizeRegex.Match(line);
                    if (sizeMatch.Success) System.Windows.Application.Current.Dispatcher.BeginInvoke(() => item.FileSize = sizeMatch.Groups["size"].Value);

                    var speedMatch = speedRegex.Match(line);
                    if (speedMatch.Success) System.Windows.Application.Current.Dispatcher.BeginInvoke(() => item.Speed = speedMatch.Groups["speed"].Value);

                    var etaMatch = etaRegex.Match(line);
                    if (etaMatch.Success) System.Windows.Application.Current.Dispatcher.BeginInvoke(() => item.Eta = etaMatch.Groups["eta"].Value);
                }
            }
        });

        await process.WaitForExitAsync();
        await Task.WhenAll(stderrTask, stdoutTask);

        if (item.Status == "Cancelled") return false;

        if (process.ExitCode == 0)
        {
            item.Progress = 100;
            item.Status = item.ConvertToKeypad ? "Downloaded, converting..." : "Completed";
            return true;
        }
        else
        {
            item.Status = "Error";
            var errText = string.Join(" ", stderrLines.TakeLast(5));
            item.Eta = errText.Length > 200 ? errText.Substring(0, 200) : (errText.Length > 0 ? errText : $"Exit code: {process.ExitCode}");
            return false;
        }
    }

    private string FindDownloadedFile(string folder)
    {
        try
        {
            var dir = new DirectoryInfo(folder);
            var extensions = new[] { "*.mp4", "*.webm", "*.mkv", "*.avi" };
            FileInfo? newest = null;
            foreach (var ext in extensions)
            {
                var files = dir.GetFiles(ext, SearchOption.TopDirectoryOnly);
                foreach (var f in files)
                {
                    if (newest == null || f.LastWriteTime > newest.LastWriteTime)
                        newest = f;
                }
            }
            if (newest != null) return newest.FullName;
        }
        catch { }
        return "";
    }

    private async Task<bool> ConvertResolution(string inputPath, string outputPath, DownloadItem item, string height, string? width = null)
    {
        try
        {
            if (!File.Exists(inputPath))
            {
                item.Eta = "Input file not found for conversion";
                return false;
            }

            double totalSeconds = 0;
            var probePsi = new ProcessStartInfo
            {
                FileName = _ffmpegExe,
                Arguments = $"-i \"{inputPath}\"",
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using (var probe = Process.Start(probePsi))
            {
                var probeErr = await probe!.StandardError.ReadToEndAsync();
                await probe.WaitForExitAsync();
                var durMatch = Regex.Match(probeErr, @"Duration:\s+(\d+):(\d+):(\d+)\.(\d+)");
                if (durMatch.Success)
                {
                    totalSeconds = int.Parse(durMatch.Groups[1].Value) * 3600
                                 + int.Parse(durMatch.Groups[2].Value) * 60
                                 + int.Parse(durMatch.Groups[3].Value)
                                 + int.Parse(durMatch.Groups[4].Value) / 100.0;
                }
            }

            string scaleFilter = width != null
                ? $"scale=trunc({width}/2)*2:trunc({height}/2)*2"
                : $"scale=-2:trunc({height}/2)*2";

            var psi = new ProcessStartInfo
            {
                FileName = _ffmpegExe,
                Arguments = $"-i \"{inputPath}\" -y -vf scale=128:160:force_original_aspect_ratio=decrease,pad=128:160:(ow-iw)/2:(oh-ih)/2 -c:v libx264 -preset ultrafast -crf 23 -pix_fmt yuv420p -c:a aac -b:a 64k \"{outputPath}\"",
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                item.Eta = "Failed to start ffmpeg";
                return false;
            }

            var timeRegex = new Regex(@"time=(\d+):(\d+):(\d+)\.(\d+)", RegexOptions.Compiled);
            var errLines = new List<string>();

            while (true)
            {
                var line = await process.StandardError.ReadLineAsync();
                if (line == null) break;
                errLines.Add(line);

                var match = timeRegex.Match(line);
                if (match.Success && totalSeconds > 0)
                {
                    double current = int.Parse(match.Groups[1].Value) * 3600
                                   + int.Parse(match.Groups[2].Value) * 60
                                   + int.Parse(match.Groups[3].Value)
                                   + int.Parse(match.Groups[4].Value) / 100.0;
                    double pct = Math.Min(99, (current / totalSeconds) * 100);
                    item.Progress = Math.Round(pct, 1);
                }
            }

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var last = errLines.TakeLast(5).Aggregate((a, b) => a + " | " + b);
                item.Eta = $"ffmpeg error (code {process.ExitCode}): {last}";
            }

            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            item.Eta = $"Conversion exception: {ex.Message}";
            return false;
        }
    }
}
