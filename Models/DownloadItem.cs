using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace EasyDL.Models;

public partial class DownloadItem : ObservableObject
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [ObservableProperty]
    private string _url = string.Empty;

    [ObservableProperty]
    private string _title = "Fetching info...";

    [ObservableProperty]
    private string _channel = string.Empty;

    [ObservableProperty]
    private string _thumbnailUrl = string.Empty;

    [ObservableProperty]
    private string _duration = "00:00";

    [ObservableProperty]
    private double _progress = 0;

    [ObservableProperty]
    private string _speed = "0 MB/s";

    [ObservableProperty]
    private string _eta = "Unknown";

    [ObservableProperty]
    private string _fileSize = "Unknown";

    [ObservableProperty]
    private string _status = "Pending";

    public Process? ActiveProcess { get; set; }

    public ObservableCollection<string> AvailableResolutions { get; } = new ObservableCollection<string>
    {
        "1080p",
        "720p",
        "480p",
        "320p",
        "Audio (MP3)"
    };

    [ObservableProperty]
    private string _selectedResolution = "720p";

    [ObservableProperty]
    private bool _convertToKeypad = false;

    public DownloadItem()
    {
        var s = EasyDL.Services.SettingsManager.Settings;
        if (s.VideoQualityMode == 0)
        {
            _selectedResolution = "1080p";
        }
        else if (s.VideoQualityMode == 2)
        {
            _selectedResolution = "320p";
        }
        else
        {
            _selectedResolution = s.VideoQualityResolution;
        }
    }
}
