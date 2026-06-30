using System;

namespace EasyDL.Models;

public class AppSettings
{
    public int SimultaneousDownloads { get; set; } = 4;
    
    public bool GlobalBandwidthLimitEnabled { get; set; } = false;
    public int GlobalBandwidthLimitKBps { get; set; } = 500;
    
    public bool SpeedLimitModeEnabled { get; set; } = false;
    public int SpeedLimitModeKBps { get; set; } = 200;
    
    public bool PreventSleep { get; set; } = false;
    public bool Ignore30Fps { get; set; } = false;
    public bool Ignore360Aec { get; set; } = true;
    public bool PreferHdr { get; set; } = false;
    public bool PreferAv1 { get; set; } = false;
    
    public int VideoQualityMode { get; set; } = 1; // 0=highest, 1=select, 2=lowest
    public string VideoQualityResolution { get; set; } = "480p";
    
    public string AudioFolder { get; set; } = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "EasyDL");
    public bool AudioSubfolderPlaylist { get; set; } = false;
    
    public string VideoFolder { get; set; } = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "EasyDL");
    public bool VideoSubfolderPlaylist { get; set; } = false;
    public bool VideoSameAsAudio { get; set; } = true;
    
    public int TempFolderMode { get; set; } = 0; // 0=system temp, 1=custom
    public string TempFolderCustom { get; set; } = "";
    
    public bool UseCookies { get; set; } = false;
    public string CookiesBrowser { get; set; } = "chrome";
    public string CookiesFilePath { get; set; } = "";
}
