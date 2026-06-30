using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasyDL.Models;
using EasyDL.Services;

namespace EasyDL.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly YtDlpService _ytDlpService;
    private readonly DispatcherTimer _clipboardTimer;
    private string _lastClipboardText = string.Empty;

    private readonly Regex _ytRegex = new Regex(@"(?:https?:\/\/)?(?:www\.)?(?:youtube\.com\/(?:[^\/\n\s]+\/\S+\/|(?:v|e(?:mbed)?)\/|\S*?[?&]v=)|youtu\.be\/)([a-zA-Z0-9_-]{11})", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    [ObservableProperty]
    private string _urlInput = string.Empty;

    [ObservableProperty]
    private bool _isClipboardTrackingEnabled = true;

    [ObservableProperty]
    private bool _isReady = false;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private string _pillAllText = "All (0)";

    [ObservableProperty]
    private string _pillReadyText = "Ready (0)";

    [ObservableProperty]
    private string _pillProcessingText = "Processing (0)";

    [ObservableProperty]
    private string _pillFailedText = "Failed (0)";

    [ObservableProperty]
    private string _pillSkippedText = "Skipped (0)";

    [ObservableProperty]
    private string _pillCompleteText = "Complete (0)";

    [ObservableProperty]
    private string _activeFilter = "All";

    [ObservableProperty]
    private bool _isDarkMode = false;

    [ObservableProperty]
    private bool _globalConvertToKeypad = false;

    [ObservableProperty]
    private bool _hasUpdate = false;

    [ObservableProperty]
    private string _updateVersion = "";

    public ObservableCollection<DownloadItem> AllItems { get; } = new();
    public ICollectionView FilteredView { get; }

    public MainViewModel()
    {
        SettingsManager.Load();
        _ytDlpService = new YtDlpService();

        FilteredView = CollectionViewSource.GetDefaultView(AllItems);
        FilteredView.Filter = FilterItem;

        AllItems.CollectionChanged += (_, _) => RefreshCounts();
        foreach (var item in AllItems)
            item.PropertyChanged += (_, _) => RefreshCounts();

        _clipboardTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };
        _clipboardTimer.Tick += ClipboardTimer_Tick;
        _clipboardTimer.Start();

        Task.Run(InitializeDependencies);
        Task.Run(CheckForUpdate);
    }

    private bool FilterItem(object obj)
    {
        if (ActiveFilter == "All") return true;
        if (obj is not DownloadItem item) return false;
        return ActiveFilter switch
        {
            "Ready" => item.Status == "Ready",
            "Processing" => item.Status == "Downloading" || item.Status.Contains("Converting"),
            "Failed" => item.Status == "Error",
            "Skipped" => item.Status == "Skipped",
            "Complete" => item.Status == "Completed",
            _ => true
        };
    }

    private async Task CheckForUpdate()
    {
        var (hasUpdate, version, _) = await UpdateService.CheckForUpdateAsync();
        if (hasUpdate)
        {
            HasUpdate = true;
            UpdateVersion = version;
        }
    }

    [RelayCommand]
    private void OpenUpdatePage()
    {
        UpdateService.OpenReleasePage();
    }

    public void SetFilter(string filter)
    {
        ActiveFilter = filter;
        FilteredView.Refresh();
    }

    public void RefreshCounts()
    {
        int all = AllItems.Count;
        int ready = AllItems.Count(i => i.Status == "Ready");
        int processing = AllItems.Count(i => i.Status == "Downloading" || i.Status.Contains("Converting"));
        int failed = AllItems.Count(i => i.Status == "Error");
        int skipped = AllItems.Count(i => i.Status == "Skipped");
        int complete = AllItems.Count(i => i.Status == "Completed");

        PillAllText = $"All ({all})";
        PillReadyText = $"Ready ({ready})";
        PillProcessingText = $"Processing ({processing})";
        PillFailedText = $"Failed ({failed})";
        PillSkippedText = $"Skipped ({skipped})";
        PillCompleteText = $"Complete ({complete})";
    }

    private async Task InitializeDependencies()
    {
        StatusMessage = "Downloading core dependencies (yt-dlp, ffmpeg)...";
        try
        {
            await _ytDlpService.EnsureDependenciesAsync();
            IsReady = true;
            StatusMessage = "Ready";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    private void ClipboardTimer_Tick(object? sender, EventArgs e)
    {
        if (!IsClipboardTrackingEnabled || !IsReady) return;

        try
        {
            if (Clipboard.ContainsText())
            {
                var text = Clipboard.GetText(TextDataFormat.UnicodeText)?.Trim()
                        ?? Clipboard.GetText(TextDataFormat.Text)?.Trim()
                        ?? "";
                if (text.Length > 0 && text != _lastClipboardText && _ytRegex.IsMatch(text))
                {
                    _lastClipboardText = text;
                    _ = AddLinkFromClipboardAsync(text);
                }
            }
        }
        catch { }
    }

    private async Task AddLinkFromClipboardAsync(string url)
    {
        url = url.Trim();
        if (string.IsNullOrWhiteSpace(url) || !_ytRegex.IsMatch(url)) return;

        foreach (var q in AllItems)
        {
            if (q.Url == url) return;
        }

        var item = new DownloadItem { Url = url, Status = "Fetching info...", ConvertToKeypad = GlobalConvertToKeypad };
        item.PropertyChanged += (_, _) => RefreshCounts();
        AllItems.Insert(0, item);
        StatusMessage = "Fetching metadata...";

        await _ytDlpService.FetchMetadataAsync(item);
        if (item.Title == "Error fetching metadata")
        {
            item.Status = "Error";
            StatusMessage = "Failed to fetch video info.";
        }
        else
        {
            item.Status = "Ready";
            StatusMessage = "Ready";
        }
        RefreshCounts();
        System.Windows.Data.CollectionViewSource.GetDefaultView(AllItems).Refresh();
    }

    [RelayCommand]
    private void ToggleClipboardTracking()
    {
        IsClipboardTrackingEnabled = !IsClipboardTrackingEnabled;
    }

    [RelayCommand]
    private void DeleteItem(DownloadItem? item)
    {
        if (item == null) return;
        AllItems.Remove(item);
    }

    [RelayCommand]
    private async Task AddLinkAsync()
    {
        if (!IsReady) return;

        string url = UrlInput;
        if (string.IsNullOrWhiteSpace(url))
        {
            try
            {
                if (Clipboard.ContainsText())
                    url = Clipboard.GetText();
            }
            catch { }
        }

        url = url?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(url) || !_ytRegex.IsMatch(url))
        {
            StatusMessage = "Invalid YouTube link.";
            return;
        }

        UrlInput = string.Empty;

        foreach (var q in AllItems)
        {
            if (q.Url == url) return;
        }

        var item = new DownloadItem { Url = url, Status = "Fetching info...", ConvertToKeypad = GlobalConvertToKeypad };
        item.PropertyChanged += (_, _) => RefreshCounts();
        AllItems.Insert(0, item);
        StatusMessage = "Fetching metadata...";

        await _ytDlpService.FetchMetadataAsync(item);
        if (item.Title == "Error fetching metadata")
        {
            item.Status = "Error";
            StatusMessage = "Failed to fetch video info.";
        }
        else
        {
            item.Status = "Ready";
            StatusMessage = "Ready";
        }
        RefreshCounts();
        System.Windows.Data.CollectionViewSource.GetDefaultView(AllItems).Refresh();
    }

    [RelayCommand]
    private async Task DownloadAllAsync()
    {
        var itemsToDownload = AllItems.Where(q => q.Status == "Ready").ToList();
        if (!itemsToDownload.Any()) return;

        foreach (var item in itemsToDownload)
        {
            await DownloadSingleAsync(item);
        }
    }

    [RelayCommand]
    private async Task DownloadSingleAsync(DownloadItem? item)
    {
        if (item == null) return;
        if (item.Status != "Ready")
        {
            StatusMessage = "Item is not ready for download.";
            return;
        }

        await _ytDlpService.DownloadAsync(item);
        StatusMessage = item.Status == "Completed" ? "Download completed!" : $"Error: {item.Eta}";
        RefreshCounts();
        System.Windows.Data.CollectionViewSource.GetDefaultView(AllItems).Refresh();
    }

    [RelayCommand]
    private void CancelDownload(DownloadItem? item)
    {
        if (item == null) return;
        try
        {
            item.ActiveProcess?.Kill(entireProcessTree: true);
        }
        catch { }
        item.ActiveProcess = null;
        item.Status = "Cancelled";
        item.Eta = "Cancelled by user";
        RefreshCounts();
        System.Windows.Data.CollectionViewSource.GetDefaultView(AllItems).Refresh();
    }

    [RelayCommand]
    private void OpenSettings()
    {
        var settingsWindow = new Views.SettingsWindow();
        settingsWindow.ShowDialog();
    }

    public void SetAllResolutions(string resolution)
    {
        foreach (var item in AllItems)
        {
            item.SelectedResolution = resolution;
        }
    }

    public void SetAllConvertToKeypad(bool enabled)
    {
        GlobalConvertToKeypad = enabled;
        foreach (var item in AllItems)
        {
            item.ConvertToKeypad = enabled;
        }
    }

    public async Task RefreshItemAsync(DownloadItem item)
    {
        item.Status = "Fetching info...";
        item.Progress = 0;
        item.Eta = "";
        RefreshCounts();
        System.Windows.Data.CollectionViewSource.GetDefaultView(AllItems).Refresh();

        await _ytDlpService.FetchMetadataAsync(item);
        if (item.Title == "Error fetching metadata")
        {
            item.Status = "Error";
        }
        else
        {
            item.Status = "Ready";
        }
        RefreshCounts();
        System.Windows.Data.CollectionViewSource.GetDefaultView(AllItems).Refresh();
    }
}
