[Setup]
AppId={{Ca8D3121-34E1-4A99-9188-7E441CDA0DD0}
AppName=Tyflopodcast
AppVersion=0.3
AppVerName=Tyflopodcast 0.3
AppPublisher=Dawid Pieper
AppPublisherURL=https://elten-net.eu
AppSupportURL=https://github.com/dawidpieper/tyflopodcast/issues
AppUpdatesURL=https://github.com/dawidpieper/tyflopodcast/releases
DefaultDirName={commonpf}\Tyflopodcast
DefaultGroupName=Tyflopodcast
AllowNoIcons=yes
OutputDir=out
OutputBaseFilename=tyflopodcast_setup
Compression=lzma2/max
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "pl"; MessagesFile: "compiler:Languages\Polish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}";

[Files]
Source: "bin\*"; DestDir: "{app}"; Flags: "ignoreversion createallsubdirs recursesubdirs"

[Icons]
Name: "{group}\Tyflopodcast"; Filename: "{app}\tyflopodcast.exe"
Name: "{group}\{cm:ProgramOnTheWeb,Tyflopodcast}"; Filename: "https://github.com/dawidpieper/tyflopodcast"
Name: "{group}\{cm:UninstallProgram,Tyflopodcast}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\Tyflopodcast"; Filename: "{app}\tyflopodcast.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\tyflopodcast.exe"; Description: "{cm:LaunchProgram,Tyflopodcast}}"; Flags: nowait postinstall

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\tyflopodcast"