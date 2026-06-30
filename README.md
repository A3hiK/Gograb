# Gograb

A lightweight YouTube video & audio downloader for Windows, built with WPF + .NET 8.

![Platform](https://img.shields.io/badge/platform-Windows-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![License](https://img.shields.io/badge/license-MIT-green)

## Features

- Download YouTube videos in **1080p, 720p, 480p, 320p**
- Download YouTube audio as **MP3**
- **For Mobile** mode: converts to **128x160** resolution for mobile phones
- **Dark mode** support
- **Clipboard auto-detect** — copies a YouTube link and it appears automatically
- **Batch download** — add multiple videos, download all at once
- **Status filters** — All, Ready, Processing, Failed, Skipped, Complete
- **Cancel downloads** — stop any download mid-progress
- Built-in **yt-dlp** and **ffmpeg** — no manual setup needed
- **Self-contained** — no .NET runtime installation required

## Screenshot

<div align="center">
  <img src="screenshot.png" width="400" alt="Gograb Screenshot">
</div>

## Download

### Installer (Recommended)
Download **`Gograb-v1.0.0-Setup.exe`** from [Releases](../../releases/latest). Everything is bundled — just install and use.

### Build from Source
```bash
git clone https://github.com/yourusername/Gograb.git
cd Gograb
dotnet build
```

## Requirements

**None!** The installer includes everything:
- .NET 8 Runtime (bundled)
- yt-dlp (bundled)
- ffmpeg (bundled)
- Node.js (optional, for YouTube JS challenges — app handles this automatically)

## Project Structure

```
Gograb/
├── App.xaml / App.xaml.cs          # Application entry, global error handler
├── MainWindow.xaml / .cs           # Main UI — 420px compact layout
├── DarkTheme.xaml                  # Dark mode colors & control styles
├── LightTheme.xaml                 # Light mode colors & control styles
├── AssemblyInfo.cs                 # Theme info
├── Views/
│   ├── AboutWindow.xaml / .cs      # Slim about dialog
│   └── SettingsWindow.xaml / .cs   # Settings (removed in current version)
├── ViewModels/
│   └── MainViewModel.cs            # MVVM logic, queue management
├── Models/
│   ├── DownloadItem.cs             # Download item properties
│   └── AppSettings.cs              # Settings model
├── Services/
│   ├── YtDlpService.cs             # yt-dlp & ffmpeg integration
│   └── SettingsManager.cs          # JSON settings persistence
├── Converters/
│   ├── InverseBooleanConverter.cs
│   └── CountToVisibilityConverter.cs
├── logo.ico                        # App icon
├── logo.png                        # App logo source
├── Gograb.csproj                   # Project file
├── setup.iss                       # Inno Setup installer script
└── installer/
    └── Gograb-v1.0.0-Setup.exe     # Pre-built installer
```

## Tech Stack

| Component | Technology |
|---|---|
| UI Framework | WPF (.NET 8) |
| MVVM | CommunityToolkit.Mvvm |
| Video Downloader | yt-dlp (nightly) |
| Video Converter | ffmpeg |
| Installer | Inno Setup 6 |

## How It Works

1. **Paste a YouTube link** — or it auto-detects from clipboard
2. **Select resolution** — 1080p / 720p / 480p / 320p
3. **Toggle For Mobile** — converts to 128x160 for mobile phones
4. **Click Download** — progress shows as percentage text
5. **Click again to cancel** — stops the download completely

## Building the Installer

```bash
# 1. Publish self-contained
dotnet publish -c Release --self-contained -r win-x64 -o publish

# 2. Copy yt-dlp.exe and ffmpeg to publish folder

# 3. Compile installer (requires Inno Setup 6)
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" setup.iss
```

## License

MIT License — see [LICENSE](LICENSE) for details.

## Acknowledgments

- [yt-dlp](https://github.com/yt-dlp/yt-dlp) — YouTube download engine
- [FFmpeg](https://ffmpeg.org/) — Media conversion
- [Inno Setup](https://jrsoftware.org/isinfo.php) — Installer builder
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/) — MVVM toolkit
