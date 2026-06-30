[Setup]
AppName=Gograb
AppVersion=1.0.0
AppPublisher=Gograb
DefaultDirName={autopf}\Gograb
DefaultGroupName=Gograb
OutputDir=C:\Users\Admin\Desktop\EasyDL\installer
OutputBaseFilename=Gograb-v1.0.0-Setup
SetupIconFile=C:\Users\Admin\Desktop\EasyDL\logo.ico
UninstallDisplayIcon={app}\Gograb.exe
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

[Files]
Source: "C:\Users\Admin\Desktop\EasyDL\publish\*"; DestDir: "{app}"; Flags: recursesubdirs

[Icons]
Name: "{group}\Gograb"; Filename: "{app}\Gograb.exe"
Name: "{group}\Uninstall Gograb"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Gograb"; Filename: "{app}\Gograb.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create desktop shortcut"; GroupDescription: "Additional shortcuts:"

[Run]
Filename: "{app}\Gograb.exe"; Description: "Launch Gograb now"; Flags: nowait postinstall skipifsilent
