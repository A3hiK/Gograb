using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasyDL.Services;
using System.Collections.ObjectModel;

namespace EasyDL.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty] private bool _speedLimitModeEnabled;
    [ObservableProperty] private int _speedLimitModeKBps;
    [ObservableProperty] private bool _highestResolution;
    [ObservableProperty] private bool _selectResolution;
    [ObservableProperty] private bool _lowestResolution;
    [ObservableProperty] private string _selectedResolutionStr = "";
    [ObservableProperty] private string _downloadFolder = "";

    public ObservableCollection<string> Resolutions { get; } = new() { "1080p", "720p", "480p", "320p" };

    public SettingsViewModel()
    {
        var s = SettingsManager.Settings;
        SpeedLimitModeEnabled = s.SpeedLimitModeEnabled;
        SpeedLimitModeKBps = s.SpeedLimitModeKBps;
        HighestResolution = s.VideoQualityMode == 0;
        SelectResolution = s.VideoQualityMode == 1;
        LowestResolution = s.VideoQualityMode == 2;
        SelectedResolutionStr = s.VideoQualityResolution;
        DownloadFolder = s.VideoFolder;
    }

    public void SaveSettings()
    {
        var s = SettingsManager.Settings;
        s.SpeedLimitModeEnabled = SpeedLimitModeEnabled;
        s.SpeedLimitModeKBps = SpeedLimitModeKBps;
        s.VideoQualityMode = HighestResolution ? 0 : (LowestResolution ? 2 : 1);
        s.VideoQualityResolution = SelectedResolutionStr;
        s.VideoFolder = DownloadFolder;
        SettingsManager.Save();
    }

    [RelayCommand]
    private void ChooseDownloadFolder()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog { Title = "Select Download Folder" };
        if (dialog.ShowDialog() == true) DownloadFolder = dialog.FolderName;
    }
}
