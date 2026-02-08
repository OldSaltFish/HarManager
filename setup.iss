[Setup]
AppName=HarManager
AppVersion=1.0
DefaultDirName={autopf}\HarManager
DefaultGroupName=HarManager
OutputDir=.
OutputBaseFilename=HarManager_Setup
Compression=lzma2
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64

[Files]
Source: "publish\HarManager.exe"; DestDir: "{app}"; Flags: ignoreversion
; Add other files if necessary (e.g. config, db templates)

[Icons]
Name: "{group}\HarManager"; Filename: "{app}\HarManager.exe"
Name: "{autodesktop}\HarManager"; Filename: "{app}\HarManager.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

